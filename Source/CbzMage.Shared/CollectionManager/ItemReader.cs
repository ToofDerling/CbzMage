using System.Collections.Concurrent;

namespace CbzMage.Shared.CollectionManager
{
    public class ItemReader
    {
        private readonly string _itemsDir;

        private readonly string _processedItemsDir;

        private readonly string _dbName;

        internal ItemReader(string itemsDir, string processedItemsDir, string dbName)
        {
            _itemsDir = itemsDir;
            _processedItemsDir = processedItemsDir;
            _dbName = dbName;
        }

        public async Task<IDictionary<string, FileInfo>> ReadItemsAsync()
        {
            return await ReadFilesAsync(_itemsDir, true);
        }

        public async Task<IDictionary<string, FileInfo>> ReadProcessedItemsAsync()
        {
            return await ReadFilesAsync(_processedItemsDir, false);
        }

        private async Task<IDictionary<string, FileInfo>> ReadFilesAsync(string directory, bool checkDbName)
        {
            var dict = new ConcurrentDictionary<string, FileInfo>();

            var directoryInfo = new DirectoryInfo(directory);

            await Parallel.ForEachAsync(directoryInfo.EnumerateFiles(), async (file, ct) =>
            {
                if (checkDbName && file.Name == _dbName)
                {
                    return;
                }

                var bookId = await File.ReadAllTextAsync(file.FullName, ct);
                dict[bookId] = file;
            });

            return new Dictionary<string, FileInfo>(dict);
        }
    }
}
