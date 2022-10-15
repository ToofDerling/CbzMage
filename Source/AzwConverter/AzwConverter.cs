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
        // For testing. If >0 overrules the result of GetUnconvertedBooks
        private const int maxBooks = 0;

        // Global veriables updated by processing threads
        private volatile int bookCount;
        private int totalBooks;
        private volatile int pagesCount;

        // Try to be as lenient as possible (and Trim the results).
        private readonly Regex _publisherTitleRegex = new(@"(\[)(?<publisher>.*?)(\])(?<title>.*)");

        private readonly AzwAction _action;

        private readonly string _fileOrDirectory;

        public AzwConverter(AzwAction action, string fileOrDirectory)
        {
            using IHost host = Host.CreateDefaultBuilder().Build();
            var config = host.Services.GetRequiredService<IConfiguration>();

            Settings.ReadAppSettings(config);

            ProgressReporter.Info($"Azw files: {Settings.AzwDir}");
            ProgressReporter.Info($"Title files: {Settings.TitlesDir}");
            ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            if (Settings.SaveCover && Settings.SaveCoverDir != null)
            {
                ProgressReporter.Info($"Saved covers: {Settings.SaveCoverDir}");
            }

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

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                if (updatedBooks.Count > 0 || unconvertedBooks.Count > 0)
                {
                    RunActionsInParallel(updatedBooks, unconvertedBooks, titles, convertedTitles, syncer, archive);
                }
            }
            finally
            {
                archive.SaveDb();
            }
            stopWatch.Stop();
            Console.WriteLine();

            if (_action == AzwAction.AzwConvert && pagesCount > 0 && !Settings.SaveCoverOnly)
            {
                var elapsed = stopWatch.Elapsed;
                var secsPerPage = elapsed.TotalSeconds / pagesCount;

                Console.WriteLine($"{pagesCount} pages converted in {elapsed.TotalSeconds:F3} seconds ({secsPerPage:F3} per page)");
            }
            else
            {
                Console.WriteLine("Done");
            }
        }

        private void RunActionsInParallel(List<KeyValuePair<string, FileInfo[]>> updatedBooks,
            List<KeyValuePair<string, FileInfo[]>> unconvertedBooks,
            Dictionary<string, FileInfo> titles, Dictionary<string, FileInfo> convertedTitles,
            TitleSyncer syncer, ArchiveDb archive)
        {
            if (updatedBooks.Count > 0)
            {
                totalBooks = updatedBooks.Count;
                bookCount = 0;

                Console.WriteLine();
                ProgressReporter.Info($"Scanning {updatedBooks.Count} updated book{updatedBooks.SIf1()} for changes:");

                Parallel.ForEach(updatedBooks, Settings.ParallelOptions, book =>
                    ScanUpdatedBook(book.Key, book.Value, titles[book.Key], archive));
            }

            if (unconvertedBooks.Count > 0)
            {
                totalBooks = unconvertedBooks.Count;
                bookCount = 0;

                if (_action == AzwAction.AzwConvert)
                {
                    Console.WriteLine();
                    ProgressReporter.Info($"Converting {unconvertedBooks.Count} book{unconvertedBooks.SIf1()}:");

                    Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, book =>
                        ConvertBook(book.Key, book.Value, titles[book.Key],
                        convertedTitles.ContainsKey(book.Key) ? convertedTitles[book.Key] : null,
                        syncer, archive));
                }
                else if (_action == AzwAction.AzwScan)
                {
                    Console.WriteLine();
                    ProgressReporter.Info($"Listing {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}:");

                    Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, book =>
                        SyncNewBook(book.Key, titles[book.Key], archive));
                }
            }
        }

        private void ConvertBook(string bookId, FileInfo[] dataFiles, FileInfo titleFile, FileInfo? convertedTitleFile,
            TitleSyncer syncer, ArchiveDb archive)
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
            var coverFile = GetCoverFile(titleFile, cbzFile);

            var converter = new ConverterEngine();

            var state = coverFile != null && Settings.SaveCoverOnly 
                ? converter.SaveCover(bookId, dataFiles, coverFile)
                : converter.ConvertBook(bookId, dataFiles, cbzFile, coverFile);

            var newTitleFile = RemoveMarkerFromFile(titleFile);
            syncer.SyncConvertedTitle(bookId, newTitleFile, convertedTitleFile);

            // Title may have been renamed between scanning and converting, so update the archive.
            state.Name = Path.GetFileName(newTitleFile);
            archive.SetState(bookId, state);

            PrintCbzState(cbzFile, state);
        }

        private string GetCoverFile(FileInfo titleFile, string cbzFile)
        {
            if (Settings.SaveCover)
            {
                return Settings.SaveCoverDir != null
                    ? Path.Combine(Settings.SaveCoverDir, $"{titleFile.Name.RemoveAnyMarker()}.jpg")
                    : Path.ChangeExtension(cbzFile, ".jpg");
            }

            return null;
        }

        private string BookCountOutputHelper(string path, out StringBuilder sb)
        {
            sb = new StringBuilder();
            sb.AppendLine();

            var count = Interlocked.Increment(ref bookCount);
            var str = $"{count}/{totalBooks} - ";
            var insert = " ".PadLeft(str.Length);

            sb.Append(str).Append(Path.GetFileName(path));

            return insert;
        }

        private void ScanUpdatedBook(string bookId, FileInfo[] dataFiles, FileInfo titleFile, ArchiveDb archive)
        {
            var converter = new ConverterEngine();
            var state = converter.ScanBook(bookId, dataFiles);

            state.Name = titleFile.Name;
            var updated = archive.IsStateUpdated(bookId, state);

            var insert = BookCountOutputHelper(titleFile.FullName, out var sb);

            if (updated)
            {
                var newTitleFile = AddMarkerToFile(titleFile, Settings.UpdatedTitleMarker);

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

            var newTitleFile = AddMarkerToFile(titleFile, Settings.NewTitleMarker);
            BookCountOutputHelper(newTitleFile, out var sb);

            ProgressReporter.Done(sb.ToString());
        }

        private static string AddMarkerToFile(FileInfo titleFile, string marker)
        {
            var name = titleFile.Name.AddMarker(marker);

            var newTitleFile = Path.Combine(Settings.TitlesDir, name);
            titleFile.MoveTo(newTitleFile);

            return newTitleFile;
        }

        private static string RemoveMarkerFromFile(FileInfo titleFile)
        {
            var name = titleFile.Name.RemoveAnyMarker();

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