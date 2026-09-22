using System;
using UnityEngine;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// The one place in this framework that reads <see cref="Application.logMessageReceived"/> - see
    /// CLAUDE.md's Phase 16 brief, section 29. Forwards an uncaught exception/error Unity itself
    /// logged to <see cref="IDiagnosticsService.RecordError"/> as <see cref="ErrorCategory.Unknown"/>
    /// (no <see cref="Exception"/> instance is available from this callback, only its formatted
    /// text).
    ///
    /// <see cref="_isHandling"/> guards against infinite recursion: <see cref="IDiagnosticsService.RecordError"/>
    /// itself logs through the existing logger, which re-invokes this same callback synchronously -
    /// the guard makes that re-entrant call a no-op rather than looping.
    /// </summary>
    internal sealed class UnhandledExceptionDriver : IDisposable
    {
        private readonly IDiagnosticsService _diagnostics;
        private bool _isHandling;

        public UnhandledExceptionDriver(IDiagnosticsService diagnostics)
        {
            _diagnostics = diagnostics;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (_isHandling || (type != LogType.Exception && type != LogType.Error))
            {
                return;
            }

            _isHandling = true;
            try
            {
                _diagnostics.RecordError($"{condition}\n{stackTrace}", ErrorCategory.Unknown);
            }
            finally
            {
                _isHandling = false;
            }
        }
    }
}
