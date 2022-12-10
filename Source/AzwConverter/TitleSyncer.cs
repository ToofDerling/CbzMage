using AzwConverter.Engine;
using MobiMetadata;
using System.Collections.Concurrent;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public async Task<int> SyncBooksToTitlesAsync(IDictionary<string, FileInfo[]> books, 
            IDictionary<string, FileInfo> titles, ArchiveDb archive)
        {
            var booksWithErrors = new ConcurrentBag<string>();
            var newOrArchivedTitles = new ConcurrentDictionary<string, FileInfo>();

            await Parallel.ForEachAsync(books, Settings.ParallelOptions, async (book, ct) =>
            {
                var bookId = book.Key;

                // Book is not in current titles
                if (!titles.ContainsKey(bookId))
                {
                    // Try the archive
                    if (archive.TryGetName(bookId, out var name))
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
                            var bookFiles = book.Value;

                            var engine = new MetadataEngine();
                            (metadata, disposables) = await engine.ReadMetadataAsync(bookFiles);

                            MetadataManager.CacheMetadata(bookId, metadata, disposables);
                        }
                        catch (MobiMetadataException)
                        {
                            booksWithErrors.Add(bookId);
                            return;
                        }

                        var title = metadata.MobiHeader.ExthHeader.UpdatedTitle;
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            title = metadata.MobiHeader.FullName;
                        }
                        title = title.ToFileSystemString();

                        var publisher = metadata.MobiHeader.ExthHeader.Publisher.ToFileSystemString();
                        publisher = TrimPublisher(publisher);

                        await SyncAsync($"[{publisher}] {title}");
                    }

                    async Task SyncAsync(string titleFile)
                    {
                        var file = Path.Combine(Settings.TitlesDir, titleFile);
                        await File.WriteAllTextAsync(file, bookId, CancellationToken.None);

                        // Add archived/scanned title to list of current titles
                        newOrArchivedTitles[bookId] = new FileInfo(file);
                    }
                }
            });

            foreach (var bookId in booksWithErrors)
            {
                books.Remove(bookId);
            }

            foreach (var title in newOrArchivedTitles)
            {
                titles.Add(title);                
            }

            return newOrArchivedTitles.Count;
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

        public int SyncTitlesToArchive(IDictionary<string, FileInfo> titles, ArchiveDb archive, IDictionary<string, FileInfo[]> books)
        {
            var idsToRemove = new ConcurrentBag<string>();

            titles.AsParallel().ForAll(title =>
            {
                var bookId = title.Key;
                var titleFile = title.Value;

                archive.SetOrCreateName(bookId, titleFile.Name);

                // Delete title if no longer in books.
                if (!books.ContainsKey(bookId))
                {
                    idsToRemove.Add(bookId);
                    titleFile.Delete();
                }
            });

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                titles.Remove(bookId);
            }

            return idsToRemove.Count;
        }

        public string SyncConvertedTitle(string titleFile, FileInfo? convertedTitleFile)
        {
            convertedTitleFile?.Delete();

            var name = Path.GetFileName(titleFile);
            name = name.RemoveAnyMarker();

            var dest = Path.Combine(Settings.ConvertedTitlesDir, name);

            File.Copy(titleFile, dest);
            File.SetLastWriteTime(dest, DateTime.Now);

            return name;
        }
    }
}
