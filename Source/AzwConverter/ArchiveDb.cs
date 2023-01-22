﻿using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace AzwConverter
{
    public sealed class ArchiveDb
    {
        public static string DbName => "archive.db";

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
                if (string.IsNullOrWhiteSpace(linesString))
                {
                    return;
                }

                var lines = linesString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    return;
                }

                // TODO: remove this when 1.0 is released
                var splitMode = false;
                try
                {
                    JsonSerializer.Deserialize<CbzState>(lines[0]);
                }
                catch
                {
                    splitMode = true;
                }

                lines.AsParallel().ForAll(line =>
                {
                    CbzState cbzState;

                    // TODO: remove this when 1.0 is released
                    if (splitMode)
                    {
                        // The beginning of the CbzState json
                        const string split = "{\"Name\":";

                        var tokens = line.Split(split, 2);

                        var bookId = tokens[0].TrimEnd();
                        var json = $"{split}{tokens[1]}"; // Finish CbzState json

                        cbzState = JsonSerializer.Deserialize<CbzState>(json);
                        cbzState.Id = bookId;
                    }
                    else
                    {
                        cbzState = JsonSerializer.Deserialize<CbzState>(line);
                    }

                    if (!_db.TryAdd(cbzState.Id, cbzState))
                    {
                        throw new InvalidOperationException($"[{cbzState.Id}] already in archive");
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
                _db[bookId] = new CbzState { Id = bookId, Name = name };
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
            state.Id = bookId;  // Ensure id

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

            await File.WriteAllLinesAsync(_dbFile, _db.Values.Select(x => JsonSerializer.Serialize(x)));
        }
    }
}
