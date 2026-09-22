using System;
using System.Collections.Generic;
using GameFramework.Platform;
using GameFramework.Runtime.Services;
using UnityEngine;
using Log = GameFramework.Runtime.Diagnostics.Log;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// Default <see cref="IDiagnosticsService"/> implementation. Populates initial diagnostic context
    /// (framework/app version, platform, device model) from <see cref="Platform.IPlatformService"/>/
    /// <see cref="Platform.IDeviceInfoService"/> when registered (resolved softly - see CLAUDE.md's
    /// Phase 16 brief, section 55) and reuses <see cref="Platform.IDeviceInfoService"/> rather than
    /// duplicating device queries.
    ///
    /// <b>No recursive diagnostics (section 83):</b> <see cref="Report"/> is the only place that calls
    /// <see cref="ICrashReportingProvider.Report"/>; if that call throws, the failure is logged only
    /// through the plain <see cref="Runtime.Diagnostics.Log"/> facade - it is structurally impossible
    /// for that catch block to call back into <see cref="RecordException"/>/<see cref="RecordError"/>,
    /// since it never does.
    /// </summary>
    public sealed class DiagnosticsService : IDiagnosticsService
    {
        private const string LogCategory = "Diagnostics";

        private readonly DiagnosticsConfiguration _configuration;
        private readonly ICrashReportingProvider _provider;
        private readonly BreadcrumbRingBuffer _breadcrumbs;
        private readonly Dictionary<string, string> _context = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();

        private UnhandledExceptionDriver _unhandledExceptionDriver;

        public DiagnosticsService(DiagnosticsConfiguration configuration, ICrashReportingProvider provider)
        {
            _configuration = configuration != null ? configuration : ScriptableObject.CreateInstance<DiagnosticsConfiguration>();
            _provider = provider ?? new NoOpCrashReportingProvider();
            _breadcrumbs = new BreadcrumbRingBuffer(_configuration.BreadcrumbCapacity);
        }

        public bool IsEnabled => _configuration.Enabled;

        public void Initialize(IServiceRegistry registry)
        {
            SetContext("framework_version", FrameworkInfo.Version);
            SetContext("app_version", Application.version);

            if (registry.TryGet(out IPlatformService platform))
            {
                SetContext("platform", platform.Platform.ToString());
            }

            if (registry.TryGet(out IDeviceInfoService deviceInfo))
            {
                SetContext("device_model", deviceInfo.Current.Model);
                SetContext("operating_system", deviceInfo.Current.OperatingSystem);
            }

            try
            {
                _provider.Initialize();
            }
            catch (Exception ex)
            {
                LogProviderFailure(ex);
            }

            if (_configuration.CaptureUnhandledExceptions)
            {
                _unhandledExceptionDriver = new UnhandledExceptionDriver(this);
            }
        }

        public void Shutdown()
        {
            _unhandledExceptionDriver?.Dispose();
            _unhandledExceptionDriver = null;
        }

        public void AddBreadcrumb(string category, string message) =>
            _breadcrumbs.Add(new Breadcrumb(category ?? string.Empty, message ?? string.Empty, DateTime.UtcNow));

        public void SetContext(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _context[key] = value ?? string.Empty;
        }

        public void SetTag(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _tags[key] = value ?? string.Empty;
        }

        public void SetUserId(string userId)
        {
            try
            {
                _provider.SetUserId(userId);
            }
            catch (Exception ex)
            {
                LogProviderFailure(ex);
            }
        }

        public void RecordException(Exception exception, ErrorCategory category = ErrorCategory.Unknown)
        {
            if (exception == null)
            {
                return;
            }

            Log.Exception(exception, LogCategory);

            if (!IsEnabled)
            {
                return;
            }

            Report(BuildReport(exception, exception.Message, category));
        }

        public void RecordError(string message, ErrorCategory category = ErrorCategory.Unknown)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Log.Error(LogCategory, message);

            if (!IsEnabled)
            {
                return;
            }

            Report(BuildReport(null, message, category));
        }

        public IReadOnlyList<Breadcrumb> GetBreadcrumbs() => _breadcrumbs.ToArray();

        private DiagnosticReport BuildReport(Exception exception, string message, ErrorCategory category) => new DiagnosticReport(
            exception, message, category, DateTime.UtcNow,
            new Dictionary<string, string>(_context), new Dictionary<string, string>(_tags), _breadcrumbs.ToArray());

        private void Report(DiagnosticReport report)
        {
            try
            {
                _provider.Report(report);
            }
            catch (Exception ex)
            {
                // Deliberately does NOT call RecordException/RecordError again - see this class's
                // remarks and CLAUDE.md's Phase 16 brief, section 83.
                LogProviderFailure(ex);
            }
        }

        private static void LogProviderFailure(Exception ex) => Log.Exception(ex, LogCategory);
    }
}
