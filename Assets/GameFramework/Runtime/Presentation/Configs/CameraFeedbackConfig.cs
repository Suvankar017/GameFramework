using System;
using UnityEngine;

namespace GameFramework.Presentation.Configs
{
    /// <summary>Authored camera-shake parameters - executed by whichever
    /// <see cref="Camera.ICameraFeedbackDriver"/> is currently registered with
    /// <see cref="IPresentationService.RegisterCameraDriver"/>; a no-op (logged once) if none is.</summary>
    [Serializable]
    public sealed class CameraFeedbackConfig
    {
        public bool Enabled;

        [Min(0f)] public float Amplitude = 0.3f;
        [Min(0.01f)] public float Frequency = 20f;
        [Min(0f)] public float Duration = 0.3f;

        [Tooltip("Evaluated over [0, 1] normalized elapsed time to scale Amplitude down over the " +
            "shake's Duration. Left empty, a linear 1 -> 0 falloff is used.")]
        public AnimationCurve Falloff;

        [Tooltip("Zero out Z so a 2D game's shake never fights its fixed camera depth.")]
        public bool ConstrainToXY;
    }
}
