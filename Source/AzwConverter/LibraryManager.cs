using AzwConverter.Engine;
using CbzMage.Shared.CollectionManager;
using CbzMage.Shared.Extensions;
using MobiMetadata;
using System.Collections.Concurrent;

namespace AzwConverter
{
    public class LibraryManager
    {
        private readonly CollectionDb<CbzItem> _collectionDb;

        public LibraryManager(CollectionDb<CbzItem> collectionDb)
        {
            _collectionDb = collectionDb;
        }

        public IDictionary<string, FileInfo[]> ReadBooks()
        {
            var dict = new ConcurrentDictionary<string, FileInfo[]>();

            var directoryInfo = new DirectoryInfo(Settings.AzwDir);
            var allFiles = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);

            allFiles.ToLookup(dir => dir.DirectoryName).AsParallel().ForAll(files =>
            {
                if (files.Any(file => file.IsAzwOrAzw3File()))
                {
                    dict[Path.GetFileName(files.Key)!] = files.ToArray();
                }
            });

            return new Dictionary<string, FileInfo[]>(dict);
        }

        public async Task<(int skippedItems, int ignoredItems)> SyncBooksToCollectionAsync(IDictionary<string, FileInfo[]> books, IDictionary<string, FileInfo> titles)
        {
            var itemsWithErrors = new ConcurrentBag<string>();
            var skippedItems = new ConcurrentBag<string>();

            var addedTitles = new ConcurrentDictionary<string, FileInfo>();

            await Parallel.ForEachAsync(books, Settings.ParallelOptions, async (item, ct) =>
            {
                var itemId = item.Key;

                // Book is not in current titles
                if (!titles.ContainsKey(itemId))
                {
                    // Try the archive
                    if (_collectionDb.TryGetName(itemId, out var name))
                    {
                        await SyncAsync(name);
                    }
                    else
                    {
                        // Or scan the book file
                        MobiMetadata.MobiMetadata metadata;
                        IDisposable[] disposables;
                        try
                        {
                            var bookFiles = item.Value;

                            var engine = new MetadataEngine();
                            (metadata, disposables) = await engine.GetMetadataAsync(bookFiles);

                            if (!Settings.ConvertAllBookTypes
                                && !metadata.MobiHeader.ExthHeader.BookType.EqualsIgnoreCase("comic"))
                            {
                                skippedItems.Add(itemId);
                                return;
                            }

                            MetadataManager.CacheMetadata(itemId, metadata, disposables);
                        }
                        catch (MobiMetadataException)
                        {
                            itemsWithErrors.Add(itemId);
                            return;
                        }

                        var title = metadata.MobiHeader.GetFullTitle();
                        title = title.ToFileSystemString();

                        var publisher = metadata.MobiHeader.ExthHeader.Publisher.ToFileSystemString();
                        publisher = TrimPublisher(publisher);

                        await SyncAsync($"[{publisher}] {title}");
                    }

                    async Task SyncAsync(string titleFile)
                    {
                        var file = Path.Combine(Settings.TitlesDir, titleFile);
                        await File.WriteAllTextAsync(file, itemId, CancellationToken.None);

                        // Add archived/scanned title to list of current titles
                        addedTitles[itemId] = new FileInfo(file);
                    }
                }
            });

            foreach (var bookId in itemsWithErrors)
            {
                books.Remove(bookId);
            }

            foreach (var bookId in skippedItems)
            {
                books.Remove(bookId);
            }

            foreach (var title in addedTitles)
            {
                titles.Add(title);
            }

            return (skippedItems: addedTitles.Count, ignoredItems: skippedItems.Count);
        }

        private static string TrimPublisher(string publisher)
        {
            // Normalize publisher name
            foreach (var trimmedName in Settings.TrimPublishers)
            {
                if (publisher.StartsWith(trimmedName, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedName;
                }
            }

            return publisher;
        }

        // In version 23 and earlier a converted titlefile did not get archived together with the
        // main titlefile. So we must trim the converted titles to be consistent with version 24+
        // The trimming is only expensive first time it's run.
        public static void TrimConvertedTitles(IDictionary<string, FileInfo> convertedTitles, IDictionary<string, FileInfo> titles)
        {
            var idsToRemove = new ConcurrentBag<string>();

            convertedTitles.AsParallel().ForAll(convertedTitle =>
            {
                if (!titles.TryGetValue(convertedTitle.Key, out var _))
                {
                    idsToRemove.Add(convertedTitle.Key);
                    convertedTitle.Value.Delete();
                }
            });

            foreach (var bookId in idsToRemove)
            {
                convertedTitles.Remove(bookId);
            }
        }
    }
}
