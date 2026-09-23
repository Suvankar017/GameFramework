using System;
using System.Collections.Generic;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using UnityEngine;
using Log = GameFramework.Runtime.Diagnostics.Log;
using Object = UnityEngine.Object;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// Default <see cref="IDeepLinkService"/> - see CLAUDE.md's Phase 18 brief for the full design.
    ///
    /// <b>Duplicate protection (section 23):</b> the chosen, deterministic strategy is "identical to
    /// the immediately-previous processed raw URI" - <see cref="_lastProcessedRawUri"/> is updated the
    /// instant a URI passes the empty/null check, before parsing/dispatch, so a byte-identical repeat
    /// (the common case: the OS redelivering the same cold-start intent/URL across a config change or
    /// a duplicate callback) is rejected as <see cref="DeepLinkResultKind.Duplicate"/> without
    /// re-running any handler. This is a "not equal to the last one" check, not a time-boxed cache -
    /// documented explicitly here since the brief calls for a documented choice rather than an
    /// unstated assumption. A provider/platform that supplies a real per-event identifier could dedupe
    /// more precisely; Unity's own <see cref="UnityEngine.Application.deepLinkActivated"/>/
    /// <see cref="UnityEngine.Application.absoluteURL"/> do not.
    ///
    /// <b>Deferred processing (section 22):</b> only ever holds the single most-recently-deferred
    /// link - a second link arriving while still not <see cref="IsReady"/> replaces the first rather
    /// than queuing (the practical case is "the app was cold-started via exactly one link"; a genuine
    /// simultaneous second cold-start link cannot happen). <see cref="SetReady"/> clears
    /// <see cref="PendingRawUri"/> before dispatching it, so it is processed exactly once even if
    /// dispatch itself somehow re-enters <see cref="SetReady"/>.
    /// </summary>
    public sealed class DeepLinkService : IDeepLinkService
    {
        private const string LogCategory = "DeepLinks";

        private sealed class HandlerEntry
        {
            public IDeepLinkHandler Handler;
            public int Priority;
            public int InsertionOrder;
        }

        private readonly List<HandlerEntry> _handlers = new List<HandlerEntry>();
        private int _nextInsertionOrder;

        private IEventService _events;
        private ILoggingService _log;
        private DeepLinkCaptureDriver _driver;
        private string _lastProcessedRawUri;

        public bool IsReady { get; private set; }
        public string PendingRawUri { get; private set; }

        public event Action<string> DeepLinkReceived;
        public event Action<string> DeepLinkHandled;
        public event Action<string, string> DeepLinkRejected;

        public void Initialize(IServiceRegistry registry)
        {
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);

            var driverObject = new GameObject(nameof(DeepLinkCaptureDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<DeepLinkCaptureDriver>();
            _driver.Owner = this;

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            if (_driver == null)
            {
                return;
            }

            GameObject driverObject = _driver.gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(driverObject);
            }
            else
            {
                Object.DestroyImmediate(driverObject);
            }

            _driver = null;
        }

        public void RegisterHandler(IDeepLinkHandler handler, int priority = 0)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            for (int i = 0; i < _handlers.Count; i++)
            {
                if (ReferenceEquals(_handlers[i].Handler, handler))
                {
                    return; // Already registered - a no-op, matching IEventService.Subscribe's precedent.
                }
            }

            int insertAt = _handlers.Count;
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (priority > _handlers[i].Priority)
                {
                    insertAt = i;
                    break;
                }
            }

            _handlers.Insert(insertAt, new HandlerEntry { Handler = handler, Priority = priority, InsertionOrder = _nextInsertionOrder++ });
        }

        public void UnregisterHandler(IDeepLinkHandler handler)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (ReferenceEquals(_handlers[i].Handler, handler))
                {
                    _handlers.RemoveAt(i);
                    return;
                }
            }
        }

        public DeepLinkResult Process(string rawUri)
        {
            if (string.IsNullOrWhiteSpace(rawUri))
            {
                return DeepLinkResult.Failure(DeepLinkResultKind.Rejected, rawUri, "Empty URI.");
            }

            if (string.Equals(rawUri, _lastProcessedRawUri, StringComparison.Ordinal))
            {
                return new DeepLinkResult(DeepLinkResultKind.Duplicate, rawUri);
            }

            _lastProcessedRawUri = rawUri;

            _events.Publish(new DeepLinkReceivedEvent(rawUri));
            DeepLinkReceived?.Invoke(rawUri);

            if (!DeepLinkParser.TryParse(rawUri, out DeepLink link))
            {
                RaiseRejected(rawUri, "Malformed URI.");
                return DeepLinkResult.Failure(DeepLinkResultKind.Rejected, rawUri, "Malformed URI.");
            }

            if (!IsReady)
            {
                PendingRawUri = rawUri;
                return new DeepLinkResult(DeepLinkResultKind.Deferred, rawUri);
            }

            return Dispatch(rawUri, link);
        }

        public void SetReady(bool ready = true)
        {
            IsReady = ready;

            if (!ready || string.IsNullOrEmpty(PendingRawUri))
            {
                return;
            }

            string pending = PendingRawUri;
            PendingRawUri = null;

            if (DeepLinkParser.TryParse(pending, out DeepLink link))
            {
                Dispatch(pending, link);
            }
            else
            {
                RaiseRejected(pending, "Malformed URI.");
            }
        }

        public DeepLinkDiagnostics GetDiagnostics() => new DeepLinkDiagnostics(IsReady, _handlers.Count, PendingRawUri, _lastProcessedRawUri);

        private DeepLinkResult Dispatch(string rawUri, DeepLink link)
        {
            // Snapshot before iterating - a handler registering/unregistering another handler from
            // inside its own Handle() call must not affect this in-progress dispatch, the same
            // reentrancy safety IEventService.Publish already guarantees for its own subscribers.
            var snapshot = new List<HandlerEntry>(_handlers);

            for (int i = 0; i < snapshot.Count; i++)
            {
                IDeepLinkHandler handler = snapshot[i].Handler;

                bool canHandle;
                try
                {
                    canHandle = handler.CanHandle(link);
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                    continue;
                }

                if (!canHandle)
                {
                    continue;
                }

                DeepLinkHandlerResult result;
                try
                {
                    result = handler.Handle(link);
                }
                catch (Exception exception)
                {
                    _log?.LogException(exception, LogCategory);
                    result = DeepLinkHandlerResult.Failed;
                }

                if (result == DeepLinkHandlerResult.Handled)
                {
                    _events.Publish(new DeepLinkHandledEvent(rawUri));
                    DeepLinkHandled?.Invoke(rawUri);
                    return new DeepLinkResult(DeepLinkResultKind.Handled, rawUri);
                }

                if (result == DeepLinkHandlerResult.Failed)
                {
                    RaiseRejected(rawUri, "A matching handler reported failure.");
                    return DeepLinkResult.Failure(DeepLinkResultKind.NoHandlerFound, rawUri, "A matching handler reported failure.");
                }
                // NotApplicable - try the next handler.
            }

            RaiseRejected(rawUri, "No handler found.");
            return new DeepLinkResult(DeepLinkResultKind.NoHandlerFound, rawUri);
        }

        private void RaiseRejected(string rawUri, string reason)
        {
            _log?.Log(LogLevel.Warning, LogCategory, $"Deep link rejected ('{rawUri}'): {reason}");
            _events.Publish(new DeepLinkRejectedEvent(rawUri, reason));
            DeepLinkRejected?.Invoke(rawUri, reason);
        }
    }
}
