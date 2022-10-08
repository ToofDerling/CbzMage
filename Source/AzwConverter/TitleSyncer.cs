using AzwConverter.Metadata;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public int SyncBooksToTitles(Dictionary<string, string[]> books, Dictionary<string, string> titles, ArchiveDb archive)
        {
            var syncedBookCount = 0;

            foreach (var book in books)
            {
                var bookId = book.Key;

                // Book is not in current titles
                if (!titles.ContainsKey(bookId))
                {
                    syncedBookCount++;

                    // Try the archive
                    var name = archive.GetName(bookId);
                    if (name != null)
                    {
                        Sync(name);
                    }
                    else
                    {
                        // Or scan the book file
                        var bookFiles = book.Value;
                        var azwFile = bookFiles.First(b => b.EndsWith(Settings.AzwExt));

                        using var stream = File.Open(azwFile, FileMode.Open);
                        var metadata = new MobiMetadata(stream);

                        var title = metadata.MobiHeader.FullName.ToFileSystemString();
                        var publisher = metadata.MobiHeader.EXTHHeader.Publisher.ToFileSystemString();

                        // Normalize publisher names
                        foreach (var trimmedName in Settings.TrimPublishers)
                        {
                            if (publisher.StartsWith(trimmedName, StringComparison.OrdinalIgnoreCase))
                            {
                                publisher = trimmedName;
                                break;
                            }
                        }

                        Sync($"[{publisher}] {title}");
                    }

                    void Sync(string titleFile)
                    {
                        var file = Path.Combine(Settings.TitlesDir, titleFile);
                        File.WriteAllText(file, bookId);

                        // Add archived/scanned title to list of current titles
                        titles[bookId] = file;
                    }
                }
            }

            return syncedBookCount;
        }

        public int SyncTitlesToArchive(Dictionary<string, string> titles, ArchiveDb archive, Dictionary<string, string[]> books)
        {
            var idsToRemove = new List<string>();
            var archivedTitleCount = 0;

            foreach (var title in titles)
            {
                var bookId = title.Key;
                var titleFile = title.Value;

                var emptyState = new CbzState { Name = Path.GetFileName(titleFile) };
                archive.SetState(bookId, emptyState);

                // Delete title if no longer in books.
                if (!books.ContainsKey(bookId))
                {
                    archivedTitleCount++;

                    idsToRemove.Add(bookId);
                    File.Delete(titleFile);
                }
            }

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                titles.Remove(bookId);
            }

            return archivedTitleCount;
        }

        public string SyncTitleTo(string bookId, string titleFile, Dictionary<string, string> toFiles, string toDir)
        {
            var foundExisting = toFiles.TryGetValue(bookId, out var existingTitleFile);

            // Prevent duplicate bookId 
            if (foundExisting)
            {
                File.Delete(existingTitleFile);
            }

            var name = Path.GetFileName(titleFile);
            name = name.RemoveAllMarkers();

            name = name.Trim();
            var dest = Path.Combine(toDir, name);
            File.Copy(titleFile, dest);

            // Update with the synced title 
            toFiles[bookId] = dest;
            return name;
        }
    }
}
