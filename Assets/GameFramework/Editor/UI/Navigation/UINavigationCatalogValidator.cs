using GameFramework.UI.Navigation;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.UI.Navigation
{
    /// <summary>
    /// Lightweight authoring-time checks for a selected <see cref="UINavigationCatalog"/> asset,
    /// following the same pattern as <c>CameraConfigurationValidator</c>/<c>FeedbackContentValidator</c>.
    /// <see cref="UINavigationCatalog.OnValidate"/> already reports duplicate/missing ids and
    /// prefabs as the asset is edited - this menu item re-runs the same checks on demand plus a
    /// cross-check <c>OnValidate</c> cannot do cheaply per-keystroke (missing modal
    /// <see cref="UIPopup"/> configuration warnings for popups authored as non-modal, which is valid
    /// but worth surfacing explicitly).
    /// </summary>
    internal static class UINavigationCatalogValidator
    {
        [MenuItem("GameFramework/UI/Validate Selected Navigation Catalog")]
        private static void ValidateSelected()
        {
            if (Selection.activeObject is UINavigationCatalog catalog)
            {
                Validate(catalog);
            }
            else
            {
                Debug.LogWarning("[UI.Navigation] Select a UINavigationCatalog asset in the Project window first.");
            }
        }

        private static void Validate(UINavigationCatalog catalog)
        {
            int issues = 0;
            var seenScreenIds = new System.Collections.Generic.HashSet<string>();

            foreach (UINavigationScreenEntry entry in catalog.Screens)
            {
                if (!entry.Id.IsValid)
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' has a screen entry with no Id assigned.", catalog);
                    issues++;
                    continue;
                }

                if (!seenScreenIds.Add(entry.Id.Value))
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' has a duplicate screen id '{entry.Id}'.", catalog);
                    issues++;
                }

                if (entry.Prefab == null)
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' screen '{entry.Id}' has no Prefab assigned.", catalog);
                    issues++;
                }
            }

            var seenPopupIds = new System.Collections.Generic.HashSet<string>();
            foreach (UINavigationPopupEntry entry in catalog.Popups)
            {
                if (!entry.Id.IsValid)
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' has a popup entry with no Id assigned.", catalog);
                    issues++;
                    continue;
                }

                if (!seenPopupIds.Add(entry.Id.Value))
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' has a duplicate popup id '{entry.Id}'.", catalog);
                    issues++;
                }

                if (entry.Prefab == null)
                {
                    Debug.LogWarning($"[UI.Navigation] '{catalog.name}' popup '{entry.Id}' has no Prefab assigned.", catalog);
                    issues++;
                }
            }

            Debug.Log(issues == 0
                ? $"[UI.Navigation] '{catalog.name}' validated with no issues."
                : $"[UI.Navigation] '{catalog.name}' validation found {issues} issue(s). See warnings above.");
        }
    }
}
