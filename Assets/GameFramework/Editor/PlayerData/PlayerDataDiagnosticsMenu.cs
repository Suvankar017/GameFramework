using GameFramework.PlayerData;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.PlayerData
{
    /// <summary>
    /// Development-time inspection for Phase 13. Unlike the other phases' content validators
    /// (<c>TutorialContentValidator</c>, <c>QuestContentValidator</c>, ...), Player Data has no
    /// ScriptableObject assets to scan - sections are plain C# classes registered at runtime via
    /// <see cref="IPlayerProfileService.RegisterSection{TSection}"/> - so there is nothing
    /// meaningful to validate before Play Mode. This logs <see cref="PlayerProfileDiagnostics"/>
    /// for whichever <see cref="GameBootstrapper"/> instance is currently running, which is the
    /// practical equivalent for this phase - see CLAUDE.md's Phase 13 brief, section 55 ("do not
    /// create an unnecessarily large editor suite").
    /// </summary>
    internal static class PlayerDataDiagnosticsMenu
    {
        [MenuItem("GameFramework/Player Data/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[PlayerData] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;
            if (!registry.TryGet(out IPlayerProfileService playerData))
            {
                Debug.LogWarning("[PlayerData] No IPlayerProfileService is registered on the running GameBootstrapper.");
                return;
            }

            PlayerProfileDiagnostics diagnostics = playerData.GetDiagnostics();

            Debug.Log(
                "[PlayerData] State=" + diagnostics.State +
                ", ActiveProfile=" + (diagnostics.HasActiveProfile ? diagnostics.ActiveProfileId.ToString() : "<none>") +
                ", IsDirty=" + diagnostics.IsDirty +
                ", AutosavePending=" + diagnostics.AutosavePending +
                ", LastLoadResult=" + diagnostics.LastLoadResult.Kind +
                ", LastSaveResult=" + diagnostics.LastSaveResult.Kind +
                ", RegisteredSections=[" + string.Join(", ", diagnostics.RegisteredSectionIds) + "]");
        }
    }
}
