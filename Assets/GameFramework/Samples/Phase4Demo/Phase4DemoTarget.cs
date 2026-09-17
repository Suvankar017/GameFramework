using System;
using GameFramework.Gameplay.Entities;
using GameFramework.Gameplay.Interaction;
using GameFramework.Gameplay.Lifecycle;
using GameFramework.Runtime.Diagnostics;
using UnityEngine;

namespace GameFramework.Samples.Phase4Demo
{
    /// <summary>
    /// One pooled, interactable demo target. Implements <see cref="IGameplayObjectLifecycle"/>
    /// directly (no internal <see cref="GameplayObjectLifecycleRunner"/>) because
    /// <see cref="GameFramework.Gameplay.Pooling.GameObjectPool"/> already drives
    /// Initialize/Activate/Deactivate/Dispose itself for pooled instances - adding a second,
    /// self-driven runner on top would invoke this component's own phases twice per cycle. The
    /// Runner is demonstrated separately, on a standalone (non-pooled) object - see
    /// <see cref="Phase4DemoLifecycleProbe"/>.
    /// </summary>
    [RequireComponent(typeof(EntityIdentity))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class Phase4DemoTarget : MonoBehaviour, IGameplayObjectLifecycle, IInteractable
    {
        private const string LogCategory = "Phase4Demo";

        private static readonly Color AvailableColor = Color.cyan;
        private static readonly Color CollectedColor = Color.green;

        private EntityIdentity _identity;
        private MeshRenderer _renderer;
        private bool _collected;

        /// <summary>Raised when a player interacts with this target while it is still available.</summary>
        public event Action<Phase4DemoTarget> Collected;

        public void Initialize()
        {
            _identity = GetComponent<EntityIdentity>();
            _renderer = GetComponent<MeshRenderer>();
            Log.Debug(LogCategory, $"Target {_identity.Id} initialized (once, ever).");
        }

        public void Activate()
        {
            _collected = false;
            _renderer.material.color = AvailableColor;
        }

        public void Deactivate()
        {
            Collected = null; // A fresh subscriber list per pool cycle - nothing should reach across reuse.
        }

        public void Dispose()
        {
            Log.Debug(LogCategory, $"Target {_identity.Id} disposed (evicted from the pool, once, ever).");
        }

        public bool CanInteract(InteractionContext context) => !_collected;

        public void Interact(InteractionContext context)
        {
            if (_collected)
            {
                return;
            }

            _collected = true;
            _renderer.material.color = CollectedColor;
            Collected?.Invoke(this);
        }
    }
}
