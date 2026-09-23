namespace GameFramework.Runtime.Security
{
    /// <summary>
    /// The one place the framework asks "is this a development build?" - resolved purely from
    /// Unity's own compile-time defines (<c>UNITY_EDITOR</c>, <c>DEVELOPMENT_BUILD</c>), never from a
    /// runtime heuristic (bundle id, device model, a remote flag). A release player build is always
    /// non-development; the Editor and a "Development Build" player are always development.
    ///
    /// Used to keep development-only functionality (mock providers, simulation APIs) from becoming
    /// active in a production build by accident - see <see cref="DevelopmentProviderGuard"/>. Not an
    /// environment-management system: Editor/Development/Production is all the framework needs to
    /// distinguish, and <c>RemoteConfig.RemoteConfigEnvironment</c> already covers remote-config
    /// environments separately and explicitly.
    /// </summary>
    public static class BuildEnvironment
    {
        /// <summary>True in the Editor and in a player built with "Development Build" enabled.</summary>
        public static bool IsDevelopmentBuild =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        /// <summary>Phase 20: the deployment environment the framework build pipeline stamped into
        /// this player via a GAMEFRAMEWORK_ENV_* define, or Unspecified (Editor, or a build made
        /// outside the pipeline). Compile-time only, so it cannot be changed on a device.</summary>
        public static DeploymentEnvironment Deployment =>
#if GAMEFRAMEWORK_ENV_PRODUCTION
            DeploymentEnvironment.Production;
#elif GAMEFRAMEWORK_ENV_STAGING
            DeploymentEnvironment.Staging;
#elif GAMEFRAMEWORK_ENV_DEVELOPMENT
            DeploymentEnvironment.Development;
#else
            DeploymentEnvironment.Unspecified;
#endif

        /// <summary>True only inside the Unity Editor (Edit or Play Mode).</summary>
        public static bool IsEditor =>
#if UNITY_EDITOR
            true;
#else
            false;
#endif
    }
}
