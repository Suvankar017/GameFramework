namespace GameFramework.Runtime
{
    /// <summary>
    /// This framework's own version identity. It is stamped into diagnostic context and into build
    /// metadata and release manifests. Three different versions exist; do not conflate them:
    /// <list type="bullet">
    /// <item><b>Framework version</b> (this constant): which GameFramework code a game was built
    /// against. It changes only when the framework itself changes (major = framework phase).</item>
    /// <item><b>Application version</b> (<c>PlayerSettings.bundleVersion</c> /
    /// <c>Application.version</c>): the game's own user-facing version, owned by the game.</item>
    /// <item><b>Platform build number</b> (Android <c>versionCode</c>, iOS <c>CFBundleVersion</c>): the
    /// store's monotonically increasing integer, owned by the game's release process.</item>
    /// </list>
    /// The framework is not distributed as a UPM package, so there is no separate package version.
    /// </summary>
    public static class FrameworkVersion
    {
        public const string Version = "20.0.0";
    }
}
