namespace GameFramework.RemoteConfig
{
    /// <summary>See CLAUDE.md's Phase 17 brief, section 58. Provider selection/wiring for a given
    /// environment is a game/bootstrapper decision - this framework never infers environment from
    /// <c>Application.isEditor</c> alone (section 59).</summary>
    public enum RemoteConfigEnvironment
    {
        Development,
        Staging,
        Production
    }
}
