using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace AzwConverter
{
    public class ArchiveDb
    {
        public static string DbName => "archive.db";

        private readonly string _dbFile;

        private readonly ConcurrentDictionary<string, CbzState> _db;

        private bool _isDirty = false;

        public ArchiveDb()
        {
            _dbFile = Path.Combine(Settings.TitlesDir, DbName);
            _db = new();

            if (File.Exists(_dbFile))
            {
                var lines = File.ReadAllLines(_dbFile);

                Parallel.ForEach(lines, line =>
                {
                    var tokens = line.Split(' ', 2);
                    var bookId = tokens[0];

                    if (_db.ContainsKey(bookId))
                    {
                        throw new InvalidOperationException($"{bookId} already in archive");
                    }
                    _db[bookId] = JsonSerializer.Deserialize<CbzState>(tokens[1]);
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

        public void SaveDb()
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

            File.WriteAllText(_dbFile, sb.ToString());
        }
    }
}
