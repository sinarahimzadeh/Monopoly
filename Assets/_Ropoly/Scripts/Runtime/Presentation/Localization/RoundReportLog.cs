using System.Collections.Generic;

namespace Ropoly.Presentation.Localization
{
    /// <summary>
    /// Stores reportable gameplay actions for the match log feed below the board status prompt.
    /// Keeps the full match history by default (<c>maxEntries &lt;= 0</c>) or caps at <c>maxEntries</c> if specified.
    /// </summary>
    public sealed class RoundReportLog
    {
        private readonly List<string> _entries = new List<string>();
        private readonly int _maxEntries;

        public RoundReportLog(int maxEntries = 0)
        {
            _maxEntries = maxEntries < 0 ? 0 : maxEntries;
        }

        public IReadOnlyList<string> Entries => _entries;

        public bool AddEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            string trimmed = message.Trim();
            if (_entries.Count > 0 && _entries[_entries.Count - 1] == trimmed)
            {
                return false;
            }

            _entries.Add(trimmed);
            if (_maxEntries > 0)
            {
                while (_entries.Count > _maxEntries)
                {
                    _entries.RemoveAt(0);
                }
            }

            return true;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public string FormatReport()
        {
            return _entries.Count == 0
                ? string.Empty
                : string.Join("\n", _entries);
        }
    }
}
