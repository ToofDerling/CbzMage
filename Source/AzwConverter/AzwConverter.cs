using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AzwConverter.Engine;
using CbzMage.Shared;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;

namespace AzwConverter
{
    public class AzwConverter
    {
        // For testing. If >0 overrules the result of GetUnconvertedBooks
        private const int maxBooks = 0;

        private int totalBooks;

        // Global veriables updated by processing threads
        private volatile int bookCount;
        private volatile int pagesCount;

        // Try to be as lenient as possible (and Trim the results).
        private readonly Regex _publisherTitleRegex = new(@"(\[)(?<publisher>.*?)(\])(?<title>.*)");

        private readonly CbzMageAction _action;

        //TODO private readonly string _fileOrDirectory;

        public AzwConverter(CbzMageAction action/*, string fileOrDirectory*/)
        {
            _action = action;

            var config = new AzwSettings();
            config.CreateSettings();

            ProgressReporter.Info($"Azw files: {Settings.AzwDir}");
            ProgressReporter.Info($"Title files: {Settings.TitlesDir}");
            ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            if (Settings.SaveCover && Settings.SaveCoverDir != null)
            {
                ProgressReporter.Info($"Cover gallery: {Settings.SaveCoverDir}");
            }
            Console.WriteLine();

            Console.WriteLine($"Conversion threads: {Settings.NumberOfThreads}");
            Console.WriteLine($"Cbz compression: {Settings.CompressionLevel}");

            Console.WriteLine();

            //TODO _fileOrDirectory = fileOrDirectory; 
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

            // Number of books is stable after title syncing.
            var added = syncer.SyncBooksToTitles(books, titles, archive);
            Console.WriteLine($"Added {added} missing title{added.SIf1()}");

            var archived = syncer.SyncTitlesToArchive(titles, archive, books);
            Console.WriteLine($"Archived {archived} title{archived.SIf1()}");

            Console.WriteLine();

            var updatedBooks = GetUpdatedBooks(books, convertedTitles, archive);
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

            if (_action == CbzMageAction.AzwConvert && pagesCount > 0)
            {
                var elapsed = stopWatch.Elapsed;
                var secsPerPage = elapsed.TotalSeconds / pagesCount;

                if (Settings.SaveCoverOnly)
                {
                    Console.WriteLine($"{bookCount} covers saved in {elapsed.Hhmmss()}");
                }
                else if (unconvertedBooks.Count > 0)
                {
                    Console.WriteLine($"{pagesCount} pages converted in {elapsed.Hhmmss()} ({secsPerPage:F2} sec/page)");
                }
                else
                {
                    Console.WriteLine("Done");
                }
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
                ProgressReporter.Info($"Checking {updatedBooks.Count} updated book{updatedBooks.SIf1()}:");

                Parallel.ForEach(updatedBooks, Settings.ParallelOptions, async book =>
                    await ScanUpdatedBookAsync(book.Key, book.Value, titles[book.Key], archive));
            }

            if (unconvertedBooks.Count == 0)
            {
                return;
            }

            totalBooks = unconvertedBooks.Count;
            bookCount = 0;

            Console.WriteLine();

            if (_action == CbzMageAction.AzwConvert)
            {
                ProgressReporter.Info($"Converting {unconvertedBooks.Count} book{unconvertedBooks.SIf1()}:");

                Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, async book =>
                    await ConvertBookAsync(book.Key, book.Value, titles[book.Key],
                    convertedTitles.ContainsKey(book.Key) ? convertedTitles[book.Key] : null,
                    syncer, archive));
            }
            else if (_action == CbzMageAction.AzwScan)
            {
                ProgressReporter.Info($"Listing {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}:");

                Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, book =>
                    SyncNewBook(book.Key, titles[book.Key], archive));
            }
            else if (_action == CbzMageAction.AzwAnalyze)
            {
                ProgressReporter.Info($"Analyzing {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}:");

                Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, async book =>
                    await AnalyzeBookAsync(book.Key, book.Value, titles[book.Key]));
            }
        }

        private async Task AnalyzeBookAsync(string bookId, FileInfo[] dataFiles, FileInfo titleFile)
        {
            string bookDir;

            if (!string.IsNullOrEmpty(Settings.AnalysisDir))
            {
                bookDir = Path.Combine(Settings.AnalysisDir, titleFile.Name);
            }
            else
            {
                if (!TryParseTitleFile(titleFile, out var publisher, out var title))
                {
                    return;
                }
                bookDir = Path.Combine(Settings.CbzDir, publisher, title);
            }

            var engine = new AnalyzeEngine();
            var res = await engine.AnalyzeBookAsync(bookId, dataFiles, bookDir);

            PrintCbzState(bookDir, res.state, errorMsg: res.analyzeMessage, showAllCovers: true);
        }

        private async Task ConvertBookAsync(string bookId, FileInfo[] dataFiles, FileInfo titleFile, FileInfo? convertedTitleFile,
            TitleSyncer syncer, ArchiveDb archive)
        {
            if (!TryParseTitleFile(titleFile, out var publisher, out var title))
            {
                return;
            }

            var publisherDir = Path.Combine(Settings.CbzDir, publisher);
            publisherDir.CreateDirIfNotExists();

            var cbzFile = Path.Combine(publisherDir, $"{title}.cbz");
            var coverFile = GetCoverFile(titleFile, cbzFile);

            CbzState? state = null;

            if (coverFile != null && Settings.SaveCoverOnly)
            {
                await SaveCoverAsync(bookId, dataFiles, coverFile);
            }
            else
            {
                var engine = new ConvertEngine();
                state = await engine.ConvertBookAsync(bookId, dataFiles, cbzFile, coverFile);
            }

            var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile);
            syncer.SyncConvertedTitle(newTitleFile, convertedTitleFile);

            if (state != null)
            {
                PrintCbzState(cbzFile, state);
            }
            else
            {
                state = new CbzState();
            }

            // Title may have been renamed between scanning and converting, so update the archive.
            // This also removes any Changed state. If the book is updated it will be scanned again.  
            state.Name = Path.GetFileName(newTitleFile).RemoveAnyMarker();
            archive.SetState(bookId, state);
        }

        private bool TryParseTitleFile(FileInfo titleFile, out string publisher, out string title)
        {
            publisher = title = null;

            var match = _publisherTitleRegex.Match(titleFile.Name);
            if (!match.Success)
            {
                ProgressReporter.Error($"Invalid title file: {titleFile.Name}");
                return false;
            }

            publisher = match.Groups["publisher"].Value.Trim();
            title = match.Groups["title"].Value.Trim();

            return true;
        }

        private async Task SaveCoverAsync(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            var engine = new CoverEngine();
            await engine.SaveCoverAsync(bookId, dataFiles, coverFile);

            var insert = BookCountOutputHelper(coverFile, out var sb);

            sb.AppendLine();
            sb.Append(insert).Append(engine.GetCoverString());

            Console.WriteLine(sb.ToString());
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

        private async Task ScanUpdatedBookAsync(string bookId, FileInfo[] dataFiles, FileInfo titleFile, ArchiveDb archive)
        {
            CbzState state;

            var oldState = archive.GetState(bookId);
            if (oldState.Changed != null)
            {
                // If the Changed state hasn't been removed by converting the book 
                // there's no need to rescan - we can use the old values to display
                // any up/downgrade.
                state = oldState;
                oldState = oldState.Changed;
            }
            else
            {
                var engine = new ScanEngine();
                state = await engine.ScanBookAsync(bookId, dataFiles);

                state.Name = titleFile.Name.RemoveAnyMarker();
            }

            var coverUpgraded = state.HdCover && !oldState.HdCover;
            var pagesUpgraded = state.HdImages > oldState.HdImages;

            var coverDowngraded = !coverUpgraded && (!state.HdCover && oldState.HdCover);
            var pagesDowngraded = !pagesUpgraded && (state.HdImages < oldState.HdImages);

            string downgradedMessage = null;
            if (coverDowngraded || pagesDowngraded)
            {
                downgradedMessage = GetUpdatedMessage(state, oldState, coverDowngraded, pagesDowngraded,
                    "HD cover removed", "HD pages removed");
            }

            string upgradedMessage = null;
            if (coverUpgraded || pagesUpgraded)
            {
                upgradedMessage = GetUpdatedMessage(state, oldState, coverUpgraded, pagesUpgraded,
                    "HD cover added", "HD pages added");
            }

            if (downgradedMessage != null || upgradedMessage != null)
            {
                state.Changed = oldState;
                archive.SetState(bookId, state);

                var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile, Settings.UpdatedTitleMarker);
                PrintCbzState(newTitleFile, state, doneMsg: upgradedMessage, errorMsg: downgradedMessage);
                return;
            }

            // If there's no changes set the checked date to prevent book being scanned again
            archive.UpdateCheckedDate(bookId);
        }

        private string GetUpdatedMessage(CbzState state, CbzState oldState,
            bool coverUpdated, bool pagesUpdated, string coverMsg, string pagesMsg)
        {
            var sb = new StringBuilder();

            if (coverUpdated)
            {
                sb.Append(coverMsg);
            }

            if (pagesUpdated)
            {
                if (coverUpdated)
                {
                    sb.Append(". ");
                }
                sb.Append(pagesMsg);
                sb.Append(" (");
                sb.Append(oldState.HdImages).Append(" -> ").Append(state.HdImages);
                sb.Append(')');
            }

            return sb.ToString();
        }

        private void SyncNewBook(string bookId, FileInfo titleFile, ArchiveDb archive)
        {
            // Sync title before the .NEW marker is added.
            archive.SetOrCreateName(bookId, titleFile.Name);

            var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile, Settings.NewTitleMarker);
            BookCountOutputHelper(newTitleFile, out var sb);

            ProgressReporter.Done(sb.ToString());
        }

        private static string AddMarkerOrRemoveAnyMarker(FileInfo titleFile, string addMarker = null)
        {
            var name = addMarker != null
                ? titleFile.Name.AddMarker(addMarker)
                : titleFile.Name.RemoveAnyMarker();

            if (name == titleFile.Name)
            {
                return titleFile.FullName;
            }

            var newTitleFile = Path.Combine(Settings.TitlesDir, name);
            titleFile.MoveTo(newTitleFile);

            return newTitleFile;
        }

        private static List<KeyValuePair<string, FileInfo[]>> GetUpdatedBooks(Dictionary<string, FileInfo[]> books,
            Dictionary<string, FileInfo> convertedTitles, ArchiveDb archive)
        {
            var updatedBooks = new List<KeyValuePair<string, FileInfo[]>>();

            foreach (var book in books)
            {
                // If the title has been converted
                if (convertedTitles.TryGetValue(book.Key, out var convertedTitle))
                {
                    var checkedDate = archive.GetCheckedDate(book.Key)
                        ?? convertedTitle.LastWriteTime;

                    // Test if the two datafiles has been updated since last check
                    if (book.Value.Any(file => (file.IsAzwFile() || file.IsAzwResFile())
                        && file.LastWriteTime > checkedDate))
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

        private void PrintCbzState(string cbzFile, CbzState state,
            string doneMsg = null, string errorMsg = null, bool showAllCovers = false)
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

            sb.Append(". ");
            if (showAllCovers)
            {
                if (state.HdCover)
                {
                    sb.Append("HD");
                    if (state.SdCover)
                    {
                        sb.Append('/');
                    }
                }
                if (state.SdCover)
                {
                    sb.Append("SD");
                }
                if (!state.HdCover && !state.SdCover)
                {
                    sb.Append("No");
                }
                sb.Append(" cover");
            }
            else
            {
                if (state.HdCover)
                {
                    sb.Append("HD cover");
                }
                else if (state.HdCover)
                {
                    sb.Append("SD cover)");
                }
                else
                {
                    sb.Append("No cover");
                }
            }
            sb.Append(')');

            if (doneMsg != null || errorMsg != null)
            {
                lock (msgLock)
                {
                    Console.WriteLine(sb.ToString());

                    if (doneMsg != null)
                    {
                        ProgressReporter.Done($"{insert}{doneMsg}");
                    }
                    if (errorMsg != null)
                    {
                        ProgressReporter.Error($"{insert}{errorMsg}");
                    }
                }
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }

        private static readonly object msgLock = new();
    }
}