namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// The practical set of value types this framework supports - see CLAUDE.md's Phase 17 brief,
    /// section 11. Deliberately does not support arbitrary/generic object serialization; a game
    /// needing structured data reads several primitive keys instead (section 11: "do not build a
    /// generic arbitrary-object serialization system").
    /// </summary>
    public enum RemoteConfigValueType
    {
        Bool,
        Int,
        Long,
        Float,
        Double,
        String
    }
}
