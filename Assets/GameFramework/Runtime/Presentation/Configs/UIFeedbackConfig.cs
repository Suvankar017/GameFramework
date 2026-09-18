using System;

namespace GameFramework.Presentation.Configs
{
    /// <summary>Publishes <see cref="UIFeedbackRequestedEvent"/> instead of touching
    /// <c>UI.UIScreen</c>/<c>UI.UIPopup</c> directly - a game's own UI layer decides what, if
    /// anything, to show (a toast, a popup, an icon pulse) in response. <see cref="Tag"/> is a
    /// free-form, game-defined hint (e.g. "RewardReceived", "AchievementUnlocked") the framework
    /// never interprets itself.</summary>
    [Serializable]
    public sealed class UIFeedbackConfig
    {
        public bool Enabled;
        public string Tag;
    }
}
