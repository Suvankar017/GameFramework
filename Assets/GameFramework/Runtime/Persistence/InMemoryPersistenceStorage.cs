using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// Volatile, process-memory-only storage. Exists specifically so persistence/settings tests
    /// run against isolated, deterministic storage instead of the developer's real save files —
    /// not intended for production use (nothing here survives an app restart).
    /// </summary>
    public sealed class InMemoryPersistenceStorage : IPersistenceStorage
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public bool Exists(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            return _values.ContainsKey(key);
        }

        public string ReadText(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            return _values.TryGetValue(key, out string value) ? value : null;
        }

        public void WriteText(string key, string contents)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            _values[key] = contents;
        }

        public void Delete(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            _values.Remove(key);
        }
    }
}
