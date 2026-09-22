using System;
using System.Collections.Generic;
using GameFramework.Analytics;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.Analytics
{
    /// <summary>
    /// Minimal UI Toolkit Editor window for simulating Analytics/Diagnostics activity while in Play
    /// Mode - see CLAUDE.md's Phase 16 brief, section 51 ("simulate: Analytics event, Exception,
    /// Breadcrumb, Consent change, Provider failure/unavailable... this should not communicate with
    /// production analytics unless explicitly configured, clearly distinguish simulated events").
    /// Every simulated event/exception is tagged with a "simulated" parameter/prefix so it is never
    /// mistaken for real telemetry if a real provider happens to be wired up. Deliberately not a full
    /// dashboard (section 85/86) - only enough to exercise the two services by hand.
    /// </summary>
    internal sealed class AnalyticsEventSimulatorWindow : EditorWindow
    {
        [MenuItem("GameFramework/Analytics/Event Simulator")]
        private static void Open() => GetWindow<AnalyticsEventSimulatorWindow>("Analytics Simulator");

        private Label _statusLabel;

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;

            _statusLabel = new Label();
            root.Add(_statusLabel);

            root.Add(MakeButton("Track Simulated Event", SimulateEvent));
            root.Add(MakeButton("Track Simulated Screen View", SimulateScreenView));
            root.Add(MakeButton("Record Simulated Exception", SimulateException));
            root.Add(MakeButton("Record Simulated Error", SimulateError));
            root.Add(MakeButton("Add Simulated Breadcrumb", SimulateBreadcrumb));
            root.Add(MakeButton("Set Consent: Granted", () => SimulateConsent(ConsentState.Granted)));
            root.Add(MakeButton("Set Consent: Denied", () => SimulateConsent(ConsentState.Denied)));
            root.Add(MakeButton("Set Consent: Unknown", () => SimulateConsent(ConsentState.Unknown)));
            root.Add(MakeButton("Reset Analytics Identity", SimulateResetIdentity));
            root.Add(MakeButton("Flush Queued Events", SimulateFlush));

            RefreshStatus();
        }

        private void OnInspectorUpdate() => RefreshStatus();

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
                _statusLabel.text = "No ready GameBootstrapper - enter Play Mode with an AnalyticsBootstrapper.";
                return;
            }

            string analyticsStatus = registry.TryGet(out IAnalyticsService analytics)
                ? $"Analytics: Enabled={analytics.IsEnabled}, Consent={analytics.Consent}, Session={analytics.SessionId}"
                : "Analytics: not registered.";

            string diagnosticsStatus = registry.TryGet(out IDiagnosticsService diagnostics)
                ? $"Diagnostics: Enabled={diagnostics.IsEnabled}, Breadcrumbs={diagnostics.GetBreadcrumbs().Count}"
                : "Diagnostics: not registered.";

            _statusLabel.text = analyticsStatus + "\n" + diagnosticsStatus;
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

        private void SimulateEvent()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IAnalyticsService analytics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IAnalyticsService registered.");
                return;
            }

            analytics.Track("simulator_event", new Dictionary<string, object>
            {
                ["simulated"] = true,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            });
            RefreshStatus();
        }

        private void SimulateScreenView()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IAnalyticsService analytics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IAnalyticsService registered.");
                return;
            }

            analytics.TrackScreenView("SimulatorScreen", new Dictionary<string, object> { ["simulated"] = true });
            RefreshStatus();
        }

        private void SimulateException()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IDiagnosticsService diagnostics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IDiagnosticsService registered.");
                return;
            }

            diagnostics.RecordException(new InvalidOperationException("[Simulated] AnalyticsEventSimulatorWindow test exception."), ErrorCategory.Framework);
            RefreshStatus();
        }

        private void SimulateError()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IDiagnosticsService diagnostics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IDiagnosticsService registered.");
                return;
            }

            diagnostics.RecordError("[Simulated] AnalyticsEventSimulatorWindow test error.", ErrorCategory.Framework);
            RefreshStatus();
        }

        private void SimulateBreadcrumb()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IDiagnosticsService diagnostics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IDiagnosticsService registered.");
                return;
            }

            diagnostics.AddBreadcrumb("Simulator", $"Manual breadcrumb at {DateTime.UtcNow:O}");
            RefreshStatus();
        }

        private void SimulateConsent(ConsentState state)
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IAnalyticsService analytics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IAnalyticsService registered.");
                return;
            }

            analytics.SetConsent(state);
            RefreshStatus();
        }

        private void SimulateResetIdentity()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IAnalyticsService analytics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IAnalyticsService registered.");
                return;
            }

            analytics.ResetIdentity();
            RefreshStatus();
        }

        private void SimulateFlush()
        {
            if (!TryGetRegistry(out IServiceRegistry registry) || !registry.TryGet(out IAnalyticsService analytics))
            {
                Debug.LogWarning("[Analytics] Cannot simulate - no IAnalyticsService registered.");
                return;
            }

            analytics.Flush();
            RefreshStatus();
        }
    }
}
