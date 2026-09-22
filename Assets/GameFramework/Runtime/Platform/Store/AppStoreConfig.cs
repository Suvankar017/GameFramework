using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Store identifiers are project-specific configuration, never hard-coded in framework code
    /// (see CLAUDE.md's Phase 14 brief, section 21). Optional on <see cref="PlatformBootstrapper"/> -
    /// without one, <see cref="AppStoreService"/> safely reports no configuration rather than
    /// throwing.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Platform/App Store Config", fileName = "AppStoreConfig")]
    public sealed class AppStoreConfig : ScriptableObject
    {
        [Tooltip("Android package name, e.g. com.company.game.")]
        public string AndroidPackageName;

        [Tooltip("Numeric iOS App Store id, e.g. 123456789.")]
        public string IOSAppStoreId;
    }
}
