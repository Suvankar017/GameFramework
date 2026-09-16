namespace GameFramework.Audio
{
    /// <summary>A cue's playback bus. "Master" is not a member here — it's a separate multiplier
    /// applied on top of every category, not something an individual cue belongs to.</summary>
    public enum AudioCategory
    {
        Music,
        Sfx,
        Ui,
        Voice
    }
}
