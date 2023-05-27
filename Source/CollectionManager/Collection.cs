namespace CollectionManager
{
    public class Collection<T> where T : CollectionItem, new()
    {
        public CollectionDb<T> Db { get; private set; }

        public ItemReader Reader { get; private set; }

        public ItemSyncer<T> Syncer { get; private set; }

        public Collection(string itemsDir, string? processedItemsDirName = null, string? dbName = null)
        {
            if (!Directory.Exists(itemsDir))
            {
                Directory.CreateDirectory(itemsDir);
            }

            // Db

            if (string.IsNullOrWhiteSpace(dbName))
            {
                dbName = "collection.db";
            }

            var dbPath = Path.Combine(itemsDir, dbName);
            Db = new CollectionDb<T>(dbPath);

            // Reader

            if (string.IsNullOrEmpty(processedItemsDirName))
            {
                processedItemsDirName = "Processed Items";
            }

            var processedItemsDir = Path.Combine(itemsDir, processedItemsDirName);
            Reader = new ItemReader(itemsDir, processedItemsDir, dbName);

            // Syncer

            Syncer = new ItemSyncer<T>(Db, processedItemsDir);
        }
    }
}
