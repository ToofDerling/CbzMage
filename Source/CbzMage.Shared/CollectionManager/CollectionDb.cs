using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace CbzMage.Shared.CollectionManager
{
    public sealed class CollectionDb<T> where T : CollectionItem, new()
    {
        public string DbFile { get; private set; }

        private readonly ConcurrentDictionary<string, T> _db;

        private bool _isDirty = false;

        internal CollectionDb(string dbFile)
        {
            DbFile = dbFile;
            _db = new();
        }

        public async Task ReadDbAsync()
        {
            var dbFileInfo = new FileInfo(DbFile);

            if (dbFileInfo.Exists)
            {
                using var mappedFile = MemoryMappedFile.CreateFromFile(dbFileInfo.FullName, FileMode.Open);
                using var stream = mappedFile.CreateViewStream();

                var linesData = new byte[dbFileInfo.Length].AsMemory();
                await stream.ReadAsync(linesData);

                var linesString = Encoding.UTF8.GetString(linesData.Span);
                if (string.IsNullOrWhiteSpace(linesString))
                {
                    return;
                }

                var lines = linesString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    return;
                }

                lines.AsParallel().ForAll(line =>
                {
                    var item = JsonSerializer.Deserialize<T>(line);

                    if (!_db.TryAdd(item.Id, item))
                    {
                        throw new InvalidOperationException($"[{item.Id}] already in archive");
                    }
                });
            }
        }

        public int Count => _db.Count;

        public bool TryGetName(string bookId, out string name)
        {
            name = null;

            if (_db.TryGetValue(bookId, out var state))
            {
                name = state.Name;
            }

            return name != null;
        }

        public void SetOrCreateName(string itemId, string name)
        {
            if (!_db.ContainsKey(itemId))
            {
                _db[itemId] = new T { Id = itemId, Name = name };
            }
            else
            {
                _db[itemId].Name = name;
            }

            _isDirty = true;
        }

        public T GetItem(string itemId) => _db[itemId];

        public void SetItem(string itemId, T item)
        {
            item.Id = itemId;  // Ensure id

            _db[itemId] = item;
            _isDirty = true;
        }

        public async Task SaveArchiveDbAsync()
        {
            if (!_isDirty)
            {
                return;
            }

            await File.WriteAllLinesAsync(DbFile, _db.Values.Select(x => JsonSerializer.Serialize(x)));
        }
    }
}
