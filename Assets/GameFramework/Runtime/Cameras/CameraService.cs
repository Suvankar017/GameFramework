using System;
using System.Collections.Generic;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Default <see cref="ICameraService"/>. <see cref="Priority"/> recorded at
    /// <see cref="RegisterCamera"/> is informational/diagnostic only (mirrors
    /// <c>Performance.Ticking.TickGroup</c>'s exact "does not affect execution order by itself"
    /// precedent) - which camera is actually active is always the result of an explicit
    /// <see cref="Activate"/>/<see cref="PushOverride"/> call, never an implicit priority
    /// comparison, so "which camera is live" is always predictable from the last command issued
    /// (CLAUDE.md's Phase 11 brief, section 7's "predictable camera selection mechanism").
    /// </summary>
    public sealed class CameraService : ICameraService, IUpdatableService
    {
        private const string LogCategory = "Cameras";

        /// <summary>Accessibility toggle (default off) - when on, every built-in
        /// <see cref="ICameraMode"/>/zoom skips damping/smoothing entirely rather than disabling
        /// camera motion outright (CLAUDE.md's Phase 11 brief, section 29). Deliberately separate
        /// from Presentation's "Presentation.Channel.Camera" (that one gates camera *shake*, an
        /// unrelated concern already owned by Phase 10 - see CameraDriver's remarks on why this
        /// framework never duplicates it). Public (not internal) so an external camera backend
        /// integration (e.g. <c>Cameras.CinemachineIntegration</c>) can read the same setting for its
        /// own zoom damping rather than re-declaring this key a second time.</summary>
        public const string ReduceMotionSettingKey = "Camera.ReduceMotion";

        private sealed class Registration
        {
            public CameraController Controller;
            public int Priority;
            public string Owner;
        }

        private readonly Dictionary<CameraId, Registration> _cameras = new Dictionary<CameraId, Registration>();
        private readonly Dictionary<CameraController, CameraId> _idsByController = new Dictionary<CameraController, CameraId>();
        private readonly List<CameraOverrideHandle> _overrideStack = new List<CameraOverrideHandle>();

        private IEventService _events;
        private ILoggingService _log;

        private CameraId _baseActiveId;
        private CameraId _resolvedActiveId;

        public CameraId ActiveCameraId => _resolvedActiveId;
        public CameraController ActiveCamera => _resolvedActiveId.IsValid && _cameras.TryGetValue(_resolvedActiveId, out Registration reg) ? reg.Controller : null;

        public event Action<CameraId, CameraId> ActiveCameraChanged;

        public void Initialize(IServiceRegistry registry)
        {
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);

            if (registry.TryGet(out ISettingsService settings))
            {
                settings.Register(new SettingDefinition<bool>(ReduceMotionSettingKey, false, category: "Camera"));
                settings.Load(); // re-load: Settings.Initialize's own Load already ran before this - mirrors PresentationService.Initialize
            }
        }

        public void Shutdown()
        {
            _cameras.Clear();
            _idsByController.Clear();
            _overrideStack.Clear();
            _baseActiveId = CameraId.None;
            _resolvedActiveId = CameraId.None;
        }

        public void Tick()
        {
            // No per-frame work of its own - CameraDriver pulls ActiveCamera/pose each LateUpdate.
            // Present only so this service can be ticked uniformly alongside every other
            // IUpdatableService, matching GameFlowService/PresentationService's own shape, and kept
            // as a documented extension seam for future housekeeping (e.g. transition bookkeeping
            // moved here) rather than adding it speculatively now.
        }

        public CameraId RegisterCamera(CameraController controller, int priority = 0, string owner = null)
        {
            if (controller == null)
            {
                return CameraId.None;
            }

            if (_idsByController.TryGetValue(controller, out CameraId existing))
            {
                return existing;
            }

            CameraId id = CameraId.New();
            _cameras.Add(id, new Registration { Controller = controller, Priority = priority, Owner = owner });
            _idsByController.Add(controller, id);

            _events.Publish(new CameraRegisteredEvent(id));
            return id;
        }

        public void UnregisterCamera(CameraId id)
        {
            if (!_cameras.TryGetValue(id, out Registration registration))
            {
                return;
            }

            _cameras.Remove(id);
            _idsByController.Remove(registration.Controller);

            for (int i = _overrideStack.Count - 1; i >= 0; i--)
            {
                if (_overrideStack[i].CameraId == id)
                {
                    _overrideStack.RemoveAt(i);
                }
            }

            if (_baseActiveId == id)
            {
                _baseActiveId = CameraId.None;
            }

            _events.Publish(new CameraUnregisteredEvent(id));
            RecomputeActive();
        }

        public bool IsRegistered(CameraId id) => _cameras.ContainsKey(id);

        public CameraController GetController(CameraId id) => _cameras.TryGetValue(id, out Registration reg) ? reg.Controller : null;

        public bool SetTarget(CameraId id, ICameraTarget target)
        {
            if (!_cameras.TryGetValue(id, out Registration reg))
            {
                return false;
            }

            reg.Controller.SetTarget(target);
            _events.Publish(new CameraTargetChangedEvent(id));
            return true;
        }

        public bool ClearTarget(CameraId id) => SetTarget(id, null);

        public bool SetMode(CameraId id, ICameraMode mode)
        {
            if (!_cameras.TryGetValue(id, out Registration reg))
            {
                return false;
            }

            reg.Controller.SetMode(mode);
            return true;
        }

        public bool Activate(CameraId id)
        {
            if (!_cameras.ContainsKey(id))
            {
                LogUnknown("Activate", id);
                return false;
            }

            _baseActiveId = id;
            RecomputeActive();
            return true;
        }

        public ICameraOverrideHandle PushOverride(CameraId id)
        {
            if (!_cameras.ContainsKey(id))
            {
                LogUnknown("PushOverride", id);
                return null;
            }

            var handle = new CameraOverrideHandle(id, OnOverrideReleased);
            _overrideStack.Add(handle);
            _events.Publish(new CameraOverridePushedEvent(id));
            RecomputeActive();
            return handle;
        }

        private void OnOverrideReleased(CameraOverrideHandle handle)
        {
            _overrideStack.Remove(handle);
            _events.Publish(new CameraOverridePoppedEvent(handle.CameraId));
            RecomputeActive();
        }

        public bool ResetCamera(CameraId id)
        {
            if (!_cameras.TryGetValue(id, out Registration reg))
            {
                return false;
            }

            reg.Controller.Snap();
            _events.Publish(new CameraResetEvent(id));
            return true;
        }

        public CameraRuntimeState GetState(CameraId id)
        {
            if (!_cameras.TryGetValue(id, out Registration reg))
            {
                return default;
            }

            bool isOverride = false;
            for (int i = 0; i < _overrideStack.Count; i++)
            {
                if (_overrideStack[i].CameraId == id)
                {
                    isOverride = true;
                    break;
                }
            }

            return new CameraRuntimeState(id, reg.Priority, reg.Owner, id == _resolvedActiveId, isOverride,
                reg.Controller.HasTarget, reg.Controller.CurrentPose);
        }

        private void RecomputeActive()
        {
            CameraId resolved = _overrideStack.Count > 0 ? _overrideStack[_overrideStack.Count - 1].CameraId : _baseActiveId;

            if (resolved == _resolvedActiveId)
            {
                return;
            }

            CameraId previous = _resolvedActiveId;
            _resolvedActiveId = resolved;
            ActiveCameraChanged?.Invoke(previous, resolved);
            _events.Publish(new ActiveCameraChangedEvent(previous, resolved));
        }

        private void LogUnknown(string operation, CameraId id) =>
            _log?.Log(LogLevel.Warning, LogCategory, $"{operation} called with unknown CameraId '{id}'.");
    }
}
