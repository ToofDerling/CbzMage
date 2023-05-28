using System.Collections.Concurrent;

namespace CbzMage.Shared.CollectionManager
{
    public class ItemSyncer<T> where T : CollectionItem, new()
    {
        private readonly string _processedItemsDir;

        private readonly CollectionDb<T> _collectionDb;

        internal ItemSyncer(CollectionDb<T> collectionDb, string processedItemsDir)
        {
            _collectionDb = collectionDb;

            _processedItemsDir = processedItemsDir;
        }

        public int SyncAndArchiveItems(IDictionary<string, FileInfo> items, IDictionary<string, FileInfo> processedItems, IDictionary<string, FileInfo[]> books)
        {
            var idsToRemove = new ConcurrentBag<string>();

            items.AsParallel().ForAll(item =>
            {
                var itemId = item.Key;
                var itemFile = item.Value;

                _collectionDb.SetOrCreateName(itemId, itemFile.Name);

                // Delete title if no longer in books.
                if (!books.ContainsKey(itemId))
                {
                    idsToRemove.Add(itemId);

                    itemFile.Delete();

                    // Also delete the converted title 
                    if (processedItems.TryGetValue(itemId, out var convertedTitle))
                    {
                        convertedTitle.Delete();
                    }
                }
                else
                {
                    // Sync title -> converted title
                    if (processedItems.TryGetValue(itemId, out var convertedTitleFile) && convertedTitleFile.Name != itemFile.Name)
                    {
                        var newConvertedTitleFile = Path.Combine(convertedTitleFile.DirectoryName!, itemFile.Name);
                        convertedTitleFile.MoveTo(newConvertedTitleFile);
                    }
                }
            });

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                items.Remove(bookId);
                processedItems.Remove(bookId); // This is safe even if title is not converted
            }

            return idsToRemove.Count;
        }

        public string SyncProcessedItem(string itemFile, FileInfo? convertedItemFile)
        {
            convertedItemFile?.Delete();

            var name = Path.GetFileName(itemFile);
            var dest = Path.Combine(_processedItemsDir, name);

            File.Copy(itemFile, dest);
            File.SetLastWriteTime(dest, DateTime.Now);

            return name;
        }
    }
}
