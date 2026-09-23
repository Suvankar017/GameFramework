using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>
    /// Game-facing Live Operations API - see CLAUDE.md's Phase 17 brief, section 50. Provides
    /// queries only; this framework never builds a Live Ops UI (the game's UI decides how to present
    /// an event).
    /// </summary>
    public interface ILiveOpsService : IGameService
    {
        IReadOnlyList<LiveEventId> GetEventIds();

        bool TryGetEvent(LiveEventId id, out LiveEvent liveEvent);

        LiveEventState GetState(LiveEventId id);

        bool IsActive(LiveEventId id);

        IReadOnlyList<LiveEventId> GetActiveEvents();

        /// <summary>Raised the moment a poll or configuration activation detects an event transitioning
        /// into <see cref="LiveEventState.Active"/>.</summary>
        event Action<LiveEventId> LiveEventStarted;

        /// <summary>Raised the moment a poll or configuration activation detects an event transitioning
        /// out of <see cref="LiveEventState.Active"/> (into <see cref="LiveEventState.Ended"/> or
        /// <see cref="LiveEventState.Disabled"/>).</summary>
        event Action<LiveEventId> LiveEventEnded;

        LiveOpsDiagnostics GetDiagnostics();
    }
}
