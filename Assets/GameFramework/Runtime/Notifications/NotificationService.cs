using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Localization;
using GameFramework.Notifications.Providers;
using GameFramework.Performance.Mobile;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using Log = GameFramework.Runtime.Diagnostics.Log;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Default <see cref="INotificationService"/>, orchestrating one <see cref="INotificationProvider"/>
    /// - see CLAUDE.md's Phase 18 brief for the full design.
    ///
    /// <see cref="ILocalizationService"/> is resolved softly (<see cref="IServiceRegistry.TryGet{TService}"/>):
    /// a <see cref="NotificationText"/> built from a localization key with no registered
    /// <see cref="ILocalizationService"/> falls back to the raw key text and logs a warning, rather
    /// than throwing - the same graceful-degradation pattern <c>Purchases.PurchaseService"</c> already
    /// established for its own soft dependencies.
    /// </summary>
    public sealed class NotificationService : INotificationService
    {
        private const string LogCategory = "Notifications";

        private readonly INotificationProvider _provider;
        private readonly INotificationClock _clock;

        private IEventService _events;
        private ILoggingService _log;
        private ILocalizationService _localization;
        private bool _providerReady;

        public bool IsSupported => _provider.IsSupported;

        public NotificationPermissionStatus PermissionStatus { get; private set; } = NotificationPermissionStatus.Unknown;

        public string LastError { get; private set; }

        public event Action<NotificationPermissionStatus> PermissionChanged;
        public event Action<NotificationOpenedInfo> NotificationOpened;
        public event Action<NotificationOpenedInfo> NotificationReceived;

        public NotificationService(INotificationProvider provider, INotificationClock clock = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _clock = clock ?? new SystemNotificationClock();
        }

        public void Initialize(IServiceRegistry registry)
        {
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            registry.TryGet(out _localization);

            _provider.NotificationOpened += OnProviderNotificationOpened;
            _provider.NotificationReceived += OnProviderNotificationReceived;
            _events.Subscribe<ApplicationResumedEvent>(OnApplicationResumed);

            _provider.Initialize(success =>
            {
                _providerReady = success;
                PermissionStatus = _provider.GetPermissionStatus();

                if (!success)
                {
                    LastError = "Notification provider failed to initialize.";
                    _log?.Log(LogLevel.Warning, LogCategory, LastError);
                    return;
                }

                // Cold start: the application may have just been launched by the user tapping a
                // notification - see CLAUDE.md's Phase 18 brief, section 21.
                if (_provider.TryGetLaunchNotification(out NotificationOpenedInfo launchInfo))
                {
                    OnProviderNotificationOpened(launchInfo);
                }
            });
        }

        public void Shutdown()
        {
            _provider.NotificationOpened -= OnProviderNotificationOpened;
            _provider.NotificationReceived -= OnProviderNotificationReceived;
            _events.Unsubscribe<ApplicationResumedEvent>(OnApplicationResumed);
        }

        // The OS notification-permission prompt lives outside this application entirely (the user
        // can grant/revoke it from device Settings while backgrounded) - re-check on every resume
        // rather than only trusting whatever RequestPermission last reported. Phase 14's
        // ApplicationLifecycleService publishes this regardless of whether Notifications is used, and
        // this subscription is a soft, event-only integration (CLAUDE.md's Phase 18 brief, section
        // 31) - if that service isn't registered, nothing publishes the event and this simply never
        // fires, exactly like every other soft dependency in this framework.
        private void OnApplicationResumed(ApplicationResumedEvent evt)
        {
            if (!_providerReady)
            {
                return;
            }

            NotificationPermissionStatus current = _provider.GetPermissionStatus();
            if (current == PermissionStatus)
            {
                return;
            }

            PermissionStatus = current;
            PermissionChanged?.Invoke(current);
            _events.Publish(new NotificationPermissionChangedEvent(current));
        }

        public bool CanRequestPermission() => _providerReady && _provider.CanRequestPermission();

        public void RequestPermission(Action<NotificationPermissionStatus> onResult = null)
        {
            if (!_providerReady)
            {
                onResult?.Invoke(PermissionStatus);
                return;
            }

            _events.Publish(new NotificationPermissionRequestedEvent());

            _provider.RequestPermission(status =>
            {
                bool changed = status != PermissionStatus;
                PermissionStatus = status;

                if (changed)
                {
                    PermissionChanged?.Invoke(status);
                    _events.Publish(new NotificationPermissionChangedEvent(status));
                }

                onResult?.Invoke(status);
            });
        }

        public void RegisterChannel(NotificationChannelDefinition channel)
        {
            if (!channel.IsValid)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "RegisterChannel called with no channel Id.");
                return;
            }

            _provider.RegisterChannel(channel);
        }

        public NotificationResult Schedule(NotificationRequest request)
        {
            Guard.NotNull(request, nameof(request));

            if (!request.Id.IsValid)
            {
                return NotificationResult.Failure(NotificationResultKind.InvalidRequest, request.Id, "NotificationId is required.");
            }

            // A one-shot request for a time already past can never fire - reject it deterministically
            // rather than leaving that judgment call to whichever provider happens to be behind this
            // (CLAUDE.md's Phase 18 brief, section 45). A repeating request is allowed through even if
            // today's occurrence has already passed - computing "the next occurrence" is provider-
            // specific and out of scope here (section 10).
            if (request.Repeat == NotificationRepeatMode.None && request.ScheduledTimeUtc <= _clock.UtcNow)
            {
                return NotificationResult.Failure(NotificationResultKind.InvalidRequest, request.Id, "ScheduledTimeUtc must be in the future.");
            }

            if (!_providerReady)
            {
                return NotificationResult.Failure(NotificationResultKind.ProviderUnavailable, request.Id, "Notification provider is not ready.");
            }

            if (!_provider.IsSupported)
            {
                return NotificationResult.Failure(NotificationResultKind.Unsupported, request.Id, "Notifications are not supported on this platform/provider.");
            }

            NotificationPermissionStatus permission = _provider.GetPermissionStatus();
            if (permission != NotificationPermissionStatus.Authorized && permission != NotificationPermissionStatus.Provisional)
            {
                return NotificationResult.Failure(NotificationResultKind.PermissionDenied, request.Id, $"Notification permission is {permission}.");
            }

            NotificationRequest resolved = ResolveContent(request);
            NotificationResult result = _provider.Schedule(resolved);

            if (result.Success)
            {
                LastError = null;
                _events.Publish(new NotificationScheduledEvent(request.Id));
            }
            else
            {
                LastError = result.FailureDetail;
                _log?.Log(LogLevel.Warning, LogCategory, $"Schedule('{request.Id}') failed: {result.FailureDetail}");
            }

            return result;
        }

        public NotificationResult Cancel(NotificationId id)
        {
            if (!id.IsValid)
            {
                return NotificationResult.Failure(NotificationResultKind.NotFound, id);
            }

            NotificationResult result = _provider.Cancel(id);
            if (result.Kind == NotificationResultKind.Cancelled)
            {
                _events.Publish(new NotificationCancelledEvent(id));
            }

            return result;
        }

        public void CancelAll() => _provider.CancelAll();

        public IReadOnlyList<NotificationRequest> GetScheduled() => _provider.GetScheduled();

        public bool IsScheduled(NotificationId id)
        {
            if (!id.IsValid)
            {
                return false;
            }

            IReadOnlyList<NotificationRequest> scheduled = _provider.GetScheduled();
            for (int i = 0; i < scheduled.Count; i++)
            {
                if (scheduled[i].Id.Equals(id))
                {
                    return true;
                }
            }

            return false;
        }

        public void SimulateNotificationOpened(NotificationId id, NotificationPayload payloadOverride = null) =>
            OnProviderNotificationOpened(BuildSimulatedInfo(id, payloadOverride));

        public void SimulateNotificationReceived(NotificationId id, NotificationPayload payloadOverride = null) =>
            OnProviderNotificationReceived(BuildSimulatedInfo(id, payloadOverride));

        public NotificationDiagnostics GetDiagnostics()
        {
            IReadOnlyList<NotificationRequest> scheduled = _provider.GetScheduled();
            var ids = new List<NotificationId>(scheduled.Count);
            for (int i = 0; i < scheduled.Count; i++)
            {
                ids.Add(scheduled[i].Id);
            }

            return new NotificationDiagnostics(_provider.IsSupported, PermissionStatus, ids, LastError);
        }

        private NotificationOpenedInfo BuildSimulatedInfo(NotificationId id, NotificationPayload payloadOverride)
        {
            NotificationPayload payload = payloadOverride;
            if (payload == null)
            {
                IReadOnlyList<NotificationRequest> scheduled = _provider.GetScheduled();
                for (int i = 0; i < scheduled.Count; i++)
                {
                    if (scheduled[i].Id.Equals(id))
                    {
                        payload = scheduled[i].Payload;
                        break;
                    }
                }
            }

            return new NotificationOpenedInfo(id, payload ?? NotificationPayload.Empty, wasSimulated: true);
        }

        private NotificationRequest ResolveContent(NotificationRequest request)
        {
            NotificationContent content = new NotificationContent(
                NotificationText.FromText(ResolveText(request.Content.Title)),
                NotificationText.FromText(ResolveText(request.Content.Body)),
                request.Content.Subtitle.IsEmpty ? default : NotificationText.FromText(ResolveText(request.Content.Subtitle)));

            return new NotificationRequest(
                request.Id, content, request.ScheduledTimeUtc, request.Repeat, request.Payload,
                request.Priority, request.ChannelId, request.PlaySound, request.BadgeCount);
        }

        private string ResolveText(NotificationText text)
        {
            if (!string.IsNullOrEmpty(text.LocalizationKey) && _localization == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory,
                    $"NotificationText requests localization key '{text.LocalizationKey}' but no ILocalizationService is registered - using the key as literal text.");
            }

            return text.Resolve(_localization);
        }

        private void OnProviderNotificationOpened(NotificationOpenedInfo info)
        {
            NotificationOpened?.Invoke(info);
            _events.Publish(new NotificationOpenedEvent(info));
        }

        private void OnProviderNotificationReceived(NotificationOpenedInfo info)
        {
            NotificationReceived?.Invoke(info);
            _events.Publish(new NotificationReceivedEvent(info));
        }
    }
}
