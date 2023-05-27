using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using AzwConverter.Engine;
using CbzMage.Shared;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using CollectionManager;

namespace AzwConverter.Converter
{
    public class AzwLibraryConverter : BaseAzwConverter
    {
        // For testing. If >0 overrules the result of GetUnconvertedBooks
        private const int _maxBooks = 0;

        // Try to be as lenient as possible (and Trim the results).
        private readonly Regex _publisherTitleRegex = new(@"(\[)(?<publisher>.*?)(\])(?<title>.*)");

        private readonly Collection<CbzItem> _collection;

        public AzwLibraryConverter(CbzMageAction action) : base(action)
        {
            var config = new AzwConvertSettings();
            config.CreateSettings();

            var dbFile = Path.Combine(Settings.TitlesDir, Settings.ArchiveName);

            _collection = new Collection<CbzItem>(Settings.TitlesDir, Settings.ConvertedTitlesDirName, Settings.ArchiveName);

            ProgressReporter.Info($"Azw files: {Settings.AzwDir}");
            ProgressReporter.Info($"Title files: {Settings.TitlesDir}");
            ProgressReporter.Info($"Cbz backups: {Settings.CbzDir}");
            if (Settings.SaveCover && Settings.SaveCoverDir != null)
            {
                ProgressReporter.Info($"Cover gallery: {Settings.SaveCoverDir}");
            }
            ProgressReporter.Line();

            ProgressReporter.Info($"Conversion threads: {Settings.NumberOfThreads}");
            ProgressReporter.Info($"Cbz compression: {Settings.CompressionLevel}");

            ProgressReporter.Line();
        }

        public async Task ConvertOrScanAsync()
        {
            Console.Write("Reading current titles: ");
            var titles = await _collection.Reader.ReadItemsAsync();
            Console.WriteLine(titles.Count);

            Console.Write("Reading archived titles: ");
            await _collection.Db.ReadDbAsync();
            Console.WriteLine(_collection.Db.Count);

            Console.Write("Reading converted titles: ");
            var convertedTitles = await _collection.Reader.ReadProcessedItemsAsync();
            Console.WriteLine(convertedTitles.Count);
            Console.WriteLine();

            // Key is the book id, Value is a list of book datafiles 
            Console.Write("Reading books: ");
            var library = new LibraryManager(_collection.Db);
            var books = library.ReadBooks();
            Console.WriteLine(books.Count);

            // Number of books is stable after title syncing.
            var (added, skipped) = await library.SyncBooksToCollectionAsync(books, titles);

            Console.Write($"Added {added} missing title{added.SIf1()}");
            if (!Settings.ConvertAllBookTypes)
            {
                Console.WriteLine($" (skipped {skipped})");
            }
            else
            {
                Console.WriteLine();
            }

            var archived = _collection.Syncer.SyncAndArchiveItems(titles, convertedTitles, books);
            Console.WriteLine($"Archived {archived} title{archived.SIf1()}");

            Console.WriteLine();

            var updatedBooks = GetUpdatedBooks(books, convertedTitles);
            ProgressReporter.Info($"Found {updatedBooks.Count} updated book{updatedBooks.SIf1()}");

            var unconvertedBooks = GetUnconvertedBooks(books, convertedTitles);
            ProgressReporter.DoneOrInfo($"Found {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}", unconvertedBooks.Count);

            ConversionBegin();
            try
            {
                if (updatedBooks.Count > 0 || unconvertedBooks.Count > 0 || Action == CbzMageAction.AzwAnalyze)
                {
                    await RunActionsInParallelAsync(books, updatedBooks, unconvertedBooks, titles, convertedTitles);
                }
            }
            finally
            {
                await _collection.Db.SaveArchiveDbAsync();
            }
            ConversionEnd(unconvertedBooks.Count);

#if DEBUG
            MetadataManager.ThrowIfCacheNotEmpty();
#endif
        }

