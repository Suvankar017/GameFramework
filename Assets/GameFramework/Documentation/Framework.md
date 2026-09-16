# GameFramework

A reusable Unity framework intended to be shared across multiple games built on this project's
tooling and target platforms.

## Status

**Phase 4 — Gameplay Infrastructure.** Phase 0 laid the structural foundation, Phase 1 built
Bootstrap/Services/Logging/GameState/SceneManagement, Phase 2 added the infrastructure layer (Time,
Timers, Events, Persistence, Settings), and Phase 3 added five reusable player-facing systems
(Input, Localization, Audio, UI Foundation, Feedback/Haptics). Phase 4 adds generic gameplay
infrastructure on top: a gameplay loop, entity/component utilities, object lifecycle, spawning,
pooling, commands, interaction/targeting, and objectives/checkpoints — see
[Gameplay Infrastructure](#gameplay-infrastructure). No player character, enemy AI, weapons,
inventory, progression, economy, or other game-specific content exists yet — see
[Roadmap](#roadmap).

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
Game Features ─────────────────────────────────┐
  ↓                                             ↓
GameFramework.PlayerSystems              GameFramework.Gameplay
(composition root: registers the             (Loop, Entities, Lifecycle, Spawning,
 five Phase 3 services below)                  Pooling, Commands, Interaction, Objectives)
  ↓                                             │
GameFramework.UI  ──────┬──────────────┬───────┤ (no dependency either direction —
  ↓                     ↓              ↓        │  siblings, see below)
GameFramework.Localization  GameFramework.Audio  GameFramework.Feedback
  ↓                     ↓              ↓              ↓
GameFramework.Input     └──────────────┴──────────────┘
  ↓                                             ↓
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
`GameFramework.Editor` (added in Phase 3 for one small localization validation menu item, extended
in Phase 4 for Gameplay config validation) is Editor-only and referenced by nothing at runtime.

`GameFramework.Gameplay` (Phase 4) references only `GameFramework.Core`/`GameFramework.Runtime` —
deliberately **not** `GameFramework.PlayerSystems` or any of Input/UI/Audio/Feedback. Gameplay
Infrastructure and Player Experience are siblings that both sit on top of the Core Framework,
neither depending on the other: nothing under `GameFramework.Gameplay.*` references an Input/UI/
Audio/Feedback type. A game wanting both registers Phase 4's two services
(`IGameplayService`/`IPoolService`) in its own `PlayerSystemsBootstrapper` subclass — see
[Game Flow Integration](#game-flow-integration) — rather than Phase 4 hard-depending on Phase 3.

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
│   ├── PlayerSystems/            GameFramework.PlayerSystems.asmdef (Phase 3 composition root)
│   └── Gameplay/                 GameFramework.Gameplay.asmdef (Phase 4)
│       ├── Entities/             EntityId.cs, EntityIdentity.cs, ComponentLookup.cs
│       ├── Lifecycle/            IGameplayObjectLifecycle.cs, GameplayObjectLifecycleRunner.cs,
│       │                         GameplayObjectLifecycleState.cs
│       ├── Spawning/             SpawnRequest.cs, SpawnResult.cs, SpawnFailureReason.cs,
│       │                         ISpawnProvider.cs, InstantiateSpawnProvider.cs,
│       │                         PooledSpawnProvider.cs, SpawnConfiguration.cs, Spawner.cs
│       ├── Pooling/              GameObjectPool.cs, GameObjectPoolConfig.cs, PoolConfiguration.cs,
│       │                         IPoolService.cs, PoolService.cs
│       ├── Commands/             IGameplayCommand.cs, CommandResult.cs, CommandResultStatus.cs,
│       │                         GameplayCommandInvoker.cs, GameplayCommandQueue.cs
│       ├── Interaction/          IInteractable.cs, InteractionContext.cs, TargetingUtility.cs
│       ├── Objectives/           IObjective.cs, ObjectiveBase.cs, ObjectiveState.cs,
│       │                         ObjectiveEvents.cs, ObjectiveDefinition.cs, Checkpoint.cs
│       └── (root)                IGameplayService.cs, GameplayService.cs, GameplayLoopDriver.cs,
│                                  IGameplayLifecycle.cs, IGameplayTickable.cs,
│                                  IGameplayFixedTickable.cs, IGameplayLateTickable.cs,
│                                  GameplayLoopState.cs, GameplayBootstrapper.cs
├── Editor/
│   ├── GameFramework.Editor.asmdef (Phase 3, extended Phase 4)
│   ├── Localization/             LocalizationTableValidator.cs
│   └── Gameplay/                 GameplayConfigValidator.cs (Phase 4)
├── Tests/
│   ├── Editor/                          GameFramework.Core.Tests.asmdef (EditMode)
│   │   ├── Validation/ , Extensions/
│   │   ├── Runtime/                     GameFramework.Runtime.Tests.asmdef (EditMode)
│   │   │   ├── Services/ , Diagnostics/ , State/ , SceneManagement/
│   │   │   └── Time/ , Timers/ , Events/ , Persistence/ , Settings/
│   │   ├── Input/                       GameFramework.Input.Tests.asmdef (EditMode)
│   │   ├── Localization/                GameFramework.Localization.Tests.asmdef (EditMode)
│   │   ├── Audio/                       GameFramework.Audio.Tests.asmdef (EditMode)
│   │   ├── Feedback/                    GameFramework.Feedback.Tests.asmdef (EditMode)
│   │   └── Gameplay/                    GameFramework.Gameplay.Tests.asmdef (EditMode, Phase 4)
│   │       ├── Entities/ , Lifecycle/ , Commands/ , Objectives/
│   └── Runtime/                         GameFramework.Core.Tests.Runtime.asmdef (PlayMode)
│       ├── Extensions/
│       ├── Framework/                   GameFramework.Runtime.Tests.Runtime.asmdef (PlayMode)
│       │   └── Bootstrap/
│       ├── Audio/                       GameFramework.Audio.Tests.Runtime.asmdef (PlayMode)
│       ├── UI/                          GameFramework.UI.Tests.Runtime.asmdef (PlayMode)
│       ├── PlayerSystems/               GameFramework.PlayerSystems.Tests.Runtime.asmdef (PlayMode)
│       └── Gameplay/                    GameFramework.Gameplay.Tests.Runtime.asmdef (PlayMode, Phase 4)
│           ├── Pooling/ , Spawning/ , Interaction/
└── Documentation/
    └── Framework.md                     (this file)
```

## Assemblies

| Assembly | Location | References | Purpose |
|---|---|---|---|
| `GameFramework.Core` | `Runtime/Core` | none | Framework-level utilities with no game or higher-framework dependencies. |
| `GameFramework.Runtime` | `Runtime/` (root, excluding `Core/`) | `GameFramework.Core` | Bootstrap, services, logging, game state, scene management, time, timers, events, persistence, settings. |
| `GameFramework.Core.Tests` | `Tests/Editor` | `GameFramework.Core`, TestRunner | EditMode tests for `GameFramework.Core`. |
| `GameFramework.Runtime.Tests` | `Tests/Editor/Runtime` | `GameFramework.Core`, `GameFramework.Runtime`, TestRunner | EditMode tests for `GameFramework.Runtime` (everything that doesn't need `Awake`/`DontDestroyOnLoad`). |
| `GameFramework.Core.Tests.Runtime` | `Tests/Runtime` | `GameFramework.Core`, TestRunner | PlayMode tests for the subset of `GameFramework.Core` that Edit Mode cannot exercise. |
| `GameFramework.Runtime.Tests.Runtime` | `Tests/Runtime/Framework` | `GameFramework.Core`, `GameFramework.Runtime`, TestRunner | PlayMode tests for `GameBootstrapper` (needs real `Awake`/`DontDestroyOnLoad`/`Update`). |
| `GameFramework.Input` | `Runtime/Input` | `GameFramework.Core`, `GameFramework.Runtime`, `Unity.InputSystem` | Logical input actions, contexts, pointer/touch, gestures. Legacy Input Manager and New Input System (keyboard/mouse/gamepad) bindings both supported per action. |
| `GameFramework.Localization` | `Runtime/Localization` | `GameFramework.Core`, `GameFramework.Runtime`, `Unity.TextMeshPro` | Language tables, lookup/fallback, language-specific fonts/assets. |
| `GameFramework.Audio` | `Runtime/Audio` | `GameFramework.Core`, `GameFramework.Runtime` | Pooled playback, categories/volumes, cues, music crossfade. |
| `GameFramework.Feedback` | `Runtime/Feedback` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Audio` | Haptics abstraction, presets coordinating haptics + audio. |
| `GameFramework.UI` | `Runtime/UI` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Localization`, `GameFramework.Audio`, `GameFramework.Feedback`, `Unity.TextMeshPro` | Layered canvases (Screen Space - Overlay or Camera, via `UICanvasConfig`), screen stack, popups/modals, localized UI components. |
| `GameFramework.PlayerSystems` | `Runtime/PlayerSystems` | all of the above + `GameFramework.Runtime` | Composition root: `PlayerSystemsBootstrapper`. |
| `GameFramework.Gameplay` | `Runtime/Gameplay` | `GameFramework.Core`, `GameFramework.Runtime` (sibling of `PlayerSystems` — no Input/UI/Audio/Feedback reference) | Gameplay loop, entity/component utilities, object lifecycle, spawning, pooling, commands, interaction/targeting, objectives/checkpoints. Composition root: `GameplayBootstrapper`. |
| `GameFramework.Editor` | `Editor/` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Localization`, `GameFramework.Gameplay` | Editor-only. Localization table validation and Gameplay config validation menu items. |
| `GameFramework.Input.Tests` / `.Localization.Tests` / `.Audio.Tests` / `.Feedback.Tests` / `.Gameplay.Tests` | `Tests/Editor/<System>` | matching runtime assembly + Core/Runtime, TestRunner | EditMode tests for each system's pure logic. |
| `GameFramework.Audio.Tests.Runtime` / `.UI.Tests.Runtime` / `.PlayerSystems.Tests.Runtime` / `.Gameplay.Tests.Runtime` | `Tests/Runtime/<System>` | matching runtime assembly + Core/Runtime, TestRunner | PlayMode tests for behavior that genuinely needs a running engine (real `AudioSource` playback, `AddComponent`-able UI test doubles, `GameBootstrapper.Awake`, real GameObject pooling/physics). |

Phase 2 added no new assembly (its five modules share no dependency boundary worth enforcing).
Phase 3 is the opposite case: UI's dependency on Localization/Audio/Feedback (and Feedback's on
Audio) is exactly the kind of one-way relationship an assembly boundary makes a compile error to
violate, so each of the five systems — plus the `PlayerSystems` composition root and one small
`Editor` assembly — got its own `.asmdef`. Phase 4 is closer to Phase 2's case internally (its eight
subsystems share no dependency boundary worth enforcing against each other — Spawning depending on
Pooling is the only real edge, and both are meant to be used together anyway) but still gets exactly
one new assembly, `GameFramework.Gameplay`, specifically to keep it a compile-time-enforced sibling
of `GameFramework.PlayerSystems` rather than a dependent of it — see
[Architecture](#architecture). `GameFramework.UI.Tests` intentionally does **not**
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
- `GameFramework.Input` — `IInputService`, `InputService`, `InputActionMapAsset`, `InputActionBindingDefinition`, `InputActionState`, `PointerState`, `InputContextDefinition`, `PointerGestureRecognizer`, `InputActionBuffer`, `NewInputMouseButton`, `GamepadAxisSource`, `GamepadStickSource`.
- `GameFramework.Localization` — `ILocalizationService`, `LocalizationService`, `LocalizationTableAsset`, `LocalizationConfigAsset`, `LanguageInfo`, `LanguageChangedEvent`.
- `GameFramework.Audio` — `IAudioService`, `AudioService`, `AudioCueAsset`, `AudioCategory`, `IAudioHandle`, `NullAudioHandle`, `IRandomSource`.
- `GameFramework.Feedback` — `IFeedbackService`, `FeedbackService`, `HapticStrength`, `IHapticProvider`, `FeedbackPresetAsset`.
- `GameFramework.UI` — `IUIService`, `UIService`, `UIScreen`, `UIPopup`, `UILayer`, `UIRenderMode`, `UICanvasConfig`, `LocalizedTextBase`/`LocalizedTMPText`/`LocalizedText`/`LocalizedImage`, `UIButtonFeedback`.
- `GameFramework.PlayerSystems` — `PlayerSystemsBootstrapper`.
- `GameFramework.Gameplay` — `IGameplayService`, `GameplayService`, `GameplayBootstrapper`, `IGameplayLifecycle`, `IGameplayTickable`/`IGameplayFixedTickable`/`IGameplayLateTickable`, `GameplayLoopState`.
- `GameFramework.Gameplay.Entities` — `EntityId`, `EntityIdentity`, `ComponentLookup`.
- `GameFramework.Gameplay.Lifecycle` — `IGameplayObjectLifecycle`, `GameplayObjectLifecycleRunner`, `GameplayObjectLifecycleState`.
- `GameFramework.Gameplay.Spawning` — `Spawner`, `SpawnRequest`, `SpawnResult`, `SpawnFailureReason`, `ISpawnProvider`, `InstantiateSpawnProvider`, `PooledSpawnProvider`, `SpawnConfiguration`.
- `GameFramework.Gameplay.Pooling` — `GameObjectPool`, `GameObjectPoolConfig`, `PoolConfiguration`, `IPoolService`, `PoolService`.
- `GameFramework.Gameplay.Commands` — `IGameplayCommand`, `CommandResult`, `CommandResultStatus`, `GameplayCommandInvoker`, `GameplayCommandQueue`.
- `GameFramework.Gameplay.Interaction` — `IInteractable`, `InteractionContext`, `TargetingUtility`.
- `GameFramework.Gameplay.Objectives` — `IObjective`, `ObjectiveBase`, `ObjectiveState`, `ObjectiveActivatedEvent`/`ObjectiveCompletedEvent`/`ObjectiveFailedEvent`, `ObjectiveDefinition`, `Checkpoint`, `CheckpointData`.
- `GameFramework.Editor.Localization` — `LocalizationTableValidator` (Editor-only).
- `GameFramework.Editor.Gameplay` — `GameplayConfigValidator` (Editor-only).

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

**Devices — both the legacy Input Manager and the New Input System.** `com.unity.inputsystem` is
now a project dependency and Project Settings > Player > Active Input Handling is set to **Both**,
so `InputActionBindingDefinition` accepts bindings from either backend on the same logical action:
`KeyboardKeys`/`MouseButtons`/`LegacyAxisName(X/Y)` (legacy Input Manager — legacy default axes like
`"Horizontal"` still blend keyboard and joystick with no extra Project Settings changes) alongside
`NewInputKeyboardKeys`/`NewInputMouseButtons`/`GamepadButtons`/`GamepadAxis`/`GamepadStick` (New
Input System). Every source list is additive — bind only legacy, only new, or both; whichever is
physically active satisfies the action. For `Axis`/`Vector2` actions, the legacy value and the
gamepad value are combined by taking whichever has the larger magnitude that frame (`InputService.
SampleAxis`/`SampleVector2`), so an idle gamepad never overrides an active keyboard axis and vice
versa. Real sampling goes through `IInputSampler` (`UnityInputSampler` in production); tests inject
a fake, so button/axis/gamepad/touch/pointer-over-UI logic is deterministic without a live device.

`UnityInputSampler`'s legacy members are compiled behind `ENABLE_LEGACY_INPUT_MANAGER` and its New
Input System members behind `ENABLE_INPUT_SYSTEM` — the same two scripting defines Unity itself
sets from Active Input Handling — so the sampler degrades to a safe "no input" no-op (never throws)
for whichever backend a project doesn't have active, rather than assuming "Both" specifically.
Gamepad support (`GamepadButton`/`GamepadAxisSource`/`GamepadStickSource`, via `Gamepad.current`)
exists only through the New Input System — the legacy Input Manager has no cross-platform gamepad
API of its own. Touch/pointer sampling (below) intentionally stays on the legacy `Input.touches`
path, since it already works correctly under "Both" and a New Input System `Touchscreen.current`
path would duplicate it for no behavioral gain; this is a scoping decision, not an oversight.

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

**Render mode — Screen Space Overlay or Camera.** Every layer's `Canvas.renderMode` comes from the
`UICanvasConfig` passed to `UIService`'s constructor: `UIRenderMode.ScreenSpaceOverlay` (default —
the original zero-setup behavior, no camera dependency) or `UIRenderMode.ScreenSpaceCamera`, which
sets `canvas.worldCamera`/`canvas.planeDistance` from `UICanvasConfig.WorldCamera`/`PlaneDistance`
on every layer — the mode many 2D games need so UI shares a camera stack with the world (URP camera
stacking, camera-driven zoom/shake or post-processing that should also affect UI). If Camera mode is
requested with no `WorldCamera` assigned, `UIService` logs one warning and falls back to Overlay for
every layer rather than silently producing invisible UI. `PlayerSystemsBootstrapper` exposes this as
two Inspector fields (`UI Render Mode`, `UI Camera`) and builds the config via its overridable
`CreateUIService()` — a game needing per-layer plane distances or other customization overrides that
one method instead of duplicating `RegisterServices`. `WorldSpace` is intentionally not exposed —
see `UIRenderMode`'s doc comment for why.

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

## Gameplay Infrastructure

Phase 4. Reusable gameplay building blocks that stay independent of any specific game's content —
no player character, enemy AI, weapons, inventory, progression, or economy lives here (see
[Roadmap](#roadmap)). Registered services: `IGameplayService` (the gameplay loop) and
`IPoolService` (named pool registry) — see [Game Flow Integration](#game-flow-integration) for how
a game registers them. Everything else (`Spawner`, `GameObjectPool`, commands, interactables,
objectives) is scene/object-lifetime and constructed directly, never a service — see section 96 of
the Phase 4 brief's ownership guidance, reflected throughout below.

### Gameplay Loop

`IGameplayService` gives gameplay participants predictable lifecycle hooks without forcing every
object to inherit from a framework base class, and without duplicating Unity's PlayerLoop or
`IGameStateService`'s state machine:

```csharp
IGameplayService gameplay = GameBootstrapper.Instance.Services.Get<IGameplayService>();
gameplay.BeginPlay(); // typically called from the game's own "Gameplay" IGameState.Enter()

public class Enemy : MonoBehaviour, IGameplayLifecycle, IGameplayTickable
{
    private void OnEnable() => gameplayService.RegisterLifecycle(this);
    private void OnDisable() => gameplayService.UnregisterLifecycle(this);

    public void OnGameplayInitialize() { /* once, ever */ }
    public void OnGameplayBeginPlay() { /* every session start */ }
    public void OnGameplayPause() { }
    public void OnGameplayResume() { }
    public void OnGameplayShutdown() { /* unsubscribe, cancel timers, clear refs */ }

    public void GameplayTick(float deltaTime) { /* instead of Update() */ }
}
```

**Phases.** Exactly the five named in the brief — Initialize (once, at registration), BeginPlay,
Pause, Resume, Shutdown — plus three independent opt-in tick interfaces
(`IGameplayTickable`/`IGameplayFixedTickable`/`IGameplayLateTickable`) so a participant only
implements the phase(s) it needs; nothing is forced to implement all of them. A late-registering
participant (e.g. an enemy spawned mid-level) immediately receives whatever phases it missed
(Initialize, then BeginPlay, then Pause if currently paused) so it ends up in the same state as
everyone else, with no ordering bugs from registering late.

**Pause is fully reactive — never called directly.** `GameplayService.Tick()` polls
`ITimeService.IsPaused` once per frame and edge-detects the transition itself; nothing in this
framework ever writes `Time.timeScale`. A game pauses exactly the way it always would
(`timeService.Pause()`), and every registered `IGameplayTickable` simply stops being ticked at all
while paused — not ticked with a zero delta — while UI/Audio (on entirely separate paths, never
routed through this service) keep working, satisfying "Gameplay updates stop, UI continues" with no
special-casing anywhere.

**FixedUpdate/LateUpdate, without extending `IUpdatableService`.** Regular per-frame ticking reuses
`IUpdatableService.Tick()` exactly like every other Phase 2/3 service. Fixed/Late ticking needed a
Unity callback a plain C# service can't receive, so `GameplayService` creates its own tiny
`DontDestroyOnLoad` driver GameObject (`GameplayLoopDriver`) in `Initialize` — the same pattern
`AudioApplicationLifecycleHook` already established for Audio's application-pause callback — rather
than adding Fixed/Late methods to `IUpdatableService` itself, which would have forced every existing
`IUpdatableService` implementer (just `TimerService` today) to grow two new no-op methods.

### Entity / Component Utilities

Not an ECS — an entity is still just a GameObject with MonoBehaviours. `EntityId` is a
process-lifetime-unique monotonic counter, explicitly not `GetInstanceID()` (which is reused after
destruction and meaningless across a save/load) and not itself persisted — a game that needs an
identity to survive save/load persists the assigned value itself through
`IPersistenceService`, per [Persistence](#persistence); this does not become a second identity/save
system. `EntityIdentity` is an optional component that assigns one on `Awake`; most GameObjects
don't need it. `ComponentLookup.RequireInParent<T>`/`RequireInChildren<T>` fill the one gap
`[RequireComponent]` doesn't cover (same-GameObject requirements should still just use
`[RequireComponent]`) — named static methods, not extension methods, specifically so a hierarchy
walk is always visible at the call site.

### Object Lifecycle

`IGameplayObjectLifecycle` (`Initialize`/`Activate`/`Deactivate`/`Dispose`) is a *composition*, not
inheritance: hold a `GameplayObjectLifecycleRunner` as a field, which guards the phases so
`Initialize` really only ever fires once regardless of how many times the object is later reused —
the exact guarantee Pooling needs to make "Get from pool" behave differently from "freshly
instantiated" only where that's actually intended:

```csharp
public class Projectile : MonoBehaviour, IGameplayObjectLifecycle
{
    private GameplayObjectLifecycleRunner _lifecycle;
    private void Awake() { _lifecycle = new GameplayObjectLifecycleRunner(this); _lifecycle.Initialize(); }
    public void Initialize() { /* cache components - once, ever */ }
    public void Activate() { /* reset health/timers - every reuse */ }
    public void Deactivate() { /* unsubscribe, cancel timers - every reuse */ }
    public void Dispose() { /* release permanent resources - once, ever */ }
}
```

A simple object with no meaningful reset state doesn't need to implement this interface at all —
`GameObjectPool` (below) still pools it correctly, it just gets plain `SetActive(true/false)`.

### Spawning

`Spawner` (scene/object-lifetime `MonoBehaviour`, not a service) answers what/where/when/how-many
and delegates *how* to an `ISpawnProvider` — swapping `InstantiateSpawnProvider` for
`PooledSpawnProvider` makes it pooled without any calling code changing:

```csharp
SpawnResult result = spawner.TrySpawn(); // or TrySpawn(position, rotation), or a full SpawnRequest
if (result.Success) { /* use result.Instance */ }
else Log.Warning("Game", $"Spawn failed: {result.FailureReason}");

spawner.SetProvider(new PooledSpawnProvider(myPool)); // opt into pooling, no other code changes
```

Limits (`MaxActiveInstances`, `MaxTotalInstances`, `SpawnCooldownSeconds`) are optional (0/0f =
unlimited) and checked before the provider is ever called; a `SpawnConfiguration` asset can supply
prefab + limits as a designer-authored preset instead of per-instance Inspector fields. Failures are
a `SpawnResult`/`SpawnFailureReason`, never an exception or a silent no-op.

### Pooling

`GameObjectPool` wraps `UnityEngine.Pool.ObjectPool<T>` (built into Unity since 2021.1) rather than
reimplementing a free-list, adding the Unity-specific parts on top:

```csharp
var pool = new GameObjectPool(prefab, new GameObjectPoolConfig { DefaultCapacity = 10, MaxSize = 100, PrewarmCount = 10 });
GameObject instance = pool.Get(position, rotation);
pool.Release(instance); // never Object.Destroy a pooled instance directly
```

**Lifecycle** — `Get` activates the GameObject and calls `Activate()` on every component
implementing `IGameplayObjectLifecycle` (found once per instance via a reused, non-allocating
`GetComponents` buffer — never per-frame); `Release` calls `Deactivate()` and deactivates it,
reparenting it back under the pool's own container; an instance released beyond `MaxSize` is
destroyed instead (calling `Dispose()` first) rather than retained unboundedly. Prewarming (opt-in,
0 by default) cycles each instance through Activate/Deactivate once, since `ObjectPool<T>` has no
lower-level way to seed its free list without going through Get/Release.

**Ownership.** Every instance lives under a container Transform the pool creates
(`DontDestroyOnLoad`, for an application-lifetime pool) or one you supply (a plain scene-local
Transform, disposed alongside the scene that owns it, for a scene-lifetime pool) — see
`IPoolService`'s doc comment for when to register a pool there (application-lifetime, shared by key)
versus just constructing a `GameObjectPool` directly (any other lifetime).

**Safety, not just happy-path.** `Release` catches `ObjectPool<T>`'s own duplicate-release detection
(`collectionCheck: true`) and logs instead of letting the exception escape; `Get` detects a pooled
instance that was destroyed externally (e.g. a scene-scoped pool's scene unloading without the pool
being disposed first) and transparently creates a replacement rather than handing back a broken
reference — both are covered by `GameObjectPoolTests`, not just asserted in comments.

### Commands

`IGameplayCommand` (`CanExecute`/`Execute`) decouples a discrete gameplay action's request from its
execution, with no undo/redo and no `Dictionary<string, object>` payload — each concrete command
carries its own strongly-typed fields:

```csharp
GameplayCommandInvoker.Invoke(new OpenDoorCommand(door, keyId));
// CanExecute() == false -> CommandResultStatus.Rejected, Execute() never called
// Execute() returning Failure -> logged automatically
```

`GameplayCommandQueue` is an explicitly-instantiated, optional FIFO for the (uncommon) case where
commands must run sequentially rather than immediately — nothing is global, and invoking a command
directly through `GameplayCommandInvoker` adds no latency.

### Interaction / Targeting

`IInteractable` (`CanInteract`/`Interact`) takes a deliberately minimal `InteractionContext`
(Interactor, Target, Position, Direction only — no input state, no free-form context slot) so it
stays a small, composable contract rather than a "universal interaction object." Cooldowns are
intentionally not part of the contract — compose them separately per interactable if needed.

`TargetingUtility` provides small, allocation-conscious helpers — non-allocating overlap queries
(`FindTargetsInRadius`/`FindTargetsInRadius2D`, caller-owned buffer and explicit layer mask),
`GetClosest`/`GetClosest2D`, and a cheap dot-product `IsWithinViewCone` cone check meant to run
*before* a physics query to cull candidates. This is not an AI targeting system — no line-of-sight
raycasting, priority scoring, or perception model is implemented.

**Decoupled from Input, by construction.** Nothing under `GameFramework.Gameplay.Interaction`
references `GameFramework.Input`. A game's own code reads `IInputService.GetButtonDown("Interact")`
and then calls into interaction/targeting itself — the framework never calls Input APIs from
gameplay infrastructure.

### Objectives / Checkpoints

`ObjectiveBase` (`Inactive → Active → Completed|Failed → Inactive` via `Reset`) owns the state
machine and event publication only — a game subclasses it and calls `Complete()`/`Fail()` when its
own condition is met; no concrete objective ("Collect 10 Coins") exists in the framework. Invalid
transitions are rejected with a logged warning, never an exception or a silent state change:

```csharp
public class CollectCoinsObjective : ObjectiveBase
{
    public CollectCoinsObjective(string id, IEventService events) : base(id, events) { }
    public void OnCoinCollected() { if (++_collected >= _target) Complete(); }
}

events.Subscribe<ObjectiveCompletedEvent>(e => Log.Info("Game", $"Objective '{e.ObjectiveId}' complete"));
```

State changes publish `ObjectiveActivatedEvent`/`ObjectiveCompletedEvent`/`ObjectiveFailedEvent`
through `IEventService` — the same Phase 2 Event System every other cross-system notification uses,
not a separate mechanism. The `IEventService` passed to the constructor is optional (null just skips
publishing), so an objective is fully unit-testable with zero service wiring.

`Checkpoint` (a `MonoBehaviour` marking a scene location) and `CheckpointData` (its serializable
payload — Id/Position/Rotation) carry data only: no automatic respawn, and no second save system —
persist `CheckpointData` through `IPersistenceService` exactly like any other save data if a game
needs it to survive across sessions. There is deliberately no aggregate "objective tracker" or
progression layer on top — that would start to be Level Progression, out of Phase 4's scope.

### Game Flow Integration

No new GameManager. `GameplayBootstrapper` (`GameFramework.Gameplay`) is a `GameBootstrapper`
subclass — sibling to `PlayerSystemsBootstrapper`, not descending from it (see
[Architecture](#architecture)) — that registers `IGameplayService`/`IPoolService` the same way every
other phase's bootstrapper subclass adds its own services. A game wanting both Player Systems and
Gameplay Infrastructure combines them in its own small subclass:

```csharp
public class MyGameBootstrapper : PlayerSystemsBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1/2/3
        registry.Register<IGameplayService>(new GameplayService());
        registry.Register<IPoolService>(new PoolService());
    }
}
```

Everything else follows from decisions already described above rather than new integration code:
gameplay pausing reads `ITimeService.IsPaused` (never writes `Time.timeScale`); a spawn/pool/
objective/interaction failure is a typed result/log, never a `GameManager`-style global flag; and no
Gameplay type references Input/UI/Audio/Feedback, so "UI/Audio/Feedback integration remains
decoupled" holds by construction, not by convention alone.

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
- Phase 4's `GameFramework.Gameplay.Tests` (EditMode) covers everything with no real GameObject
  lifecycle dependency: `GameplayObjectLifecycleRunnerTests` (phase ordering/guards),
  `GameplayCommandTests`, `ObjectiveBaseTests` (including event publication via a real
  `EventService`, and that a null `IEventService` still transitions state correctly),
  `EntityIdTests`, and `GameplayServiceTests` (loop/pause/tick behavior against a fake
  `ITimeService`, reusing the same `MarkInitialized`-based registry pattern Phase 3 established —
  `GameFramework.Gameplay.Tests`/`.Tests.Runtime` were added to `GameFramework.Runtime`'s
  `AssemblyInfo.cs` `InternalsVisibleTo` list for this).
- `GameFramework.Gameplay.Tests.Runtime` (PlayMode) covers what genuinely needs real GameObjects or
  physics: `GameObjectPoolTests` (get/release/reuse, prewarm, growth beyond default capacity, max
  size destroying excess, duplicate release, an instance destroyed externally being transparently
  replaced, and Dispose) and `SpawnerTests` (limits, cooldown via a fake `ITimeService`, events, and
  swapping in a `PooledSpawnProvider`) both instantiate/destroy real GameObjects;
  `TargetingUtilityTests` exercises real `Physics`/`Physics2D` overlap queries against real
  Colliders, which an EditMode fake could not do meaningfully.

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

Phase 3 deliberately did **not** include: Progression, Rewards, Currency, Inventory, Economy,
Tutorial, Pooling (beyond Audio's own internal voice pool, which exists to solve a real,
Phase-3-scoped problem), Ads, Analytics, IAP, Remote Config, Feature Flags, Notifications, or any
game-specific menu/HUD/screen content — [Pooling](#pooling) and [Spawning](#spawning) are Phase 4's
answer to the general-purpose version of that gap; Phase 3's voice pool remains Audio-internal.

Phase 4 deliberately does **not** include: Player Character, Enemy AI, Weapons, Combat, Inventory,
Currency, Economy, Quests, Level Progression, Rewards, Tutorial, Ads, Analytics, IAP, Remote Config,
Leaderboards, Multiplayer, or any game-specific vehicle/controller — these consume Phase 4's
infrastructure (commands, interaction, objectives, spawning, pooling) rather than living inside it.
A general-purpose Tick Scheduler/Job System/ECS update graph was also deliberately not built now —
`IGameplayService`'s three tick interfaces are kept forward-compatible with one (see
[Gameplay Loop](#gameplay-loop)), not a substitute for it. Planned next:

- **Phase 5** — Performance, Tick System, Optimization, Memory & Resource Management.
