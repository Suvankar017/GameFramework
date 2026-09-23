namespace GameFramework.Runtime.Security
{
    /// <summary>
    /// Which deployment a player build was made for. This is set by the Phase 20 build pipeline
    /// (<c>Editor.Build.FrameworkBuildPipeline</c>), which injects exactly one
    /// <c>GAMEFRAMEWORK_ENV_*</c> scripting define for the build only (through
    /// <c>BuildPlayerOptions.extraScriptingDefines</c>, so Player Settings are never rewritten).
    /// Read it through <see cref="BuildEnvironment.Deployment"/>.
    ///
    /// Deliberately separate from <see cref="BuildEnvironment.IsDevelopmentBuild"/>. A Staging build
    /// may or may not be a Unity "Development Build"; a Production build never is (the pipeline
    /// enforces that).
    /// </summary>
    public enum DeploymentEnvironment
    {
        /// <summary>No environment define was present: the Editor, or a build made outside the
        /// framework pipeline (e.g. Unity's own Build Settings window).</summary>
        Unspecified,
        Development,
        Staging,
        Production
    }
}