        private async Task RunActionsInParallelAsync(IDictionary<string, FileInfo[]> books,
            IReadOnlyCollection<KeyValuePair<string, FileInfo[]>> updatedBooks,
            IReadOnlyCollection<KeyValuePair<string, FileInfo[]>> unconvertedBooks,
            IDictionary<string, FileInfo> titles, IDictionary<string, FileInfo> convertedTitles)
        {
            if (updatedBooks.Count > 0)
            {
                _totalBooks = updatedBooks.Count;
                _bookCount = 0;

                Console.WriteLine();
                ProgressReporter.Info($"Checking {updatedBooks.Count} updated book{updatedBooks.SIf1()}:");

                await Parallel.ForEachAsync(updatedBooks, Settings.ParallelOptions,
                    async (book, _) =>
                        await ScanUpdatedBookAsync(book.Key, book.Value, titles[book.Key],
                        convertedTitles.TryGetValue(book.Key, out var convertedTitle) ? convertedTitle : null));
            }

            if (unconvertedBooks.Count == 0 && Action != CbzMageAction.AzwAnalyze)
            {
                return;
            }

            _bookCount = 0;

            Console.WriteLine();

            if (Action == CbzMageAction.AzwConvert)
            {
                ProgressReporter.Info($"Converting {unconvertedBooks.Count} book{unconvertedBooks.SIf1()}:");
                _totalBooks = unconvertedBooks.Count;

                await Parallel.ForEachAsync(unconvertedBooks, Settings.ParallelOptions,
                    async (book, _) =>
                        await ConvertBookAsync(book.Key, book.Value, titles[book.Key],
                        convertedTitles.TryGetValue(book.Key, out var convertedTitle)
                            ? convertedTitle
                            : null));
            }
            else if (Action == CbzMageAction.AzwScan)
            {
                ProgressReporter.Info($"Listing {unconvertedBooks.Count} unconverted book{unconvertedBooks.SIf1()}:");
                _totalBooks = unconvertedBooks.Count;

                Parallel.ForEach(unconvertedBooks, Settings.ParallelOptions, book =>
                    SyncNewBook(book.Key, titles[book.Key]));
            }
            else if (Action == CbzMageAction.AzwAnalyze)
            {
                ProgressReporter.Info($"Analyzing {books.Count} book{books.SIf1()}:");
                _totalBooks = books.Count;

                await Parallel.ForEachAsync(books, Settings.ParallelOptions,
                    async (book, _) =>
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
            var state = await engine.AnalyzeBookAsync(bookId, dataFiles, analyzeImages: false, bookDir);

            var analyzeMessageOk = engine.GetAnalyzeMessageOk();
            var analyzeMessageError = engine.GetAnalyzeMessageError();

            if (analyzeMessageOk != null || analyzeMessageError != null)
            {
                PrintCbzState(bookDir, state, showPagesAndCover: false, doneMsg: analyzeMessageOk, errorMsg: analyzeMessageError);
            }
            else
            {
                Interlocked.Increment(ref _bookCount);
            }
        }

        private async Task ConvertBookAsync(string bookId, FileInfo[] dataFiles, FileInfo titleFile, FileInfo? convertedTitleFile)
        {
            if (!TryParseTitleFile(titleFile, out var publisher, out var title))
            {
                return;
            }

            var publisherDir = Path.Combine(Settings.CbzDir, publisher);
            publisherDir.CreateDirIfNotExists();

            var cbzFile = Path.Combine(publisherDir, $"{title}.cbz");
            var coverFile = GetCoverFile(titleFile, cbzFile);

            CbzItem? state = null;

            if (coverFile != null && Settings.SaveCoverOnly)
            {
                await SaveCoverAsync(bookId, dataFiles, coverFile);
            }
            else
            {
                var engine = new ConvertBookEngine();
                state = await engine.ConvertBookAsync(bookId, dataFiles, cbzFile, coverFile);
            }

            var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile);
            _collection.Syncer.SyncProcessedItem(newTitleFile, convertedTitleFile);

            if (state != null)
            {
                PrintCbzState(cbzFile, state);
            }
            else
            {
                state = new CbzItem();
            }

            // Title may have been renamed between scanning and converting, so update the archive.
            // This also removes any Changed state: if the book is updated it will be scanned again.  
            state.Name = Path.GetFileName(newTitleFile).RemoveAnyMarker();
            _collection.Db.SetItem(bookId, state);
        }

        private bool TryParseTitleFile(FileInfo titleFile, out string publisher, out string title)
        {
            publisher = title = null!;

            var match = _publisherTitleRegex.Match(titleFile.Name);
            if (!match.Success)
            {
                ProgressReporter.Error($"Invalid title file [{titleFile.Name}]");
                return false;
            }

            publisher = match.Groups["publisher"].Value.Trim();
            title = match.Groups["title"].Value.Trim();

            return true;
        }

        private async Task SaveCoverAsync(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            var engine = new SaveBookCoverEngine();
            await engine.SaveBookCoverAsync(bookId, dataFiles, coverFile);

            PrintCoverString(coverFile, engine.GetCoverString()!);
        }

        private static string? GetCoverFile(FileInfo titleFile, string cbzFile)
        {
            if (Settings.SaveCover)
            {
                return Settings.SaveCoverDir != null
                    ? Path.Combine(Settings.SaveCoverDir, $"{titleFile.Name.RemoveAnyMarker()}.jpg")
                    : Path.ChangeExtension(cbzFile, ".jpg");
            }

            return null;
        }

        private async Task ScanUpdatedBookAsync(string bookId, FileInfo[] dataFiles, FileInfo titleFile, FileInfo? convertedTitleFile)
        {
            CbzItem state;

            var oldState = _collection.Db.GetItem(bookId);
            if (oldState.Changed != null)
            {
                // If the Changed state has not been removed by converting the book there is
                // no need to rescan - we can use the old values to display any up/downgrade.
                state = oldState;
                oldState = oldState.Changed;
            }
            else
            {
                var engine = new ScanBookEngine();
                state = await engine.ScanBookAsync(bookId, dataFiles);

                state.Name = titleFile.Name.RemoveAnyMarker();
            }

            var coverUpgraded = state.HdCover && !oldState.HdCover;
            var pagesUpgraded = state.HdImages > oldState.HdImages;

            var coverDowngraded = !coverUpgraded && !state.HdCover && oldState.HdCover;
            var pagesDowngraded = !pagesUpgraded && state.HdImages < oldState.HdImages;

            string? downgradedMessage = null;
            if (coverDowngraded || pagesDowngraded)
            {
                downgradedMessage = GetUpdatedMessage(state, oldState, coverDowngraded, pagesDowngraded,
                    "HD cover removed", "HD pages removed");
            }

            string? upgradedMessage = null;
            if (coverUpgraded || pagesUpgraded)
            {
                upgradedMessage = GetUpdatedMessage(state, oldState, coverUpgraded, pagesUpgraded,
                    "HD cover added", "HD pages added");
            }

            if (downgradedMessage != null || upgradedMessage != null)
            {
                state.Changed = oldState;

                var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile, Settings.UpdatedTitleMarker);

                PrintCbzState(newTitleFile, state, convertedDate: convertedTitleFile?.LastWriteTime,
                    doneMsg: upgradedMessage, errorMsg: downgradedMessage);
            }
            else
            {
                // If there's no changes set the checked date to prevent book being scanned again
                state.Checked = DateTime.Now;
            }

            _collection.Db.SetItem(bookId, state);
        }

