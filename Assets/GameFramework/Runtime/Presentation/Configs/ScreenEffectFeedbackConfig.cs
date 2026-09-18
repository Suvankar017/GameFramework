using System;
using UnityEngine;

namespace GameFramework.Presentation.Configs
{
    /// <summary>A full-screen color overlay (flash/fade/hit-overlay/vignette-adjacent) rendered
    /// through the existing <c>UI.IUIService</c> layer root - never a new <c>Canvas</c>. See
    /// <see cref="Screen.ScreenEffectController"/>. This channel is exclusive (see
    /// <see cref="PresentationService"/>'s remarks on composable vs. exclusive channels): only one
    /// screen effect plays at a time.</summary>
    [Serializable]
    public sealed class ScreenEffectFeedbackConfig
    {
        public bool Enabled;
        public Color Color = Color.white;

        [Min(0f)] public float FadeInSeconds = 0.05f;
        [Min(0f)] public float HoldSeconds;
        [Min(0f)] public float FadeOutSeconds = 0.2f;
    }
}
