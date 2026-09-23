using System;
using System.Collections.Generic;
using GameFramework.DeepLinks;
using GameFramework.Notifications;
using GameFramework.Performance.Mobile;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.Notifications
{
    /// <summary>
    /// Minimal UI Toolkit Editor window for exercising Notifications/DeepLinks by hand while in Play
    /// Mode - see CLAUDE.md's Phase 18 brief, sections 17/48. Every simulated notification event is
    /// reported through <see cref="INotificationService.SimulateNotificationOpened"/>/
    /// <see cref="INotificationService.SimulateNotificationReceived"/>, which always tags the result
    /// <see cref="NotificationOpenedInfo.WasSimulated"/> = true (section 16 - never claim a platform
    /// delivered something the Editor only pretended to). Deliberately not a full dashboard - only
    /// enough to exercise the two services by hand, the same scope
    /// <c>Analytics.AnalyticsEventSimulatorWindow</c> already established.
    /// </summary>
    internal sealed class NotificationsDebugWindow : EditorWindow
    {
        [MenuItem("GameFramework/Notifications/Debug Window")]
        private static void Open() => GetWindow<NotificationsDebugWindow>("Notifications Debug");

        private Label _statusLabel;
        private TextField _notificationIdField;
        private TextField _titleField;
        private TextField _bodyField;
        private IntegerField _minutesFromNowField;
        private TextField _deepLinkUriField;

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;

            _statusLabel = new Label();
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(_statusLabel);

            root.Add(Header("Notifications"));
            root.Add(MakeButton("Request Permission", RequestPermission));

            _titleField = new TextField("Title") { value = "Test Notification" };
            _bodyField = new TextField("Body") { value = "Scheduled from the Editor debug window." };
            _minutesFromNowField = new IntegerField("Minutes From Now") { value = 1 };
            _notificationIdField = new TextField("Notification Id") { value = "debug_notification" };
            root.Add(_notificationIdField);
            root.Add(_titleField);
            root.Add(_bodyField);
            root.Add(_minutesFromNowField);
            root.Add(MakeButton("Schedule", ScheduleTestNotification));
            root.Add(MakeButton("Cancel", CancelTestNotification));
            root.Add(MakeButton("Cancel All", CancelAllNotifications));
            root.Add(MakeButton("Simulate Opened", () => SimulateNotification(opened: true)));
            root.Add(MakeButton("Simulate Received", () => SimulateNotification(opened: false)));

            root.Add(Header("Deep Links"));
            _deepLinkUriField = new TextField("Raw URI") { value = "mygame://daily-reward?source=debug" };
            root.Add(_deepLinkUriField);
            root.Add(MakeButton("Process", ProcessDeepLink));
            root.Add(MakeButton("Set Ready", () => SetDeepLinksReady(true)));
            root.Add(MakeButton("Set Not Ready", () => SetDeepLinksReady(false)));

            RefreshStatus();
        }

        private void OnInspectorUpdate() => RefreshStatus();

        private static Label Header(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 10;
            return label;
        }

        private static Button MakeButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.marginTop = 2;
            button.style.marginBottom = 2;
            return button;
        }

        private void RefreshStatus()
        {
            if (_statusLabel == null)
            {
                return;
            }

            if (!TryGetRegistry(out IServiceRegistry registry))
            {
                _statusLabel.text = "No ready GameBootstrapper - enter Play Mode with a NotificationsBootstrapper/DeepLinksBootstrapper.";
                return;
            }

            string notificationStatus = registry.TryGet(out INotificationService notifications)
                ? BuildNotificationStatus(notifications)
                : "Notifications: not registered.";

            string deepLinkStatus = registry.TryGet(out IDeepLinkService deepLinks)
                ? BuildDeepLinkStatus(deepLinks)
                : "DeepLinks: not registered.";

            string lifecycleStatus = registry.TryGet(out IApplicationLifecycleService lifecycle)
                ? $"Lifecycle: Paused={lifecycle.IsPaused}, HasFocus={lifecycle.HasFocus}"
                : "Lifecycle: IApplicationLifecycleService not registered.";

            _statusLabel.text = notificationStatus + "\n" + deepLinkStatus + "\n" + lifecycleStatus;
        }

        private static string BuildNotificationStatus(INotificationService notifications)
        {
            NotificationDiagnostics diag = notifications.GetDiagnostics();
            return $"Notifications: Supported={diag.IsSupported}, Permission={diag.PermissionStatus}, Scheduled=[{string.Join(", ", diag.ScheduledIds)}], LastError={diag.LastError}";
        }

        private static string BuildDeepLinkStatus(IDeepLinkService deepLinks)
        {
            DeepLinkDiagnostics diag = deepLinks.GetDiagnostics();
            return $"DeepLinks: Ready={diag.IsReady}, Handlers={diag.HandlerCount}, Pending={diag.PendingRawUri}, LastProcessed={diag.LastProcessedRawUri}";
        }

        private static bool TryGetRegistry(out IServiceRegistry registry)
        {
            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                registry = GameBootstrapper.Instance.Services;
                return true;
            }

            registry = null;
            return false;
        }

        private void RequestPermission()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out INotificationService notifications))
            {
                Debug.LogWarning("[Notifications] Cannot request permission - no INotificationService registered.");
                return;
            }

            notifications.RequestPermission(status => Debug.Log($"[Notifications] Permission request result: {status}"));
            RefreshStatus();
        }

        private void ScheduleTestNotification()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out INotificationService notifications))
            {
                Debug.LogWarning("[Notifications] Cannot schedule - no INotificationService registered.");
                return;
            }

            var request = new NotificationRequest(
                new NotificationId(_notificationIdField.value),
                NotificationContent.FromText(_titleField.value, _bodyField.value),
                DateTime.UtcNow.AddMinutes(Math.Max(0.01, _minutesFromNowField.value)),
                payload: new NotificationPayload(type: "debug", route: "debug", parameters: new Dictionary<string, string> { ["source"] = "editor" }));

            NotificationResult result = notifications.Schedule(request);
            Debug.Log($"[Notifications] Schedule result: {result.Kind} ({result.FailureDetail})");
            RefreshStatus();
        }

        private void CancelTestNotification()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out INotificationService notifications))
            {
                Debug.LogWarning("[Notifications] Cannot cancel - no INotificationService registered.");
                return;
            }

            NotificationResult result = notifications.Cancel(new NotificationId(_notificationIdField.value));
            Debug.Log($"[Notifications] Cancel result: {result.Kind}");
            RefreshStatus();
        }

        private void CancelAllNotifications()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out INotificationService notifications))
            {
                Debug.LogWarning("[Notifications] Cannot cancel all - no INotificationService registered.");
                return;
            }

            notifications.CancelAll();
            RefreshStatus();
        }

        private void SimulateNotification(bool opened)
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out INotificationService notifications))
            {
                Debug.LogWarning("[Notifications] Cannot simulate - no INotificationService registered.");
                return;
            }

            var id = new NotificationId(_notificationIdField.value);
            if (opened)
            {
                notifications.SimulateNotificationOpened(id);
            }
            else
            {
                notifications.SimulateNotificationReceived(id);
            }

            RefreshStatus();
        }

        private void ProcessDeepLink()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IDeepLinkService deepLinks))
            {
                Debug.LogWarning("[DeepLinks] Cannot process - no IDeepLinkService registered.");
                return;
            }

            DeepLinkResult result = deepLinks.Process(_deepLinkUriField.value);
            Debug.Log($"[DeepLinks] Process result: {result.Kind} ({result.FailureDetail})");
            RefreshStatus();
        }

        private void SetDeepLinksReady(bool ready)
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IDeepLinkService deepLinks))
            {
                Debug.LogWarning("[DeepLinks] Cannot set ready - no IDeepLinkService registered.");
                return;
            }

            deepLinks.SetReady(ready);
            RefreshStatus();
        }
    }
}