        private static string GetUpdatedMessage(CbzItem state, CbzItem oldState,
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

        private void SyncNewBook(string bookId, FileInfo titleFile)
        {
            // Sync title before the .NEW marker is added.
            _collection.Db.SetOrCreateName(bookId, titleFile.Name);

            // We're in scan mode so the metadata is not needed anymore.
            MetadataManager.DisposeCachedMetadata(bookId);

            var newTitleFile = AddMarkerOrRemoveAnyMarker(titleFile, Settings.NewTitleMarker);

            BookCountOutputHelper(newTitleFile, out var sb);
            ProgressReporter.Done(sb.ToString());
        }

        private static string AddMarkerOrRemoveAnyMarker(FileInfo titleFile, string? addMarker = null)
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

        private IReadOnlyCollection<KeyValuePair<string, FileInfo[]>> GetUpdatedBooks(IDictionary<string, FileInfo[]> books,
            IDictionary<string, FileInfo> convertedTitles)
        {
            var updatedBooks = new ConcurrentBag<KeyValuePair<string, FileInfo[]>>();

            books.AsParallel().ForAll(book =>
            {
                // If the title has been converted--
                if (convertedTitles.TryGetValue(book.Key, out var convertedTitle))
                {
                    var item = _collection.Db.GetItem(book.Key);

                    var checkedDate = item.Checked ?? convertedTitle.LastWriteTime;

                    // --test if the two datafiles has been updated since last check
                    if (book.Value.Any(file => (file.IsAzwOrAzw3File() || file.IsAzwResOrAzw6File())
                        && file.LastWriteTime > checkedDate))
                    {
                        updatedBooks.Add(book);
                    }
                }
            });

            return updatedBooks;
        }

        private static IReadOnlyCollection<KeyValuePair<string, FileInfo[]>> GetUnconvertedBooks(IDictionary<string, FileInfo[]> books,
            IDictionary<string, FileInfo> convertedTitles)
        {
            var unConvertedBooks = books.AsParallel().Where(b => !convertedTitles.ContainsKey(b.Key)).ToList();
            if (_maxBooks > 0)
            {
                unConvertedBooks = unConvertedBooks.Take(_maxBooks).ToList();
            }
            return unConvertedBooks;
        }
    }
}