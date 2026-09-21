using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>Id -> prefab lookup for registered popups. Internal - see <see cref="UIScreenRegistry"/>'s remarks.</summary>
    internal sealed class UIPopupRegistry
    {
        private readonly Dictionary<UIPopupId, UIPopup> _prefabs = new Dictionary<UIPopupId, UIPopup>();

        public int Count => _prefabs.Count;
        public IEnumerable<UIPopupId> Ids => _prefabs.Keys;

        public void Register(UIPopupId id, UIPopup prefab)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Popup id must be valid.", nameof(id));
            }

            Guard.NotNull(prefab, nameof(prefab));

            if (_prefabs.ContainsKey(id))
            {
                throw new InvalidOperationException($"Popup id '{id}' is already registered.");
            }

            _prefabs.Add(id, prefab);
        }

        public void Unregister(UIPopupId id) => _prefabs.Remove(id);
        public bool IsRegistered(UIPopupId id) => _prefabs.ContainsKey(id);
        public bool TryGet(UIPopupId id, out UIPopup prefab) => _prefabs.TryGetValue(id, out prefab);
        public void Clear() => _prefabs.Clear();
    }
}
