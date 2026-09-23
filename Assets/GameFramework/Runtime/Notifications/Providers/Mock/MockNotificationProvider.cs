using System;
using System.Collections.Generic;

namespace GameFramework.Notifications.Providers.Mock
{
    /// <summary>
    /// Deterministic <see cref="INotificationProvider"/> for the Editor and local development - see
    /// CLAUDE.md's Phase 18 brief, sections 16/39. This is the framework's only shipped provider in
    /// this phase - no Unity Mobile Notifications/Firebase/OneSignal adapter exists because none of
    /// those packages/SDKs is installed in this project (see <see cref="INotificationProvider"/>'s
    /// remarks). Everything stays in memory; nothing is ever actually delivered by the OS - every
    /// simulated event reported through this provider is exactly that, simulated (section 16).
    /// </summary>
    public sealed class MockNotificationProvider : INotificationProvider
    {
        private readonly MockNotificationSimulationMode _mode;
        private readonly Dictionary<string, NotificationRequest> _scheduled = new Dictionary<string, NotificationRequest>(StringComparer.Ordinal);
        private readonly Dictionary<string, NotificationChannelDefinition> _channels = new Dictionary<string, NotificationChannelDefinition>(StringComparer.Ordinal);
        private NotificationOpenedInfo? _pendingLaunchNotification;

        public bool IsSupported => _mode != MockNotificationSimulationMode.AlwaysUnsupported;

        public NotificationPermissionStatus PermissionStatus { get; private set; } = NotificationPermissionStatus.NotDetermined;

        public event Action<NotificationOpenedInfo> NotificationOpened;
        public event Action<NotificationOpenedInfo> NotificationReceived;

        public MockNotificationProvider(MockNotificationSimulationMode mode = MockNotificationSimulationMode.AlwaysSucceed)
        {
            _mode = mode;
        }

        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(true);

        public NotificationPermissionStatus GetPermissionStatus() => IsSupported ? PermissionStatus : NotificationPermissionStatus.Unsupported;

        public bool CanRequestPermission() => IsSupported && PermissionStatus != NotificationPermissionStatus.Authorized;

        public void RequestPermission(Action<NotificationPermissionStatus> onResult)
        {
            if (!IsSupported)
            {
                onResult?.Invoke(NotificationPermissionStatus.Unsupported);
                return;
            }

            PermissionStatus = _mode == MockNotificationSimulationMode.AlwaysPermissionDenied
                ? NotificationPermissionStatus.Denied
                : NotificationPermissionStatus.Authorized;

            onResult?.Invoke(PermissionStatus);
        }

        public void RegisterChannel(NotificationChannelDefinition channel)
        {
            if (channel.IsValid)
            {
                _channels[channel.Id] = channel;
            }
        }

        public bool IsChannelRegistered(string channelId) => _channels.ContainsKey(channelId ?? string.Empty);

        public NotificationResult Schedule(NotificationRequest request)
        {
            if (!IsSupported)
            {
                return NotificationResult.Failure(NotificationResultKind.Unsupported, request.Id, "Mock: simulated unsupported platform.");
            }

            if (!request.Id.IsValid)
            {
                return NotificationResult.Failure(NotificationResultKind.InvalidRequest, request.Id, "Mock: NotificationId is required.");
            }

            // Replace-if-exists - see NotificationId's remarks.
            _scheduled[request.Id.Value] = request;
            return NotificationResult.Ok(request.Id);
        }

        public NotificationResult Cancel(NotificationId id)
        {
            if (!id.IsValid || !_scheduled.Remove(id.Value ?? string.Empty))
            {
                return NotificationResult.Failure(NotificationResultKind.NotFound, id);
            }

            return new NotificationResult(NotificationResultKind.Cancelled, id);
        }

        public void CancelAll() => _scheduled.Clear();

        public IReadOnlyList<NotificationRequest> GetScheduled() => new List<NotificationRequest>(_scheduled.Values);

        public bool TryGetLaunchNotification(out NotificationOpenedInfo info)
        {
            if (_pendingLaunchNotification.HasValue)
            {
                info = _pendingLaunchNotification.Value;
                _pendingLaunchNotification = null;
                return true;
            }

            info = default;
            return false;
        }

        /// <summary>Test/QA hook - simulates the user tapping a scheduled (or arbitrary) notification.</summary>
        internal void SimulateOpened(NotificationId id, NotificationPayload payloadOverride = null)
        {
            _scheduled.TryGetValue(id.Value ?? string.Empty, out NotificationRequest request);
            NotificationPayload payload = payloadOverride ?? request?.Payload ?? NotificationPayload.Empty;
            NotificationOpened?.Invoke(new NotificationOpenedInfo(id, payload, wasSimulated: true));
        }

        /// <summary>Test/QA hook - simulates delivery while the app is already running (hot state).</summary>
        internal void SimulateReceived(NotificationId id, NotificationPayload payloadOverride = null)
        {
            _scheduled.TryGetValue(id.Value ?? string.Empty, out NotificationRequest request);
            NotificationPayload payload = payloadOverride ?? request?.Payload ?? NotificationPayload.Empty;
            NotificationReceived?.Invoke(new NotificationOpenedInfo(id, payload, wasSimulated: true));
        }

        /// <summary>Test/QA hook - simulates a cold start caused by tapping a notification; consumed
        /// once by the next <see cref="TryGetLaunchNotification"/> call (typically during
        /// <see cref="NotificationService"/>'s own <c>Initialize</c>).</summary>
        internal void SimulateColdStartLaunch(NotificationId id, NotificationPayload payload = null) =>
            _pendingLaunchNotification = new NotificationOpenedInfo(id, payload ?? NotificationPayload.Empty, wasSimulated: true);
    }
}
