using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CbzMage.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzwConverter
{
    public class AzwConverter
    {
        // Global veriables updated by processing threads
        private volatile int bookCount;
        private int totalBooks;
        private volatile int pagesCount;

        // For testing. If >0 overrules the result of GetUnconvertedBooks
        private const int maxBooks = 0;
        // For testing. If >0 overrules the result of GetNumberOfThreads
        private const int maxThreads = 1;

        // Try to be as lenient as possible (and Trim the results).
        private Regex _publisherTitleRegex = new(@"(\[)(?<publisher>.*?)(\])(?<title>.*)");

        private readonly AzwAction _action;

        private readonly string _fileOrDirectory;

        public AzwConverter(AzwAction action, string fileOrDirectory)
        {
            using IHost host = Host.CreateDefaultBuilder().Build();
            var config = host.Services.GetRequiredService<IConfiguration>();

            Settings.ReadAppSettings(config);

            Console.WriteLine($"Azw files: {Settings.AzwDir}");
            Console.WriteLine($"Titles files: {Settings.TitlesDir}");
            Console.WriteLine($"Cbz backups: {Settings.CbzDir}");
            Console.WriteLine();

            _action = action;
            _fileOrDirectory = fileOrDirectory; //TODO
        }

        public void ConvertOrScan()
        {
            var reader = new TitleReader();
  
            Console.Write("Reading current titles: ");
            var titles = reader.ReadTitles();
            Console.WriteLine(titles.Count);

            //if (_action == AzwAction.Analyze)
            //{
            //    var analyzer = new AzwAnalyzer();
            //    analyzer.Analyze(books, titles, GetNumberOfThreads());

            //    return;
            //}

            Console.Write("Reading archived titles: ");
            var archive = new ArchiveDb();
            Console.WriteLine(archive.Count);

            Console.Write("Reading converted titles: ");
            var convertedTitles = reader.ReadConvertedTitles();
            Console.WriteLine(convertedTitles.Count);
            Console.WriteLine();

            // Key is the book id, Value is a list of book datafiles 
            Console.Write("Reading books: ");
            var books = reader.ReadBooks();
            Console.WriteLine(books.Count);

            var syncer = new TitleSyncer();

            // Number of books are stable after title syncing.
            var added = syncer.SyncBooksToTitles(books, titles, archive);
            Console.WriteLine($"Added {added} missing titles");

            var archived = syncer.SyncTitlesToArchive(titles, archive, books);
            Console.WriteLine($"Archived {archived} titles");

            Console.WriteLine();
            // Display the scanning results
            var updatedBooks = GetUpdatedBooks(books, convertedTitles);
            ProgressReporter.Info($"Found {updatedBooks.Count} updated book{updatedBooks.SIf1()}");

            var unconvertedBooks = GetUnconvertedBooks(books, convertedTitles);
            ProgressReporter.DoneOrInfo($"Found {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}", unconvertedBooks.Count);

            // Determine if we have more work to do
            if (updatedBooks.Count == 0 && unconvertedBooks.Count == 0)
            {
                Console.WriteLine();
                ProgressReporter.Info("Done");
                return;
            }

            RunActionsInParallel(books, updatedBooks, unconvertedBooks, titles, convertedTitles, archive, syncer);
        }
        
        private void RunActionsInParallel(Dictionary<string, FileInfo[]> books,
            List<KeyValuePair<string, FileInfo[]>> updatedBooks, List<KeyValuePair<string, FileInfo[]>> unconvertedBooks,
            Dictionary<string, FileInfo> titles, Dictionary<string, FileInfo> convertedTitles,
            ArchiveDb archive, TitleSyncer syncer)
        {
      
            var numberOfThreads = GetNumberOfThreads();
            var options = new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads };

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            try
            {
                if (updatedBooks.Count > 0)
                {
                    totalBooks = updatedBooks.Count;
                    bookCount = 0;

                    Console.WriteLine();
                    ProgressReporter.Info($"Scanning {updatedBooks.Count} updated book{updatedBooks.SIf1()} for changes:");

                    var converter = new ConverterEngine(readonlyMode: true);
                    Parallel.ForEach(updatedBooks, options, b => ScanUpdatedBook(converter, archive, b.Key, b.Value, titles[b.Key]));
                }

                if (unconvertedBooks.Count > 0)
                {
                    totalBooks = unconvertedBooks.Count;
                    bookCount = 0;

                    if (_action == AzwAction.Convert)
                    {
                        Console.WriteLine();
                        ProgressReporter.Info($"Converting {unconvertedBooks.Count} book{unconvertedBooks.SIf1()}:");

                        var converter = new ConverterEngine();
                        Parallel.ForEach(unconvertedBooks, options, book => 
                            ConvertBook(book.Key, book.Value, titles[book.Key], convertedTitles, converter, syncer, archive));
                    }
                    else if (_action == AzwAction.Scan)
                    {
                        Console.WriteLine();
                        ProgressReporter.Info($"Listing {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}:");

                        Parallel.ForEach(unconvertedBooks, options, b => SyncNewBook(b.Key, titles[b.Key], archive));
                    }
                }
            }
            finally
            {
                archive.SaveDb();
            }

            stopWatch.Stop();
            var elapsed = stopWatch.Elapsed;
            var secsPerPage = elapsed.TotalSeconds / pagesCount;

            Console.WriteLine();
            if (_action == AzwAction.Convert)
            {
                Console.WriteLine($"{pagesCount} pages converted in {elapsed.TotalSeconds:F3} seconds ({secsPerPage:F3} per page)");
            }
            else
            {
                Console.WriteLine("Done");
            }
        }

        private void ConvertBook(string bookId, FileInfo[] dataFiles, 
            FileInfo titleFile, Dictionary<string, FileInfo> convertedTitles,
            ConverterEngine converter, TitleSyncer syncer, ArchiveDb archive)
        {
            // Validate the titlefile
            var match = _publisherTitleRegex.Match(titleFile.Name);
            if (!match.Success)
            {
                ProgressReporter.Error($"Invalid title file: {titleFile.Name}");
                return;
            }
            var publisher = match.Groups["publisher"].Value.Trim();
            var title = match.Groups["title"].Value.Trim();

            var publisherDir = Path.Combine(Settings.CbzDir, publisher);
            publisherDir.CreateDirIfNotExists();

            var cbzFile = Path.Combine(publisherDir, $"{title}.cbz");
            var state = converter.ConvertBook(bookId, dataFiles, cbzFile);

            var newTitleFile = RemoveMarker(titleFile);
            syncer.SyncConvertedTitle(bookId, newTitleFile, convertedTitles);

            // Title may have been renamed between scanning and converting, so sync it again.
            state.Name = Path.GetFileName(newTitleFile);
            archive.SetState(bookId, state);

            PrintCbzState(cbzFile, state);
        }

        private string BookCountOutputHelper(string path, out StringBuilder sb)
        {
            sb = new StringBuilder();
            sb.AppendLine();

            var i = Interlocked.Increment(ref bookCount);
            var str = $"{i}/{totalBooks} - ";
            var insert = " ".PadLeft(str.Length);

            sb.Append(str).Append(Path.GetFileName(path));

            return insert;
        }

        private void ScanUpdatedBook(ConverterEngine converter, ArchiveDb archive,
            string bookId, FileInfo[] dataFiles, FileInfo titleFile)
        {
            var state = converter.ScanBook(bookId, dataFiles);

            state.Name = titleFile.Name;
            var updated = archive.IsStateUpdated(bookId, state);

            var insert = BookCountOutputHelper(titleFile.FullName, out var sb);

            if (updated)
            {
                var newTitleFile = AddMarker(titleFile, Settings.UpdatedTitleMarker);

                sb.AppendLine();
                sb.Append(insert).Append(Path.GetFileName(newTitleFile));

                ProgressReporter.Done(sb.ToString());
            }
            else
            {
                ProgressReporter.Info(sb.ToString());
            }
        }

        private void SyncNewBook(string bookId, FileInfo titleFile, ArchiveDb archive)
        {
            // Sync title before the .NEW marker is added.
            var emptyState = new CbzState { Name = titleFile.Name };
            archive.SetState(bookId, emptyState);

            var newTitleFile = AddMarker(titleFile, Settings.NewTitleMarker);
            BookCountOutputHelper(newTitleFile, out var sb);

            ProgressReporter.Done(sb.ToString());
        }

        private static string AddMarker(FileInfo titleFile, string marker)
        {
            var name = titleFile.Name;
            if (!name.StartsWith(marker))
            {
                name = $"{marker} {name}";
            }

            var newTitleFile = Path.Combine(Settings.TitlesDir, name);
            titleFile.MoveTo(newTitleFile);

            return newTitleFile;
        }

        private static string RemoveMarker(FileInfo titleFile)
        {
            var name = titleFile.Name.RemoveAllMarkers();

            var newTitleFile = Path.Combine(Settings.TitlesDir, name);
            titleFile.MoveTo(newTitleFile);

            return newTitleFile;
        }

        private static List<KeyValuePair<string, FileInfo[]>> GetUpdatedBooks(Dictionary<string, FileInfo[]> books,
            Dictionary<string, FileInfo> convertedTitles)
        {
            var updatedBooks = new List<KeyValuePair<string, FileInfo[]>>();

            foreach (var book in books)
            {
                // If the title has been converted
                if (convertedTitles.TryGetValue(book.Key, out var convertedTitle))
                {
                    var convertedDate = convertedTitle.LastWriteTime;

                    // Test if the two datafiles has been updated since the conversion
                    var azwFile = book.Value.First(file => file.IsAzwFile());
                    if (azwFile.LastWriteTime > convertedDate)
                    {
                        updatedBooks.Add(book);
                        continue;
                    }

                    var azwResFile = book.Value.FirstOrDefault(file => file.IsAzwResFile());
                    if (azwResFile != null && azwFile.LastWriteTime > convertedDate)
                    {
                        updatedBooks.Add(book);
                    }
                }
            }

            return updatedBooks;
        }

        private static List<KeyValuePair<string, FileInfo[]>> GetUnconvertedBooks(Dictionary<string, FileInfo[]> books,
            Dictionary<string, FileInfo> convertedTitles)
        {
            var unConvertedBooks = books.Where(b => !convertedTitles.ContainsKey(b.Key)).ToList();
            if (maxBooks > 0)
            {
                unConvertedBooks = unConvertedBooks.Take(maxBooks).ToList();

            }
            return unConvertedBooks;
        }

        private static int GetNumberOfThreads()
        {
            var numberOfThreads = Math.Min(Settings.NumberOfThreads, Environment.ProcessorCount);
            if (maxThreads > 0)
            {
                numberOfThreads = maxThreads;
            }
            return numberOfThreads;
        }

        private void PrintCbzState(string cbzFile, CbzState state)
        {
            Interlocked.Add(ref pagesCount, state.Pages);

            var insert = BookCountOutputHelper(cbzFile, out var sb);
            sb.AppendLine();

            sb.Append(insert);
            sb.Append(state.Pages).Append(" pages (");
            if (state.HdImages > 0)
            {
                sb.Append(state.HdImages).Append(" HD");
                if (state.SdImages > 0)
                {
                    sb.Append('/');
                }
            }
            if (state.SdImages > 0)
            {
                sb.Append(state.SdImages).Append(" SD");
            }
            if (!state.HdCover && !state.SdCover)
            {
                sb.Append(". NO COVER)");
            }
            else if (!state.HdCover)
            {
                sb.Append(". No HD cover)");
            }
            else
            {
                sb.Append(')');
            }
            Console.WriteLine(sb.ToString());
        }
    }
}