using AzwConverter.Engine;
using CbzMage.Shared.Extensions;
using MobiMetadata;
using System.Collections.Concurrent;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public async Task<(int skippedBooks, int ignoredBooks)> SyncBooksToTitlesAsync(
            IDictionary<string, FileInfo[]> books, IDictionary<string, FileInfo> titles, ArchiveDb archive)
        {
            var booksWithErrors = new ConcurrentBag<string>();
            var skippedBooks = new ConcurrentBag<string>();

            var addedTitles = new ConcurrentDictionary<string, FileInfo>();

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

                            if (!metadata.MobiHeader.ExthHeader.BookType.EqualsIgnoreCase("comic"))
                            {
                                skippedBooks.Add(bookId);
                                return;
                            }

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
                        addedTitles[bookId] = new FileInfo(file);
                    }
                }
            });

            foreach (var bookId in booksWithErrors)
            {
                books.Remove(bookId);
            }

            foreach (var bookId in skippedBooks)
            { 
                books.Remove(bookId);
            }

            foreach (var title in addedTitles)
            {
                titles.Add(title);
            }

            return (skippedBooks: addedTitles.Count, ignoredBooks: skippedBooks.Count);
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

        public int SyncAndArchiveTitles(IDictionary<string, FileInfo> titles,
            IDictionary<string, FileInfo> convertedTitles,
            ArchiveDb archive, IDictionary<string, FileInfo[]> books)
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

                    // Also delete the converted title 
                    if (convertedTitles.TryGetValue(bookId, out var convertedTitle))
                    {
                        convertedTitle.Delete();
                    }
                }
                else
                {
                    // Sync title -> converted title
                    if (convertedTitles.TryGetValue(bookId, out var convertedTitleFile)
                        && convertedTitleFile.Name != titleFile.Name)
                    {
                        var newConvertedTitleFile = Path.Combine(convertedTitleFile.DirectoryName!, titleFile.Name);
                        convertedTitleFile.MoveTo(newConvertedTitleFile);
                    }
                }
            });

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                titles.Remove(bookId);
                convertedTitles.Remove(bookId); // This is safe even if title is not converted
            }

            // In version 23 and earlier a converted titlefile did not get archived together with the
            // main titlefile. So we must trim the converted titles to be consistent with version 24+
            // The trimming is only expensive first time it's run.
            TrimConvertedTitles(convertedTitles, titles);

            return idsToRemove.Count;
        }

        private void TrimConvertedTitles(IDictionary<string, FileInfo> convertedTitles,
            IDictionary<string, FileInfo> titles)
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
