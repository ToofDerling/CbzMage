using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;
using System.Net;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public async Task<int> SyncBooksToTitlesAsync(IDictionary<string, FileInfo[]> books, IDictionary<string, FileInfo> titles, ArchiveDb archive)
        {
            var syncedBookCount = 0;
            var booksWithErrors = new List<string>();

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
                        var bookFiles = book.Value;
                        var azwFile = bookFiles.First(file => file.IsAzwFile());

                        var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
                        var stream = mappedFile.CreateViewStream();

                        var metadata = MetadataManager.ConfigureFullMetadata();

                        try
                        {
                            await metadata.ReadMetadataAsync(stream);
                        }
                        catch (Exception ex)
                        {
                            ProgressReporter.Error($"Error reading {bookId}.", ex);

                            MetadataManager.Dispose(stream, mappedFile);

                            booksWithErrors.Add(bookId);
                            return;
                        }

                        MetadataManager.CacheMetadata(bookId, metadata, stream, mappedFile);

                        var title = metadata.MobiHeader.ExthHeader.UpdatedTitle;
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            title = metadata.MobiHeader.FullName;
                        }

                        title = CleanStr(metadata.MobiHeader.FullName);
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
            });

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
            var idsToRemove = new List<string>();
            var archivedTitleCount = 0;

            foreach (var title in titles)
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
            }

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
