using System;
using UnityEngine;

namespace GameFramework.Presentation.Configs
{
    /// <summary>Spawns <see cref="EffectPrefab"/> at the request's world position (or the origin, if
    /// none was supplied) and releases it after <see cref="Lifetime"/> seconds - via the existing
    /// <c>Gameplay.Pooling.IPoolService</c> when <see cref="UsePooling"/> is true and one is
    /// registered, otherwise a plain <c>Instantiate</c>/scheduled <c>Destroy</c>.</summary>
    [Serializable]
    public sealed class VisualEffectFeedbackConfig
    {
        public bool Enabled;
        public GameObject EffectPrefab;

        [Min(0.01f)] public float Lifetime = 2f;

        [Tooltip("Route through Gameplay.Pooling.IPoolService when one is registered, instead of a " +
            "plain Instantiate/Destroy.")]
        public bool UsePooling = true;
    }
}
