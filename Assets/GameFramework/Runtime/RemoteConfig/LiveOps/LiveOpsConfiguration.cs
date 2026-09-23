using System;
using UnityEngine;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Reusable, game-authored Live Ops schedule - see CLAUDE.md's Phase 17 brief, sections
    /// 37/43-44. Deliberately a flat list of simple UTC start/end windows, not a calendar/recurrence
    /// engine (section 44).</summary>
    [CreateAssetMenu(menuName = "GameFramework/RemoteConfig/Live Ops Configuration", fileName = "LiveOpsConfiguration")]
    public sealed class LiveOpsConfiguration : ScriptableObject
    {
        [SerializeField] private LiveEventDefinition[] _events = Array.Empty<LiveEventDefinition>();

        [Tooltip("How often (seconds) LiveOpsService re-evaluates every event's schedule to detect Upcoming/Active/Ended transitions between configuration activations. Kept low-frequency deliberately - see CLAUDE.md's Tick Rules.")]
        [SerializeField] private float _pollIntervalSeconds = 15f;

        public LiveEventDefinition[] Events => _events;
        public float PollIntervalSeconds => _pollIntervalSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_pollIntervalSeconds < 1f)
            {
                _pollIntervalSeconds = 1f;
            }
        }
#endif
    }
}
