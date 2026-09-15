# GameFramework

A reusable Unity framework intended to be shared across multiple games built on this project's
tooling and target platforms.

## Status

**Phase 3 — Player Experience Foundation.** Phase 0 laid the structural foundation, Phase 1 built
Bootstrap/Services/Logging/GameState/SceneManagement, and Phase 2 added the infrastructure layer
(Time, Timers, Events, Persistence, Settings). Phase 3 adds five reusable player-facing systems on
top of that: Input, Localization, Audio, UI Foundation, and Feedback/Haptics. No progression,
rewards, economy, or game-specific UI exist yet — see [Roadmap](#roadmap).

## Target environment

- Unity `2022.3.62f3`
- C# 9.0-compatible syntax only — do not assume newer .NET or C# language features, or Unity APIs
  introduced after this version. Concretely bit this project already: the 3-argument
  `File.Move(src, dst, overwrite: true)` (.NET Standard 2.1) does **not** compile here despite the
  project's API compatibility level being set to .NET Standard 2.1 — Unity's actual reference
  assemblies are a curated subset, not the full BCL surface. Verify BCL API availability against
  this project directly; don't trust the compatibility-level setting alone.
- Mobile-first (Android/iOS).

## Architecture

Dependency direction is one-way, top to bottom. A lower layer must never reference a higher one:

```text
Game
  ↓
Game Features
  ↓
GameFramework.PlayerSystems   (composition root: registers the five services below)
  ↓
GameFramework.UI  ──────┬──────────────┬──────────────┐
  ↓                     ↓              ↓              ↓
GameFramework.Localization  GameFramework.Audio  GameFramework.Feedback
  ↓                     ↓              ↓              ↓
GameFramework.Input     └──────────────┴──────────────┘
  ↓
GameFramework.Runtime   (Bootstrap, Services, Diagnostics, State, SceneManagement,
  ↓                      Time, Timers, Events, Persistence, Settings)
GameFramework.Core       (Validation, Extensions)
  ↓
Unity / .NET
```

`GameFramework.Core` has no dependencies beyond Unity/.NET and must stay that way.
`GameFramework.Runtime` depends only on `GameFramework.Core`. Phase 3 introduced five new
assemblies — Input, Localization, Audio, Feedback, UI (see [Assemblies](#assemblies)) — specifically
because, unlike Phase 2's modules, they have a real dependency direction worth enforcing at compile
time: `GameFramework.UI` references Localization/Audio/Feedback, `GameFramework.Feedback`
references Audio (to play a preset's cue), and Input/Localization/Audio have no dependencies on
each other or on UI. `GameFramework.PlayerSystems` is the composition root that wires all five into
`GameBootstrapper` (see [Player Systems Bootstrap](#player-systems-bootstrap)); none of Phase 0–2
was modified to add it. Runtime assemblies must never reference Editor assemblies —
`GameFramework.Editor` (added in Phase 3 for one small localization validation menu item) is
Editor-only and referenced by nothing at runtime.

Everything is wired together by `GameBootstrapper`, which now owns eight built-in services:

```text
                                  GameBootstrapper
                                        │
                                        ▼
                                 ServiceRegistry
                                        │
      ┌──────────┬──────────┬──────────┼──────────┬──────────┬──────────┬──────────┐
      ▼          ▼          ▼          ▼          ▼          ▼          ▼          ▼
  Logging    GameState    Scene       Time       Timer      Event   Persistence  Settings
  Service     Service    Service    Service     Service    Service    Service    Service
```

Registration order is load-bearing (see [Services](#services) and [Bootstrap](#bootstrap)):
Timer requires Time to already be initialized, and Settings requires both Persistence and Events
to be. `GameBootstrapper.RegisterServices` registers them in exactly that dependency order:
Logging, GameState, Scene, Time, Timer, Event, Persistence, Settings.

Phase 3's five services are registered by `PlayerSystemsBootstrapper`, a `GameBootstrapper`
subclass (see [Player Systems Bootstrap](#player-systems-bootstrap)) — `GameBootstrapper` itself is
unchanged, since a lower layer must never reference the higher-level assemblies those services live
in.

## Folder structure

```text
Assets/GameFramework/
├── Runtime/
│   ├── Core/                    GameFramework.Core.asmdef
│   │   ├── Validation/          Guard.cs
│   │   └── Extensions/          GameObjectExtensions.cs, ComponentExtensions.cs,
│   │                             TransformExtensions.cs, UnityObjectExtensions.cs
│   ├── GameFramework.Runtime.asmdef
│   ├── AssemblyInfo.cs          InternalsVisibleTo for the Runtime test assemblies
│   ├── Bootstrap/               BootstrapState.cs, GameBootstrapper.cs
│   ├── Services/                IGameService.cs, IServiceRegistry.cs, ServiceRegistry.cs,
│   │                             ServiceNotFoundException.cs, IUpdatableService.cs
│   ├── Diagnostics/             LogLevel.cs, ILoggingService.cs, LoggingService.cs, Log.cs
│   ├── State/                   IGameState.cs, IGameStateService.cs, GameStateService.cs
│   ├── SceneManagement/         SceneLoadMode.cs, ISceneService.cs, SceneService.cs,
│   │                             SceneNotFoundException.cs
│   ├── Time/                    ITimeService.cs, TimeService.cs
│   ├── Timers/                  TimerTimeMode.cs, TimerState.cs, ITimerHandle.cs,
│   │                             ITimerService.cs, TimerService.cs
│   ├── Events/                  IEventSubscription.cs, IEventService.cs, EventService.cs
│   ├── Persistence/              IPersistenceStorage.cs, IPersistenceSerializer.cs,
│   │                             JsonPersistenceSerializer.cs, FilePersistenceStorage.cs,
│   │                             InMemoryPersistenceStorage.cs, ISaveMigration.cs,
│   │                             SaveEnvelope.cs, IPersistenceService.cs, PersistenceService.cs
│   ├── Settings/                SettingDefinition.cs, SettingChangedEvent.cs,
│   │                             ISettingsService.cs, SettingsSnapshot.cs, SettingsService.cs
│   ├── Input/                    GameFramework.Input.asmdef (Phase 3)
│   ├── Localization/             GameFramework.Localization.asmdef (Phase 3)
│   ├── Audio/                    GameFramework.Audio.asmdef (Phase 3)
│   ├── Feedback/                 GameFramework.Feedback.asmdef (Phase 3)
│   ├── UI/                       GameFramework.UI.asmdef (Phase 3)
│   └── PlayerSystems/            GameFramework.PlayerSystems.asmdef (Phase 3 composition root)
├── Editor/
│   ├── GameFramework.Editor.asmdef (Phase 3)
│   └── Localization/             LocalizationTableValidator.cs
├── Tests/
│   ├── Editor/                          GameFramework.Core.Tests.asmdef (EditMode)
│   │   ├── Validation/ , Extensions/
│   │   ├── Runtime/                     GameFramework.Runtime.Tests.asmdef (EditMode)
│   │   │   ├── Services/ , Diagnostics/ , State/ , SceneManagement/
│   │   │   └── Time/ , Timers/ , Events/ , Persistence/ , Settings/
│   │   ├── Input/                       GameFramework.Input.Tests.asmdef (EditMode)
│   │   ├── Localization/                GameFramework.Localization.Tests.asmdef (EditMode)
│   │   ├── Audio/                       GameFramework.Audio.Tests.asmdef (EditMode)
│   │   └── Feedback/                    GameFramework.Feedback.Tests.asmdef (EditMode)
│   └── Runtime/                         GameFramework.Core.Tests.Runtime.asmdef (PlayMode)
│       ├── Extensions/
│       ├── Framework/                   GameFramework.Runtime.Tests.Runtime.asmdef (PlayMode)
│       │   └── Bootstrap/
│       ├── Audio/                       GameFramework.Audio.Tests.Runtime.asmdef (PlayMode)
│       ├── UI/                          GameFramework.UI.Tests.Runtime.asmdef (PlayMode)
│       └── PlayerSystems/               GameFramework.PlayerSystems.Tests.Runtime.asmdef (PlayMode)
└── Documentation/
    └── Framework.md                     (this file)
```

`Editor/` and `Samples/` folders (under `GameFramework/`) are still not created — no editor
tooling exists, and every Phase 2 API is exercised in its own tests, so nothing needed a sample.

## Assemblies

| Assembly | Location | References | Purpose |
|---|---|---|---|
| `GameFramework.Core` | `Runtime/Core` | none | Framework-level utilities with no game or higher-framework dependencies. |
| `GameFramework.Runtime` | `Runtime/` (root, excluding `Core/`) | `GameFramework.Core` | Bootstrap, services, logging, game state, scene management, time, timers, events, persistence, settings. |
| `GameFramework.Core.Tests` | `Tests/Editor` | `GameFramework.Core`, TestRunner | EditMode tests for `GameFramework.Core`. |
| `GameFramework.Runtime.Tests` | `Tests/Editor/Runtime` | `GameFramework.Core`, `GameFramework.Runtime`, TestRunner | EditMode tests for `GameFramework.Runtime` (everything that doesn't need `Awake`/`DontDestroyOnLoad`). |
| `GameFramework.Core.Tests.Runtime` | `Tests/Runtime` | `GameFramework.Core`, TestRunner | PlayMode tests for the subset of `GameFramework.Core` that Edit Mode cannot exercise. |
| `GameFramework.Runtime.Tests.Runtime` | `Tests/Runtime/Framework` | `GameFramework.Core`, `GameFramework.Runtime`, TestRunner | PlayMode tests for `GameBootstrapper` (needs real `Awake`/`DontDestroyOnLoad`/`Update`). |
| `GameFramework.Input` | `Runtime/Input` | `GameFramework.Core`, `GameFramework.Runtime` | Logical input actions, contexts, pointer/touch, gestures. |
| `GameFramework.Localization` | `Runtime/Localization` | `GameFramework.Core`, `GameFramework.Runtime`, `Unity.TextMeshPro` | Language tables, lookup/fallback, language-specific fonts/assets. |
| `GameFramework.Audio` | `Runtime/Audio` | `GameFramework.Core`, `GameFramework.Runtime` | Pooled playback, categories/volumes, cues, music crossfade. |
| `GameFramework.Feedback` | `Runtime/Feedback` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Audio` | Haptics abstraction, presets coordinating haptics + audio. |
| `GameFramework.UI` | `Runtime/UI` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Localization`, `GameFramework.Audio`, `GameFramework.Feedback`, `Unity.TextMeshPro` | Layered canvases, screen stack, popups/modals, localized UI components. |
| `GameFramework.PlayerSystems` | `Runtime/PlayerSystems` | all of the above + `GameFramework.Runtime` | Composition root: `PlayerSystemsBootstrapper`. |
| `GameFramework.Editor` | `Editor/` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Localization` | Editor-only. Localization table validation menu item. |
| `GameFramework.Input.Tests` / `.Localization.Tests` / `.Audio.Tests` / `.Feedback.Tests` | `Tests/Editor/<System>` | matching runtime assembly + Core/Runtime, TestRunner | EditMode tests for each system's pure logic. |
| `GameFramework.Audio.Tests.Runtime` / `.UI.Tests.Runtime` / `.PlayerSystems.Tests.Runtime` | `Tests/Runtime/<System>` | matching runtime assembly + Core/Runtime, TestRunner | PlayMode tests for behavior that genuinely needs a running engine (real `AudioSource` playback, `AddComponent`-able UI test doubles, `GameBootstrapper.Awake`). |

Phase 2 added no new assembly (its five modules share no dependency boundary worth enforcing).
Phase 3 is the opposite case: UI's dependency on Localization/Audio/Feedback (and Feedback's on
Audio) is exactly the kind of one-way relationship an assembly boundary makes a compile error to
violate, so each of the five systems — plus the `PlayerSystems` composition root and one small
`Editor` assembly — got its own `.asmdef`. `GameFramework.UI.Tests` intentionally does **not**
exist as an EditMode assembly: `UIScreen`/`UIPopup` test doubles are MonoBehaviours, and
`AddComponent` refuses a script whose declaring assembly is Editor-only (`includePlatforms:
["Editor"]`) — discovered during Phase 3 development when EditMode `UIServiceTests` intermittently
returned null from `AddComponent`, not because the framework had a bug, but because the *test*
assembly was the wrong kind. All `UIScreen`/`UIPopup`-touching tests live in
`GameFramework.UI.Tests.Runtime` (PlayMode) instead.

## Namespace conventions

Block-style namespaces only, never file-scoped. Current namespaces:

- `GameFramework.Core.Validation`, `GameFramework.Core.Extensions` — see [Phase 0](#phase-0--core-utilities).
- `GameFramework.Runtime.Bootstrap` — `GameBootstrapper`, `BootstrapState`.
- `GameFramework.Runtime.Services` — `IGameService`, `IServiceRegistry`, `ServiceRegistry`, `ServiceNotFoundException`, `IUpdatableService`.
- `GameFramework.Runtime.Diagnostics` — `LogLevel`, `ILoggingService`, `LoggingService`, the static `Log` facade.
- `GameFramework.Runtime.State` — `IGameState`, `IGameStateService`, `GameStateService`.
- `GameFramework.Runtime.SceneManagement` — `SceneLoadMode`, `ISceneService`, `SceneService`, `SceneNotFoundException`.
- `GameFramework.Runtime.Time` — `ITimeService`, `TimeService`.
- `GameFramework.Runtime.Timers` — `TimerTimeMode`, `TimerState`, `ITimerHandle`, `ITimerService`, `TimerService`.
- `GameFramework.Runtime.Events` — `IEventSubscription`, `IEventService`, `EventService`.
- `GameFramework.Runtime.Persistence` — `IPersistenceStorage`, `IPersistenceSerializer`, `JsonPersistenceSerializer`, `FilePersistenceStorage`, `InMemoryPersistenceStorage`, `ISaveMigration`, `IPersistenceService`, `PersistenceService`.
- `GameFramework.Runtime.Settings` — `SettingDefinition<T>`, `SettingChangedEvent`, `ISettingsService`, `SettingsService`.
- `GameFramework.Input` — `IInputService`, `InputService`, `InputActionMapAsset`, `InputActionBindingDefinition`, `InputActionState`, `PointerState`, `InputContextDefinition`, `PointerGestureRecognizer`, `InputActionBuffer`.
- `GameFramework.Localization` — `ILocalizationService`, `LocalizationService`, `LocalizationTableAsset`, `LocalizationConfigAsset`, `LanguageInfo`, `LanguageChangedEvent`.
- `GameFramework.Audio` — `IAudioService`, `AudioService`, `AudioCueAsset`, `AudioCategory`, `IAudioHandle`, `NullAudioHandle`, `IRandomSource`.
- `GameFramework.Feedback` — `IFeedbackService`, `FeedbackService`, `HapticStrength`, `IHapticProvider`, `FeedbackPresetAsset`.
- `GameFramework.UI` — `IUIService`, `UIService`, `UIScreen`, `UIPopup`, `UILayer`, `LocalizedTextBase`/`LocalizedTMPText`/`LocalizedText`/`LocalizedImage`, `UIButtonFeedback`.
- `GameFramework.PlayerSystems` — `PlayerSystemsBootstrapper`.
- `GameFramework.Editor.Localization` — `LocalizationTableValidator` (Editor-only).

A naming note: `GameFramework.Runtime.Time` and `GameFramework.Runtime.Timers` share a word with
`UnityEngine.Time`/nothing, respectively, but that hasn't caused the ambiguity you might expect —
see [Bootstrap](#bootstrap)'s note on the one real collision Phase 1 hit (`Log` the class vs `Log`
the method) for how that class of problem actually arises and gets fixed.

## Bootstrap

`GameBootstrapper` (`GameFramework.Runtime.Bootstrap`) is the framework's entry point. Put one
instance on a `GameObject` in a startup scene. Its lifecycle is linear and one-way:

```text
Created → Initializing → Ready → ShuttingDown → Shutdown
```

`Awake()` claims a static singleton, calls `DontDestroyOnLoad`, then walks straight from `Created`
to `Ready`: `RegisterServices` followed by calling `Initialize(registry)` on each registered
service in registration order. A second `GameBootstrapper` anywhere in any scene logs a warning
and destroys its own `GameObject` in `Awake` rather than replacing the first instance.

```csharp
public class MyGameBootstrapper : GameBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // keep all eight built-in services, in their required order
        // registry.Register<IMyGameService>(new MyGameService());
    }
}
```

`Shutdown()` (called automatically from `OnApplicationQuit`/`OnDestroy`, or manually) shuts down
every service in reverse registration order and clears the registry. It is idempotent. Calling
`Initialize` a second time throws — that is a setup bug, not a teardown race.

**Phase 2 addition — per-frame ticking.** `GameBootstrapper.Update()` calls `Tick()` on every
initialized service that implements `IUpdatableService` (currently just `TimerService`), in
registration order, and only while `State == Ready`. This is a small, internal, deliberately
minimal mechanism — not a general-purpose scheduling/ordering/tick-group system. If a future phase
needs real scheduling (tick groups, priorities, fixed-step vs. variable-step ordering across many
systems), that's new design work, not an extension of this.

## Services

`IServiceRegistry` is a `Type → instance` lookup (`Register`, `Get`, `TryGet`, `IsRegistered`) —
one dictionary hit, no reflection scanning, no scene search. `Get<TService>` only succeeds once
that service has actually been initialized, which is what makes cross-service dependencies safe:
a service can call `registry.Get<TOther>()` from inside its own `Initialize` and it will succeed if
`TOther` was registered (and therefore already initialized) earlier in the sequence, or throw
immediately and clearly if not. This is exactly how Phase 2 wires its own dependencies — see
[Architecture](#architecture) for the required order.

```csharp
public sealed class SettingsService : ISettingsService
{
    public void Initialize(IServiceRegistry registry)
    {
        _persistence = registry.Get<IPersistenceService>(); // safe: registered/initialized earlier
        _events = registry.Get<IEventService>();
    }
}
```

## Logging

Unchanged from Phase 1. `Log.Info/Warning/Error/...` is a safe no-op when unbound; every Phase 2
service that can usefully log resolves `ILoggingService` via `registry.TryGet` (soft — none of them
*require* logging to function) and logs exceptions from user callbacks (timer/event callbacks,
persistence/migration failures) through it rather than letting them escape.

## Game State / Scene Management

Unchanged from Phase 1.

## Time

`ITimeService` wraps every `UnityEngine.Time` property this framework needs, with names that make
scaled vs. unscaled unambiguous: `ScaledDeltaTime`/`UnscaledDeltaTime`,
`FixedDeltaTime`/`UnscaledFixedDeltaTime`, `ScaledTime`/`UnscaledTime`, and `Realtime`
(`realtimeSinceStartup` — keeps advancing even while paused). Nothing outside this service should
write `UnityEngine.Time.timeScale` directly.

```csharp
ITimeService time = GameBootstrapper.Instance.Services.Get<ITimeService>();

time.Pause();          // e.g. opening a pause menu
time.SetTimeScale(0.3f); // e.g. a slow-motion ability — remembered, applied once fully resumed
time.Resume();          // releases this Pause() specifically
```

**Time-scale ownership.** `Pause`/`Resume` are reference-counted: two independent systems (a pause
menu and a cutscene, say) can each call them without one's `Resume` undoing the other's still-active
`Pause`. `SetTimeScale` is the one place a *non-zero* scale (slow motion, etc.) is requested — the
most recent call wins there, which is safe specifically because the dangerous "last caller wins"
case (one `Resume` silently cancelling an unrelated `Pause`) is the case the reference count exists
to prevent. While any `Pause` is outstanding, the engine's actual time scale is forced to 0
regardless of what `SetTimeScale` last requested; that requested value is restored once the last
matching `Resume` runs. No priority stack, no ownership tokens — this was kept intentionally simple.

**Pause and scaled/unscaled** aren't the framework's decision: `Pause()` only ever zeroes
`Time.timeScale`, and Unity's own unscaled properties are unaffected by `timeScale` by definition —
so an `Unscaled`-mode consumer (UI, a pause menu, a tutorial overlay) keeps moving through a pause
for free, with no framework-side special-casing needed.

## Timers

`ITimerService` is built on `ITimeService` — it never reads `UnityEngine.Time` directly, and
advances every active timer once per frame from `GameBootstrapper.Update()` via
`IUpdatableService.Tick()` (see [Bootstrap](#bootstrap)). Four factory methods cover the required
timer shapes, all backed by one internal entry type — they exist as separate names for their
different callback shapes and call-site intent, not as separate implementations:

```csharp
ITimerService timers = GameBootstrapper.Instance.Services.Get<ITimerService>();

timers.StartOneShot(1.5f, () => Log.Info("Timers", "Done."));
timers.StartDelay(0.5f, DoSomethingOnce);                 // alias for StartOneShot
timers.StartRepeating(1f, TickEverySecond, repeatCount: 5); // 0 repeats = indefinite
timers.StartCountdown(10f, remaining => UpdateHud(remaining), OnCountdownComplete);

ITimerHandle handle = timers.StartOneShot(3f, Explode, owner: gameObject);
handle.Cancel(); // or handle.Pause() / handle.Resume()
```

`StartCountdown` and `StartRepeating` are the one place two genuinely different tick semantics
exist: Repeating's callback fires once per elapsed *interval*; Countdown's `onTick` fires once per
*frame* with the remaining time, for a smooth UI readout.

**Owner lifetime.** Pass a `UnityEngine.Object` as `owner` to tie a timer to that object's
lifetime — once it's destroyed, the timer is silently cancelled (no callback fires) on the next
tick rather than invoking a callback that might touch a destroyed object. This reuses Phase 0's
`UnityObjectExtensions.IsNullOrDestroyed` — the exact case that utility exists for. Omit `owner`
for an application-lifetime timer.

**Timing modes.** `Scaled` (stops during `ITimeService.Pause()`) and `Unscaled` (keeps running).
No third mode: `Unscaled` already covers "must survive a pause," since `Pause()` only zeroes time
scale, which unscaled delta time ignores by definition.

**Callback safety.** A timer callback can cancel itself, start another timer, or throw, and none of
these corrupt the update loop: state is re-checked after every callback invocation before deciding
whether to remove/repeat an entry; a newly-started timer is appended and picked up on a later
`Tick()`, never the one in progress; and an exception is caught, logged (category `"Timers"`), and
does not stop the remaining timers in that tick from firing.

**Performance.** `Tick()` iterates a single `List<TimerEntry>` backward (safe in-place removal, no
LINQ, no per-tick allocation) — the one place a closure would otherwise have been allocated every
frame (the progress-callback path) was rewritten to avoid it; see `TimerService.InvokeFire`/the
inline `OnProgress` call in `Tick()`.

## Events

`IEventService` is strongly-typed publish/subscribe, not a replacement for ordinary method calls —
reach for it only when the publisher genuinely shouldn't know who's listening. Payloads are plain
game-defined types (prefer a `readonly struct` for high-frequency events, to avoid allocating the
payload itself):

```csharp
public readonly struct LevelCompletedEvent
{
    public readonly int LevelId;
    public LevelCompletedEvent(int levelId) => LevelId = levelId;
}

IEventService events = GameBootstrapper.Instance.Services.Get<IEventService>();

IEventSubscription subscription = events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
// ... later, e.g. in OnDestroy: ...
subscription.Dispose(); // the recommended cleanup pattern — can't be mismatched with the wrong delegate

events.Publish(new LevelCompletedEvent(levelId: 4));
```

**Lifecycle safety.** Subscribing the same delegate twice is a no-op (it only runs once per
publish). `Publish` snapshots the current subscriber list into a pooled `Delegate[]`
(`System.Buffers.ArrayPool<Delegate>.Shared` — confirmed available under this project's actual API
surface) before iterating, so Subscribe/Unsubscribe calls from inside a handler — including a
handler unsubscribing itself, or a nested `Publish` of the same event type — never mutate the
sequence currently being iterated, and never allocate in steady state. If a subscriber throws, the
exception is logged (category `"Events"`) and the remaining subscribers still run.

**Scope.** Global only — no scoped/local event bus. A game that needs a narrower-scoped pub/sub can
trivially build one itself (or just use a plain C# event) where that's genuinely needed; the
framework doesn't generalize that until there's a concrete case for it.

## Persistence

```text
Game Data  →  IPersistenceService  →  IPersistenceSerializer  →  IPersistenceStorage
```

Each layer is independently swappable. Phase 2 ships one serializer
(`JsonPersistenceSerializer`, via `UnityEngine.JsonUtility` — inherits its constraints:
`[Serializable]` types with public/serialized fields only, no properties/interfaces/polymorphism —
exactly the shape of the small dedicated data classes, e.g. `PlayerProgressData`, this framework
already asks save data to be) and two storage backends: `FilePersistenceStorage` (production —
one file per key under `Application.persistentDataPath/Saves`) and `InMemoryPersistenceStorage`
(process-memory only, built specifically so tests run against isolated, deterministic storage
instead of the developer's real save files).

```csharp
IPersistenceService persistence = GameBootstrapper.Instance.Services.Get<IPersistenceService>();

persistence.Save("PlayerProgress", new PlayerProgressData { Level = 4 }, version: 2);

PlayerProgressData data = persistence.Load("PlayerProgress", currentVersion: 2, new PlayerProgressData());
```

`Save`/`Load` are synchronous — the underlying `JsonUtility` and file I/O are synchronous, so there
is no `SaveAsync`/`LoadAsync` that would only be pretending to be asynchronous.

**Atomic-ish writes.** `FilePersistenceStorage` writes to a `.tmp` file, then renames it into
place, so a crash mid-write leaves either the previous file intact or the new one, never a
half-written one. The obvious `File.Move(src, dst, overwrite: true)` one-call version doesn't
compile in this project (see [Target environment](#target-environment)), so an existing target is
deleted immediately before a plain two-argument `File.Move` — leaving a brief window where neither
file exists; a crash exactly then is the one scenario this doesn't fully protect against.

**Versioning and migration.** Every save is wrapped in an envelope carrying a version number
alongside the payload — stored as the payload's *own already-serialized JSON text* (double-encoded
as a string), not a nested JSON object. That trade-off (slightly less readable raw output, one
extra escape pass) is what lets the version be read without first knowing the payload's shape,
since `JsonUtility` has no generic "parse into an untyped tree" mode. A migration is a small,
pure, testable function of raw JSON text:

```csharp
public sealed class PlayerProgressV1ToV2 : ISaveMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public string Migrate(string payload)
    {
        var old = JsonUtility.FromJson<PlayerProgressDataV1>(payload);
        return JsonUtility.ToJson(new PlayerProgressData { Level = old.Level, NewField = "default" });
    }
}

persistence.RegisterMigration("PlayerProgress", new PlayerProgressV1ToV2());
```

`Load` walks the registered chain for a key, one `FromVersion → ToVersion` step at a time, until it
reaches the requested current version. If any version gap in that chain has no registered
migration, or any step (including the final deserialize) throws, the failure is logged and the
caller's `defaultValue` is returned — the corrupt/unmigratable raw data is preserved under
`"{key}.corrupt"` rather than discarded, so nothing is silently deleted.

## Settings

Built entirely on Persistence and Events — it implements no persistence mechanism of its own, and
change notifications go through `IEventService` rather than a bespoke callback.

```csharp
ISettingsService settings = GameBootstrapper.Instance.Services.Get<ISettingsService>();

settings.Register(new SettingDefinition<int>("MasterVolume", 100, v => v is >= 0 and <= 100, category: "Audio"));
settings.Register(new SettingDefinition<bool>("Vibration", true, category: "Gameplay"));

int volume = settings.Get<int>("MasterVolume");
settings.Set("MasterVolume", 80); // validated; a no-op (and no event) if the value is unchanged

events.Subscribe<SettingChangedEvent>(e => Log.Info("Settings", $"{e.Key} changed"));

settings.Save(); // explicit — Settings never auto-saves on every Set
```

Every setting is registered with a default (validated against its own validator at Register time)
before it can be read or written; `Get<TValue>`/`Set<TValue>` are strongly typed (supported types:
`bool`, `int`, `float`, `string`, enum) even though values are transported to storage as strings
internally — `Dictionary` isn't `JsonUtility`-serializable, so the persisted shape is parallel
`Keys`/`Values` lists, an implementation detail the public API never exposes.

**Load behavior** (also run automatically once at the end of `Initialize`, so persisted settings
are in effect from the moment Bootstrap reaches `Ready`): a stored key no longer registered in this
build is silently ignored; a stored value that fails to parse or fails its validator is discarded
in favor of the current (default) value — corrupted or stale settings data never crashes startup
or introduces an invalid live value.

**Save policy.** `Set` never writes to disk by itself — only an explicit `Save()` does, avoiding
the "save on every property access" anti-pattern. The one automatic write: `Shutdown()` saves if
anything changed since the last `Save`/`Load` (a single, conservative, dirty-flag-gated policy, not
aggressive auto-saving), so a player's changed settings aren't lost just because they never pressed
an explicit "Apply."

## Player Systems Bootstrap

`PlayerSystemsBootstrapper` (`GameFramework.PlayerSystems`) is a `GameBootstrapper` subclass that
registers Phase 3's five services on top of the eight Phase 1/2 ones, using exactly the extension
point `GameBootstrapper` has documented since Phase 1 ([Bootstrap](#bootstrap)):

```csharp
public class MyGameBootstrapper : PlayerSystemsBootstrapper // instead of : GameBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1/2's eight services + Phase 3's five
        // registry.Register<IMyGameService>(new MyGameService());
    }
}
```

A game that doesn't need to add its own services can put `PlayerSystemsBootstrapper` directly on
its bootstrap `GameObject` with no subclass at all. Registration order —
Input → Localization → Audio → UI → Feedback — matters only where a real dependency exists: UI's
`Initialize` doesn't reach into Localization/Audio/Feedback (those are resolved lazily, per call,
by the localized-UI components and `UIButtonFeedback`), so the order mainly documents intent; the
one load-bearing rule is that all five run after Settings/Persistence/Events (Phase 1/2), since
Localization, Audio, and Feedback all register their own settings during `Initialize`.

## Input

`IInputService` separates physical input from game logic: Physical Input → Input Mapping (an
`InputActionMapAsset` a game authors) → Logical Action → Game Feature. State is sampled once per
frame via `IUpdatableService.Tick` (the same mechanism `TimerService` uses) and cached; reading an
action is a pure cache lookup, never a device query.

```csharp
IInputService input = GameBootstrapper.Instance.Services.Get<IInputService>();
input.RegisterActionMap(myGameActionMap); // an InputActionMapAsset the game authored

if (input.GetButtonDown("Jump")) { /* ... */ }
Vector2 move = input.GetVector2("Move");
```

**Devices.** Built on `UnityEngine.Input` (the legacy manager) rather than the newer Input System
package, which this project doesn't have installed — legacy default axes like `"Horizontal"` and
`"Fire1"` already blend keyboard and joystick with no Project Settings changes required. Real
sampling goes through `IInputSampler` (`UnityInputSampler` in production); tests inject a fake, so
button/axis/touch/pointer-over-UI logic is deterministic without a live device.

**Contexts.** `PushContext`/`PopContext` maintain a stack; only the top context's allowed actions
are enabled, and `GetActionState`/`GetButtonDown`/etc. enforce this automatically — a popup pushing
a restricted context means every caller reading "Jump" sees no input, with no per-caller
`if (popupOpen)` check anywhere in gameplay code. A base "Gameplay" (allow-all) context always
remains on the stack; `PopContext` on it alone is a no-op (logged), never an empty-stack crash.

**Touch/pointer.** `Pointer` is the primary pointer (first active touch, else the mouse) in
screen-space pixels; `ActiveTouches` covers multi-touch. `PointerGestureRecognizer` (tap/long-press/
drag/swipe) and `InputActionBuffer` (short-lived input buffering) are both deliberately *not* part
of `IInputService` — they're opt-in plain C# helpers a feature instantiates itself and feeds every
frame, kept out of the core service so gesture recognition and buffering stay optional per section
12/15 of the Phase 3 brief rather than mandatory overhead.

## Localization

Key → Localization Provider → Localized Value, with zero translations shipped by the framework
itself — a game supplies a `LocalizationConfigAsset` (a `DefaultLanguageCode` plus one
`LocalizationTableAsset` per language).

```csharp
ILocalizationService localization = GameBootstrapper.Instance.Services.Get<ILocalizationService>();
localization.LoadConfig(myGameLocalizationConfig);

string play = localization.GetString("UI.Play");
localization.SetLanguage("fr"); // persisted, and publishes LanguageChangedEvent
```

**Persistence.** The active language is a normal `Settings.ISettingsService` string setting
(`"Localization.Language"`), not a bespoke save path — it goes through the same save file and
`SettingChangedEvent`-adjacent plumbing (a dedicated `LanguageChangedEvent` is published
separately, since "language changed" is a distinct, more specific concept UI code wants to
subscribe to directly). Because the setting's valid-value validator depends on which languages
`LoadConfig` actually supplies, it's registered there rather than in `Initialize` — and since
`Settings.Initialize`'s own automatic `Load()` already ran by the time `LoadConfig` is called,
`LoadConfig` explicitly re-calls `Settings.Load()` afterward to pick up a previously-persisted
selection.

**Fallback.** `GetString`/`TryGetString`/`Format` check the current language's table, then the
default language's, and only then fall back to a marker: `"[Missing Localization] {key}"` in debug
builds (`Debug.isDebugBuild`), or the raw key in release builds. Each missing key is logged once
(category `"Localization"`), not every call, to stay useful without spamming the console.

**Assets and fonts.** `LocalizationTableAsset` carries both string entries and
`LocalizationAssetEntry` (key → `UnityEngine.Object`, e.g. a per-language sprite) plus an optional
`TMP_FontAsset` override for languages with different glyph requirements — `TryGetAsset<TAsset>`
and `CurrentFontAsset` expose these. **RTL is not implemented** — no direction/mirroring/shaping
logic exists; this is called out here rather than papered over with an unused toggle (a project
inspection judgment call for a future phase, per the Phase 3 brief's explicit "don't claim RTL
support unless implemented").

**Editor validation.** `GameFramework/Localization/Validate Selected Config` (a menu item in the
Editor-only `GameFramework.Editor` assembly) flags duplicate/empty keys within a table and key
parity gaps between a table and the default language's — a key present in English but missing in
French would otherwise silently fall back at runtime with no authoring-time signal.

## Audio

Game Feature → `IAudioService` → `AudioCueAsset` (configuration) → pooled `AudioSource` playback.
`AudioService` owns a fixed-size voice pool plus two dedicated music slots, all under its own
`DontDestroyOnLoad` root created in `Initialize` — no scene setup required, the same
self-contained pattern `UIService` also uses.

```csharp
IAudioService audio = GameBootstrapper.Instance.Services.Get<IAudioService>();

IAudioHandle handle = audio.Play(footstepCue); // NullAudioHandle (never null) if suppressed
audio.PlayMusic(menuThemeCue, crossfadeSeconds: 1f);
audio.SetCategoryVolume(AudioCategory.Music, 0.6f);
```

**Categories and volume.** `AudioCategory` is `Music`/`Sfx`/`Ui`/`Voice`; `Master` is a separate
multiplier (`AudioService.MasterVolume`), not a category a cue belongs to. All five volumes are
normal `Settings.ISettingsService` float settings (`"Audio.MasterVolume"`, etc., clamped to
`[0, 1]`), registered in `Initialize` with the same re-`Load()`-after-registering pattern
Localization uses. A voice reads its effective volume live every `Tick()`
(`AudioVoice.ComputeTargetVolume` → `AudioService.GetEffectiveCategoryVolume`) rather than caching
it, so a volume change applies to already-playing sounds immediately — no restart required.

**Configuration vs. playback.** `AudioCueAsset` is pure config: a clip list (randomly selected when
there's more than one, via the pure, unit-testable `AudioCueSampler` + an injectable
`IRandomSource`), volume/pitch ranges, loop, fade in/out, and two limiting knobs —
`MaxConcurrentInstances` (0 = unlimited) and `MinRetriggerInterval` (0 = none), both checked before
a voice is even dequeued. `IAudioHandle` is the only surface game code sees for a playing sound
(`Stop`/`Pause`/`Resume`/`SetVolume`/`SetPitch`/`IsPlaying`) — the underlying `AudioVoice`
(internal, wraps one `AudioSource`) is never exposed.

**Reclaim timing.** A voice finishing (fade-out complete, or a non-looping clip ending) notifies
`AudioService` synchronously via `AudioVoice.Reclaim`, not deferred to the next `Tick` — otherwise
`handle.Stop()` immediately followed by `Play(sameCue)` in the same frame would be incorrectly
blocked by `MaxConcurrentInstances` until the following frame. This was caught by a failing test
during development (`StopAll_ImmediatelyFreesVoices_AllowingReplay`), not designed in from the
start — see the test for the exact scenario.

**Application focus.** `AudioService.PauseOnApplicationPause` (default `true`) pauses every active
voice on `OnApplicationPause(true)` and resumes on `false`, via a small internal MonoBehaviour hook
(`AudioApplicationLifecycleHook`) that forwards the Unity callback to the service — a plain C#
service class cannot receive Unity lifecycle callbacks directly.

**Play Mode only for real playback.** `AudioSource.Play()` on a real clip is only reliably driven
by Unity's audio engine while actually running; `AudioVoice` guards the real `Play()`/
`DontDestroyOnLoad` calls behind `Application.isPlaying` so the service's pooling/limiting logic
stays testable from EditMode without touching a real `AudioSource` — see
[Testing](#testing) for how this splits EditMode vs. PlayMode coverage.

## UI Foundation

Not a complete UI framework — reusable building blocks only: layered canvases, a stack-navigable
screen flow, and a popup/modal foundation with automatic input blocking. `UIService` builds its own
canvas hierarchy (one `Canvas` + `CanvasScaler` + `GraphicRaycaster` per `UILayer`, sorted by the
layer's own underlying int) under a `DontDestroyOnLoad` root in `Initialize`, and creates an
`EventSystem` if the scene has none — a game gets working UI infrastructure with zero scene setup.

```csharp
IUIService ui = GameBootstrapper.Instance.Services.Get<IUIService>();

MainMenuScreen menu = ui.OpenScreen(mainMenuScreenPrefab); // pushed onto the screen stack
ConfirmPopup popup = ui.OpenPopup(confirmPopupPrefab);      // modal by default
popup.Close(UIPopupResult.Confirmed);
```

**Screens.** `UIScreen` lifecycle is `Closed → Opened → (Hidden while covered) → Opened → Closed`.
`OpenScreen<T>` hides the current top (if any) before pushing the new one; `CloseTopScreen`/
`screen.Close()` pop and destroy the top, then show whatever is now on top again. `CloseScreen`
only closes the actual current top — closing an arbitrary non-top entry isn't supported, matching
the Phase 3 brief's "don't build a complicated navigation framework yet." Both close paths are
no-ops (not errors) on an empty stack.

**Popups and modal blocking.** `UIPopup.IsModal` (default `true`) determines its layer (`Modal` vs.
`Popup`) and whether `UIService` positions a single reusable, fully-transparent
`ModalBlocker` `Image` directly behind it — the blocker's own `GraphicRaycaster` hit stops any
raycast from reaching layers underneath, so gameplay/other UI can't accidentally receive a tap
meant for the modal. Closing a modal re-targets the blocker at whichever modal (if any) is now
topmost, so a modal stack of two behaves correctly when the top one closes. This is pure UGUI
raycasting — no coupling to `IInputService` at all, keeping Input → UI a non-dependency as the
Phase 3 architecture requires.

**Localized/audio/haptic UI components.** `LocalizedTextBase` (+ `LocalizedTMPText`/`LocalizedText`
subclasses) and `LocalizedImage` refresh from `ILocalizationService` on enable and on
`LanguageChangedEvent` — a screen never polls the current language itself. `UIButtonFeedback` is an
opt-in component with two nullable fields (an `AudioCueAsset`, a `FeedbackPresetAsset`) — feedback
is never added to a button unless a designer explicitly wires one in.

**Animation hook.** `UIScreen`/`UIPopup` expose `OnOpened`/`OnHidden`/`OnShown`/`OnClosed` virtual
methods a subclass can override to kick off its own `StartCoroutine`-driven animation — this is the
"lightweight reusable animation hook" the Phase 3 brief calls for; there is no tweening
engine/interface `UIService` itself drives, since nothing yet needs one.

**PlayMode-only testing.** `UIScreen`/`UIPopup` test doubles are MonoBehaviours, and Unity's
`AddComponent` refuses a script whose declaring assembly only targets the Editor — so
`UIServiceTests` lives in `GameFramework.UI.Tests.Runtime` (PlayMode), not an EditMode assembly.
See [Assemblies](#assemblies) for how this was discovered.

## Feedback / Haptics

A small coordinator, not a duplicate of Audio: `IFeedbackService.TriggerHaptic` calls through to an
`IHapticProvider` (`MobileHapticProvider` on `Application.isMobilePlatform`, `NoOpHapticProvider`
everywhere else — Editor/desktop fail safely rather than erroring), and `TriggerPreset` optionally
also plays a cue through `IAudioService` (a soft dependency via `registry.TryGet`, so a
`FeedbackPresetAsset` with an audio cue just logs a warning instead of throwing if no
`IAudioService` happens to be registered).

```csharp
IFeedbackService feedback = GameBootstrapper.Instance.Services.Get<IFeedbackService>();

feedback.TriggerHaptic(HapticStrength.Light);   // no-ops if the player disabled haptics
feedback.TriggerPreset(rewardFeedbackPreset);    // haptic + optional audio cue, one call
```

**Settings.** `"Feedback.HapticsEnabled"` (default `true`) is a normal boolean setting;
`TriggerHaptic` no-ops without it (or without the platform even supporting haptics) — every game
feature that calls it gets this respected automatically, with no per-call-site check needed.

**Platform limitation, stated plainly.** `MobileHapticProvider` is backed by
`Handheld.Vibrate()` — the only haptic API Unity ships without a third-party native plugin. It has
no amplitude/duration parameter, so every `HapticStrength` produces an identical physical buzz; a
game that needs true per-strength haptics must integrate a native plugin and supply its own
`IHapticProvider` (the seam this interface exists for). This is documented as a real platform
constraint, not glossed over as full haptic support.

## Testing

- Everything in Phase 2 that doesn't need Unity's per-frame lifecycle is EditMode-tested,
  including `TimerService`: its `Tick()` is a plain method, so tests inject a fake `ITimeService`
  and call `Tick()` directly with controlled deltas — fully deterministic, no real-time waiting.
- The one Phase 2 PlayMode test (`GameBootstrapperPhase2PlayModeTests.Update_TicksTimerService...`)
  exists because it's the only way to verify `GameBootstrapper.Update()` itself actually drives
  `IUpdatableService.Tick()` — everything about *timer behavior itself* is already covered
  deterministically elsewhere; this one confirms the wiring, with a bounded real-time wait as the
  necessary exception to "don't rely on real-time waiting."
- `FilePersistenceStorageTests` exercises the real file-I/O path against a temp directory (cleaned
  up in `TearDown`), separately from `PersistenceServiceTests`, which uses
  `InMemoryPersistenceStorage` for fast, isolated, deterministic coverage of save/load/versioning/
  migration/corruption logic — never the developer's real save files.
- `ServiceRegistry`'s internal orchestration members are exposed to the Runtime test assemblies via
  `InternalsVisibleTo` (`AssemblyInfo.cs`, from Phase 1) — Phase 2's tests reuse this to build
  minimal `ServiceRegistry` instances with a dependency pre-registered-and-initialized, exactly the
  pattern `GameBootstrapper` itself uses.
- Phase 3 test assemblies (`GameFramework.Localization.Tests`, `.Audio.Tests`, `.Feedback.Tests`,
  `.UI.Tests.Runtime`) reuse the same `BuildInitializedRegistry` pattern, so `GameFramework.Runtime`'s
  `AssemblyInfo.cs` grants them `InternalsVisibleTo` too.
- Each Phase 3 system isolates its one Unity-API seam behind a fake for deterministic EditMode
  coverage: Input via `IInputSampler`, Audio's clip/volume/pitch randomization via `IRandomSource`,
  Feedback's platform call via `IHapticProvider`. This is the same shape as Phase 2's fake
  `ITimeService` for `TimerServiceTests`.
- Real engine behavior is verified in PlayMode where an EditMode fake would either be untestable or
  misleading: `AudioServicePlayModeTests` confirms `AudioSource` playback and fades actually happen
  (not just that the pooling/limiting logic around them is correct), `UIServiceTests` (PlayMode-only
  — see [Assemblies](#assemblies)) confirms real `GameObject`/`Canvas`/`Destroy` behavior, and
  `PlayerSystemsBootstrapperPlayModeTests` confirms all five Phase 3 services actually initialize
  together through `GameBootstrapper.Awake`, alongside Phase 1/2's eight.

## Phase 0 — Core utilities

### `GameFramework.Core.Validation.Guard`

Argument/precondition checks for public API boundaries (`NotNull`, `NotNullOrEmpty`, `InRange`,
`IsTrue`). Throws immediately with the parameter name. Not intended for per-frame hot paths.

`Guard.NotNull<T>` only detects a true C# null reference. For a `UnityEngine.Object` value that
may have been destroyed, use `UnityObjectExtensions.IsNullOrDestroyed` instead/in addition — see
below for why.

### `GameFramework.Core.Extensions`

- `UnityObjectExtensions.IsNullOrDestroyed` / `IsAlive` — a destroyed `UnityEngine.Object` is not
  a C# null reference, but Unity overloads `==` so it compares equal to `null`. That overload only
  resolves when the compile-time type of the expression is `UnityEngine.Object` (or a subclass);
  it is silently skipped in generic code (`T : class`) and through interface references, which
  otherwise look identical to a real null check. These extensions pin the static type to `Object`
  so the check is always correct. Phase 2's timer `owner` lifetime check reuses this directly.
- `GameObjectExtensions.GetOrAddComponent<T>` / `ComponentExtensions.GetOrAddComponent<T>` —
  returns an existing component or adds one.
- `TransformExtensions.DestroyAllChildren` — destroys every direct child. Named to make the O(n)
  iteration and per-child `Destroy` call explicit; not intended for per-frame use on large
  hierarchies.

Only add an extension method when it is broadly reusable and does not hide an expensive or
surprising operation behind an innocuous-looking call. Prefer a small number of well-justified
extensions over many speculative ones.

## Roadmap

Phase 3 deliberately does **not** include: Progression, Rewards, Currency, Inventory, Economy,
Tutorial, Pooling (beyond Audio's own internal voice pool, which exists to solve a real,
Phase-3-scoped problem), Ads, Analytics, IAP, Remote Config, Feature Flags, Notifications, or any
game-specific menu/HUD/screen content. Planned next:

- **Phase 4** — Gameplay Infrastructure.
