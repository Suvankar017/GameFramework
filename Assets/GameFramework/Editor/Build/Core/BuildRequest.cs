namespace GameFramework.Editor.Build
{
    /// <summary>
    /// What a caller (the build window or the command line) asks for: a profile plus optional
    /// explicit overrides. Every override is optional; null means "use the profile / Player Settings".
    /// </summary>
    public sealed class BuildRequest
    {
        public BuildRequest(FrameworkBuildProfile profile)
        {
            Profile = profile;
        }

        public FrameworkBuildProfile Profile { get; }

        /// <summary>Overrides the profile's version and <c>PlayerSettings.bundleVersion</c>.</summary>
        public string VersionOverride { get; set; }

        /// <summary>Overrides every build-number scheme.</summary>
        public int? BuildNumberOverride { get; set; }

        /// <summary>Overrides the profile's output root (the Platform/Environment subfolders are
        /// still appended, so an override can never collapse two environments into one folder).</summary>
        public string OutputRootOverride { get; set; }

        /// <summary>Allow replacing an artifact that already exists at the exact same deterministic
        /// path (same version and build number). Off by default: a build never destroys a previous
        /// artifact implicitly.</summary>
        public bool AllowOverwrite { get; set; }

        /// <summary>Run validation only; never call Unity's build.</summary>
        public bool ValidateOnly { get; set; }
    }
}
