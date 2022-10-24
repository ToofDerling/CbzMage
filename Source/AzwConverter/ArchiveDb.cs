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

        public bool IsStateUpdated(string bookId, CbzState state)
        {
            var isInDb = _db.TryGetValue(bookId, out var oldState);
            isInDb = isInDb && !oldState.IsEmpty();

            var updated = isInDb
                && ((state.HdCover && !oldState.HdCover) || (state.HdImages > oldState.HdImages));

            var nameChanged = state.Name != oldState.Name;

            if (!isInDb || updated || nameChanged)
            {
                SetState(bookId, state);
            }
            return updated;
        }

        public int Count => _db.Count;

        public string GetName(string bookId)
        {
            return _db.TryGetValue(bookId, out var state) ? state.Name : null;
        }

        public void SetState(string bookId, CbzState state)
        {
            if (state.IsEmpty() && _db.TryGetValue(bookId, out var oldState))
            {
                oldState.Name = state.Name;
                state = oldState;
            }

            state.Name = state.Name.RemoveAnyMarker();

            _db[bookId] = state;
            _isDirty = true;
        }

        public DateTime? GetCheckedDate(string bookId)
        {
            return _db.TryGetValue(bookId, out var state) ? state.Checked : null;
        }

        public void UpdateCheckedDate(string bookId)
        {
            if (_db.TryGetValue(bookId, out var state))
            { 
                state.Checked = DateTime.Now;
            }
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
