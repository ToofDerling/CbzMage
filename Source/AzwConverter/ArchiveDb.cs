using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace AzwConverter
{
    public sealed class ArchiveDb
    {
        public static string DbName => "archive.db";

        // The beginning of the CbzState json
        private const string _split = "{\"Name\":\"";

        private readonly string _dbFile;

        private readonly ConcurrentDictionary<string, CbzState> _db;

        private bool _isDirty = false;

        public ArchiveDb()
        {
            _dbFile = Path.Combine(Settings.TitlesDir, DbName);
            _db = new();
        }

        public async Task ReadArchiveDbAsync()
        {
            var dbFileInfo = new FileInfo(_dbFile);

            if (dbFileInfo.Exists)
            {
                using var mappedFile = MemoryMappedFile.CreateFromFile(dbFileInfo.FullName, FileMode.Open);
                using var stream = mappedFile.CreateViewStream();

                var linesData = new byte[dbFileInfo.Length].AsMemory();
                await stream.ReadAsync(linesData);

                var linesString = Encoding.UTF8.GetString(linesData.Span);
                var lines = linesString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                lines.AsParallel().ForAll(line =>
                {
                    var tokens = line.Split(_split, 2);
                    
                    var bookId = tokens[0].TrimEnd();
                    var json = $"{_split}{tokens[1]}"; // Finish CbzState json

                    if (!_db.TryAdd(bookId, JsonSerializer.Deserialize<CbzState>(json)))
                    {
                        throw new InvalidOperationException($"{bookId} already in archive");
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

        public void SetOrCreateName(string bookId, string name)
        {
            if (!_db.ContainsKey(bookId))
            {
                _db[bookId] = new CbzState { Name = name };
            }
            else
            {
                _db[bookId].Name = name;
            }

            _isDirty = true;
        }

        public CbzState GetState(string bookId)
        {
            return _db[bookId];
        }

        public void SetState(string bookId, CbzState state)
        {
            _db[bookId] = state;
            _isDirty = true;
        }

        public DateTime? GetCheckedDate(string bookId)
        {
            return _db[bookId].Checked;
        }

        public void UpdateCheckedDate(string bookId)
        {
            _db[bookId].Checked = DateTime.Now;
            _isDirty = true;
        }

        public void RemoveChangedState(string bookId)
        {
            _db[bookId].Changed = null;
            _isDirty = true;
        }

        public async Task SaveArchiveDbAsync()
        {
            if (!_isDirty)
            {
                return;
            }

            var sb = new StringBuilder(32000);

            foreach (var x in _db)
            {
                sb.Append(x.Key).Append(' ').AppendLine(JsonSerializer.Serialize(x.Value));
            }

            await File.WriteAllTextAsync(_dbFile, sb.ToString(), CancellationToken.None);
        }
    }
}
