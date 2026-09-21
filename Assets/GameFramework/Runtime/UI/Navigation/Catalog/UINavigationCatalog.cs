using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Optional designer-friendly bulk registration - one asset listing every screen/popup a game
    /// wants <see cref="INavigationService"/> to know about, applied once via
    /// <see cref="ApplyTo"/> at composition-root time (CLAUDE.md's Phase 12 brief, section 39:
    /// "ScriptableObject metadata"). Purely a convenience over calling
    /// <see cref="INavigationService.RegisterScreen"/>/<see cref="INavigationService.RegisterPopup"/>
    /// directly in code - either approach is fully supported, and both can be combined.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/UI/Navigation Catalog", fileName = "UINavigationCatalog")]
    public sealed class UINavigationCatalog : ScriptableObject
    {
        [SerializeField] private List<UINavigationScreenEntry> _screens = new List<UINavigationScreenEntry>();
        [SerializeField] private List<UINavigationPopupEntry> _popups = new List<UINavigationPopupEntry>();

        public IReadOnlyList<UINavigationScreenEntry> Screens => _screens;
        public IReadOnlyList<UINavigationPopupEntry> Popups => _popups;

        public void ApplyTo(INavigationService navigation)
        {
            foreach (UINavigationScreenEntry entry in _screens)
            {
                if (entry.Id.IsValid && entry.Prefab != null)
                {
                    navigation.RegisterScreen(entry.Id, entry.Prefab);
                }
            }

            foreach (UINavigationPopupEntry entry in _popups)
            {
                if (entry.Id.IsValid && entry.Prefab != null)
                {
                    navigation.RegisterPopup(entry.Id, entry.Prefab);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seenScreenIds = new HashSet<string>();
            foreach (UINavigationScreenEntry entry in _screens)
            {
                if (!entry.Id.IsValid)
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' has a screen entry with no Id assigned.", this);
                    continue;
                }

                if (!seenScreenIds.Add(entry.Id.Value))
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' has a duplicate screen id '{entry.Id}'.", this);
                }

                if (entry.Prefab == null)
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' screen '{entry.Id}' has no Prefab assigned.", this);
                }
            }

            var seenPopupIds = new HashSet<string>();
            foreach (UINavigationPopupEntry entry in _popups)
            {
                if (!entry.Id.IsValid)
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' has a popup entry with no Id assigned.", this);
                    continue;
                }

                if (!seenPopupIds.Add(entry.Id.Value))
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' has a duplicate popup id '{entry.Id}'.", this);
                }

                if (entry.Prefab == null)
                {
                    Debug.LogWarning($"[UINavigationCatalog] '{name}' popup '{entry.Id}' has no Prefab assigned.", this);
                }
            }
        }
#endif
    }
}
