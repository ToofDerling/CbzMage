using AzwConverter.Engine;
using System.Collections.Concurrent;
using System.Net;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public async Task<int> SyncBooksToTitlesAsync(IDictionary<string, FileInfo[]> books, 
            IDictionary<string, FileInfo> titles, ArchiveDb archive)
        {
            var syncedBookCount = 0;
            var booksWithErrors = new List<string>();

            foreach (var book in books)
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
                        catch (MobiMetadata.MobiMetadataException)
                        {
                            booksWithErrors.Add(bookId);
                            continue;
                        }

                        var title = metadata.MobiHeader.ExthHeader.UpdatedTitle;
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            title = metadata.MobiHeader.FullName;
                        }

                        title = CleanStr(title);
                        var publisher = CleanStr(metadata.MobiHeader.ExthHeader.Publisher);

                        publisher = TrimPublisher(publisher);
                        await SyncAsync($"[{publisher}] {title}");
                    }

                    async Task SyncAsync(string titleFile)
                    {
                        var file = Path.Combine(Settings.TitlesDir, titleFile);
                        await File.WriteAllTextAsync(file, bookId, CancellationToken.None);

                        // Add archived/scanned title to list of current titles
                        titles[bookId] = new FileInfo(file);
                        syncedBookCount++;
                    }
                }
            }

            foreach (var bookId in booksWithErrors)
            {
                books.Remove(bookId);
            }

            return syncedBookCount;
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

        private static string CleanStr(string str)
        {
            str = WebUtility.HtmlDecode(str);
            return str.ToFileSystemString();
        }

        public int SyncTitlesToArchive(IDictionary<string, FileInfo> titles, ArchiveDb archive, IDictionary<string, FileInfo[]> books)
        {
            var idsToRemove = new ConcurrentBag<string>();
            var archivedTitleCount = 0;

            titles.AsParallel().ForAll(title =>
            {
                var bookId = title.Key;
                var titleFile = title.Value;

                archive.SetOrCreateName(bookId, titleFile.Name);

                // Delete title if no longer in books.
                if (!books.ContainsKey(bookId))
                {
                    archivedTitleCount++;

                    idsToRemove.Add(bookId);
                    titleFile.Delete();
                }
            });

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                titles.Remove(bookId);
            }

            return archivedTitleCount;
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
