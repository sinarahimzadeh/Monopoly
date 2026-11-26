using System.Collections.Generic;

namespace Ropoly.Presentation.Localization
{
    /// <summary>
    /// Stores a concise rolling window of reportable gameplay actions for display below the board status prompt.
    /// </summary>
    public sealed class RoundReportLog
    {
        private readonly List<string> _entries = new List<string>();
        private readonly int _maxEntries;

        public RoundReportLog(int maxEntries = 3)
        {
            _maxEntries = maxEntries < 1 ? 1 : maxEntries;
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
            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
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
