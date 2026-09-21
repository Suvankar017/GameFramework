using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>Id -> prefab lookup for registered screens. Internal - games interact with this
    /// only through <see cref="INavigationService"/>'s Register/Unregister/IsRegistered methods.</summary>
    internal sealed class UIScreenRegistry
    {
        private readonly Dictionary<UIScreenId, UIScreen> _prefabs = new Dictionary<UIScreenId, UIScreen>();

        public int Count => _prefabs.Count;
        public IEnumerable<UIScreenId> Ids => _prefabs.Keys;

        public void Register(UIScreenId id, UIScreen prefab)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Screen id must be valid.", nameof(id));
            }

            Guard.NotNull(prefab, nameof(prefab));

            if (_prefabs.ContainsKey(id))
            {
                throw new InvalidOperationException($"Screen id '{id}' is already registered.");
            }

            _prefabs.Add(id, prefab);
        }

        public void Unregister(UIScreenId id) => _prefabs.Remove(id);
        public bool IsRegistered(UIScreenId id) => _prefabs.ContainsKey(id);
        public bool TryGet(UIScreenId id, out UIScreen prefab) => _prefabs.TryGetValue(id, out prefab);
        public void Clear() => _prefabs.Clear();
    }
}
