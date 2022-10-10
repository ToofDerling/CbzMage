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
        private const int maxThreads = 0;

        private readonly AzwAction _action;

        private readonly string _fileOrDirectory;

        public AzwConverter(AzwAction action, string fileOrDirectory)
        {
            using IHost host = Host.CreateDefaultBuilder().Build();
            var config = host.Services.GetRequiredService<IConfiguration>();

            Settings.ReadAppSettings(config);

            Console.WriteLine($"Azw directory: {Settings.AzwDir}");
            Console.WriteLine($"Titles directory: {Settings.TitlesDir}");
            Console.WriteLine($"Cbz directory: {Settings.CbzDir}");
            Console.WriteLine();

            _action = action;
            _fileOrDirectory = fileOrDirectory; //TODO
        }

        public void ConvertOrScan()
        {
            var reader = new TitleReader();
            // Key is the book id, Value is a list of book datafiles 
            var books = reader.ReadBooks();

            var titles = reader.ReadTitles();
            Console.WriteLine($"Found {titles.Count} current title{titles.SIf1()}");

            //if (_action == AzwAction.Analyze)
            //{
            //    var analyzer = new AzwAnalyzer();
            //    analyzer.Analyze(books, titles, GetNumberOfThreads());

            //    return;
            //}

            var archive = new ArchiveDb();
            Console.WriteLine($"Found {archive.Count} archived title{archive.Count.SIf1()}");

            var convertedTitles = reader.ReadConvertedTitles();
            Console.WriteLine($"Found {convertedTitles.Count} converted title{convertedTitles.SIf1()}");

            var syncer = new TitleSyncer();
            Console.WriteLine();

            var added = syncer.SyncBooksToTitles(books, titles, archive);
            Console.WriteLine($"Added {added} missing titles");
            var archived = syncer.SyncTitlesToArchive(titles, archive, books);
            Console.WriteLine($"Archived {archived} titles");

            // Number of books are stable after title syncing.
            Console.WriteLine();
            Console.WriteLine($"Found {books.Count} book{books.SIf1()}");

            var unConvertedBooks = GetUnconvertedBooks(books, convertedTitles);
            var doneStr = $"Found {unConvertedBooks.Count} unconverted book{unConvertedBooks.SIf1()}";
            if (unConvertedBooks.Any())
            {
                ProgressReporter.Done(doneStr);
            }
            else
            { 
                ProgressReporter.Info(doneStr);
                if (_action != AzwAction.ScanUpdated)
                {
                    Console.WriteLine();
                    ProgressReporter.Info("Done");
                    return;
                }
            }

            RunActionsInParallel(books, unConvertedBooks, titles, convertedTitles, archive, syncer);
        }

        private void RunActionsInParallel(Dictionary<string, FileInfo[]> books, List<KeyValuePair<string, FileInfo[]>> unConvertedBooks,
            Dictionary<string, FileInfo> titles, Dictionary<string, FileInfo> convertedTitles,
            ArchiveDb archive, TitleSyncer syncer)
        {
            var numberOfThreads = GetNumberOfThreads();
            var converter = new ConverterEngine(_action);

            // Try to be as lenient as possible (and Trim the results).
            var publisherTitleRegex = new Regex(@"(\[)(?<publisher>.*?)(\])(?<title>.*)");

            Console.WriteLine();
            Console.WriteLine(_action == AzwAction.Convert ? "Converting..." : "Scanning...");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var options = new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads };

            try
            {
                totalBooks = unConvertedBooks.Count;
                if (_action == AzwAction.Convert)
                {
                    Parallel.ForEach(unConvertedBooks, options, b => ConvertBook(b.Key, b.Value, titles[b.Key]));
                }
                else if (_action == AzwAction.ScanNew)
                {
                    Parallel.ForEach(unConvertedBooks, options, b => SyncNewBook(b.Key, titles[b.Key], archive));
                }
                else if (_action == AzwAction.ScanUpdated)
                {
                    Parallel.ForEach(unConvertedBooks, options, b => SyncNewBook(b.Key, titles[b.Key], archive));

                    var checkUpdated = books.Except(unConvertedBooks);
                    totalBooks = checkUpdated.Count();
                    bookCount = 0; // Reset

                    Parallel.ForEach(checkUpdated, options, b => ScanAndSyncUpdatedBook(converter, archive, b.Key, b.Value, titles[b.Key]));
                }
            }
            finally
            {
                archive.SaveDb();
            }

            void ConvertBook(string bookId, FileInfo[] dataFiles, FileInfo titleFile)
            {
                // Validate the titlefile
                var match = publisherTitleRegex.Match(titleFile.Name);
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

        private void ScanAndSyncUpdatedBook(ConverterEngine converter, ArchiveDb archive,
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