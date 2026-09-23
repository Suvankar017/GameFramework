using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Game-facing local notification API - see CLAUDE.md's Phase 18 brief, section 6/44. A game
    /// should be able to write <c>notifications.Schedule(request)</c> without knowing anything about
    /// the provider underneath.
    ///
    /// Deliberately synchronous, not <c>Task</c>/<c>async</c>-based - scheduling a local notification
    /// is a fast, local operation, the same reasoning
    /// <see cref="Runtime.Persistence.IPersistenceService"/> already documents for its own
    /// synchronous Save/Load ("wrapping a synchronous implementation in a Task would only pretend to
    /// be asynchronous").
    /// </summary>
    public interface INotificationService : IGameService
    {
        bool IsSupported { get; }

        NotificationPermissionStatus PermissionStatus { get; }

        string LastError { get; }

        event Action<NotificationPermissionStatus> PermissionChanged;

        /// <summary>Raised when a scheduled notification is opened (tapped) by the user - including
        /// once during <see cref="IGameService.Initialize"/> if the provider reports the application
        /// was cold-started by tapping one (see <see cref="Providers.INotificationProvider.TryGetLaunchNotification"/>).</summary>
        event Action<NotificationOpenedInfo> NotificationOpened;

        /// <summary>Raised when a notification is delivered while the app is already running.</summary>
        event Action<NotificationOpenedInfo> NotificationReceived;

        bool CanRequestPermission();

        /// <summary>Never called automatically by this framework (section 13) - only in direct
        /// response to a game/UI-initiated call. Invokes <paramref name="onResult"/> exactly once,
        /// synchronously with the current status if already determined, otherwise after the OS
        /// prompt (if any) closes.</summary>
        void RequestPermission(Action<NotificationPermissionStatus> onResult = null);

        /// <summary>Registers a channel/category ahead of scheduling anything that references it
        /// (section 6/14). Providers/platforms without a channel concept ignore this.</summary>
        void RegisterChannel(NotificationChannelDefinition channel);

        /// <summary>
        /// Schedules <paramref name="request"/>. Scheduling an id that is already scheduled replaces
        /// it (see <see cref="NotificationId"/>'s remarks). Never throws for an expected condition -
        /// see <see cref="NotificationResultKind"/>. <paramref name="request"/> itself must not be
        /// null (a programmer error, not an expected condition).
        /// </summary>
        NotificationResult Schedule(NotificationRequest request);

        NotificationResult Cancel(NotificationId id);

        /// <summary>Cancels every notification this service has scheduled. Does not raise a
        /// per-id <see cref="NotificationCancelledEvent"/> for each one - a bulk operation, not N
        /// individual cancellations.</summary>
        void CancelAll();

        IReadOnlyList<NotificationRequest> GetScheduled();

        bool IsScheduled(NotificationId id);

        /// <summary>
        /// Editor/QA simulation only - see CLAUDE.md's Phase 18 brief, section 16. The resulting
        /// <see cref="NotificationOpenedInfo.WasSimulated"/> is always true; this never claims a real
        /// platform delivery, regardless of which provider is registered (including
        /// <c>NoOpNotificationProvider</c>, against which this still safely raises the event with an
        /// empty payload unless <paramref name="payloadOverride"/> is supplied). Phase 19: ignored (and
        /// logged as an error) in a non-development build - see <c>Runtime.Security.BuildEnvironment</c>.
        /// A simulated payload goes through the same <see cref="NotificationPayloadValidator"/> check as
        /// a real one.
        /// </summary>
        void SimulateNotificationOpened(NotificationId id, NotificationPayload payloadOverride = null);

        void SimulateNotificationReceived(NotificationId id, NotificationPayload payloadOverride = null);

        NotificationDiagnostics GetDiagnostics();
    }
}
