using System;
using System.Collections;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using UnityEngine;

namespace GameFramework.Samples.Phase2Demo
{
    /// <summary>
    /// Drives the Phase 2 demonstration scene: exercises Time, Timers, Events, Persistence, and
    /// Settings together, driven by number-key presses so each system can be triggered on demand
    /// and its result read from the console. Not part of the reusable framework - sample/demo
    /// content only, kept in its own assembly and scene, separate from any production game content.
    /// </summary>
    public sealed class Phase2DemoController : MonoBehaviour
    {
        private const string SaveKey = "Phase2Demo.Counter";
        private const string VolumeSettingKey = "Phase2Demo.Volume";
        private const int SaveVersion = 1;

        private ITimeService _time;
        private ITimerService _timers;
        private IEventService _events;
        private IPersistenceService _persistence;
        private ISettingsService _settings;
        private bool _paused;
        private int _lastLoggedCountdownSecond = -1;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _time = services.Get<ITimeService>();
            _timers = services.Get<ITimerService>();
            _events = services.Get<IEventService>();
            _persistence = services.Get<IPersistenceService>();
            _settings = services.Get<ISettingsService>();

            _events.Subscribe<DemoPingEvent>(OnDemoPing);
            _events.Subscribe<SettingChangedEvent>(OnSettingChanged);

            _settings.Register(new SettingDefinition<float>(
                VolumeSettingKey, defaultValue: 0.5f, validator: v => v is >= 0f and <= 1f, category: "Demo"));

            Debug.Log("[Phase2Demo] Ready. Keys: 1 Pause/Resume, 2 Slow-mo toggle, 3 One-shot timer, " +
                "4 Repeating timer, 5 Countdown timer, 6 Publish event, 7 Save counter, 8 Load counter, " +
                "9 Bump+save volume setting.");
        }

        private void Update()
        {
            if (_time == null)
            {
                return; // Start's coroutine hasn't reached Ready yet.
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) DemoPauseToggle();
            if (Input.GetKeyDown(KeyCode.Alpha2)) DemoSlowMotionToggle();
            if (Input.GetKeyDown(KeyCode.Alpha3)) DemoOneShotTimer();
            if (Input.GetKeyDown(KeyCode.Alpha4)) DemoRepeatingTimer();
            if (Input.GetKeyDown(KeyCode.Alpha5)) DemoCountdownTimer();
            if (Input.GetKeyDown(KeyCode.Alpha6)) DemoPublishEvent();
            if (Input.GetKeyDown(KeyCode.Alpha7)) DemoSaveCounter();
            if (Input.GetKeyDown(KeyCode.Alpha8)) DemoLoadCounter();
            if (Input.GetKeyDown(KeyCode.Alpha9)) DemoBumpSetting();
        }

        private void DemoPauseToggle()
        {
            _paused = !_paused;
            if (_paused)
            {
                _time.Pause();
            }
            else
            {
                _time.Resume();
            }

            Debug.Log($"[Phase2Demo] Time.IsPaused={_time.IsPaused}, TimeScale={_time.TimeScale}.");
        }

        private void DemoSlowMotionToggle()
        {
            _time.SetTimeScale(Mathf.Approximately(_time.TimeScale, 1f) && !_time.IsPaused ? 0.3f : 1f);
            Debug.Log($"[Phase2Demo] Requested TimeScale, resolved TimeScale={_time.TimeScale} (IsPaused={_time.IsPaused}).");
        }

        private void DemoOneShotTimer()
        {
            Debug.Log("[Phase2Demo] One-shot timer started (1.5s)...");
            _timers.StartOneShot(1.5f, () => Debug.Log("[Phase2Demo] One-shot timer fired."), owner: this);
        }

        private void DemoRepeatingTimer()
        {
            Debug.Log("[Phase2Demo] Repeating timer started (0.5s x 3)...");
            int tick = 0;
            _timers.StartRepeating(0.5f, () => Debug.Log($"[Phase2Demo] Repeating timer tick {++tick}/3."), repeatCount: 3, owner: this);
        }

        private void DemoCountdownTimer()
        {
            Debug.Log("[Phase2Demo] Countdown timer started (5s)...");
            _lastLoggedCountdownSecond = -1;
            _timers.StartCountdown(
                5f,
                onTick: remaining =>
                {
                    int whole = Mathf.CeilToInt(remaining);
                    if (whole != _lastLoggedCountdownSecond)
                    {
                        _lastLoggedCountdownSecond = whole;
                        Debug.Log($"[Phase2Demo] Countdown: {whole}s remaining.");
                    }
                },
                onComplete: () => Debug.Log("[Phase2Demo] Countdown complete."),
                owner: this);
        }

        private void DemoPublishEvent()
        {
            _events.Publish(new DemoPingEvent(UnityEngine.Time.frameCount));
        }

        private void OnDemoPing(DemoPingEvent ping)
        {
            Debug.Log($"[Phase2Demo] Subscriber received DemoPingEvent from frame {ping.SourceFrame}.");
        }

        private void OnSettingChanged(SettingChangedEvent change)
        {
            Debug.Log($"[Phase2Demo] SettingChangedEvent: '{change.Key}'.");
        }

        private void DemoSaveCounter()
        {
            DemoSaveData data = _persistence.Load(SaveKey, SaveVersion, new DemoSaveData());
            data.Counter++;
            _persistence.Save(SaveKey, data, SaveVersion);
            Debug.Log($"[Phase2Demo] Saved counter = {data.Counter} under key '{SaveKey}'.");
        }

        private void DemoLoadCounter()
        {
            DemoSaveData data = _persistence.Load(SaveKey, SaveVersion, new DemoSaveData());
            Debug.Log($"[Phase2Demo] Loaded counter = {data.Counter} (0 means nothing saved yet).");
        }

        private void DemoBumpSetting()
        {
            float current = _settings.Get<float>(VolumeSettingKey);
            float next = current >= 1f ? 0f : Mathf.Min(1f, current + 0.25f);
            _settings.Set(VolumeSettingKey, next);
            _settings.Save();
            Debug.Log($"[Phase2Demo] '{VolumeSettingKey}' set to {next} and saved.");
        }

        private readonly struct DemoPingEvent
        {
            public readonly int SourceFrame;
            public DemoPingEvent(int sourceFrame) => SourceFrame = sourceFrame;
        }

        [Serializable]
        private sealed class DemoSaveData
        {
            public int Counter;
        }
    }
}
