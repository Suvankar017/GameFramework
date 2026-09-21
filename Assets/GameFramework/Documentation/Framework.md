# GameFramework

A reusable Unity framework intended to be shared across multiple games built on this project's
tooling and target platforms.

## Status

**Phase 11 — Camera Framework.** Phase 0 laid the structural foundation,
Phase 1 built Bootstrap/Services/Logging/GameState/SceneManagement, Phase 2 added the
infrastructure layer (Time, Timers, Events, Persistence, Settings), Phase 3 added five reusable
player-facing systems (Input, Localization, Audio, UI Foundation, Feedback/Haptics), Phase 4 added
generic gameplay infrastructure (a gameplay loop, entity/component utilities, object lifecycle,
spawning, pooling, commands, interaction/targeting, and objectives/checkpoints — see
[Gameplay Infrastructure](#gameplay-infrastructure)), Phase 5 added a cross-cutting performance
layer (profiling, a centralized tick system, pooling hardening, resource loading, mobile
utilities, memory diagnostics, performance budgets — see
[Performance Infrastructure](#performance-infrastructure)), and Phase 6 added the framework's first
player-progression content systems — Economy (multi-currency balances), Inventory (item
ownership/quantities), Experience (levels/XP), Unlocks (composable requirement-gated content), and
Rewards (idempotent, transaction-safe grant orchestration across all four) — see
[Progression, Economy, Inventory, Unlocks & Rewards](#progression-economy-inventory-unlocks--rewards).
Phase 7 adds a reusable gameplay-state/objective layer on top of that: generic Statistics tracking,
a composable Condition system, condition-backed Objectives (extending Phase 4's objective state
machine), Quests, Achievements, and threshold Milestones, all claiming rewards through Phase 6's
existing idempotent `IRewardService` — see
[Objectives, Quests, Achievements & Milestones](#objectives-quests-achievements--milestones).
Phase 8 adds the reusable game-flow/orchestration layer on top of all of that: one state machine
(`LevelFlowState`) covering level-load lifecycle and gameplay-session state together, a generic
`GameplaySession` (attempt tracking, elapsed time, result), session-scoped checkpoints/respawn,
reference-counted pause with labeled tokens (built entirely on Phase 2's existing
`ITimeService.Pause`/`Resume`), and restart/retry flows — all driven by one registered
`IGameFlowService` that never references Progression/Unlocks/Rewards/Quests directly, only ever
notifying them via events — see
[Game Flow, Sessions & Checkpoints](#game-flow-sessions--checkpoints).
Phase 9 adds a reusable tutorial/onboarding execution layer on top of Core/Runtime/Input only: a
lifecycle-managed `ITutorialService` (Inactive → Starting → Running ⇄ Paused →
Completing/Cancelling → Completed/Cancelled), a minimal sequential step model with five built-in
step types (Instruction, Wait, Input, Event, Condition — game code supplies its own condition/event
types, never the framework), prerequisites/repeat/skip/persistence policies, optional gameplay pause
(built on `ITimeService`, no new pause abstraction) and optional input-context gating (built on
`IInputService`'s existing context stack) — see
[Tutorial & Onboarding Framework](#tutorial--onboarding-framework). It deliberately never
references GameFlow/Gameplay/Progression/Unlocks/Rewards/Quests, so it stays usable by a game that
has none of those systems.
Phase 10 adds a coordinated game-feel/presentation orchestration layer on top of Phase 3's Audio/
Feedback and Phase 4/5's Gameplay/Performance: one registered `IPresentationService` gameplay code
calls to say "something important happened" (`Play(feedbackId, ...)`), which dispatches a
`FeedbackDefinition` asset's enabled channels — Audio (via the existing `IAudioService`), Haptics
(via the existing `IFeedbackService`), Camera shake (a small composable driver a game attaches to
its own camera), Visual effects (via the existing pooling), Screen flash/fade (rendered through the
existing UI layer roots, never a new Canvas), UI reactions (published as an event, never a direct
`UIScreen`/`UIPopup` call), and Time (a brief hit-stop built entirely on the existing `ITimeService`,
never `Pause`/`Resume`) — without gameplay code knowing how any single channel is implemented. See
[Game Feel, Feedback & Presentation](#game-feel-feedback--presentation). It is explicitly not a
replacement for Phase 3's `IFeedbackService`/`IAudioService` - it orchestrates them.
Phase 11 adds a reusable camera orchestration layer on top of Core/Runtime/Performance only: one
registered `ICameraService` owns camera registration/selection (base activation plus a temporary
override stack, e.g. a Boss camera taking over and later releasing back to the gameplay camera), a
small extensible `ICameraMode` strategy (Follow/Static/TargetLook/Manual) computes each registered
`CameraController`'s pose entirely in pure, Unity-lifecycle-free C#, and exactly one `CameraDriver`
per scene applies whichever camera is currently active to the real `UnityEngine.Camera` each
`LateUpdate`. Gameplay never touches `Camera.main`/`transform.position`/`orthographicSize` directly —
it expresses intent ("follow this target," "use this configuration," "temporarily use this camera")
through the framework instead. See [Camera Framework](#camera-framework). It composes with Phase
10's existing `Presentation.CameraFeedbackDriver` through script execution order alone
(`[DefaultExecutionOrder(-100)]`) — zero compile-time coupling between the two assemblies in either
direction, and no second camera-shake system.
The framework still defines no concrete currencies, items, levels, rewards, quests, achievements,
tutorial content, feedback content, or camera content for any specific game — no player character,
enemy AI, weapons, economy backend, live ops, or IAP exists yet — see [Roadmap](#roadmap).

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

`GameFramework.Performance` (Phase 5) references only `GameFramework.Core`/`GameFramework.Runtime` —
the same shape as `GameFramework.Gameplay` — specifically so it stays usable by any game regardless
of which of Player Systems/Gameplay Infrastructure it also uses; nothing under
`GameFramework.Performance.*` references Input/UI/Audio/Feedback/Gameplay types. Its four services
(`ITickService`, `IPerformanceMonitorService`, `IApplicationLifecycleService`,
`IMobilePerformanceService`) are registered by `PerformanceBootstrapper`, a third
`GameBootstrapper` subclass sibling to `PlayerSystemsBootstrapper`/`GameplayBootstrapper` — see
[Performance Infrastructure](#performance-infrastructure). The one exception to "Performance stays
below everything else": `GameFramework.Gameplay` itself takes a one-way reference *on*
`GameFramework.Performance` (not the other way around) so `GameObjectPool`/`Spawner` can use
`ProfileScope` and `GameObjectPool` can expose `PoolStatistics` — a lower layer providing
diagnostics that a higher layer opts into, not a dependency cycle.

`GameFramework.Tutorials` (Phase 9) references only `GameFramework.Core`/`GameFramework.Runtime`/
`GameFramework.Input` — deliberately **not** GameFlow/Gameplay/Performance/Progression/Unlocks/
Rewards/Quests/UI/Localization/Audio/Feedback. It reuses `IInputService`'s existing logical-action
sampling and context stack (an `InputStep` reads an action; a tutorial optionally pushes/pops one
`InputContextDefinition` for its whole run) and `ITimeService`'s existing reference-counted
`Pause`/`Resume` (a `TutorialPausePolicy.PausesGameplay` tutorial calls `Pause`/`Resume` directly —
no new pause-token type, since unlike `GameFramework.GameFlow.IPauseToken` nothing external needs
to hold or release this pause itself). A `ConditionStep` wraps a small local `ITutorialCondition`
rather than reusing `GameFramework.Quests.Conditions.ICondition`, for the same reason
`GameFramework.Quests` didn't reuse `GameFramework.Unlocks.IUnlockRequirement`: the shape is
mirrored, not the assembly reference, since pulling in Quests would drag in Rewards/Unlocks/
Progression too. A game wanting Tutorials alongside GameFlow/Gameplay/Progression/etc. combines
them in its own small bootstrapper subclass, exactly like every other pair of sibling systems in
this framework.

`GameFramework.Presentation` (Phase 10) is architecturally different from every sibling above it:
where Tutorials/GameFlow/Progression deliberately stay *minimal* (Core/Runtime plus at most one
more), Presentation is a genuine orchestration layer over several existing lower ones by design —
it references `GameFramework.Core`/`GameFramework.Runtime`/`GameFramework.Audio`/
`GameFramework.Feedback`/`GameFramework.Gameplay`/`GameFramework.Performance`/`GameFramework.UI`.
This is still a clean one-way layering, not a new cycle risk: none of those seven assemblies
reference each other in a way that would loop back, and nothing in them will ever reference
Presentation. Every one of those references is resolved *softly* at runtime
(`registry.TryGet`) inside `PresentationService.Initialize` — a game can register Presentation
alone, with none of Audio/Feedback/Gameplay/Performance/UI present, and it still works, just with
every channel that needed one of them logging a single "ignored" warning instead of executing. This
is why `PresentationBootstrapper` is still a plain sibling of `PlayerSystemsBootstrapper`/
`GameplayBootstrapper`/`PerformanceBootstrapper` (see [Bootstrap Integration](#bootstrap-integration-2))
rather than extending any of them, even though its *assembly* references far more than a typical
sibling does. Presentation deliberately never references GameFlow/Progression/Unlocks/Rewards/
Quests/Tutorials/Localization/Input at all - see
[Game Feel, Feedback & Presentation](#game-feel-feedback--presentation) for the full channel-by-
channel reuse breakdown and why `GameFramework.Feedback` (Phase 3's existing haptics+audio-preset
service) was not overloaded with this orchestration responsibility instead of adding a new assembly.

`GameFramework.Cameras` (Phase 11) goes back to the *minimal* sibling shape (like Tutorials/GameFlow,
not Presentation): it references only `GameFramework.Core`/`GameFramework.Runtime`/
`GameFramework.Performance` — the last one solely for `ProfileScope`/`ProfilingCategory.Cameras`,
mirroring exactly why `GameFramework.Gameplay` already references `GameFramework.Performance`. It
deliberately has **no** reference to `GameFramework.Presentation` in either direction, even though
Phase 11's base camera pose and Phase 10's camera shake visibly compose on the same `Transform` every
frame. This works because `Presentation.CameraFeedbackDriver` was already written (Phase 10) to
never assume *anything* about what wrote a camera's transform earlier in the frame — it just
subtracts its own previous contribution and adds a new one (see that class's remarks) — so all
`Cameras.CameraDriver` has to do is write the base pose *first*. `[DefaultExecutionOrder(-100)]` on
`CameraDriver` guarantees that ordering deterministically against `CameraFeedbackDriver`'s own
(default-order) `LateUpdate`, regardless of GameObject/component creation order, entirely through a
Unity script-execution-order attribute — no assembly reference, no shared interface, no event. A game
using both simply attaches both driver components to the same camera GameObject; a game using only
one still works unmodified. See [Camera Framework](#camera-framework) for the full pipeline.

`GameFramework.Cameras.Cinemachine` (Phase 11, added once Cinemachine 2.10.7 was installed in this
project) is a **ninth, optional** assembly, one layer above `GameFramework.Cameras`: it references
`GameFramework.Cameras` (one-way) plus the `Cinemachine` package, and is the *only* assembly in the
entire framework that references Cinemachine at all. `GameFramework.Cameras` itself was **not**
changed to depend on Cinemachine — every existing Phase 11 type (`ICameraService`, `CameraController`,
`CameraDriver`, the built-in modes, bounds/zoom/transitions) is completely unaware this assembly
exists, and a game without Cinemachine installed still gets the full Phase 11 feature set unmodified.
This is a deliberate "backend swap," not a migration: a game picks *either* `Cameras.CameraDriver`
(the pure-C# pipeline) *or* `CinemachineIntegration.CinemachineCameraBackend` (Cinemachine-backed) for
a given scene's camera, against the exact same `ICameraService`/`CameraController` orchestration
either way. See [Cinemachine Integration](#cinemachine-integration) for the full design.

`GameFramework.UI.Navigation` (Phase 12) breaks the *minimal* sibling shape deliberately, the same
way `GameFramework.Presentation` did for a different reason: it references `GameFramework.UI` as a
genuine **hard** dependency (it directly calls `IUIService.OpenScreen`/`OpenPopup`/`CloseScreen`/
`ClosePopup` — orchestration, not an optional channel), plus `GameFramework.Input`/`GameFramework.GameFlow`
as *soft* runtime dependencies (`registry.TryGet`, resolved for Android back-button routing and
popup pause-token acquisition respectively — a game without either registered still gets full
navigation, just without that one piece). Because the `IUIService` dependency is hard, its own
composition root (`NavigationBootstrapper`) subclasses `PlayerSystemsBootstrapper` directly instead of
sitting beside it as a plain `GameBootstrapper` sibling — the same reasoning `Quests.QuestsBootstrapper`
already established for its own hard dependency on `Rewards`. The one change to an existing assembly
this phase makes is additive and backward-compatible: `UI.IUIService.OpenScreen<T>`/`OpenPopup<T>`
gained an optional `Action<T> onBeforeOpen = null` parameter (default `null`, so every existing call
site is unaffected) — the one seam Navigation needed to deliver typed parameters to a screen/popup
before its own `OnOpened` lifecycle hook fires, without Phase 3 needing to know Navigation exists.
See [UI Navigation & Menu Flow Framework](#ui-navigation--menu-flow-framework) for the full design.

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
│   │   └── Navigation/           GameFramework.UI.Navigation.asmdef (Phase 12) - Ids, requests/
│   │                             results, guards, transitions/back-handler hooks, the registries/
│   │                             stack-entry types (internal), INavigationService/NavigationService,
│   │                             NavigationBootstrapper, Catalog/ (UINavigationCatalog + entries)
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
│   └── Performance/               GameFramework.Performance.asmdef (Phase 5)
│       ├── AssemblyInfo.cs        InternalsVisibleTo for GameFramework.Performance.Tests
│       ├── Profiling/             ProfilingCategory.cs, ProfilingMode.cs, PerformanceSettings.cs,
│       │                          ProfileScope.cs, PerformanceBudget.cs, FrameTimeStats.cs,
│       │                          IPerformanceMonitorService.cs, PerformanceMonitorService.cs,
│       │                          PerformanceOverlay.cs
│       ├── Ticking/               ITickable.cs, IFixedTickable.cs, ILateTickable.cs, TickGroup.cs,
│       │                          TickRegistry.cs, ITickService.cs, TickService.cs,
│       │                          TickServiceDriver.cs
│       ├── Memory/                ManagedMemorySample.cs, MemoryDiagnostics.cs
│       ├── Resources/             IAssetHandle.cs, AssetHandle.cs, IAssetProvider.cs,
│       │                          ResourcesAssetProvider.cs
│       ├── Mobile/                DeviceInfo.cs, PerformanceProfile.cs, PerformanceProfileConfig.cs,
│       │                          IMobilePerformanceService.cs, MobilePerformanceService.cs,
│       │                          ApplicationLifecycleEvents.cs, ApplicationLifecycleDriver.cs,
│       │                          IApplicationLifecycleService.cs, ApplicationLifecycleService.cs
│       └── (root)                 PerformanceBootstrapper.cs
│   └── Cameras/                    GameFramework.Cameras.asmdef (Phase 11)
│       ├── AssemblyInfo.cs         InternalsVisibleTo for GameFramework.Cameras.Tests(.Runtime)
│       ├── Modes/                  CameraDeadZone.cs, FollowCameraMode.cs, StaticCameraMode.cs,
│       │                           TargetLookCameraMode.cs, ManualCameraMode.cs
│       ├── Configuration/          CameraFollowSettings.cs, CameraTargetLookSettings.cs,
│       │                           CameraZoomSettings.cs, CameraBoundsSettings.cs,
│       │                           CameraTransitionSettings.cs, CameraConfiguration.cs
│       └── (root)                  CameraId.cs, CameraPose.cs, ICameraTarget.cs,
│                                    TransformCameraTarget.cs, ICameraMode.cs, CameraModeContext.cs,
│                                    CameraModeKind.cs, CameraBoundsConstraint.cs,
│                                    CameraZoomController.cs, CameraTransitionRunner.cs,
│                                    CameraPoseController.cs, CameraEvents.cs,
│                                    ICameraOverrideHandle.cs, CameraOverrideHandle.cs,
│                                    CameraRuntimeState.cs, ICameraService.cs, CameraService.cs,
│                                    CameraController.cs, CameraDriver.cs, CameraBootstrapper.cs
│       └── Integration/
│           └── Cinemachine/        GameFramework.Cameras.Cinemachine.asmdef (Phase 11, optional -
│                                    only built if the Cinemachine package is installed)
│               ├── AssemblyInfo.cs InternalsVisibleTo for .Cinemachine.Tests(.Runtime)
│               └── (root)          CinemachineBoundsTranslator.cs, CinemachineCameraAdapter.cs,
│                                    CinemachineCameraBackend.cs
├── Editor/
│   ├── GameFramework.Editor.asmdef (Phase 3, extended Phase 4/10/11)
│   ├── Localization/             LocalizationTableValidator.cs
│   ├── Gameplay/                 GameplayConfigValidator.cs (Phase 4)
│   ├── Cameras/                  CameraConfigurationValidator.cs (Phase 11)
│   ├── Cameras/Cinemachine/      GameFramework.Cameras.Cinemachine.Editor.asmdef (Phase 11, optional) -
│   │                              CinemachineCameraSetupValidator.cs
│   └── UI/Navigation/            UINavigationCatalogValidator.cs (Phase 12)
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
│   │   ├── Gameplay/                    GameFramework.Gameplay.Tests.asmdef (EditMode, Phase 4)
│   │   │   ├── Entities/ , Lifecycle/ , Commands/ , Objectives/
│   │   └── Performance/                 GameFramework.Performance.Tests.asmdef (EditMode, Phase 5)
│   │       ├── Ticking/ , Profiling/
│   │   └── Cameras/                     GameFramework.Cameras.Tests.asmdef (EditMode, Phase 11)
│   │       └── Cinemachine/             GameFramework.Cameras.Cinemachine.Tests.asmdef (EditMode, optional)
│   └── Runtime/                         GameFramework.Core.Tests.Runtime.asmdef (PlayMode)
│       ├── Extensions/
│       ├── Framework/                   GameFramework.Runtime.Tests.Runtime.asmdef (PlayMode)
│       │   └── Bootstrap/
│       ├── Audio/                       GameFramework.Audio.Tests.Runtime.asmdef (PlayMode)
│       ├── UI/                          GameFramework.UI.Tests.Runtime.asmdef (PlayMode)
│       ├── PlayerSystems/               GameFramework.PlayerSystems.Tests.Runtime.asmdef (PlayMode)
│       ├── Gameplay/                    GameFramework.Gameplay.Tests.Runtime.asmdef (PlayMode, Phase 4)
│       │   ├── Pooling/ , Spawning/ , Interaction/    (Pooling/ also covers Phase 5 hardening)
│       ├── Cameras/                     GameFramework.Cameras.Tests.Runtime.asmdef (PlayMode, Phase 11)
│       │   └── Cinemachine/             GameFramework.Cameras.Cinemachine.Tests.Runtime.asmdef (PlayMode, optional)
│       └── UI/Navigation/               GameFramework.UI.Navigation.Tests.Runtime.asmdef (PlayMode, Phase 12 -
│                                          no EditMode variant, same reason as GameFramework.UI.Tests)
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
| `GameFramework.Gameplay` | `Runtime/Gameplay` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Performance` (sibling of `PlayerSystems` — no Input/UI/Audio/Feedback reference) | Gameplay loop, entity/component utilities, object lifecycle, spawning, pooling, commands, interaction/targeting, objectives/checkpoints. Composition root: `GameplayBootstrapper`. |
| `GameFramework.Performance` | `Runtime/Performance` | `GameFramework.Core`, `GameFramework.Runtime` (sibling of `PlayerSystems`/`Gameplay` — no Input/UI/Audio/Feedback/Gameplay reference) | Profiling markers/frame diagnostics, centralized tick system, memory diagnostics, resource-loading abstraction, mobile performance utilities, performance budgets. Composition root: `PerformanceBootstrapper`. |
| `GameFramework.Progression` | `Runtime/Progression` | `GameFramework.Core`, `GameFramework.Runtime` | Economy, Inventory, Experience (Phase 6) and Statistics (Phase 7) — mutually independent leaves, one assembly. |
| `GameFramework.Unlocks` | `Runtime/Unlocks` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Progression` | Composable requirement-gated unlock tracking. |
| `GameFramework.Rewards` | `Runtime/Rewards` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Progression`, `GameFramework.Unlocks` | Idempotent, transaction-safe reward grant orchestration. Composition root: `ProgressionBootstrapper`. |
| `GameFramework.Quests` | `Runtime/Quests` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Progression`, `GameFramework.Unlocks`, `GameFramework.Rewards`, `GameFramework.Gameplay` | Conditions, condition-backed Objectives, Quests, Achievements, Milestones (Phase 7). Composition root: `QuestsBootstrapper`. |
| `GameFramework.GameFlow` | `Runtime/GameFlow` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Gameplay` | Level-load/gameplay-session state machine, sessions, checkpoints/respawn, pause tokens, restart/retry (Phase 8). Composition root: `GameFlowBootstrapper`. |
| `GameFramework.Tutorials` | `Runtime/Tutorials` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Input` (sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow` — no UI/Localization/Audio/Feedback/Gameplay/GameFlow/Progression/Unlocks/Rewards/Quests reference) | Tutorial lifecycle/state machine, sequential steps (Instruction/Wait/Input/Event/Condition), prerequisites/repeat/skip/persistence policies, optional gameplay pause and input-context gating (Phase 9). Composition root: `TutorialBootstrapper`. |
| `GameFramework.Presentation` | `Runtime/Presentation` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Audio`, `GameFramework.Feedback`, `GameFramework.Gameplay`, `GameFramework.Performance`, `GameFramework.UI` (every dependency resolved *softly* at runtime — see [Architecture](#architecture)) | Coordinated feedback/presentation orchestration: `FeedbackDefinition` bundles Audio/Haptic/Camera/Visual/Screen/UI/Time channels behind one `IPresentationService.Play` call (Phase 10). Composition root: `PresentationBootstrapper`. |
| `GameFramework.Cameras` | `Runtime/Cameras` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Performance` (sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow`/`Tutorials` — no Input/UI/Audio/Feedback/Gameplay/Presentation reference either direction) | Camera orchestration: `ICameraService` (registration, base activation, override stack), `ICameraMode` (Follow/Static/TargetLook/Manual), world bounds, damped zoom, transitions (Phase 11). Composition root: `CameraBootstrapper`. |
| `GameFramework.Cameras.Cinemachine` | `Runtime/Cameras/Integration/Cinemachine` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Performance`, `GameFramework.Cameras`, `Cinemachine` (the only assembly in the framework that references it) | Optional alternative driver for `ICameraService`/`CameraController`, backed by a real `CinemachineVirtualCamera`/`CinemachineBrain` instead of `CameraDriver`'s pure-C# pipeline (Phase 11). No composition root/service of its own — plain scene composition (`CinemachineCameraAdapter` + `CinemachineCameraBackend`). |
| `GameFramework.Editor` | `Editor/` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Localization`, `GameFramework.Gameplay`, `GameFramework.Progression`, `GameFramework.Unlocks`, `GameFramework.Rewards`, `GameFramework.Quests`, `GameFramework.Tutorials`, `GameFramework.Presentation`, `GameFramework.Audio`, `GameFramework.Cameras` | Editor-only. Localization table, Gameplay config, Quest content, Tutorial content, Feedback content, and Camera Configuration validation menu items. |
| `GameFramework.Cameras.Cinemachine.Editor` | `Editor/Cameras/Cinemachine` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.Cameras`, `GameFramework.Cameras.Cinemachine`, `Cinemachine` | Editor-only, separate from `GameFramework.Editor` specifically so that assembly stays Cinemachine-free. Validates a scene's Cinemachine-backed cameras (missing Brain/Controller/Virtual Camera/Backend, duplicate controller ownership, an assigned Confiner with nothing to confine against). |
| `GameFramework.UI.Navigation` | `Runtime/UI/Navigation` | `GameFramework.Core`, `GameFramework.Runtime`, `GameFramework.UI` (hard), `GameFramework.Input`, `GameFramework.GameFlow` (both soft, resolved via `registry.TryGet`), `GameFramework.PlayerSystems` (only for `NavigationBootstrapper` to subclass `PlayerSystemsBootstrapper`) | UI navigation/menu-flow orchestration on top of Phase 3's `IUIService`: stable-id screen/popup registration, a navigation stack independent of Unity's scene history, back-navigation priority, typed parameters/results, guards, and centralized Android/back-button routing (Phase 12). Composition root: `NavigationBootstrapper`. |
| `GameFramework.Input.Tests` / `.Localization.Tests` / `.Audio.Tests` / `.Feedback.Tests` / `.Gameplay.Tests` / `.Performance.Tests` / `.Progression.Tests` / `.Unlocks.Tests` / `.Rewards.Tests` / `.Quests.Tests` / `.GameFlow.Tests` / `.Tutorials.Tests` / `.Presentation.Tests` / `.Cameras.Tests` / `.Cameras.Cinemachine.Tests` | `Tests/Editor/<System>` | matching runtime assembly + Core/Runtime, TestRunner | EditMode tests for each system's pure logic. |
| `GameFramework.Audio.Tests.Runtime` / `.UI.Tests.Runtime` / `.PlayerSystems.Tests.Runtime` / `.Gameplay.Tests.Runtime` / `.Presentation.Tests.Runtime` / `.Cameras.Tests.Runtime` / `.Cameras.Cinemachine.Tests.Runtime` / `.UI.Navigation.Tests.Runtime` | `Tests/Runtime/<System>` | matching runtime assembly + Core/Runtime, TestRunner | PlayMode tests for behavior that genuinely needs a running engine (real `AudioSource` playback, `AddComponent`-able UI test doubles, `GameBootstrapper.Awake`, real GameObject pooling/physics, Phase 5 pool-hardening additions live alongside the Phase 4 pooling tests here; Phase 10's visual-effect spawning, since `UnityEngine.Object.Destroy` is refused outside Play Mode; Phase 11's `CameraDriver.LateUpdate` actually firing, since EditMode never runs the player loop; the Cinemachine integration's activation/target/zoom/confiner wiring against a real `CinemachineBrain`/`CinemachineVirtualCamera`; Phase 12's `NavigationService` orchestrating real `UIScreen`/`UIPopup` instances). |

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

Phase 5 gets one new assembly, `GameFramework.Performance`, for the same reason Phase 4 got
`GameFramework.Gameplay`: it needs to be a compile-time-enforced sibling of `PlayerSystems`/
`Gameplay` (usable by either without depending on either), which an assembly boundary makes a
compile error to violate. Its six internal areas (Profiling/Ticking/Memory/Resources/Mobile, plus
the root `PerformanceBootstrapper`) share no dependency boundary worth enforcing against each
other, so they stay namespaces within one assembly rather than becoming six more `.asmdef`s — the
same reasoning Phase 2's five modules and Phase 4's eight subsystems already established. The one
new *reference* Phase 5 adds to an existing assembly is `GameFramework.Gameplay` → `GameFramework.Performance`
(pooling hardening uses `ProfileScope` and exposes `PoolStatistics`) — additive, one-way, and
already reflected in the table above.

Phase 6 gets three new assemblies (`GameFramework.Progression`, `.Unlocks`, `.Rewards`) matching the
real one-way relationships between them (see
[Progression, Economy, Inventory, Unlocks & Rewards](#progression-economy-inventory-unlocks--rewards)).
Phase 7 gets exactly one more, `GameFramework.Quests`, rather than splitting Conditions/Objectives/
Quests/Achievements/Milestones into five — they share no dependency boundary worth enforcing against
each other (the same reasoning behind Phase 2's five modules and Phase 5's five internal areas
staying namespaces in one assembly), and all of them need the same reference set (Progression,
Unlocks, Rewards, Gameplay) regardless. Statistics is the one Phase 7 exception: it became a new
*namespace inside* `GameFramework.Progression` rather than its own assembly, since it needs no
reference `GameFramework.Progression` doesn't already have and is a natural sibling of
Economy/Inventory/Experience.

Phase 9 gets exactly one new assembly, `GameFramework.Tutorials`, rather than splitting the step
system/conditions/persistence into more: they share no dependency boundary worth enforcing against
each other (the same reasoning behind every other phase's internal-namespace-vs-new-assembly
decision above), and the whole system needs the same three-reference set (Core/Runtime/Input)
regardless. Unlike Phase 7/8, it deliberately stays a *minimal* sibling — no reference on Gameplay/
GameFlow/Progression/Unlocks/Rewards/Quests at all, only Input (for logical-action sampling/context
gating) — so it remains usable by a game that has none of the framework's other higher-level
systems, per CLAUDE.md's Phase 9 brief.

Phase 10 gets exactly one new assembly, `GameFramework.Presentation`, for a different reason than
Phase 9's: not because its seven channels share no dependency boundary (they genuinely don't need
one - each is its own config type/executor, not cross-referencing the others), but because
CLAUDE.md's Phase 10 brief explicitly calls out that `GameFramework.Feedback` already exists
(Phase 3's haptics+audio-preset coordinator) and must not be overloaded with unrelated orchestration
responsibility - a new, distinctly-named assembly was the correct call, not a name collision to work
around by cramming Phase 10 into Phase 3's namespace. `Presentation` was chosen over `GameFeel`
(the brief's other suggested name) as a plainer noun consistent with this framework's existing
naming (`Progression`, `Unlocks`, `Rewards`, `GameFlow`, `Tutorials`) rather than game-development
jargon. See [Architecture](#architecture) for why this one assembly's *reference* list is wider than
every sibling before it, and every one of those references is still resolved softly at runtime.

Phase 11 gets exactly one new assembly, `GameFramework.Cameras`, going back to the *minimal* sibling
shape Phase 9/8 established rather than Phase 10's wide-orchestration one: its internal areas
(Modes/Configuration, plus the root service/controller/driver types) share no dependency boundary
worth enforcing against each other, so they stay namespaces within one assembly. It references only
Core/Runtime/Performance (the last purely for `ProfileScope`); it has no reference on
`GameFramework.Presentation` at all, in either direction — see [Architecture](#architecture) for how
Phase 11's `CameraDriver` and Phase 10's `Presentation.CameraFeedbackDriver` still compose correctly
on the same camera with zero assembly coupling, through `[DefaultExecutionOrder(-100)]` alone.

Phase 12 gets exactly one new assembly, `GameFramework.UI.Navigation`, rather than the multi-assembly
split (`.Navigation.Core`/`.Navigation.Runtime`/`.Navigation.Editor`) CLAUDE.md's Phase 12 brief
floated as one possible shape — its internal areas (ids, requests/results, guards, the internal
registries/stack-entry types, the catalog ScriptableObjects) share no dependency boundary worth
enforcing against each other, the same reasoning behind every other phase's internal-namespace-vs-
new-assembly decision above. Unlike Phase 9's/Phase 11's *minimal*-sibling shape, this one has a real
**hard** reference on `GameFramework.UI` (it orchestrates Phase 3's screen/popup stack directly,
never merely soft-looks-up an optional channel the way `Presentation` does its seven), which is also
why its bootstrapper (`NavigationBootstrapper`) subclasses `PlayerSystemsBootstrapper` directly rather
than sitting alongside it as a sibling `GameBootstrapper` subclass — the same reasoning
`Quests.QuestsBootstrapper` already established for its own hard dependency on Rewards. Its editor
tooling (`UINavigationCatalogValidator`) lives in the shared `GameFramework.Editor` assembly rather
than a separate one, since (unlike the Cinemachine integration) it introduces no third-party package
dependency that assembly needs to stay free of.

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
- `GameFramework.Performance` — `PerformanceBootstrapper`.
- `GameFramework.Performance.Profiling` — `ProfilingCategory`, `ProfilingMode`, `PerformanceSettings`, `ProfileScope`, `PerformanceBudget`, `FrameTimeStats`, `IPerformanceMonitorService`, `PerformanceMonitorService`, `PerformanceOverlay`.
- `GameFramework.Performance.Ticking` — `ITickable`, `IFixedTickable`, `ILateTickable`, `TickGroup`, `ITickService`, `TickService`.
- `GameFramework.Performance.Memory` — `ManagedMemorySample`, `MemoryDiagnostics`.
- `GameFramework.Performance.Resources` — `IAssetHandle<T>`, `IAssetProvider`, `ResourcesAssetProvider`.
- `GameFramework.Performance.Mobile` — `DeviceInfo`, `PerformanceProfile`, `PerformanceProfileConfig`, `IMobilePerformanceService`, `MobilePerformanceService`, `IApplicationLifecycleService`, `ApplicationLifecycleService`, `ApplicationPausedEvent`/`ApplicationResumedEvent`/`ApplicationFocusChangedEvent`/`ApplicationQuittingEvent`.
- `GameFramework.Gameplay.Pooling` also gains `PoolStatistics` in Phase 5 (see [Pooling](#pooling) — extended, not moved).
- `GameFramework.GameFlow` — `LevelFlowState`, `TransitionResult`, `LevelFlowStateMachine` (internal), `LevelId`, `LevelDefinition`, `GameplayResult`/`GameplayResultKind`, `LevelLoadResult`, `RespawnResult`, `IGameFlowService`, `GameFlowService`, `GameFlowBootstrapper`, `IPauseToken`, and the `*Event` structs listed under [Game Flow, Sessions & Checkpoints](#game-flow-sessions--checkpoints).
- `GameFramework.GameFlow.Session` — `SessionId`, `GameplaySession`, `GameplaySessionState`.
- `GameFramework.GameFlow.Checkpoints` — `IGameplaySnapshot`, `CheckpointRecord`, `CheckpointSystem`.
- `GameFramework.Cameras` — `CameraId`, `CameraPose`, `ICameraTarget`, `TransformCameraTarget` (its `Transform` getter added for the Cinemachine integration), `ICameraMode`, `CameraModeContext`, `CameraModeKind`, `CameraBoundsConstraint` (internal), `CameraZoomController` (internal), `CameraTransitionRunner` (internal), `CameraPoseController` (internal), `ICameraService`, `CameraService` (its `ReduceMotionSettingKey` made public for the same reason), `CameraController` (its `Target` getter added for the same reason), `CameraDriver`, `CameraBootstrapper`, `ICameraOverrideHandle`, `CameraOverrideHandle` (internal), `CameraRuntimeState`, and the `*Event` structs (`CameraRegisteredEvent`/`CameraUnregisteredEvent`/`ActiveCameraChangedEvent`/`CameraTargetChangedEvent`/`CameraOverridePushedEvent`/`CameraOverridePoppedEvent`/`CameraResetEvent`, the last one added for the Cinemachine integration - see [Cinemachine Integration](#cinemachine-integration)).
- `GameFramework.Cameras.Modes` — `CameraDeadZone` (internal), `FollowCameraMode`/`StaticCameraMode`/`TargetLookCameraMode`/`ManualCameraMode` (all internal — built-in strategies behind the public `ICameraMode`, the same "internal implementation behind a public seam" shape `Presentation.CameraShakeState` already established).
- `GameFramework.Cameras.Configuration` — `CameraFollowSettings`, `CameraTargetLookSettings`, `CameraZoomSettings`, `CameraBoundsSettings`, `CameraTransitionSettings`, `CameraConfiguration`.
- `GameFramework.Editor.Cameras` — `CameraConfigurationValidator` (Editor-only).
- `GameFramework.Cameras.CinemachineIntegration` — `CinemachineCameraAdapter`, `CinemachineCameraBackend`, `CinemachineBoundsTranslator` (internal). Optional (only present when the Cinemachine package is installed) - see [Cinemachine Integration](#cinemachine-integration) for why this namespace, not `GameFramework.Cameras.Cinemachine`, despite the *assembly* being named that.
- `GameFramework.Editor.Cameras.CinemachineIntegration` — `CinemachineCameraSetupValidator` (Editor-only, optional).
- `GameFramework.UI.Navigation` — `UIScreenId`, `UIPopupId`, `NavigationMode`, `NavigationResultKind`, `NavigationResult`, `NavigationRequestOptions`, `IUINavigationParameterReceiver`, `IUINavigationBackHandler`, `IUINavigationTransitionHandler`, `INavigationGuard`, `NavigationGuardResult`, `NavigationGuardContext`, `INavigationService`, `NavigationService`, `NavigationBootstrapper`, `NavigationDiagnosticsSnapshot`, the `*Event` structs (`ScreenNavigatedEvent`/`PopupOpenedEvent`/`PopupClosedEvent`/`BackRequestedAtRootEvent`/`NavigationBlockedEvent`), `UIScreenRegistry`/`UIPopupRegistry`/`NavigationEntry`/`PopupEntry`/`NavigationCoroutineRunner`/`NavigationBackButtonDriver` (all internal), and `UINavigationCatalog`/`UINavigationScreenEntry`/`UINavigationPopupEntry` (optional ScriptableObject-based bulk registration).
- `GameFramework.Editor.UI.Navigation` — `UINavigationCatalogValidator` (Editor-only).

A naming note: `GameFramework.Runtime.Time` and `GameFramework.Runtime.Timers` share a word with
`UnityEngine.Time`/nothing, respectively, but that hasn't caused the ambiguity you might expect —
see [Bootstrap](#bootstrap)'s note on the one real collision Phase 1 hit (`Log` the class vs `Log`
the method) for how that class of problem actually arises and gets fixed. `GameFramework.Cameras`
took a different fix for the same underlying risk: `Presentation.ICameraFeedbackDriver`'s own remarks
already document that a nested `.Camera` namespace segment shadows `UnityEngine.Camera` for any bare
`Camera` reference in that segment's own ancestor-namespace chain, which is why Presentation's camera
files stay in a flat `GameFramework.Presentation` namespace despite living under a `Camera/` folder.
Phase 11's entire job is manipulating `UnityEngine.Camera` — as a field type, `GetComponent<Camera>()`,
etc. — in nearly every file, so fully qualifying `UnityEngine.Camera` everywhere (`Runtime.Time`'s own
fix for its much smaller handful of `UnityEngine.Time.*` references) would be pervasive and easy to
get wrong by omission. The assembly/namespace is `GameFramework.Cameras` (plural) instead — a
different string entirely, so it is never found ahead of `UnityEngine.Camera` during simple-name
lookup, with no qualification needed anywhere in the assembly.

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

**Phase 5 hardening.** `Release` now also rejects a foreign object — one this specific pool never
handed out via `Get` (tracked in a `HashSet<GameObject>` populated/cleared in the same `actionOnGet`/
`actionOnRelease` callbacks `ObjectPool<T>` already calls, so the check is O(1) and adds no new hot
path) — logging instead of silently admitting it into the free list, which would otherwise corrupt
the pool. `Dispose` now warns (but still disposes) if any instances are still active, since those
references become dangling once the container-owning pool is gone. `Get`/`Release` are wrapped in a
`ProfileScope(ProfilingCategory.Pooling)` — a no-op branch when `PerformanceSettings.Mode` is
`Disabled`, so this costs nothing in a release build. `GameObjectPool.Statistics` exposes a
`PoolStatistics` snapshot (Get/Release counts, miss count, total-created count, peak/current
active/inactive) for development diagnostics — read-only, no effect on the pool. `PrewarmStagedRoutine(totalCount, perFrame)`
spreads prewarming across multiple frames (the caller drives it via its own `StartCoroutine`) for
the rare case a large prewarm count causes a visible spike; plain `Prewarm` remains the default for
everything else. **Known limitation, stated plainly:** the active-instance tracking above only
catches an instance destroyed externally *while inactive* (the case `Get` already handled before
Phase 5); an instance destroyed externally *while still checked out* leaves a stale entry in the
tracking set until the pool is disposed — a general fix would need scene-unload hooks this pass
didn't add, since profiling gave no evidence it's a real problem worth the added complexity.

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

## Performance Infrastructure

Phase 5. A cross-cutting layer that measures and hardens what Phases 0–4 already built — it adds no
game-specific content, and per [Architecture](#architecture) it stays a sibling of
`GameFramework.PlayerSystems`/`GameFramework.Gameplay`, not a dependency of either. Registered
services: `ITickService`, `IPerformanceMonitorService`, `IApplicationLifecycleService`,
`IMobilePerformanceService` — see [Game Flow Integration](#game-flow-integration-1) for how a game
registers them. `MemoryDiagnostics` and `DeviceInfo` are static, dependency-free utilities, not
services, since they have no lifecycle of their own — just point-in-time engine queries.

**Reference budgets, not guarantees.** Every frame-rate/budget number this layer works with
(`IPerformanceMonitorService.TargetFrameRate`, `FrameBudgetMilliseconds`, a registered
`PerformanceBudget`) is a *configured target* a game opts into, checked against *measured* values.
None of it is a guarantee: actual performance depends on the device, resolution, scene content, GPU/
CPU headroom, thermal state, and Unity's own configuration, none of which this framework controls.

### Profiling & Diagnostics

`ProfileScope` (`GameFramework.Performance.Profiling`) wraps Unity's own `Unity.Profiling.ProfilerMarker`
Begin/End — this is not a replacement for the Unity Profiler, just a consistently-categorized way to
mark framework boundaries that show up in it:

```csharp
using (new ProfileScope(ProfilingCategory.Pooling))
{
    // ... work you want visible as "GameFramework.Pooling" in the Unity Profiler ...
}
```

`ProfilingCategory` is a fixed, small enum (Framework, Gameplay, Input, UI, Audio, Spawning,
Pooling, Physics, Rendering, Loading, Persistence) — add a category only for a real, recurring
boundary, never per-method. Every `ProfileScope` is gated by the static `PerformanceSettings.Mode`
(`Disabled`/`Development`/`Detailed`, defaulting to `Development` in the Editor/development builds
and `Disabled` otherwise via `#if`): when `Disabled`, construction/disposal is a single branch with
no marker call, so leaving `ProfileScope` in a hot path (pooling's `Get`/`Release`, the tick loop)
costs nothing in a release build.

`IPerformanceMonitorService` (`PerformanceMonitorService`) samples real, unscaled frame time once
per frame via `IUpdatableService.Tick()` — the same mechanism every other per-frame service uses —
into a fixed-size (120-sample) rolling buffer, with no per-frame allocation:

```csharp
IPerformanceMonitorService monitor = GameBootstrapper.Instance.Services.Get<IPerformanceMonitorService>();
monitor.TargetFrameRate = 30; // FrameBudgetMilliseconds becomes 33.33ms
monitor.RegisterBudget("GameplayTick", 8f);

FrameTimeStats stats = monitor.GetFrameStats(); // Last/Average/Worst frame ms, spike count

// Elsewhere, after doing the timed work yourself (a Stopwatch, a ProfilerMarker readback, etc.):
monitor.ReportSample("GameplayTick", elapsedMilliseconds); // rate-limited warning if over budget
```

**What this can and cannot tell you.** Frame time here is wall-clock CPU frame duration
(`ITimeService.UnscaledDeltaTime`) between two `Tick()` calls — a real, measured value. It cannot
isolate GPU time, cannot attribute a spike to a specific system, and is not a substitute for the
Unity Profiler or a platform's native tools. A frame whose time exceeds `SpikeThresholdMilliseconds`
(defaults to `FrameBudgetMilliseconds`) increments a spike counter and logs a warning — rate-limited
to once per `SpikeLogIntervalSeconds` (default 2s), never every frame. `ReportSample` follows the
same rate-limiting per budget name.

**Overlay.** `PerformanceOverlay` (add it to any `GameObject` yourself — no Bootstrapper adds it
automatically) is an optional, development-only `OnGUI` HUD showing FPS/frame time/spike
count/managed memory, toggled with F9 by default. It is compiled out of non-development builds
entirely (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`) rather than merely hidden. `OnGUI` was chosen
deliberately over `GameFramework.UI`'s canvas/screen machinery — a debug HUD has nothing to do with
player-facing UI, and pulling in Localization/Audio/Feedback for one label would be exactly the kind
of unnecessary dependency [Architecture](#architecture) avoids.

### Tick System

Phase 4 intentionally avoided a general update architecture (`IGameplayService`'s tick interfaces
are scoped to one gameplay session). Phase 5 adds `ITickService` — a lower-level, always-on
primitive usable by *any* system, gameplay session or not:

```csharp
public class Enemy : MonoBehaviour, ITickable
{
    private void OnEnable() => tickService.Register(this, priority: 0);
    private void OnDisable() => tickService.Unregister(this);
    public void Tick(float deltaTime) { /* instead of Update() */ }
}
```

**How this differs from `IGameplayTickable`.** `ITickable.Tick` fires every frame with
`ITimeService.ScaledDeltaTime` — which is itself 0 while `ITimeService.IsPaused`, the same
relationship Unity's own `Update` + `Time.deltaTime` has to `Time.timeScale`. It does **not** stop
being called while paused (unlike `IGameplayTickable`, which `GameplayService` stops calling
entirely). Use `IGameplayTickable` when a paused gameplay session should stop an object ticking at
all; use `ITickable` for framework-level or session-independent per-frame work (a debug overlay, an
always-running background system).

**Three phases**, matching `IGameplayFixedTickable`/`IGameplayLateTickable`'s split for the same
reason: `RegisterFixed`/`IFixedTickable.FixedTick` (Unity's `FixedUpdate`, integrates correctly with
physics timing — never simulate physics from the variable phase) and `RegisterLate`/`ILateTickable.LateTick`
(Unity's `LateUpdate` — camera/presentation work that must run after every `Tick` has moved things
for the frame). The variable phase reuses `IUpdatableService.Tick()` via `GameBootstrapper.Update()`
exactly like every other framework service; Fixed/Late go through `TickService`'s own tiny
`DontDestroyOnLoad` driver (`TickServiceDriver`), the same pattern `GameplayLoopDriver` and
`AudioApplicationLifecycleHook` already established, since a plain C# service cannot receive those
Unity callbacks directly.

**Priority** is optional (`Register(tickable, priority: n)`, default 0, lower ticks first) and
sorted on insert — registration is far less frequent than ticking, so sorting every frame was never
on the table. `TickGroup` (Gameplay/Physics/AI/Animation/Presentation) is recorded alongside a
registration for diagnostics only; it does not change execution order.

**Registration safety.** Register/Unregister are idempotent no-ops on a duplicate call, matching
`IEventService.Subscribe`'s semantics. Each tick phase snapshots its live registration list into a
reused, geometrically-grown buffer before invoking anything — the same technique `IEventService.Publish`
uses via `ArrayPool<Delegate>` — so Register/Unregister called from *inside* a tick callback
(including a tickable unregistering itself, or registering a new one) never corrupts the in-progress
loop: a mid-tick change is picked up on the *next* tick, never the one already snapshotted. Each
invocation is wrapped in try/catch and logged through the established logger on failure (category
`"Ticking"`) — one throwing tickable never stops the rest of that tick from running.

### Allocation & GC Policy

Phase 5 audited Phases 0–4 for the patterns listed in the project's engineering rules (`FindObjectOfType`,
`GameObject.Find`, `SendMessage`, LINQ, repeated `GetComponent`, per-frame `new List`/`new Dictionary`/
`new[]`, uncached delegates) across every `Runtime/` assembly. **Finding, stated honestly: none of
those patterns exist in a hot path anywhere in Phases 0–4.** `FindObjectOfType`/`GameObject.Find`/
`SendMessage`/LINQ do not appear in `Runtime/` at all; every `GetComponent` call already caches its
result (`LocalizedText`/`LocalizedImage`/`LocalizedTMPText`/`UIButtonFeedback`); the two
`GetComponentInParent`/`GetComponentInChildren` calls are `ComponentLookup`'s own documented,
opt-in, call-site-visible utility, not a hidden hot-path cost; `Dictionary.Values` iteration
(`InputService.Tick`) does not box or allocate (a concretely-typed `ValueCollection` enumerator is a
struct). This is why Phase 5 adds hardening and new capability rather than a wave of hot-path
rewrites — there was no measured problem to fix, only new infrastructure to build carefully so it
doesn't introduce one. This audit is a snapshot, not a standing guarantee for code added after it.

**The policy going forward**, for this layer and anything built on it:

- **Hot paths** (tick loops, pooling `Get`/`Release`, physics queries, input polling, spawning) —
  no LINQ, no per-call allocation, no uncached `GetComponent`, no reflection, no `Find*`. `ProfileScope`
  is safe here specifically because it is a `readonly struct` gated by a single branch.
- **Initialization** (service `Initialize`, pool construction, `DeviceInfo`'s one-time capture) — a
  more expensive one-time operation is fine if it prevents repeated runtime work; nothing here is
  optimized at the expense of clarity.
- **Editor-only code** — clarity over runtime micro-optimization; none of it ships.
- **Rare/explicit operations** (`ResourcesAssetProvider.LoadAsync`'s one closure per call,
  `TickRegistry<T>.Register`'s O(n) `Contains` check) — acceptable; these are not per-frame paths,
  and the alternative (a second index structure) would add real complexity for a cost nothing has
  measured as a problem.
- **`GC.Collect()`** is never called anywhere in this framework, and nothing in Phase 5 introduces a
  case for it — forcing a full collection during gameplay trades a predictable small cost for an
  unpredictable large one, which is the opposite of what a frame-budget-conscious framework wants.
- **Logging** — `ILoggingService.Log`/`Log.*` check `IsEnabled` before formatting (Phase 1); nothing
  in Phase 5 eagerly builds a diagnostic string when its log level/category is disabled.

See [Pooling](#pooling)'s "Phase 5 hardening" note for the one place this phase touched an existing
hot path directly, and [Performance Test Scene](#performance-test-scene) for the measurements that
justify these choices.

### Memory Management

`MemoryDiagnostics.Sample()` (static, no service, no lifecycle) returns a `ManagedMemorySample`:
`System.GC.GetTotalMemory(false)` always (the managed heap's best estimate, not total process
memory), plus `UnityEngine.Profiling.Profiler`'s allocated/reserved figures **only** in the Editor or
a development build (`ManagedMemorySample.HasUnityMemoryData` is `false` otherwise, rather than
presenting an unreliable 0 as if it were real) — this is not a replacement for the Unity Memory
Profiler, just "roughly how much managed memory is in use right now."

**Ownership audit, stated plainly** (see the project's Memory Rules for the questions this answers):
static state in this framework is limited to `PerformanceSettings.Mode` (a config switch, not a
retained reference) and the per-type marker array `ProfileScope` builds once; neither retains a
scene, GameObject, or asset. `IEventService` subscriptions are the framework's one standing
leak risk across every phase — a subscriber that never unsubscribes stays referenced by the
publisher for as long as both are alive — Phase 5 adds no new event-subscription pattern beyond
`Subscribe`'s existing `IEventSubscription.Dispose()` cleanup. `GameObjectPool`'s new active-instance
tracking (see [Pooling](#pooling)) holds `GameObject` references only between `Get` and `Release`/
eviction — never after. `IAssetProvider`'s handles are the one place Phase 5 introduces a new
"forgot to release" risk (see [Resource Management](#resource-management)) — deliberately explicit
(a handle, not an implicit cache) so that risk is visible at the call site rather than hidden.

### Resource Management

The project does not use Addressables or AssetBundles anywhere (checked before building this) — so
per the project's Resource Rules, Phase 5 does not introduce Addressables speculatively. `IAssetProvider`/
`ResourcesAssetProvider` (`GameFramework.Performance.Resources`) is a thin, optional abstraction over
`UnityEngine.Resources`, reference-counted per key:

```csharp
IAssetProvider assets = new ResourcesAssetProvider(); // constructed directly - not a registered service
IAssetHandle<AudioClip> handle = assets.Load<AudioClip>("Sfx/Explosion");
if (handle.IsLoaded) { /* use handle.Asset */ }
handle.Release(); // decrements the ref count; the cache entry drops once every handle is released

assets.LoadAsync<GameObject>("Enemies/Grunt", h => { /* ... */ }, owner: this);
// owner is optional - if it's destroyed before loading finishes, the callback never fires.
```

Not a registered service, deliberately: an asset provider's lifetime is naturally tied to whatever
owns it (a level, a game system), not the application. A second `Load` for an already-cached key
returns a handle to the same asset and increments the count instead of loading again. Dropping a
cache entry does **not** force-unload a `GameObject`/`Component` asset — `Resources.UnloadAsset`
does not support those types — they remain eligible for the next `Resources.UnloadUnusedAssets()`,
exactly like any other unreferenced Resources asset. If a project adopts Addressables later, it can
implement `IAssetProvider` without call sites changing.

### Mobile Performance Utilities

`IMobilePerformanceService` (`MobilePerformanceService`) wraps the handful of settings a mobile game
commonly needs — never every Unity rendering setting:

```csharp
IMobilePerformanceService perf = GameBootstrapper.Instance.Services.Get<IMobilePerformanceService>();
perf.ConfigureProfile(PerformanceProfile.Low, new PerformanceProfileConfig(qualityLevel: 0, resolutionScale: 0.75f, targetFrameRate: 30));
perf.ApplyProfile(PerformanceProfile.Low); // sets quality level, resolution (via Screen.SetResolution), and Application.targetFrameRate together
```

Default profile configs (Low/Medium/High) are derived from however many Quality levels the *project*
actually has configured (`QualitySettings.names.Length`) — never a hard-coded index count — and are
meant to be overridden via `ConfigureProfile`, not treated as correct for any specific game. Nothing
picks a profile automatically from `DeviceInfo`; that mapping is a game decision.

`DeviceInfo` (static, captured once on first access) exposes `Platform`/`ProcessorCount`/
`SystemMemoryMegabytes`/`GraphicsMemoryMegabytes`/`GraphicsDeviceName`/`ScreenWidth`/`ScreenHeight`
via `SystemInfo`/`Screen` — **hints for a coarse decision, not guarantees**: OS-reported memory can be
inaccurate or capped, and nothing here accounts for other apps competing for the same device or for
thermal throttling. This framework does not attempt universal thermal management (there is no
reliable cross-platform API for it) — if a project has platform-specific thermal signals, it reacts
to them itself; `IMobilePerformanceService.ApplyProfile` is the hook a lower quality profile would go
through.

**Application lifecycle**, centralized once rather than duplicated per subsystem:
`IApplicationLifecycleService` (`ApplicationLifecycleService`) relays `OnApplicationPause`/
`OnApplicationFocus`/`OnApplicationQuit` (via its own tiny driver, the same
`GameplayLoopDriver`/`AudioApplicationLifecycleHook` pattern) and republishes them as
`ApplicationPausedEvent`/`ApplicationResumedEvent`/`ApplicationFocusChangedEvent`/`ApplicationQuittingEvent`
through the Phase 2 `IEventService` — subscribe to those instead of writing another
`OnApplicationPause` handler. On the OS backgrounding the app, it calls `ITimeService.Pause()` (and
`Resume()` on foregrounding) — reusing Phase 2's existing reference-counted pause rather than a
second pause mechanism, which is also exactly what stops `ITickable`s (and everything else gated on
`IsPaused`) from doing unnecessary work while backgrounded, satisfying the project's battery-usage
rule without every gameplay system needing its own handler. Because `Pause`/`Resume` are
reference-counted, this composes correctly with a game's own pause menu.

### Performance Budgets & Validation

Budgets are plain, named, game-registered values (`IPerformanceMonitorService.RegisterBudget`) —
never a fixed universal set every project must fill in. `ReportSample` checks a measured value
against a registered budget and logs a rate-limited warning when it's exceeded; an unregistered name
is a silent no-op (a game that never calls `RegisterBudget` pays nothing extra). See
[Performance Test Scene](#performance-test-scene) for the benchmarks/stress tests this phase actually
ran, and the framework's top-level performance baseline in this repository's Phase 5 completion
report for the real numbers those runs produced.

### Game Flow Integration {#game-flow-integration-1}

No new GameManager, matching every previous phase. `PerformanceBootstrapper` (`GameFramework.Performance`)
is a `GameBootstrapper` subclass — sibling to `PlayerSystemsBootstrapper`/`GameplayBootstrapper`, not
descending from either — that registers `ITickService`/`IPerformanceMonitorService`/
`IApplicationLifecycleService`/`IMobilePerformanceService` the same way every other phase's
bootstrapper subclass adds its own services. A game wanting Performance alongside Player Systems
and/or Gameplay Infrastructure combines them in its own small subclass:

```csharp
public class MyGameBootstrapper : PlayerSystemsBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1/2/3
        registry.Register<IGameplayService>(new GameplayService());
        registry.Register<IPoolService>(new PoolService());
        registry.Register<ITickService>(new TickService());
        registry.Register<IPerformanceMonitorService>(new PerformanceMonitorService());
        registry.Register<IApplicationLifecycleService>(new ApplicationLifecycleService());
        registry.Register<IMobilePerformanceService>(new MobilePerformanceService());
    }
}
```

### Performance Test Scene

`Assets/GameFramework/Samples/Phase5Benchmark/` — an isolated benchmark/stress-test scene, not added
to Build Settings, matching every other phase's demo. `Phase5BenchmarkController` builds everything
in code (no authored assets): number keys register 100/500/1000 synthetic `ITickable`s, `T` runs a
100-call `Stopwatch`-timed tick benchmark against whatever's registered, `P` runs a 1000-cycle pooled
`Get`/`Release` benchmark, `M` samples managed memory, `R` logs a combined development report (frame
stats, tickable counts, pool statistics, memory). Every number it prints comes from a real
`Stopwatch`/`IPerformanceMonitorService`/`GameObjectPool.Statistics` reading of the actual production
code path - see this repository's Phase 5 completion report for the specific measurements one run of
this scene produced, and this section's remarks throughout on what those numbers do and don't prove.

## Progression, Economy, Inventory, Unlocks & Rewards

Phase 6. Three assemblies, one dependency chain, matching the real one-way relationships between
them (the same reasoning Phase 3 used to split UI from Localization/Audio/Feedback):

```text
GameFramework.Progression   (Economy, Inventory, Experience — mutually independent leaves,
                              one assembly, like Phase 2's five modules)
        ↓
GameFramework.Unlocks       (requirement types reference Economy/Inventory/Experience's
                              read APIs)
        ↓
GameFramework.Rewards       (orchestrates all four; composition root: ProgressionBootstrapper)
```

**Definitions vs. state, strictly separated.** Every `*Definition` ScriptableObject
(`CurrencyDefinition`, `ItemDefinition`, `ProgressionCurveDefinition`, `UnlockDefinition`,
`RewardDefinition`) is pure authoring data — id/display text/bounds only, exposed as read-only
properties over private `[SerializeField]`s, never mutated at runtime. Every mutable balance/
quantity/level/unlocked-set/claimed-set lives inside the corresponding service
(`EconomyService`/`InventoryService`/`ExperienceService`/`UnlockService`/`RewardService`), never on
the definition asset — the project's rule against treating a shared ScriptableObject as mutable
player-save-state is enforced by construction here, not just by convention.

**Ids are stable, designer-authored strings**, not runtime-generated — `CurrencyId`/`ItemId`/
`UnlockId`/`RewardId` are thin `readonly struct` wrappers (the same shape as Phase 4's `EntityId`,
but string-backed instead of a process-lifetime counter, because these must survive save data and
content changes across sessions).

**Persistence: one save key per domain, not one shared blob.** Each service persists itself
independently (`"GameFramework.Progression.Economy"`, `"...Inventory"`, `"...Experience"`,
`"GameFramework.Unlocks"`, `"GameFramework.Rewards"`) through the existing Phase 2
`IPersistenceService`, using the exact same explicit `Save()`/`Load()` + dirty-flag-on-`Shutdown()`
policy `SettingsService` already established — nothing here writes to disk on every mutation, and
nothing introduces a second persistence layer or a monolithic "PlayerState" save blob. Splitting by
domain means each can be versioned/migrated independently later, rather than one schema change
forcing a migration of everything.

### Economy

```csharp
IEconomyService economy = GameBootstrapper.Instance.Services.Get<IEconomyService>();

economy.TryAdd(new CurrencyId("Coins"), 100, reason: "LevelCompletion");
if (economy.CanAfford(new CurrencyId("Coins"), 500))
{
    economy.TrySpend(new CurrencyId("Coins"), 500, reason: "Purchase");
}
```

Arbitrary currencies — the framework hard-codes neither "Coins" nor "Gems", only `CurrencyId`.
`CurrencyDefinition` supplies `MaxBalance`/`MinBalance` (0 = no maximum); every `TryAdd`/`TrySpend`/
`SetBalance` clamps to that range and widens to `long` internally before clamping, so a large grant
can never silently overflow `int`. Zero/negative amounts and unregistered currencies are rejected
(logged, never thrown — an expected gameplay failure is a result, not an exception) and never
change the balance. `GetRecentTransactions()` returns the last 50 balance changes
(`EconomyTransaction`: id/currency/delta/balance-after/reason/timestamp) as an in-memory,
non-persisted ring buffer — for debugging/UI/an analytics adapter, explicitly not a financial
ledger.

### Inventory

```csharp
IInventoryService inventory = GameBootstrapper.Instance.Services.Get<IInventoryService>();

InventoryOperationResult result = inventory.TryAdd(new ItemId("HealthPotion"), 150);
// result.Success == true, result.AppliedAmount == 99, result.Remainder == 51
// (MaxStack = 99 on this item's ItemDefinition)
```

Quantity-based (one aggregate count per `ItemId`), not slot/grid-based. `ItemDefinition.MaxStack`
(0/negative = unlimited) caps the total owned quantity; overflowing it is never a silent item loss
— `TryAdd` returns exactly how much was applied and exactly how much wasn't
(`InventoryOperationResult.Remainder`), a deliberate choice over silently dropping the excess or
rejecting the whole request. `TryRemove` is all-or-nothing (never removes a partial amount) and
fails with `InsufficientQuantity` rather than removing what's available. `ItemDefinition.Category`
is a free-form string, not a closed enum — the framework does not hard-code
Consumable/Equipment/Vehicle/... category names.

### Experience

```csharp
IExperienceService experience = GameBootstrapper.Instance.Services.Get<IExperienceService>();

AddExperienceResult result = experience.AddExperience(950, reason: "Quest");
// Level 4, 950 XP granted on a flat-100-per-level curve -> Level 10, 50 XP remaining,
// with one LevelChangedEvent published per level actually crossed (4->5, 5->6, ..., 9->10).
```

`IProgressionCurve.GetRequiredExperience(level)` is the only curve contract — `ProgressionCurveDefinition`
implements it in either `Linear` mode (`BaseExperience + (level-1) * IncrementPerLevel`, with an
optional `MaxLevel` cap) or `Table` mode (an explicit per-level array; the table's length is
automatically the effective max level). A curve reporting `0` (or negative) for the current level
means "no further level exists" — `IsAtMaxLevel` reads exactly that, and a further
`AddExperience` call at max level is a no-op (`AddExperienceResult.AtMaxLevel`), not silently
accumulating meaningless excess XP. A large grant applies every level-up it crosses in one
deterministic pass (integer/long math throughout, no floating point) and publishes one
`LevelChangedEvent` per level actually crossed — never only the final transition — so a listener
that reacts to every intermediate level (e.g. a per-level unlock check) never misses one.

### Unlocks

```csharp
IUnlockService unlocks = GameBootstrapper.Instance.Services.Get<IUnlockService>();

unlocks.RegisterUnlock(veteranCarDefinition, new AllRequirement(
    new LevelRequirement(experience, minLevel: 10),
    new AnyRequirement(
        new CurrencyRequirement(economy, new CurrencyId("Coins"), 500),
        new PrerequisiteUnlockRequirement(unlocks, new UnlockId("StarterCar")))));

UnlockResult result = unlocks.TryUnlock(new UnlockId("VeteranCar"));
```

`IUnlockRequirement` (`IsSatisfied()`/`Describe()`) is composed, not a giant conditional —
`LevelRequirement`/`CurrencyRequirement`/`ItemRequirement` each wrap a direct reference to the
service they check (constructor-injected, so each is independently unit-testable without a live
service registry), and `AllRequirement`/`AnyRequirement` combine them (AND/OR; a vacuous
`AllRequirement` with no children is satisfied, a vacuous `AnyRequirement` is not — the correct
identity element for each). `UnlockDefinition` itself carries only id/display text; a requirement
is associated with it via `RegisterUnlock` in code, the same register-in-code pattern
`ISettingsService.Register` already established, deliberately avoiding a `[SerializeReference]`
custom-drawer just to author AND/OR trees in the Inspector.

**Prerequisites and cycles.** `PrerequisiteUnlockRequirement` checks `IsUnlocked` (a flag lookup),
never re-evaluates the target's own requirement — this is what makes a requirement cycle (A needs
B, B needs A) a safe, detectable *permanent content deadlock* rather than infinite recursion:
neither ever gets unlocked, but nothing ever recurses to find that out. `UnlockService.ValidateNoCycles()`
walks every registered requirement tree for prerequisite edges and runs a standard DFS cycle
detection over them (call it as part of content validation/CI, not at runtime) — it returns the
ids involved in the first cycle found, logged as an error, so a content mistake like this is caught
before it ships rather than discovered as "this content can never be unlocked" in QA.

`RegisterUnlock` happens *after* `IUnlockService.Initialize` (a requirement typically needs another
already-initialized service resolved from the registry) — see
[Game Flow Integration](#game-flow-integration-2) for exactly where that happens, and note that
`Load()` therefore cannot validate persisted unlocked-ids against registered content the way
Economy/Inventory validate against their constructor-injected definitions (see `UnlockService`'s
remarks in source for the full explanation — an id from removed content just sits unused in the
unlocked set, which is harmless, not silent corruption).

### Rewards

```csharp
IRewardService rewards = GameBootstrapper.Instance.Services.Get<IRewardService>();

rewards.RegisterReward(levelCompletionDefinition, new RewardBundle(
    new CurrencyReward(economy, new CurrencyId("Coins"), 100),
    new ItemReward(inventory, new ItemId("HealthPotion"), 1),
    new ExperienceReward(experience, 50)));

RewardClaimResult result = rewards.TryClaim(new RewardId("LevelCompletion"));
// A second TryClaim on a RewardClaimPolicy.Once reward always returns AlreadyClaimed -
// idempotency is the framework's guarantee here, not something calling code must re-check.
```

`IReward` (`CanGrant()`/`Grant()`) mirrors Phase 4's `IGameplayCommand` (`CanExecute`/`Execute`)
deliberately. `RewardService.TryClaim` validates the *entire* reward — recursing through an
arbitrary `RewardBundle` nesting via `CanGrant()` — before calling `Grant()` on any part of it, so
a reward already known to be invalid (an unregistered currency, an unknown item) never partially
mutates player state. This is validate-then-mutate, not a rollback engine: if a child's `Grant()`
somehow still fails after its own `CanGrant()` passed (which should not happen in this
single-threaded, main-thread-only framework), whatever already granted stays granted and the
failure is logged loudly rather than hidden — the project's stated "at minimum: validate before
mutating" bar, not a claim of full ACID transactions.

**Idempotency** is a persisted `HashSet<RewardId>` of claimed ids for `RewardClaimPolicy.Once`
rewards — the same "check the flag, then set it" shape `UnlockService` uses for its unlocked-set.
`RewardClaimPolicy.Repeatable` tracks no claim state at all (every `TryClaim` grants). Two distinct
events exist for a reason: `RewardGrantedEvent` fires every time content is actually granted
(including every time for a repeatable reward); `RewardClaimedEvent` fires only for a `Once` reward,
the moment its one-time flag is set — "granted" and "claimed" are genuinely different concepts, not
the same event under two names.

`ProgressionBootstrapper` (`GameFramework.Rewards`) is the composition root — a `GameBootstrapper`
subclass, sibling to `PlayerSystemsBootstrapper`/`GameplayBootstrapper`/`PerformanceBootstrapper`,
that registers all five services (`IEconomyService`/`IInventoryService`/`IExperienceService`/
`IUnlockService`/`IRewardService`) with their constructor-injected static content (`CurrencyDefinition[]`/
`ItemDefinition[]`/`ProgressionCurveDefinition`, all Inspector-assigned fields). It deliberately does
**not** register any `UnlockDefinition`/`RewardDefinition` requirement or content — which unlocks/
rewards exist and what they require is game-specific content, not framework composition, matching
"the framework defines no concrete currencies/items/unlocks/rewards" throughout this phase.

### Game Flow Integration {#game-flow-integration-2}

A game (or this framework's own Phase 6 sample) registers its actual unlock requirements and
reward contents *after* `GameBootstrapper.State` reaches `BootstrapState.Ready` — the same
"wait for Ready, then wire content" coroutine pattern every phase's demo scene already uses, and
necessary here because a requirement/reward needs `IServiceRegistry.Get<TService>()` on one of
these five services, which only succeeds once that service is actually initialized:

```csharp
private IEnumerator Start()
{
    while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
    {
        yield return null;
    }

    IServiceRegistry services = GameBootstrapper.Instance.Services;
    var unlocks = services.Get<IUnlockService>();
    var experience = services.Get<IExperienceService>();

    unlocks.RegisterUnlock(veteranCarDefinition, new LevelRequirement(experience, minLevel: 3));
    // ... RegisterReward similarly ...
}
```

A game wanting Progression alongside Player Systems/Gameplay/Performance combines them in its own
small subclass, exactly as the framework already documents for combining any two of those — none
of `GameFramework.Progression`/`.Unlocks`/`.Rewards` references Input/UI/Audio/Feedback/Gameplay/
Performance types, so it composes freely with all of them.

### Sample

`Assets/GameFramework/Samples/Phase6Demo/` — not added to Build Settings, matching every other
phase's demo. Five real `.asset` files under `Content/` (`CoinsCurrency`, `HealthPotionItem`,
`PlayerLevelCurve`, `VeteranCarUnlock`, `FirstQuestReward` — authored as actual ScriptableObject
assets, not built in code, since these are exactly the kind of content a real game authors via the
Inspector) plus `Phase6DemoController`, which registers the demo's one unlock (`VeteranCar`, requires
level 3) and one reward (`FirstQuest`: 100 Coins + 1 HealthPotion + 50 XP) after Ready, then exposes
key-driven interactions (1 = claim reward, 2 = claim again to observe idempotency, 3 = try unlocking
before level 3, G = grant XP, U = try unlocking again, R = full status report).

## Objectives, Quests, Achievements & Milestones

Phase 7. One new assembly on top of Phase 4/6, plus one new namespace inside the existing
`GameFramework.Progression` assembly:

```text
GameFramework.Progression.Statistics   (new namespace, existing assembly — Economy/Inventory/
                                         Experience's sibling, not a new assembly, since it needs
                                         no dependency the existing assembly doesn't already have)
        ↓
GameFramework.Quests                   (new assembly: Conditions, Objectives, Quests,
                                         Achievements, Milestones — references Progression,
                                         Unlocks, Rewards, and Gameplay for IObjective/ObjectiveBase)
```

`GameFramework.Quests` is the one place in the framework that legitimately references both
`GameFramework.Gameplay` (for `IObjective`/`ObjectiveBase`) and the Progression/Unlocks/Rewards
chain in the same assembly — nothing downstream of it needs to, so this doesn't create a cycle;
`GameFramework.Editor` already had a one-way path to `Gameplay` and now also references
Progression/Unlocks/Rewards/Quests for content validation.

**Conceptual pipeline** (see CLAUDE.md's Phase 7 brief): Statistics → Conditions → Objectives →
Quests/Achievements/Milestones → Rewards/Unlocks → persisted state. Not every layer depends
directly on every other — Milestones, for instance, skip the code-composed condition step entirely
(see below).

**Extends Phase 4's objective system rather than duplicating it.** `GameFramework.Gameplay.Objectives`
already had a bare `IObjective`/`ObjectiveBase` state machine (Inactive → Active → Completed/Failed
→ Inactive) with no notion of a condition or progress — Phase 7's `ConditionObjective : ObjectiveBase`
is the one concrete objective the framework ships: it wraps an `ICondition`, adds `IProgressObjective`
(`CurrentValue`/`RequiredValue`/`ProgressNormalized`), and completes itself when `Evaluate()` is
called while `Active` and the condition is satisfied. `Evaluate()` is never called from an `Update`
loop — only in response to a relevant event (see Event-Driven Evaluation below).

### Statistics

```csharp
IStatisticsService statistics = GameBootstrapper.Instance.Services.Get<IStatisticsService>();

statistics.Increment(new StatisticId("RacesCompleted"), 1, reason: "RaceFinished");
int races = statistics.Get(new StatisticId("RacesCompleted"));
```

Mirrors `EconomyService` exactly: constructor-injected `StatisticDefinition[]`, the same explicit
`Save()`/`Load()` + dirty-flag-on-`Shutdown()` policy, the same widen-to-`long`-before-clamping
overflow protection. Integer is the primary, fully-featured type (`Get`/`Set`/`Increment`/`Decrement`/
`TryGet`) per CLAUDE.md's own guidance that most progression tracking should prefer it; Float and
Boolean get their own focused `Get`/`Set` only — no general-purpose type system. `IsMonotonic`
statistics reject `Decrement` and any `Set` to a lower value (logged, not thrown); `ResetToDefaults`
bypasses that, same as every reset-support method elsewhere in the framework. `Persistent = false`
statistics (session-only counters) are never written to the save file and always start each session
at their default. `StatisticChangedEvent` (Integer/Boolean, previous/new/delta) and
`StatisticFloatChangedEvent` mirror `CurrencyChangedEvent`'s shape.

### Conditions

```csharp
var condition = new AllCondition(
    new StatisticCondition(statistics, new StatisticId("RacesCompleted"), 5),
    new StatisticCondition(statistics, new StatisticId("CoinsCollected"), 100));

bool satisfied = condition.IsSatisfied();
```

`ICondition` (`IsSatisfied()`/`Describe()`) deliberately mirrors `IUnlockRequirement`'s shape — same
composable-tree approach, same constructor-injected-service pattern for independent unit testing —
but is a separate interface, not a reuse of `IUnlockRequirement`, because this domain also needs
progress reporting (`IProgressCondition`: `CurrentValue`/`RequiredValue`), which gating unlockable
content has no use for. `AllCondition`/`AnyCondition` are the exact AND/OR identity-element
behavior `AllRequirement`/`AnyRequirement` already established (vacuous AND satisfied, vacuous OR
not); `NotCondition` is new. Concrete conditions: `StatisticCondition` (the primary building block —
five `ComparisonOperator`s), `StatisticFlagCondition` (Boolean statistics), `LevelCondition`/
`CurrencyCondition`/`InventoryCondition` (mirror `LevelRequirement`/`CurrencyRequirement`/
`ItemRequirement`, with progress added), and `UnlockCondition` (gate on `IUnlockService.IsUnlocked`).
A game adds its own conditions (`CompleteRaceCondition`, `DefeatBossCondition`, ...) by implementing
`ICondition` from outside this assembly — no framework change required.

### Event-Driven Evaluation

Never polled. `ConditionStatisticIndex.CollectStatistics` walks a condition tree once at
registration time, collecting every `StatisticId` it depends on and reporting whether the whole
tree is *purely* statistic-driven. Quest/Achievement services build a `Dictionary<StatisticId,
List<...>>` from this: a `StatisticChangedEvent` only re-evaluates the quests/achievements actually
indexed against that exact statistic. Anything with a non-statistic dependency — `LevelCondition`,
`CurrencyCondition`, `InventoryCondition`, `UnlockCondition`, or a game-specific custom `ICondition`
the indexer doesn't recognize — falls into a small fallback list re-evaluated on the corresponding
broader event (`LevelChangedEvent`/`CurrencyChangedEvent`/`ItemChangedEvent`/`UnlockChangedEvent`).
This is the "simple indexing strategy" CLAUDE.md's Phase 7 brief asks for rather than a full
dependency graph — for the dominant case (statistic-driven content, which is every example the brief
itself gives) it's true dirty evaluation; for everything else it degrades to "scan the smaller
still-active/still-pending set on a rarer event," which the brief explicitly allows at normal mobile
content scale.

### Quests

```csharp
IQuestService quests = GameBootstrapper.Instance.Services.Get<IQuestService>();

quests.RegisterQuest(firstRaceDefinition, availability: null, objectives: new[]
{
    new QuestObjectiveEntry(completeRaceObjectiveDefinition,
        new StatisticCondition(statistics, new StatisticId("RacesCompleted"), 1))
});

quests.Start(new QuestId("FirstRace"));
// ... later, once RacesCompleted >= 1, the quest auto-completes on that statistic's change event ...
QuestClaimResult result = quests.TryClaimReward(new QuestId("FirstRace"));
```

`QuestDefinition` carries only id/display text/`QuestCompletionRule` (All/Any/Count)/
`QuestRepeatPolicy` (OneTime/Repeatable/LimitedRepeats)/`RewardId` — objectives and the optional
availability condition are composed in code and passed to `RegisterQuest`, the same
register-in-code pattern `UnlockDefinition`/`RewardDefinition` already established (avoiding a
`[SerializeReference]` custom-drawer for condition trees). Each objective entry reuses Phase 4's
`ObjectiveDefinition` for id/display text rather than introducing a second "objective text" asset.

**`QuestStatus` (Locked/Available/Active/Completed/Claimed) is mostly derived, not persisted.** Only
"has this quest ever been started" and "has it completed" are genuine persisted bits;
Locked-vs-Available is derived live from the availability condition, and Claimed is derived from
`IRewardService.HasClaimed` — never tracked a second time. A quest with no `RewardId` simply stays
`Completed` forever (there's nothing to claim, so it's never reported `Claimed`). Like
Unlock/Reward, `RegisterQuest` happens *after* `IQuestService.Initialize` (its conditions typically
need another already-initialized service), so `Load()` stages persisted per-quest status into a
pending-restore map that each `RegisterQuest` call consumes for its own id.

**Repeat policy** governs `TryReset`: rejected for `OneTime`, rejected past `MaxRepeats` for
`LimitedRepeats`, otherwise the quest returns to startable and its objectives reset
(`ObjectiveBase.Reset()`). `ForceReset` bypasses policy entirely — development/testing only, same
spirit as every other `ResetToDefaults` in the framework.

### Achievements

```csharp
IAchievementService achievements = GameBootstrapper.Instance.Services.Get<IAchievementService>();

achievements.RegisterAchievement(experiencedDriverDefinition,
    new StatisticCondition(statistics, new StatisticId("RacesCompleted"), 10));
```

Simpler than quests: no availability gate, no repeat policy, exactly one condition. An achievement's
`ConditionObjective` activates the moment it's registered — there is no "not started" state, matching
the brief's framing of achievements as long-running accomplishments tracked from the start.
`AchievementDefinition.AutoClaimReward` controls whether `TryClaimReward` fires automatically the
instant the achievement completes, or waits for an explicit call (e.g. after showing a completion
popup) — both paths go through the same `IRewardService.TryClaim`, so idempotency is identical
either way.

### Milestones

```csharp
IMilestoneService milestones = GameBootstrapper.Instance.Services.Get<IMilestoneService>();

milestones.RegisterMilestone(raceMasterDefinition); // "RacesCompleted >= 100", from the asset alone
```

The one place in Phase 7 that needs no code-composed condition: a milestone is always exactly
"statistic reaches threshold" (every example in the design brief uses this shape), so
`MilestoneDefinition` carries `StatisticId` + `Threshold` directly and `MilestoneService` builds its
own internal `StatisticCondition` from its own `IStatisticsService` reference — the one Phase 7
service that resolves a Progression-layer dependency via the registry rather than direct
construction, since milestone content is pure data with nothing else for a composition root to
inject.

### Reward & Unlock Integration

Quests/Achievements/Milestones never track their own "claimed" flag — claiming always resolves to
exactly one `IRewardService.TryClaim` call, and `IsClaimed`/`GetStatus` always ask
`IRewardService.HasClaimed` rather than duplicating that bit. This is deliberate: CLAUDE.md's Phase 7
brief calls out "avoid storing redundant state — redundant state creates synchronization problems,"
and Phase 6's `RewardService` already owns exactly this idempotency guarantee. The same content can
also drive `IUnlockService.ForceUnlock` from a `UnlockReward` inside a quest/achievement's granted
`RewardBundle` — no `AchievementUnlockManager`/`QuestUnlockManager` was introduced; Phase 6's reward
content types are the integration point.

### Persistence

Each of the four new services persists itself independently, under its own key, through the
existing `IPersistenceService` — no new save system, no shared "PlayerState" blob:

| Service | Save key | What's persisted |
|---|---|---|
| `StatisticsService` | `GameFramework.Progression.Statistics` | Value per **persistent** statistic (Integer/Float/Boolean columns) |
| `QuestService` | `GameFramework.Quests` | Id/status(Active or Completed only)/repeat-count, one row per quest ever started |
| `AchievementService` | `GameFramework.Achievements` | Id, one row per **completed** achievement |
| `MilestoneService` | `GameFramework.Milestones` | Id, one row per **reached** milestone |

All four use `[Serializable] internal` DTOs with parallel `List<T>` fields (the same
`JsonUtility`-can't-serialize-a-`Dictionary` workaround `EconomySaveData` uses) and the identical
explicit `Save()`/`Load()` + dirty-flag-on-`Shutdown()` policy as every other Phase 6/7 service —
nothing here writes to disk on every statistic increment.

### Game Flow Integration

`QuestsBootstrapper` (`GameFramework.Quests`) extends `ProgressionBootstrapper` — a genuine
inheritance chain, not a flat sibling composition like `PerformanceBootstrapper`/
`GameplayBootstrapper`/`PlayerSystemsBootstrapper`, because `GameFramework.Quests` already has a
real compile-time dependency on `GameFramework.Rewards` (quest/achievement/milestone claiming goes
through `IRewardService`). It registers `IStatisticsService`/`IQuestService`/`IAchievementService`/
`IMilestoneService` on top of `ProgressionBootstrapper`'s five, with only static content
(`StatisticDefinition[]`) — exactly like `ProgressionBootstrapper` itself, it does **not** register
any `QuestDefinition`/`AchievementDefinition`/`MilestoneDefinition` content, since that's
game-specific and needs services already initialized (the same "wait for Ready, then wire content"
coroutine pattern every phase's demo scene uses).

### Sample

`Assets/GameFramework/Samples/Phase7Demo/` — not added to Build Settings, matching every other
phase's demo. Content assets under `Content/` (`RacesCompletedStatistic`, `CoinsCurrency`,
`PlayerLevelCurve`, `CompleteRaceObjective`, `FirstRaceQuest`, `ExperiencedDriverAchievement`,
`RaceMasterMilestone`, and their three `RewardDefinition`s) plus `Phase7DemoController`, which
registers all of it after Ready and demonstrates one statistic increment (a reported "race
completed") flowing into a quest objective, an achievement, and a milestone simultaneously — the
exact reusable composition this phase exists to enable. Verified live in the Editor (Play mode +
direct service calls): starting the quest, reporting one race completes it and grants its reward
(idempotently on a repeat claim), ten races completes the achievement, and a hundred reaches the
milestone.

## Game Flow, Sessions & Checkpoints

Phase 8. One new assembly, `GameFramework.GameFlow`, referencing only `GameFramework.Core`/
`GameFramework.Runtime`/`GameFramework.Gameplay` — deliberately **not** Performance/Progression/
Unlocks/Rewards/Quests/Input/UI/Audio/Feedback. It sits one layer above Gameplay Infrastructure (it
reuses `GameFramework.Gameplay.IGameplayService` to start/stop ticking gameplay participants
alongside a session, and `GameFramework.Gameplay.Objectives.CheckpointData` as the built-in
transform-checkpoint shape) but never references Phase 6/7 — completion/failure/restart only ever
*publish events*; a game's own bridge code (or nothing at all, if it doesn't need one) is what
turns a `LevelCompletedEvent` into a statistic increment, an unlock check, or a reward claim. This
keeps Phase 8 usable by a game that has no progression content at all, and keeps Phase 6/7 unaware
Phase 8 exists.

**One state machine, not two.** CLAUDE.md's Phase 8 brief distinguishes "gameplay state" from
"level/stage flow" conceptually, but they describe the same lifecycle in practice (Loading →
Initializing → Ready → Playing → Paused → Completing/Failing → Completed/Failed → Restarting →
Exiting) — `LevelFlowState` (`GameFramework.GameFlow`) is the one concrete state machine Phase 8
ships, driven by `LevelFlowStateMachine`'s fixed allowed-transition table (internal — only
`GameFlowService` drives it):

```text
Unloaded → Loading → Initializing → Ready → Playing ⇄ Paused
                                       │        │
                            ┌──────────┼────────┘
                            ▼          ▼
                       Completing  Failing → Failed ──(Respawn)──→ Playing
                            │          │        │
                            ▼          │        │
                       Completed ──────┴────────┤
                            │                    │
                            └──────► Restarting ─┤ (reload → Loading, or straight → Ready)
                                                  │
                    Ready/Playing/Paused/Completed/Failed ──► Exiting → Unloaded
```

`Failed → Playing` is the one transition reachable only through `GameFlowService.Respawn()` — no
other command ever requests it. Every command returns a `TransitionResult`
(Success/AlreadyInState/InvalidTransition/TransitionBlocked) or a dedicated result enum
(`LevelLoadResult`, `RespawnResult`) instead of throwing, matching every other "normal gameplay
outcome" API in this framework.

```csharp
IGameFlowService flow = GameBootstrapper.Instance.Services.Get<IGameFlowService>();

flow.LoadLevel(level1Definition);                 // → Loading, watches ISceneService.SceneLoaded
// ... once ready ...
flow.StartLevel();                                 // → Playing, creates a new GameplaySession
flow.CompleteLevel(GameplayResult.Success("Finished"));
flow.Retry();                                       // new attempt, same level
```

**Re-entrancy.** Every public command wraps its state transition *and* every event it publishes in
one guard, so a listener that calls another command from inside, say, a `LevelCompletedEvent`
handler gets `TransitionResult.TransitionBlocked`/`RespawnResult.Blocked` back rather than
corrupting the in-progress transition — this is the "queue or reject explicitly" choice CLAUDE.md's
brief calls for, and this framework rejects (matching `IGameStateService`'s existing precedent).

**Scene loading is event-driven, not polled.** `LoadLevel` calls `ISceneService.LoadAsync` but
detects completion through `ISceneService.SceneLoaded` rather than polling the `AsyncOperation` it
returns — deliberately, since an `AsyncOperation` cannot be constructed or driven manually outside
of a real engine scene load, which would make `GameFlowService` untestable in EditMode. `ExitLevel`
called mid-load simply stops listening (Unity itself cannot abort an in-flight scene load — see
`ISceneService`'s own remarks — so the scene may still finish loading in the background, but this
service's own state never reflects that stale completion).

**Pause is fully delegated to `ITimeService`.** `PauseGameplay(reason)` returns an `IPauseToken`
wrapping exactly one `ITimeService.Pause()`/`Resume()` pair — releasing it never resumes gameplay
while another token (or an entirely unrelated `ITimeService.Pause()` caller, e.g. Phase 5's
`ApplicationLifecycleService` backgrounding the app) is still outstanding, since `ITimeService`
itself already reference-counts. `LevelFlowState` reactively mirrors `ITimeService.IsPaused` every
tick (`GameFlowService.Tick`, via `IUpdatableService` — the same mechanism `GameplayService`/
`TimerService`/`TickService` all use for their own primary tick), the same reactive-pause pattern
`GameplayService` already established for Phase 4 — no new pause mechanism was introduced.

**`GameplaySession`** (`GameFramework.GameFlow.Session`) is a generic attempt — `SessionId`,
`LevelId`, `AttemptNumber`, `Mode`, `ElapsedGameplayTime` (accumulated `ScaledDeltaTime`, only while
Active), `PauseCount`, `RespawnCount`, `Result`, and an owned `CheckpointSystem`. Mutation methods
are internal — exactly one session is ever active per `GameFlowService`, enforced by construction.
`Retry` always creates a new session (new `SessionId`, incremented `AttemptNumber`); `Respawn` is
the one operation that continues the *same* session/attempt instead — CLAUDE.md's brief is explicit
that these are not the same operation, and this is where that distinction lives in code.

**Checkpoints stay session-scoped and transient by default.** `CheckpointSystem` (`GameFramework.
GameFlow.Checkpoints`) is a plain, session-owned registry (Register/Activate/GetCurrent/Reset/
Clear) — carries data only, exactly like Phase 4's `Checkpoint` marker it composes with (a game
typically registers one entry per `Checkpoint` in the scene, from `Checkpoint.GetData()`). A
checkpoint entry pairs the built-in transform (`CheckpointData?`) with an optional
`IGameplaySnapshot` — a marker interface for whatever extra state a specific game's checkpoint
needs (health, ammo, ...) that this framework never inspects, serializes, or persists; only the
transform is ever persisted, and only when `IGameFlowService.PersistCheckpoints` is explicitly set
true (`Runtime.Persistence.IPersistenceService`, key `"GameFramework.GameFlow.Checkpoint"`, the same
explicit-Save + no-auto-save policy every other persisted service in this framework follows).
`Respawn()` never leaves gameplay half-restored: GameFlow only ever touches its own state when a
valid checkpoint is found (`RespawnResult.NoCheckpoint` otherwise, a deterministic result the caller
can react to, e.g. by calling `Retry()` as a fallback) — actually moving/resetting "the player" is
never this framework's job; `Respawn()` publishes `PlayerRespawnedEvent` and a game's own player
controller reacts to it, since GameFlow does not know what a player is.

**Events.** `LevelFlowStateChangedEvent`, `LevelLoadingStartedEvent`/`LevelInitializingEvent`/
`LevelReadyEvent`, `LevelStartedEvent`/`GameplaySessionStartedEvent`,
`GameplaySessionPausedEvent`/`GameplaySessionResumedEvent`,
`GameplaySessionCompletedEvent`/`LevelCompletedEvent`,
`GameplaySessionFailedEvent`/`LevelFailedEvent`, `LevelRestartedEvent`, `LevelExitedEvent`,
`CheckpointActivatedEvent`, `PlayerRespawnedEvent` — all published through the existing Phase 2
`IEventService`, all immutable readonly structs carrying `LevelId`/`SessionId`/`AttemptNumber`/
result context, never a mutable reference into live GameFlow state.

### Game Flow Integration {#game-flow-integration-3}

`GameFlowBootstrapper` (`GameFramework.GameFlow`) registers `IGameFlowService` the same way every
other phase's bootstrapper subclass adds its own service — sibling to `GameplayBootstrapper`/
`PerformanceBootstrapper`/`PlayerSystemsBootstrapper`, not a base or subclass of any of them, since
its one soft dependency (`Gameplay.IGameplayService`, resolved via `registry.TryGet`) does not
require a real compile-time/registration dependency. A game combines it with Gameplay
Infrastructure (recommended, so sessions actually start/stop ticking participants) in its own small
subclass, registering `IGameFlowService` *after* `IGameplayService` so the soft lookup succeeds:

```csharp
public class MyGameBootstrapper : GameplayBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1-4, includes IGameplayService
        registry.Register<IGameFlowService>(new GameFlowService());
    }
}
```

## Tutorial & Onboarding Framework

Phase 9. One new assembly, `GameFramework.Tutorials`, referencing only `GameFramework.Core`/
`GameFramework.Runtime`/`GameFramework.Input` — deliberately **not** Gameplay/GameFlow/Performance/
Progression/Unlocks/Rewards/Quests/UI/Localization/Audio/Feedback (see [Architecture](#architecture)).
It provides tutorial *infrastructure* — lifecycle, sequencing, input/event/condition triggers, pause/
input gating, persistence, events — never tutorial *content*: no concrete tutorial, no game-specific
condition, no UI. A game defines what a tutorial teaches; this assembly only knows how to run it.

**Lifecycle.** `TutorialState` (`Inactive → Starting → Running ⇄ Paused → Completing → Completed`,
and `Running/Paused → Cancelling → Cancelled`), driven by `TutorialLifecycleStateMachine`'s fixed
allowed-transition table — the same concrete-not-generic, internal-only shape as
`GameFlow.LevelFlowStateMachine`. Skipping is deliberately not a separate state: `ITutorialService.Skip`
ends the run through the same Completing/Completed path a normal finish uses, distinguished only by
which event is published (`TutorialSkippedEvent` instead of `TutorialCompletedEvent`) and by the
persisted "was skipped" flag. `Completed`/`Cancelled` are terminal-but-transient — the service
auto-returns to `Inactive` immediately after publishing the outcome event, freeing the "one active
tutorial" slot without a separate reset call:

```text
Inactive → Starting → Running ⇄ Paused
                          │        │
                          ▼        ▼
                      Completing  Cancelling
                          │           │
                          ▼           ▼
                      Completed   Cancelled
                          │           │
                          └─────┬─────┘
                                ▼
                            Inactive (auto)
```

Only one tutorial is ever active at a time. `Start` rejects a request outright
(`TutorialStartResult.Blocked`) while a *different* tutorial is running — never queued, matching
`IGameFlowService`'s "one active level flow" policy. Every command returns a result
(`TutorialStartResult` for `Start`; the shared `TutorialCommandResult` for
`Skip`/`Cancel`/`Restart`/`CompleteCurrentStep`) instead of throwing, and every command wraps its
transition and every event it publishes in one re-entrancy guard — a listener that calls another
command from inside, say, a `TutorialCompletedEvent` handler gets `TutorialCommandResult.Blocked`/
`TutorialStartResult.Blocked` back rather than corrupting the in-progress transition, the same
pattern `GameFlowService` established.

```csharp
ITutorialService tutorials = GameBootstrapper.Instance.Services.Get<ITutorialService>();

tutorials.RegisterTutorial(firstDriveDefinition, new[]
{
    new TutorialStepEntry(introStepDefinition, new InstructionStep("Intro")),
    new TutorialStepEntry(pressMoveStepDefinition, new InputStep("PressMove", input, "Move")),
    new TutorialStepEntry(waitForMovementStepDefinition, new EventStep<CarStartedMovingEvent>("WaitForMovement", events)),
    new TutorialStepEntry(checkpointStepDefinition, new ConditionStep("ReachCheckpoint", new DelegateTutorialCondition(() => car.ReachedCheckpoint))),
    new TutorialStepEntry(outroStepDefinition, new InstructionStep("Outro"))
});

tutorials.Start(new TutorialId("FirstDrive"));
// ... later, e.g. from a "Got it" button ...
tutorials.CompleteCurrentStep();
```

**Step system.** `ITutorialStep` (`TutorialStepBase` for the shared implementation) is a small
NotStarted → Active → (Completed | Cancelled) state machine per step, with a `Reset()` back to
NotStarted — necessary because the same step *instances* passed to `RegisterTutorial` are reused
across every run of that tutorial (a `Repeatable` replay, `Restart`, or `Cancel` followed by a later
`Start`); `TutorialService` calls `Reset()` on every step immediately before beginning a run. Five
built-in step types cover CLAUDE.md's Phase 9 brief in full:

- **`InstructionStep`** — completes on explicit acknowledgement (`ITutorialService.CompleteCurrentStep`)
  or, if given a positive delay, automatically after that many unscaled seconds. With no delay, this
  is also the framework's "Manual/External Completion Step" — a pure gate for a game-specific
  condition that should stay outside the framework.
- **`WaitStep`** — completes after a fixed unscaled-time duration.
- **`InputStep`** — waits for a logical `IInputService` action (`Pressed`/`Held`/`Released`, via
  `InputTriggerType`) — never a physical key.
- **`EventStep<TEvent>`** — completes when a matching `IEventService` event is published, with an
  optional predicate; subscribes in `Begin`/unsubscribes in the step's end, so a cancelled step never
  leaves a dangling subscription.
- **`ConditionStep`** — wraps a local `ITutorialCondition` (`IsSatisfied`/`Describe`, mirroring
  `Quests.Conditions.ICondition`'s shape without the assembly reference — see
  [Architecture](#architecture)); checked once on begin and once per tick while active. Unlike
  `GameFramework.Quests`' statistic-indexed event-driven evaluation (built to avoid polling a
  potentially large *set* of simultaneously-active quests), polling here is cheap by construction:
  at most one active step, for at most one active tutorial. `DelegateTutorialCondition` is the one
  concrete condition the framework ships, wrapping a game-supplied `Func<bool>`.

Display text is never carried by a runtime step: `TutorialStepDefinition` (a small
`[CreateAssetMenu]` `ScriptableObject`, mirroring `Gameplay.Objectives.ObjectiveDefinition`'s
id/localization-key-only shape) is the paired authoring asset a UI resolves instead — see
`TutorialStepEntry`, which pairs a step's display data with its runtime behavior exactly like
`Quests.Quests.QuestObjectiveEntry` pairs an `ObjectiveDefinition` with an `ICondition`.

**Authoring.** `TutorialDefinition` (`[CreateAssetMenu]`) carries only id, display/description
localization keys, `TutorialId[]` prerequisites, and four policies:

- `TutorialRepeatPolicy` — `Once` (persisted completion blocks every future `Start`, across
  restarts), `Repeatable` (always startable again once finished), `OncePerSession` (blocked only for
  the remainder of the current process — a fresh session, even with the same persisted "completed"
  flag loaded, allows one more start).
- `TutorialSkipPolicy` — `NotSkippable` / `Skippable`.
- `TutorialPausePolicy` — `DoesNotPauseGameplay` / `PausesGameplay`.
- `TutorialPersistencePolicy` — `None` (never persisted, even once completed), `CompletionOnly`
  (default — only whether it completed/was skipped; an interrupted run always restarts from the
  first step), `ResumeProgress` (also persists the current step index while running, so an
  application restart mid-tutorial resumes from that step on the next `Start` instead of
  restarting).

No step content is embedded in `TutorialDefinition` — exactly like `Quests.Quests.QuestDefinition`
leaves objectives to be composed in code, a tutorial's step sequence is built in code and passed to
`RegisterTutorial`, since an input/event/condition wiring cannot be authored purely as Inspector
data without a `[SerializeReference]` custom drawer this framework deliberately avoids (see
`QuestObjectiveEntry`'s own remarks for the identical reasoning).

**Input integration.** `InputStep` reads a logical action through the existing `IInputService` —
no second input-sampling path. Input *gating* (restricting what the rest of the game can do while a
tutorial runs) is a tutorial-wide, not per-step, concern: `RegisterTutorial`'s optional
`InputContextDefinition?` parameter is pushed onto `IInputService`'s existing context stack once
when the tutorial starts and popped exactly once when it ends (`Completed`/`Cancelled`/`Shutdown`
mid-run) — no new context-stack mechanism, reusing `PushContext`/`PopContext` exactly as a UI popup
already does.

**Pause integration.** Built entirely on `ITimeService.Pause`/`Resume`'s existing reference
counting — no new pause-token type. A `PausesGameplay` tutorial calls `Pause` once when it starts
and `Resume` exactly once when it ends (guarded by a bool, idempotent); unlike
`GameFlow.IGameFlowService.PauseGameplay`, this pause is never handed to an external caller as a
token, so a full `IPauseToken`-style abstraction would be unused ceremony here. Separately,
`TutorialState` reactively mirrors `ITimeService.IsPaused` every tick (the same reactive-pause
pattern `GameFlowService`/`GameplayService` already established) — a tutorial correctly suspends its
own step progression (`WaitStep`/`InstructionStep` timers, `ConditionStep` polling, `InputStep`
sampling all stop ticking while `Paused`) if *anything* pauses gameplay, not only its own acquired
pause. The one-frame ordering hazard this creates — a `PausesGameplay` tutorial's own `Pause` call
would otherwise be immediately observed as an "external" pause on the very next tick — is resolved
by pre-seeding the reactive-poll baseline right after acquiring the pause, the same trick
`GameFlowService.StartLevelInternal` already uses.

**Event integration.** Every tutorial-level notification (`TutorialStartedEvent`,
`TutorialStepStartedEvent`/`TutorialStepCompletedEvent`, `TutorialPausedEvent`/`TutorialResumedEvent`,
`TutorialCompletedEvent`/`TutorialSkippedEvent`/`TutorialCancelledEvent`, `TutorialRestartedEvent`)
publishes through the existing `IEventService` — the same mechanism every other cross-system
notification in this framework uses. UI/presentation code (arrows, highlights, a "Got it" popup,
voice-over) observes these and `TutorialStepStartedEvent`'s paired `TutorialStepDefinition`
(resolved via `ITutorialService.GetDefinition`) rather than the framework depending on any concrete
UI type — the same separation `GameFlow`'s events give level/session presentation.

**Persistence.** Built entirely on the existing `IPersistenceService` — no second save system.
Follows the exact registration-after-Load ordering problem this framework already hit once in Phase
6 (`UnlockService`/`RewardService`): `RegisterTutorial` normally runs after
`ITutorialService.Initialize` (game content typically needs another already-initialized service, or
just hasn't been wired up yet), so `Load` stages persisted per-tutorial state into a pending-restore
map that each `RegisterTutorial` call consumes for its own id — a tutorial registered after `Load`
never silently loses its persisted completion. Persisted data is treated as untrusted: mismatched
column lengths or an unresolvable id are logged and ignored rather than indexed out of range or
applied to a nonexistent tutorial.

**GameFlow integration.** No direct reference in either direction. `Tutorials → Input/Pause/Events`
only; a game's own bridge code (or nothing at all) turns a `GameFlow.LevelReadyEvent` into
`tutorials.Start(...)`, or a `TutorialCompletedEvent` into whatever the game wants next — the same
"communicate through events, not direct calls" principle Phase 8 established between `GameFlow` and
Progression/Rewards/Quests. A tutorial never mutates `LevelFlowState` and `GameFlowService` never
knows a tutorial exists.

### Bootstrap Integration

`TutorialBootstrapper` (`GameFramework.Tutorials`) registers `ITutorialService` the same way every
other phase's bootstrapper subclass adds its own service — sibling to `GameFlowBootstrapper`/
`GameplayBootstrapper`/`PerformanceBootstrapper`/`PlayerSystemsBootstrapper`, not a base or subclass
of any of them. `IInputService` is a soft dependency (`registry.TryGet`) — `ITutorialService` works
fully without Input registered at all, it just then never gates input and any `InputStep` a game
registers stays permanently un-completable (a content-authoring mistake, not a framework failure). A
game combines Tutorials with Input (recommended, for `InputStep`/input gating) in its own small
subclass, registering `ITutorialService` *after* `IInputService`:

```csharp
public class MyGameBootstrapper : PlayerSystemsBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1-3, includes IInputService
        registry.Register<ITutorialService>(new TutorialService());
    }
}
```

**Editor validation.** `TutorialContentValidator` (`GameFramework.Editor`) follows the same pattern
as `QuestContentValidator`/`GameplayConfigValidator`: validate the selected `TutorialDefinition`/
`TutorialStepDefinition` asset, or scan the project for duplicate ids and dangling prerequisites
(a `TutorialDefinition` requiring a prerequisite id no asset in the project declares).

**Sample.** `Assets/GameFramework/Samples/Phase9Demo/` demonstrates a "FirstDrive" tutorial spanning
all five built-in step types (Instruction → Input → Event → Condition → Instruction), combining
`PlayerSystemsBootstrapper` (for `IInputService`) with `ITutorialService` in a small
`Phase9DemoBootstrapper`, plus pause/skip/cancel/restart and completion persistence — content assets
under `Content/` are authored `.asset` files (matching the `TutorialDefinition`/
`TutorialStepDefinition` private-field-with-`OnValidate` authoring shape), not code-built like
Phase 3's Input map. Not part of the reusable framework.

**Non-goals (explicitly not built).** A visual tutorial/node-graph editor, a hand/pointer animation
system, a tweening framework, a dialogue/voice-over/cutscene framework, a quest/achievement system
(that's `GameFramework.Quests`), an analytics/ads/IAP SDK, remote config, multiplayer tutorials, or
AI-driven tutorials — `ITutorialCondition`/`IEventService`/`ITutorialService.GetDefinition` are the
seams a game's own presentation layer or a later phase would extend, not something this phase builds
itself.

## Game Feel, Feedback & Presentation

Phase 10. One new assembly, `GameFramework.Presentation`, referencing `GameFramework.Core`/
`GameFramework.Runtime`/`GameFramework.Audio`/`GameFramework.Feedback`/`GameFramework.Gameplay`/
`GameFramework.Performance`/`GameFramework.UI` - deliberately **not** GameFlow/Tutorials/
Progression/Unlocks/Rewards/Quests/Localization/Input (see [Architecture](#architecture) for why
this one assembly's reference list is wider than every sibling before it, and why that is still
safe). It is explicitly **not** a replacement for Phase 3's `IFeedbackService`/`IAudioService` - it
orchestrates them, the same "low-level capability vs. higher-level orchestration" split CLAUDE.md's
Phase 10 brief asks for.

**Core principle.** Gameplay code describes *what happened*, never *how the player perceives it*:

```csharp
IPresentationService presentation = GameBootstrapper.Instance.Services.Get<IPresentationService>();

presentation.RegisterDefinition(heavyImpactDefinition); // once, at composition-root time
presentation.Play(new FeedbackId("HeavyImpact"), worldPosition: impactPoint, intensity: 0.8f);
```

That single call may dispatch audio, a haptic pulse, a camera shake, a spawned particle effect, a
screen flash, a UI reaction, and a brief hit-stop - or any subset of those - entirely driven by the
`FeedbackDefinition` asset registered under that id, never by the call site.

**`FeedbackDefinition`** (`[CreateAssetMenu]`) is pure, shared authoring data - id, `FeedbackPriority`
(Low/Normal/High/Critical), and one optional config block per channel
(`Configs.AudioFeedbackConfig`, `.HapticFeedbackConfig`, `.CameraFeedbackConfig`,
`.VisualEffectFeedbackConfig`, `.ScreenEffectFeedbackConfig`, `.UIFeedbackConfig`,
`.TimeFeedbackConfig`), each just an `Enabled` flag plus its own typed fields - never a
`Dictionary<string, object>` payload, and never runtime state (the same asset is shared by every
`Play` call for its id, for the whole application session). This is a fixed set of typed fields
rather than a polymorphic `[SerializeReference]` component list, the same reasoning
`Feedback.FeedbackPresetAsset` (Phase 3) already used for its own smaller haptic+audio bundle - the
seven channels are a closed set CLAUDE.md's brief enumerates by name, so there is no open-ended
authoring need a fixed list doesn't already cover.

**`FeedbackRequest`** is the one small, strongly-typed struct gameplay code actually hands the
service - `FeedbackId`, an optional world position, an optional source object (never inspected by
this framework, purely for a caller's own diagnostics), and a normalized `[0, 1]` intensity. Every
instance-varying piece of a request lives here; everything about *how* it is presented lives in the
paired `FeedbackDefinition`.

**Composable vs. exclusive channels.** Audio/Haptics/Visual/UI simply fire and forget - many
instances coexist with no notion of "current." Camera shake is composable in a different sense:
multiple simultaneous shakes sum into one offset (see `CameraShakeState`) rather than replacing each
other. Screen and Time are exclusive: each has exactly one "current" effect, and a new request only
interrupts it if the new `FeedbackDefinition.Priority` is greater than or equal to the current one's
- a lower-priority request while a higher-priority effect is still playing is silently dropped,
never queued. This is the explicit, deterministic policy CLAUDE.md's brief (sections 20-21) asks for
instead of arbitrary "last caller wins" behavior.

**Audio integration.** `Configs.AudioFeedbackConfig` requests `IAudioService.Play` for an
`AudioCueAsset` - never touches an `AudioSource` directly, never duplicates the cue's own
randomization/limiting. `FeedbackRequest.Intensity` scales the returned `IAudioHandle`'s volume via
`SetVolume` only below full intensity - at `1.0` the cue's own authored/randomized volume is left
exactly as `IAudioService` set it up, since `SetVolume` is an absolute override, not a multiplier,
and there is nothing to read back and scale from.

**Haptic integration.** `Configs.HapticFeedbackConfig` requests `Feedback.IFeedbackService.TriggerHaptic` -
never `Handheld.Vibrate()` or `IHapticProvider` directly, so the player's "Feedback.HapticsEnabled"
setting and platform support are already respected. `HapticStrength` is a fixed semantic enum, so
intensity does not scale it numerically - a documented limitation of the existing abstraction, not
an oversight.

**Camera integration.** `ICameraFeedbackDriver` is the seam - this framework never assumes a
specific camera controller/rig. `CameraFeedbackDriver` (a `MonoBehaviour`) is the reference
implementation: attach it to a game's camera (or a parent rig transform it controls) and it
self-registers with `IPresentationService.RegisterCameraDriver` in `OnEnable`. Internally,
`CameraShakeState` (pure, Unity-lifecycle-free C#, unit-testable in EditMode) sums every currently
active shake's Perlin-noise offset, seeded per shake for determinism (the same seed/amplitude/
frequency/falloff/elapsed-time always produce the same offset). `CameraFeedbackDriver` applies the
combined offset in `LateUpdate` (not `Performance.Ticking.ITickService` - CLAUDE.md's own Tick Rules
call for plain `Update`/`LateUpdate` for "anything simple, low-count, or already working," and a
scene has exactly one active camera driver) by first subtracting the *previous* frame's offset
(undoing its own prior contribution) before adding the new one - so an external camera-follow
script's own per-frame writes are always read cleanly, and this driver never permanently corrupts
the base pose it's layered on top of. `Configs.CameraFeedbackConfig.ConstrainToXY` zeroes the shake's
Z axis for a 2D game, without assuming anything about rotation or orthographic size.

**Visual-effect integration.** `Configs.VisualEffectFeedbackConfig` spawns `EffectPrefab` at the
request's world position and releases it after `Lifetime` seconds. When `UsePooling` is true and a
`Gameplay.Pooling.IPoolService` is registered, it routes through `IPoolService.GetOrCreate`/
`GameObjectPool.Get`, with `Runtime.Timers.ITimerService.StartOneShot` (owner-tied to the spawned
instance) calling `Release` after `Lifetime` - never `Destroy`ing a pooled instance directly.
Otherwise it falls back to a plain `Object.Instantiate` + `Object.Destroy(instance, lifetime)` - no
second pooling system, and no dependency on pooling being present at all.

**Screen-effect integration.** `Configs.ScreenEffectFeedbackConfig` (a full-screen flash/fade/hit-
overlay) is rendered through a single `UnityEngine.UI.Image` this service creates once, lazily,
parented under the existing `UI.IUIService.GetLayerRoot(UILayer.Overlay)` - never a new `Canvas`.
`ScreenEffectController` (pure C#, mirrors `CameraShakeState`'s testability) drives a fade-in/hold/
fade-out state machine; `PresentationService` applies its `CurrentAlpha`/`Color` to the overlay every
tick and deactivates the GameObject when idle, to avoid continued full-screen overdraw between
effects.

**UI integration.** `Configs.UIFeedbackConfig` publishes `UIFeedbackRequestedEvent` (id + a
free-form `Tag` this framework never interprets) instead of calling into `UI.UIScreen`/`UI.UIPopup`
directly - a game's own UI layer decides what, if anything, to show (a toast, a popup, an icon
pulse) in response. This is the one channel with no direct Phase 3 UI call at all, deliberately -
CLAUDE.md's brief is explicit that the core orchestration layer must not hard-code specific UI types.

**Time integration.** `Configs.TimeFeedbackConfig` (a brief hit-stop/slow-motion impulse) is built
entirely on `ITimeService.SetTimeScale`/`ResetTimeScale` - never `Time.timeScale` directly, and
never `Pause`/`Resume`, since a hit-stop must never release another system's (GameFlow's,
Tutorial's, application-background's) gameplay pause. `TimeFeedbackController` (pure C#) tracks only
*whether* a new request should take over (the same priority-gated exclusive-channel policy as
Screen) and *when* the current one should end, ticked with **unscaled** delta time specifically
because the whole point of the effect is to modify the scale, so its own duration cannot depend on
it.

**Settings integration.** Built entirely on the existing `ISettingsService`/persistence - no second
settings mechanism. Registers `"Presentation.Enabled"` (master switch), one
`"Presentation.Channel.<Channel>"` bool per channel (independent accessibility controls - "camera
shake off" without disabling audio), and `"Presentation.IntensityScale"` (a `[0, 1]` accessibility
multiplier applied on top of every request's own intensity - CLAUDE.md's brief sections 22-24). The
per-channel Haptics toggle is intentionally in addition to, not a replacement for, Phase 3's own
"Feedback.HapticsEnabled" - different scope (whether *this orchestrator* attempts haptics, vs.
whether haptics are allowed at all for any caller).

**Event integration / feedback mappings.** `IPresentationService.RegisterMapping<TEvent>(id,
requestFactory)` subscribes a feedback id to fire automatically whenever `TEvent` is published
through the existing `IEventService` - explicit and strongly typed, never reflection or string-based
event discovery (CLAUDE.md's brief, section 27). `FeedbackPlayedEvent` (id + `PlayResult`) is
published after every `Play` call, composable or exclusive channels alike, through the same
`IEventService` every other cross-system notification in this framework uses.

**GameFlow/Tutorial integration.** No direct reference in either direction, matching the same
"communicate through events, not direct calls" principle Phase 8 established. A game's own bridge
code (or nothing at all) turns a `GameFlow.LevelCompletedEvent` or a `Tutorials.TutorialStepStartedEvent`
into a `Play(...)` call - typically via `RegisterMapping`.

**Performance.** `ProfilingCategory.Presentation` (a new, additive enum value) wraps `Play` in a
`ProfileScope` - zero-cost when profiling is disabled, the same pattern every other framework
boundary uses. `PresentationService` implements `Runtime.Services.IUpdatableService` (ticked
directly by `GameBootstrapper`, the same mechanism `GameFlowService`/`TutorialService` use) rather
than registering with `Performance.Ticking.ITickService`, specifically so its own housekeeping
(screen fade progression, time-effect countdown) works even in a game that has not registered
`PerformanceBootstrapper` at all.

### Bootstrap Integration {#bootstrap-integration-2}

`PresentationBootstrapper` (`GameFramework.Presentation`) registers `IPresentationService` the same
way every other phase's bootstrapper subclass adds its own service - sibling to
`PlayerSystemsBootstrapper`/`GameplayBootstrapper`/`PerformanceBootstrapper`, not a base or subclass
of any of them, even though its assembly references all of Audio/Feedback/Gameplay/Performance/UI:
every one of those is resolved softly (`registry.TryGet`) inside `PresentationService.Initialize`,
so the service degrades channel by channel (logging once per missing channel) rather than requiring
every one of those systems to be registered. A game combines Presentation with whichever channels it
actually wants (recommended - most of the seven need at least one) in its own small subclass:

```csharp
public class MyGameBootstrapper : PlayerSystemsBootstrapper
{
    protected override void RegisterServices(IServiceRegistry registry)
    {
        base.RegisterServices(registry); // Phase 1-3, includes Audio/Feedback/UI
        registry.Register<IPresentationService>(new PresentationService());
    }
}
```

**Editor validation.** `FeedbackContentValidator` (`GameFramework.Editor`) follows the same pattern
as `QuestContentValidator`/`TutorialContentValidator`: validate the selected `FeedbackDefinition`
asset (missing cue/prefab when a channel is enabled, non-positive amplitude/duration, a fully
transparent screen color, no channel enabled at all), or scan the project for duplicate ids.

**Sample.** `Assets/GameFramework/Samples/Phase10Demo/` demonstrates two `FeedbackDefinition`
assets - "HeavyImpact" (Haptic+Camera+Screen+Time+Visual+UI together) and "Reward" (Haptic+UI only) -
combining `PlayerSystemsBootstrapper` with `IPresentationService` in a small
`Phase10DemoBootstrapper`, plus a scene camera carrying `CameraFeedbackDriver`. The Audio channel is
left disabled on both demo definitions - wiring it up needs an authored `AudioCueAsset`+`AudioClip`,
exactly like any other Phase 3 audio content, which this generic sample does not ship its own copy
of. Not part of the reusable framework.

**Non-goals (explicitly not built).** A full camera framework or Cinemachine replacement, a full
post-processing framework, a full VFX/particle-system framework, a shader framework, a tweening
engine, an animation framework, a dialogue/cutscene system, an audio-engine or haptic-engine
replacement, a UI-framework or input-framework replacement, a Time-system or pooling or
resource-management replacement, and an analytics/ads/IAP SDK - `ICameraFeedbackDriver`/
`IEventService`/`FeedbackDefinition`'s own extensibility are the seams a game's own presentation
layer or a later phase would extend, not something this phase builds itself. Phase 11 is that later
phase - see [Camera Framework](#camera-framework) below.

## Camera Framework

Phase 11. One new assembly, `GameFramework.Cameras`, referencing only `GameFramework.Core`/
`GameFramework.Runtime`/`GameFramework.Performance` - deliberately **not** Presentation, in either
direction (see [Architecture](#architecture)). It is not a Cinemachine replacement or a full
cinematic/cutscene system - see this section's own Non-goals.

**Core principle.** Gameplay code never touches `Camera.main`/`transform.position`/`orthographicSize`
directly - it expresses intent through the framework:

```csharp
ICameraService cameras = GameBootstrapper.Instance.Services.Get<ICameraService>();

// Authored once, at composition-root/scene-setup time (usually via the Inspector - see
// CameraController's own [SerializeField]s - shown here in code for clarity):
CameraId gameplayId = cameras.RegisterCamera(gameplayCameraController, priority: 0, owner: "Gameplay");
cameras.Activate(gameplayId);

// "Follow this target":
cameras.SetTarget(gameplayId, new TransformCameraTarget(player.transform));

// "Temporarily use this camera", e.g. a boss encounter:
ICameraOverrideHandle bossOverride = cameras.PushOverride(bossCameraId);
// ... later, "return to the gameplay camera":
bossOverride.Release();
```

**The pipeline** (CLAUDE.md's Phase 11 brief, section 3): Gameplay sets a `CameraController`'s target/
mode/configuration -> `ICameraMode.ComputePose` (pure C#) produces a `CameraPose` (Camera State) ->
`CameraZoomController` advances damped zoom -> `CameraBoundsConstraint` clamps the result -> exactly
one `CameraDriver` per scene (the Camera Driver layer) applies whichever camera `ICameraService`
currently resolves as active to the real `UnityEngine.Camera`/`Transform`. Every stage before
`CameraDriver` is pure, Unity-lifecycle-free C# (mirrors `Presentation.CameraShakeState`/
`ScreenEffectController`/`TimeFeedbackController`'s own "kept separate so it's EditMode-testable"
reasoning), so the whole pose pipeline is unit-testable without a live Camera or scene.

**`ICameraService`** is the one registered entry point (CLAUDE.md's Phase 11 brief, section 5) - the
Phase 11 equivalent of `GameFlow.IGameFlowService`/`Presentation.IPresentationService`:

- `RegisterCamera`/`UnregisterCamera` assign a stable `CameraId` (a process-unique monotonic counter,
  the same shape as `Gameplay.Entities.EntityId` - never `UnityEngine.Object.GetInstanceID()`, per
  CLAUDE.md section 42). Registering the same `CameraController` instance twice is idempotent and
  returns the existing id.
- `Activate(id)` sets the base (non-override) camera.
- `PushOverride(id)` returns an `ICameraOverrideHandle` and makes `id` the active camera immediately,
  regardless of priority - releasing the handle (`Release()`, safe to call more than once) removes it
  from wherever it sits in the stack (not only the top), so nested overrides can be released out of
  order without corrupting the stack (CLAUDE.md's Phase 11 brief, section 26). `ActiveCameraId` always
  resolves to the top of the override stack, or the base camera if the stack is empty.
- `SetTarget`/`ClearTarget`/`SetMode` forward to the named `CameraController`.
- `ResetCamera(id)` snaps a controller's pose to its mode/target's current value immediately,
  clearing any in-progress damping (restart/respawn/recovery, section 27).
- `GetState(id)` returns a read-only `CameraRuntimeState` snapshot (active/override flags, target
  presence, current pose) - development/UI diagnostic, never a live mutable reference.
- Every command is safe against an unknown `CameraId` (returns `false`/`null`/a default value and
  logs once, never throws) - the same "normal flow never needs a try/catch" policy
  `GameFlow.IGameFlowService` already established.

**Priority is informational only** (CLAUDE.md's Phase 11 brief, section 7's "predictable camera
selection mechanism") - it is recorded at `RegisterCamera` for diagnostics (`GetState`), mirroring
`Performance.Ticking.TickGroup`'s own "does not affect execution order by itself" precedent. Which
camera is actually active is always the direct result of the last `Activate`/`PushOverride`/handle-
`Release` call, never an implicit priority comparison, which keeps "what's live right now" fully
predictable from the command history rather than needing to reason about priority ties.

**`ICameraTarget`** is the lightweight target abstraction (section 9) - `Position`/`Rotation`/
`IsValid`/`HasVelocity`/`Velocity`. `TransformCameraTarget` is the built-in adapter over a plain
`Transform`; it deliberately reports `HasVelocity = false` rather than deriving one from per-frame
position deltas (doing so correctly needs a per-frame sample call with no natural owner - a game that
needs real velocity, e.g. for a look-ahead camera, implements `ICameraTarget` directly over its own
Rigidbody/controller). A missing or destroyed target (`IsValid == false`) makes every built-in mode
hold its previous pose unchanged rather than snapping to a default or throwing (section 35).

**`ICameraMode`** is the extension point (section 8) - `CameraPose ComputePose(CameraModeContext,
CameraPose previousPose, float deltaTime)`, pure and Unity-lifecycle-free by convention. Four built-in
modes (all `internal` - authored via `CameraConfiguration.Mode`/`CreateMode()`, or a game supplies its
own `ICameraMode` directly via `CameraController.SetMode`):

- **Follow** - tracks a target subject to a per-axis follow mask (`Vector3 FollowAxisMask`; an axis
  with mask 0 is never touched by this mode and stays exactly where this controller was last snapped
  to - "fixed camera depth" is simply a Z mask of 0), `SmoothDamp` position damping, and an optional
  dead zone/soft zone. The dead zone (`CameraDeadZone`, section 11) is evaluated relative to the
  camera's *own current position*, not the target's absolute position: the target can move freely
  within the zone; once it exits, the camera moves the minimum amount needed to keep the target at
  the zone's edge (never snapping the target back to center). The optional soft zone (section 12)
  eases the camera in gradually over an additional band beyond the dead zone before the same
  full-catch-up rule applies further out.
- **Static** - holds whatever pose this controller was last snapped to, regardless of any assigned
  target. "Static" means placing the `CameraController`'s own GameObject at the desired position/
  rotation in the scene (no target assigned) - not a position baked into the shared, reusable
  `CameraConfiguration` asset, since position is scene-specific while mode/damping settings are
  reusable across scenes.
- **TargetLook** - position tracks the target at a configurable, damped offset; rotation stays
  separately damped toward looking at the target (section 8's "track target position while
  maintaining configurable camera positioning").
- **Manual** - passes through whatever pose `CameraController.SetManualPose` last supplied, letting an
  external system (a cutscene player, a custom rig) drive camera intent without ever touching
  `Camera`/`Transform` directly (section 8's "provide camera intent without directly manipulating the
  Unity camera").

**Zoom** (`CameraZoomController`, sections 14-15) is damped and clamped independently of
`ICameraMode`, so any mode can be zoomed - `orthographicSize` or `fieldOfView`, whichever the
controller's own `Camera.orthographic` flag says (authored once per controller via the `_orthographic`
Inspector field, since a `CameraController` does not require its own `Camera` component - see
`CameraDriver`'s remarks - and therefore cannot read the flag off a live component at authoring time).

**World bounds** (`CameraBoundsConstraint`, section 13) clamp the final position after the mode/zoom
have run. Exact for an orthographic camera (subtracts half-width/half-height, computed from
orthographic size and aspect, so the *view* - not just the center point - never leaves the bounds);
for a perspective camera this clamps only the center position with no extent compensation, the
"reliable subset" the brief explicitly allows for, since a perspective camera's view extent depends on
distance-to-subject, which this framework assumes nothing about. Bounds narrower than the camera's own
view on an axis center instead of producing an inverted clamp range (section 13's "edge cases").

**Transitions** (`CameraTransitionRunner`, section 19) blend whenever `CameraDriver` detects the
resolved active camera changed (activation, override push/pop) - one combined position+rotation+zoom
blend (`CameraConfiguration.DefaultTransition`: a duration and an optional easing `AnimationCurve`),
not three independently-timed ones; separate per-channel durations were not built since no concrete
need for them was identified (a documented scope reduction, not an oversight - extend
`CameraTransitionSettings` if a game genuinely needs it). Each frame it blends from the pose actually
applied *last frame* toward the newly active controller's freshly computed pose, rather than a fixed
start/end pair, so an interrupted or re-interrupted transition always blends from wherever the camera
visually is right now (section 19's "interruptible, cancelable"). A zero (or unauthored) duration is
an immediate cut, no blending overhead.

**`CameraDriver`** is the one MonoBehaviour per scene that actually writes to a real `Camera`/
`Transform` (attach it to the GameObject carrying the `Camera` component this scene renders through).
Plain `LateUpdate`, not `Performance.Ticking.ITickService` - CLAUDE.md's own Tick Rules call for plain
`Update`/`LateUpdate` for "anything simple, low-count, or already working," and a scene has exactly
one active camera driver, the same reasoning `Presentation.CameraFeedbackDriver` already used for
itself. `[DefaultExecutionOrder(-100)]` guarantees it runs before that driver's own (default-order)
`LateUpdate` regardless of GameObject/component creation order, so `CameraFeedbackDriver` always reads
this frame's freshly-written base pose before layering its own shake offset on top - see
[Architecture](#architecture) for the full composition story, achieved with zero compile-time
reference between the two assemblies.

**`CameraController`** is the "Camera Target/Intent" authoring point (a MonoBehaviour) - it does
**not** require its own `Camera` component; only the driver's GameObject needs one. This lets a
"Boss camera" exist as a plain empty GameObject positioned at a vantage point, registered but not
activated until needed, with no separate physical camera/`AudioListener`/culling-mask juggling (an
explicit, deliberate scope reduction - see this section's Non-goals). `[SerializeField]`s expose
`CameraConfiguration`, priority/owner (diagnostic), an initial target `Transform`, the `_orthographic`
flag, aspect, and `_activateOnRegister` (default true, for the common single-camera-per-scene case;
turn it off for a camera a game will `Activate`/`PushOverride` explicitly later). It self-registers
with `ICameraService` in `OnEnable`/unregisters in `OnDisable`, the same pattern
`Presentation.CameraFeedbackDriver` already established, polling for `GameBootstrapper.Instance`
reaching `Ready` if `OnEnable` ran before Bootstrap did.

**Initial pose.** A controller's very first pose is seeded from its own `transform.position`/
`transform.rotation` - deliberately never from an assigned target's position, even though a target
may already be assigned at `Awake`. Seeding from the target would corrupt any axis this controller
does not follow (e.g. a fixed camera depth) with the target's unrelated value on that same axis; the
followed axes then converge onto the target through the mode's own normal damping on the first real
tick, which is a smooth (not instant) entrance by design. `ResetCamera`/`CameraController.Snap()`
achieves an *instant* convergence instead by ticking once with `Camera.ReduceMotion` semantics forced
on (zero damping) - reusing the same mode/zoom/bounds pipeline rather than reimplementing per-axis
"snap to target" logic a second time, so it stays correct for every mode automatically (a Static-mode
camera's `Snap()` correctly does nothing, since `StaticCameraMode.ComputePose` never reads the
target).

**Time integration** (section 21). A gameplay camera should freeze naturally while the game is paused -
achieved for free, since `CameraDriver` defaults to `ITimeService.ScaledDeltaTime` (0 while
`ITimeService.IsPaused`), exactly like `Presentation.CameraFeedbackDriver`'s own default. A menu/UI
camera that must keep moving while gameplay is paused sets `CameraDriver`'s `_useUnscaledTime`
Inspector flag instead. Never writes `Time.timeScale`/calls `Pause`/`Resume` directly.

**Tick integration** (section 22). Deliberately plain `LateUpdate`, not
`Performance.Ticking.ITickService` - see `CameraDriver`'s remarks above; `GameFramework.Cameras`
references `GameFramework.Performance` only for `ProfilingCategory.Cameras`/`ProfileScope`, wrapped
around `CameraDriver.LateUpdate`'s per-frame work (zero-cost when profiling is disabled, the same
pattern every other framework boundary uses).

**Game Flow integration** (section 23). No direct reference in either direction, matching Phase 10's
own "communicate through events, not direct calls" principle - and largely not even needed here:
gameplay pause is already handled for free by `ITimeService.ScaledDeltaTime` going to 0 (see Time
integration above), which is the bulk of what section 23 asks for. A game's own bridge code (or
nothing at all) turns a `GameFlow.PlayerRespawnedEvent`/`LevelRestartedEvent` into a
`cameras.ResetCamera(id)`/`SetTarget(...)` call if it wants the camera to react to those specifically.

**Settings integration** (sections 28-29). Built entirely on the existing `ISettingsService` - one
new setting, `"Camera.ReduceMotion"` (bool, default off, category `"Camera"`), registered by
`CameraService.Initialize` the same way `PresentationService` registers its own settings. When on,
every built-in mode/zoom skips damping entirely (position/rotation/zoom still update correctly, just
without the eased animation) rather than freezing camera movement outright - section 29's "must allow
disabling or reducing camera motion without breaking gameplay camera positioning". Deliberately
**not** a second camera-shake toggle: `"Presentation.Channel.Camera"` (Phase 10) already gates shake,
a different, already-owned concern.

**Editor tooling** (sections 32-33). `CameraConfigurationValidator` (`GameFramework.Editor.Cameras`)
follows the same pattern as `FeedbackContentValidator`/`QuestContentValidator`: validate the selected
`CameraConfiguration` asset (inverted zoom/bounds ranges, an all-zero follow axis mask, a negative
soft-zone size). `CameraController.OnDrawGizmosSelected` (Editor-only, conditional-compiled) draws the
dead zone/soft zone rectangles, world bounds, and a line to the current target - development-only
visualization with no runtime cost, satisfying section 33 without a separate UI Toolkit debug window
(not built - see Known Limitations).

**Sample.** `Assets/GameFramework/Samples/Phase11Demo/` demonstrates a Player target moved by WASD/
arrow keys, a Follow-mode gameplay camera (`Content/GameplayCameraConfig.asset` - dead zone, soft
zone, world bounds, zoom range) tracking it, a "BossCameraPoint" (a plain GameObject with a
`CameraController` in Static mode and no physical camera) pushed/popped as a temporary override, and a
`Content/CameraShake.asset` `FeedbackDefinition` demonstrating Phase 10 camera-feedback composing on
top - combining a small `Phase11DemoBootstrapper` (`ICameraService` + `IPresentationService`, no
`PlayerSystemsBootstrapper` needed since the sample only exercises the Camera channel) with
`Phase11DemoController`. Verified live in Play Mode (see Testing). Not part of the reusable framework.

**Non-goals (explicitly not built).** Full Cinemachine replacement (see
[Cinemachine Integration](#cinemachine-integration) for the actual, optional integration built
instead), cinematic director/cutscene
framework, Timeline replacement, physical multi-camera rendering/switching (culling
masks/`AudioListener`/post-processing per camera), full split-screen framework, minimap framework,
photo mode, replay camera system, a second tweening/interpolation engine (transitions reuse plain
`Vector3.Lerp`/`Quaternion.Slerp`/`Mathf.SmoothDamp`), and per-channel (position/rotation/zoom)
transition durations (a documented scope reduction - see Transitions above). `ICameraMode`/
`ICameraTarget`'s own extensibility, plus `CameraTransitionSettings`, are the seams a game's own
camera rig or a later phase would extend, not something this phase builds itself.

## Cinemachine Integration

Added within Phase 11 once Cinemachine 2.10.7 was installed in this project (`GameFramework.Cameras`
itself predates it and was not written against it). One new, **optional** assembly,
`GameFramework.Cameras.Cinemachine` (Editor tooling in a second optional assembly,
`GameFramework.Cameras.Cinemachine.Editor`) - see [Architecture](#architecture) for the exact
dependency boundary. Nothing above this integration needs to know it exists: gameplay code still
only ever calls `ICameraService`/`CameraController` - the same API whether a given scene's camera is
driven by `CameraDriver` (the pure-C# pipeline) or by this integration's
`CinemachineCameraBackend`.

**Core principle: Cinemachine owns camera *solving*; GameFramework owns camera *orchestration*.**
Identity, registration, activation/priority policy, the override stack, target assignment, and
lifecycle all stay exactly where Phase 11 already put them (`ICameraService`/`CameraController`,
completely unmodified in responsibility). What changes is *who computes the camera's position,
rotation, framing, and blend*: instead of `CameraPoseController`'s own `ICameraMode` → zoom → bounds
pipeline, a real `CinemachineVirtualCamera`'s Follow/LookAt/Body/Aim components and
`CinemachineBrain`'s blending do that work. This integration never re-implements what Cinemachine
already does well - no custom dead zone/soft zone solver, no custom confiner, no custom blend
engine - it only translates GameFramework's *decisions* (which camera is active, what its target is,
what zoom value it wants) into the Cinemachine primitives that already solve those problems.

**Two components, no new service.** Unlike every other phase, this integration registers nothing
with `IServiceRegistry` - it is pure scene composition on top of the existing `ICameraService`:

- **`CinemachineCameraAdapter`** (one per Cinemachine-backed camera) pairs one `CameraController`
  with one `CinemachineVirtualCamera`. Attach it alongside both on a camera rig prefab/GameObject -
  the same rig a Cinemachine-only project would already build, plus this one adapter.
- **`CinemachineCameraBackend`** (exactly one per scene, the Cinemachine-backed sibling of
  `CameraDriver` - attach alongside `CinemachineBrain` on the actual render `Camera` GameObject, use
  one driver or the other per scene, never both for the same camera) reacts to
  `ICameraService.ActiveCameraChanged` and maps the resolved active camera onto real Cinemachine
  `Priority`. Adapters self-register with it (`Register`/`Unregister`, called from
  `CinemachineCameraAdapter.OnEnable`/`OnDisable`) via an explicit `[SerializeField]` reference - the
  same "prefer explicit registration, never scan the scene" principle `CameraController` already
  follows with `ICameraService`. The registry is keyed by `CameraController` (never `CameraId`
  directly): an adapter can `OnEnable` before its controller has finished binding to
  `ICameraService` (and therefore before it has a valid `CameraId`), so a `CameraId` is always
  resolved fresh, on demand, through the already-public `ICameraService.GetController`.

**Activation → Priority, purely event-driven.** `CinemachineCameraBackend` never polls every frame
for "did the active camera change" - it subscribes directly to
`ICameraService.ActiveCameraChanged` (CLAUDE.md's own Tick Rules: "can this be event driven?").  On
change, it demotes the previously-active adapter's vcam back to its authored
`CinemachineCameraAdapter._basePriority` and boosts the newly-active one by a fixed
`CinemachineCameraAdapter.ActivePriorityBoost` (1000) - large enough to outrank any other registered
adapter's own base priority regardless of authored value, so activation is never ambiguous, and
since exactly one adapter is ever boosted at a time (the previous one is demoted in the very same
call), no priority tie is possible. `CameraController`'s own `Priority` field stays exactly what it
already was - informational/diagnostic only (see `CameraService`'s remarks) - it is deliberately
*not* reused as the real Cinemachine priority, so its existing "does not affect selection" contract
for the non-Cinemachine path is never surprised by this integration. `CinemachineBrain` then performs
the actual camera switch and blend entirely on its own - this integration never touches the Unity
Camera's `transform` (CLAUDE.md's Phase 11 Cinemachine brief, Rule 5/39: never fight Cinemachine by
writing the Camera transform directly while it owns that property).

**Target mirroring.** `CinemachineCameraAdapter.ApplyTarget` reads `CameraController.Target` (a new,
small, additive public getter - previously only readable indirectly via `HasTarget`) and, if it is a
`TransformCameraTarget`, mirrors its wrapped `Transform` (also a new, small, additive public
getter on `TransformCameraTarget` - previously private) onto the vcam's `Follow`/`LookAt`. Any other
`ICameraTarget` implementation clears Follow/LookAt instead of guessing - Cinemachine needs a real
`Transform`, and a game needing a non-Transform target on a Cinemachine-backed camera targets a proxy
`Transform` it updates itself. Gameplay never touches the adapter for this - it keeps calling
`ICameraService.SetTarget`/`CameraController.SetTargetTransform` exactly as before; the backend
mirrors it automatically by subscribing to the existing `CameraTargetChangedEvent` (also re-applied
once, immediately, whenever a camera newly becomes active, so a target assigned before activation is
never missed).

**Zoom stays a single call site.** Cinemachine has no time-based "damp this vcam's own lens toward a
target value" concept - blending is only *between* vcams. Rather than inventing a second,
Cinemachine-specific zoom API, gameplay keeps calling the exact same
`CameraController.SetZoom(float)` it would for the non-Cinemachine path. Each frame,
`CinemachineCameraBackend.LateUpdate` calls the *currently active* adapter's
`CameraController.ComputePose` (already public, already damped via `CameraZoomController`, already
`Camera.ReduceMotion`-aware since `CameraController` binds `ISettingsService` itself when it
registers with `ICameraService`) and passes the result to `CinemachineCameraAdapter.ApplyZoom`,
which writes only `OrthographicSize`/`FieldOfView` - whichever matches the vcam's own
`m_Lens.Orthographic` - straight to the lens, discarding the position/rotation half of that same
call (Cinemachine already owns those). This reuses the existing, tested `CameraPoseController`/
`CameraZoomController` pipeline for the one channel Cinemachine does not solve, rather than
duplicating SmoothDamp zoom logic a second time - `CameraConfiguration.Mode`'s position-related
settings (Follow/TargetLook axis mask, dead zone, offset) are simply never consulted for a
Cinemachine-backed camera, since its `_poseController` is never ticked for position by anything
(only the zoom half of the same computation is read) - author `CinemachineVirtualCamera`'s own
Body/Aim components for framing instead (see Prefab vs. Profile Responsibility below). On
`ActiveCameraChanged` and on the new `CameraResetEvent` (published by `CameraService.ResetCamera`,
alongside the existing `Controller.Snap()` call, purely additive), the backend also applies the
zoom immediately (`ComputePose(0f)`/`CurrentPose`) rather than waiting for the next `LateUpdate`, so
switching or resetting a camera never has a one-frame-stale lens value.

**Bounds → Confiner2D, only when nothing is already authored.** `CinemachineCameraAdapter` accepts
an optional `CinemachineConfiner2D` reference. If assigned and its `m_BoundingShape2D` is still
unset, `Start()` translates the same `CameraConfiguration.Bounds` rect the non-Cinemachine path
already reads (`CinemachineBoundsTranslator.TryComputeBoxBounds`, the one genuinely new piece of
math this integration adds - pure and unit-tested) into a generated, disabled-trigger
`BoxCollider2D` and assigns it, so Cinemachine performs the actual confinement math, not a
GameFramework re-implementation (section 23 of the brief: "the smallest additional layer required").
A prefab that already authors its own `m_BoundingShape2D` (any `Collider2D` shape, not only a box) is
left completely untouched - the prefab stays authoritative (see Prefab vs. Profile Responsibility).

**Transitions/blending are entirely `CinemachineBrain`'s.** `CameraTransitionRunner`/
`CameraConfiguration.DefaultTransition` are specific to the non-Cinemachine `CameraDriver` path and
are never consulted here. A Cinemachine-backed scene's blend policy is authored directly on
`CinemachineBrain.m_DefaultBlend` (or per-camera-pair via its `m_CustomBlends` asset) - exactly
"reuse existing Cinemachine Brain blend settings" rather than duplicating a second transition-duration
concept (brief section 31). `CinemachineCameraBackend` exposes one small, optional convenience
(`_overrideDefaultBlend` + `_defaultBlend`) that applies a `CinemachineBlendDefinition` to the Brain
once at startup, purely so a scene's blend policy can be authored alongside the rest of this
framework's camera setup instead of hunting for the Brain component separately - it never builds a
competing blend engine.

**Phase 10 composition needed zero code changes to `GameFramework.Presentation`.**
`Presentation.CameraFeedbackDriver` was already written (Phase 10) to never assume anything about
what wrote a camera's transform earlier in the frame - it subtracts its own previous contribution
and adds a new one each `LateUpdate` (see that class's remarks), so it composes correctly on top of
*either* `CameraDriver` or `CinemachineBrain`'s own transform write, with zero knowledge of which one
is present. The only change `CameraFeedbackDriver` picked up for this integration is
`[DefaultExecutionOrder(100)]` (previously unordered/default) - it now deterministically runs after
*any* default-order component, not only `CameraDriver` (`-100`), so it also runs after
`CinemachineBrain`'s own (default-order) `LateUpdate`. This was the one real gap: two default-order
(`0`) components' relative `LateUpdate` order is otherwise unspecified/instantiation-order-dependent.
Cinemachine Impulse (`CinemachineImpulseSource`/`CinemachineImpulseListener`) was deliberately **not**
adopted - Phase 10 already owns camera feedback/shake completely, and building a second shake
pipeline through Impulse would violate the brief's own Rule 4 ("Phase 10 owns camera feedback; Phase
11 integrates it") for no behavioral gain, since the existing composition already works unmodified.

**Editor validation.** `CinemachineCameraSetupValidator`
(`GameFramework.Editor.Cameras.CinemachineIntegration`, menu item "GameFramework/Cameras/Validate
Cinemachine Setup In Scene") follows the same modest, console-warning pattern as
`CameraConfigurationValidator` rather than a new UI Toolkit window (brief section 47: "do not build a
giant custom camera editor") - checks for a missing/duplicated `CinemachineCameraBackend` in the open
scene, a missing `CinemachineBrain` on one, and per-adapter issues (missing Controller/Virtual
Camera/Backend reference, two adapters sharing one `CameraController`, an assigned Confiner with
nothing to confine against).

**Prefab vs. Profile Responsibility.** Cinemachine-specific configuration (Follow/Aim component
choice, dead zone/soft zone/damping values, lens near/far clip, the Confiner's actual bounding
shape) belongs on the `CinemachineVirtualCamera` prefab, authored through Cinemachine's own
inspector - this integration never duplicates that surface into a GameFramework
`ScriptableObject`. `CameraConfiguration` keeps its existing, narrower role for a Cinemachine-backed
camera: only `Zoom` (min/max/default/damping) and `Bounds` (the rect `CinemachineBoundsTranslator`
reads) are actually consulted; `Mode`/`Follow`/`TargetLook`/`DefaultTransition` are simply unused for
that camera (Cinemachine's own components replace them), matching brief section 26's "do not blindly
duplicate every Cinemachine field into a ScriptableObject."

**Namespace note.** The new runtime/editor assemblies use `GameFramework.Cameras.CinemachineIntegration`/
`GameFramework.Editor.Cameras.CinemachineIntegration`, not `GameFramework.Cameras.Cinemachine` -
deliberately avoiding the exact namespace-shadowing footgun this framework has hit before (a C#
namespace segment matching a real package/engine namespace name shadows it for ancestor-namespace
lookup purposes; here `GameFramework.Cameras.Cinemachine` as a literal namespace would shadow the
real `Cinemachine` package namespace for any code nested under it, the same class of issue documented
for `Input`/`Audio`/`UI` elsewhere in this codebase) - the *assembly* is still named
`GameFramework.Cameras.Cinemachine` since assembly names don't participate in C# namespace lookup.

**Sample.** `Assets/GameFramework/Samples/Phase11Demo/Phase11CinemachineDemo.unity` - a second,
separate scene alongside the original `Phase11Demo.unity` (which still demonstrates the
non-Cinemachine `CameraDriver` path unmodified) - mirrors the original demo's exact gameplay-facing
calls (`Phase11CinemachineDemoController` never references a Cinemachine type) against a
Cinemachine-backed setup: a `CinemachineVirtualCamera` with a `CinemachineFramingTransposer` (dead
zone/damping) and a `CinemachineConfiner2D` (bounds generated from
`Content/CinemachineGameplayCameraConfig.asset`) follows the Player, a fixed-vantage "EventCamera"
vcam is pushed/popped as a temporary override with `CinemachineBrain` performing the actual blend,
`CameraController.SetZoom` drives the vcam's lens, and the same `Content/CameraShake.asset` Phase 10
feedback definition composes on top via a `Presentation.CameraFeedbackDriver` on the Main Camera.
Verified live in Play Mode (see Testing) - through direct service/reflection calls via the Unity MCP
`execute_code` tool, since Play Mode key-press simulation was not available through the tooling used
for this verification, exactly like the original Phase11Demo's own verification.

**Known limitations.** No `ICameraTargetGroup`/`CinemachineTargetGroup` wiring (brief section 35 -
"only if needed"; a game wanting multi-target framing today assigns a `CinemachineTargetGroup`'s own
`Transform` directly as a vcam's Follow target and uses `CinemachineGroupComposer`, which already
composes with this integration with zero extra code, since target mirroring only ever needs a
`Transform`). No per-camera-pair custom blend authoring UI (author `CinemachineBrain.m_CustomBlends`
directly). No runtime vcam creation/destruction helpers (author camera rigs as prefabs/scene content,
per brief section 29's "prefer authored camera prefabs for normal gameplay"). Perspective-camera
bounds inherit the same "center-only, no extent compensation" caveat `CameraBoundsConstraint` already
documents for the non-Cinemachine path - `CinemachineBoundsTranslator` only produces the bounding
shape; Confiner2D's own extent handling is unaffected by this integration either way.

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
- Phase 5's `GameFramework.Performance.Tests` (EditMode) covers `TickServiceTests` (registration/
  duplicate-registration/unregistration/priority ordering/register-and-unregister-during-tick/
  exception isolation/fixed and late phases, against a fake `ITimeService`) and
  `PerformanceMonitorServiceTests` (frame sampling, worst-frame tracking, reset, budget reporting) —
  `GameFramework.Performance` grew its own `AssemblyInfo.cs` (`InternalsVisibleTo` for
  `GameFramework.Performance.Tests`) to expose `TickService`'s internal `TickFixed`/`TickLate` to
  tests, the same pattern `GameFramework.Runtime`'s `AssemblyInfo.cs` already established.
- `PoolHardeningTests` was added alongside the existing `GameObjectPoolTests` in
  `GameFramework.Gameplay.Tests.Runtime` (PlayMode, since it needs real GameObjects) rather than a
  new assembly: foreign-object release rejection, statistics (Get/Release/miss/peak/total-created
  counts), dispose-with-active-instances, and `PrewarmStagedRoutine` reaching its exact target count.
- Phase 6's three EditMode test assemblies (`GameFramework.Progression.Tests`, `.Unlocks.Tests`,
  `.Rewards.Tests`) each build a minimal, already-initialized `ServiceRegistry` via a small
  `TestRegistryFactory` — a fake `ITimeService` (only `EconomyService` actually needs one, for
  transaction timestamps) plus the *real* `EventService` and a *real* `PersistenceService` backed by
  `InMemoryPersistenceStorage`, so save/load round-trips are tested against the genuine persistence
  code path, never the developer's real save files. Definition ScriptableObjects (`CurrencyDefinition`,
  `ItemDefinition`, ...) are built via `ScriptableObject.CreateInstance` and populated through
  reflection onto their private `[SerializeField]`s in a `TestDefinitions` helper — the same
  read-only-property trade-off `ObjectiveDefinition` already accepts elsewhere in the framework.
  `GameFramework.Runtime`'s `AssemblyInfo.cs` grants all three `InternalsVisibleTo` for the
  `MarkInitialized`-based registry pattern.
- `EconomyServiceTests`/`InventoryServiceTests`/`ExperienceServiceTests` cover each service's core
  business rules in isolation (validation, clamping, partial-add remainder, multi-level-up math,
  save/load, reset). `RequirementCompositionTests` covers `AllRequirement`/`AnyRequirement` against
  fake requirements (including the vacuous-AND-vs-vacuous-OR distinction). `UnlockServiceTests`
  covers registration/duplicate-detection/prerequisite chains/`ValidateNoCycles` against a real
  detected cycle. `RewardTypeTests`/`RewardServiceTests` cover each concrete `IReward` plus claim
  idempotency (a `Once` reward claimed five times in a row grants exactly once).
  `IntegrationTests` (in `GameFramework.Rewards.Tests`, since it can see all five services) covers
  the cross-system flows the project's testing strategy calls for: an XP reward triggering a level-up
  that satisfies a pending unlock requirement, a full claim→save→reload→replay-claim cycle proving
  idempotency survives a session boundary, and an invalid bundle member rejecting the entire claim
  with zero partial state change.
- **A real bug this testing caught before it shipped:** `UnlockService.Load()`/`RewardService.Load()`
  originally filtered persisted ids against `_entries` (mirroring Economy/Inventory's validation) —
  but `RegisterUnlock`/`RegisterReward` necessarily happen *after* `Initialize()`/`Load()` (a
  requirement/reward typically needs another already-initialized service resolved from the
  registry), so `_entries` was always empty at load time and every persisted unlock/claim was
  silently dropped on every session after the first. `SaveThenLoad_RestoresUnlockedState` failed
  immediately on the first real test run; the fix (load unconditionally, accept that a removed
  content id just sits unused rather than trying to validate at a point where validation is
  structurally impossible) is documented on both services.
- Phase 7's `GameFramework.Quests.Tests` (EditMode) reuses the exact Phase 6
  `TestRegistryFactory`/`TestDefinitions`-via-reflection pattern (`GameFramework.Runtime`'s
  `AssemblyInfo.cs` grants it the same `InternalsVisibleTo`). `StatisticsServiceTests` covers
  increment/decrement/monotonic rejection/overflow clamping/session-vs-persistent save behavior.
  `ConditionTests`/`ConditionObjectiveTests` cover All/Any/Not composition (including the vacuous
  AND/OR identities) and the Inactive/Active/Completed/Reset objective lifecycle. `QuestServiceTests`
  covers availability, start, event-driven completion (including that an unrelated statistic change
  never touches an active quest — the statistic index actually working, not just present),
  All/Any/Count completion rules, claim idempotency, repeat-policy-gated reset, and save/load
  round-trips for both an in-progress and a completed-and-claimed quest. `AchievementServiceTests`/
  `MilestoneServiceTests` cover the same completion/claim/persistence shape for their simpler
  single-condition/single-threshold cases, plus auto-claim. `IntegrationTests` covers all four flows
  CLAUDE.md's Phase 7 brief calls out by name: statistic → objective → quest completion; statistic →
  achievement → reward → currency; XP → level-up → unlock requirement and achievement condition
  updating together; and quest-complete → save → simulated restart → load → still claimed → replay
  claim rejected.
- Phase 8's `GameFramework.GameFlow.Tests` (EditMode) fakes both `ITimeService` and `ISceneService`
  (a real `AsyncOperation` cannot be constructed or driven manually outside of an actual engine
  scene load, so `FakeSceneService.RaiseSceneLoaded` stands in for it — see
  [Game Flow, Sessions & Checkpoints](#game-flow-sessions--checkpoints)) alongside a real
  `EventService`/`PersistenceService`-over-`InMemoryPersistenceStorage`, the same
  `TestRegistryFactory` pattern every phase since 3 has used. `LevelFlowStateMachineTests` covers
  the transition table directly (valid/invalid/already-in-state, the Failed→Playing respawn-only
  edge, bounded history). `GameFlowServiceTests` covers load/ready/start, reactive pause/resume
  (including two independent `PauseGameplay` token owners), complete/fail with default vs. explicit
  `GameplayResult`, respawn (no session/no checkpoint/success, both from Playing and from Failed),
  retry with and without a scene reload, exit (including cancelling a load in progress and
  confirming a stale `SceneLoaded` afterward is ignored), attempt-number tracking across retries, a
  re-entrant command issued from inside an event handler being blocked, and a persisted-checkpoint
  round trip across two separate `GameFlowService` instances sharing one `PersistenceService`.
  `CheckpointSystemTests` covers `CheckpointSystem` in isolation. `IntegrationTests` covers the five
  named flows from CLAUDE.md's Phase 8 brief end to end (load→ready→start→playing→complete;
  playing→pause→resume→playing; playing→checkpoint→failure→respawn→playing, same attempt;
  playing→failure→retry→new session→playing; complete→save→simulated restart→load→checkpoint still
  resolves). All 48 Phase 8 tests pass, alongside the full existing suite (478 EditMode + 74
  PlayMode tests project-wide, verified together after this phase's changes — zero regressions).
- Phase 9's `GameFramework.Tutorials.Tests` (EditMode) reuses the same `TestRegistryFactory`
  pattern every phase since 3 has used (a fake `ITimeService`/`IInputService`, real
  `EventService`/`PersistenceService`-over-`InMemoryPersistenceStorage`).
  `TutorialLifecycleStateMachineTests` covers the transition table directly.
  `TutorialStepTests` covers each built-in step type in isolation, including `Reset()` restoring a
  Completed/Cancelled step to NotStarted (see below) and `WaitStep` not retaining elapsed time
  across a reset. `TutorialServiceTests` covers start/complete/skip/cancel/restart, all three
  repeat policies, prerequisites (unmet/met/unregistered), an empty step list completing
  immediately, and duplicate/missing-id registration errors. `TutorialPauseAndInputGatingTests`
  covers `PausesGameplay` acquiring/releasing exactly once without the tutorial observing its own
  acquisition as an external pause, reactive Paused/Running on an external pause/resume, step
  ticking correctly freezing while Paused, and input-context push/pop (including on `Shutdown`
  mid-tutorial). `TutorialReentrancyAndCleanupTests` covers a command issued from inside a
  Started/StepCompleted/Completed event handler being blocked without corrupting the in-progress
  transition, a multi-step zero-duration cascade completing in one command with events published in
  the correct order, an `EventStep` unsubscribing on cancel, and double-`Cancel` not double-releasing
  pause. `TutorialPersistenceTests` covers all three persistence policies, the registration-after-
  Load ordering this framework already hit once in Phase 6, `OncePerSession` allowing a start in a
  new session despite a persisted completed flag, and corrupted (mismatched-length) save data being
  ignored rather than crashing. `IntegrationTests` covers the full Instruction→Input→Event→
  Condition→Instruction sequence end to end, skip-then-restart, cancel-then-normal-replay, and a
  prerequisite-gated tutorial becoming startable after its prerequisite completes, saves, and
  reloads under a simulated restart.
- **A real bug this testing caught before it shipped:** the same `ITutorialStep` *instances* passed
  to `RegisterTutorial` are reused across every run of a tutorial (a `Repeatable` replay, `Restart`,
  or `Cancel` followed by a later `Start`) — but the first implementation gave `ITutorialStep` no way
  back to `NotStarted`, so a step that already reached `Completed`/`Cancelled` once silently refused
  to `Begin` again, corrupting the second run's sequencing (observed as a run landing on the wrong
  step, or a `Restart` after full completion collapsing straight back to `Inactive` as every already-
  completed step cascaded past itself). Five tests failed on the first real run
  (`Cancel_ThenStartAgain_BeginsFromFirstStep`, `Restart_WhileMidRun_...`,
  `Restart_AfterOnceCompletion_...`, and the two matching `IntegrationTests`); the fix added
  `ITutorialStep.Reset()` (and `TutorialStepBase`'s implementation of it), called on every step of a
  tutorial immediately before `TutorialService` begins a new run. All 93 Phase 9 tests pass,
  alongside the full existing suite (571 EditMode + 74 PlayMode tests project-wide, verified
  together after this phase's changes — zero regressions), and the Phase9Demo sample's full
  five-step sequence, including a `Repeatable` second run, was additionally verified live in Play
  Mode.
- Phase 10's `GameFramework.Presentation.Tests` (EditMode) covers the pure-logic pieces directly:
  `CameraShakeStateTests` (composition of simultaneous shakes, deterministic same-seed-same-output,
  different seeds producing different output, axis masking, falloff decreasing over time averaged
  across samples to avoid single-sample noise-variance flakiness, duration expiry), and
  `ScreenEffectControllerTests`/`TimeFeedbackControllerTests` (fade-in/hold/fade-out sequencing,
  intensity scaling, the exclusive-channel priority policy - a lower-priority request while active
  is rejected, equal-or-higher replaces - and cancellation). `PresentationServiceTests` reuses the
  `TestRegistryFactory` pattern (a fake `ITimeService`, real `EventService`/`SettingsService`-over-
  `InMemoryPersistenceStorage`/`TimerService`) plus small fakes for `IAudioService`/`IFeedbackService`/
  `IUIService`/`ICameraFeedbackDriver`, registered per test since `PresentationService` resolves
  every one of them softly. Covers registration (duplicate/missing id), the master
  "Presentation.Enabled" switch, per-channel settings disabling only that channel, intensity scaling
  (including the "no SetVolume call at full intensity" rule), every channel executing correctly
  when its backing service/driver is present and silently no-op-and-logging-once when it is not,
  `FeedbackPlayedEvent`/`UIFeedbackRequestedEvent` publication, camera-driver registration (a stale
  `Unregister` from a different driver never clears the current one), event-to-feedback mappings
  (registration/duplicate-registration/unregistration), and `Shutdown` resetting an active time
  effect's time scale. `GameFramework.Presentation.Tests.Runtime` (PlayMode) covers what
  `PresentationServiceTests` genuinely cannot: the non-pooled visual-effect fallback (`Object.Destroy`
  with a delay is refused outside Play Mode) spawning a real instance, and the pooled path actually
  using `Gameplay.Pooling.IPoolService`/`GameObjectPool` and releasing the instance back to the pool
  once its `Lifetime` elapses (driven by ticking the real `TimerService`).
- **A real bug this testing caught before it shipped:** the initial `CameraShakeState` noise sampling
  fed an integer `Seed` summed with a `sampleTime` that could itself land on a whole number, against
  a constant `0f` second coordinate — classic Perlin noise is exactly `0` at integer lattice points,
  so two *different* seeds could silently produce the *identical* (near-zero) offset whenever both
  happened to land on such a point, exactly the case `Tick_DifferentSeeds_ProduceDifferentOffsets`
  hit on the first real run. Fixed by offsetting every axis with distinct golden-ratio-derived,
  non-integer constants, which also decorrelates the three axes better than the original symmetric
  x/y sampling did. Separately, an early version of the non-pooled visual-effect EditMode test tried
  to call `UnityEngine.Object.Destroy(instance, lifetime)` from an EditMode test context, which Unity
  refuses outright ("Destroy may not be called from edit mode!") — not a framework bug, but the
  reason that one case of Phase 10 coverage lives in `.Tests.Runtime` instead. All 46 Phase 10
  EditMode tests and 2 PlayMode tests pass; the full project suite (617 EditMode + 76 PlayMode tests
  project-wide) was re-run after this phase with zero regressions, and the Phase10Demo sample's
  "HeavyImpact" definition (all six wired channels) and "Reward" definition, plus the master
  "Presentation.Enabled" toggle, were additionally verified live in Play Mode.
- Phase 11: `GameFramework.Cameras.Tests` (EditMode) covers every pure-logic piece directly, kept
  separate from any MonoBehaviour exactly like Phase 10's `CameraShakeState`/`ScreenEffectController`
  pattern: `CameraDeadZoneTests` (inside the zone, exactly at its edge, beyond it with and without a
  soft zone easing the transition), `FollowCameraModeTests`/`TargetLookCameraModeTests`/
  `StaticCameraModeTests`/`ManualCameraModeTests` (missing/invalid target holding the previous pose,
  a fixed axis genuinely never moving, dead-zone containment/exit, `Camera.ReduceMotion` bypassing a
  heavily-damped configuration exactly), `CameraBoundsConstraintTests` (orthographic half-extent
  clamp, perspective center-only clamp, bounds narrower than the camera's own view centering instead
  of an inverted `Mathf.Clamp` range, the Z axis never touched), `CameraZoomControllerTests` (min/max
  clamp, damped partial movement, `Snap` clearing velocity), `CameraTransitionRunnerTests` (immediate
  zero-duration, mid-blend interpolation, full-elapsed convergence, `Cancel`), `CameraPoseControllerTests`
  (mode→zoom→bounds composing correctly together, a target destroyed mid-follow holding the last pose
  instead of throwing or resetting to zero, `Snap` converging position/zoom instantly while a
  Static-mode `Snap` correctly never jumps to the target), and `CameraServiceTests` (duplicate
  registration returning the same id, unregistering an unknown id being a no-op, activation firing
  `ActiveCameraChangedEvent` only on an actual change, nested override push/pop restoring the correct
  camera including out-of-order release, unregistering the currently active/overriding camera falling
  back correctly, `GetState`'s active/override flags). `GameFramework.Cameras.Tests.Runtime`
  (PlayMode) covers what genuinely needs a running engine - EditMode never runs the player loop, so
  `CameraDriver.LateUpdate`/`CameraController.Awake`/`OnEnable`/`OnDisable` never fire there at all.
  Reflection-injects `ICameraService` directly into `CameraDriver`/`CameraController`'s private
  `_service` field (the same technique `Presentation.Tests.PresentationServiceRuntimeTests.SetId`
  already uses for `FeedbackDefinition`'s private `_id` field) rather than spinning up a real
  `GameBootstrapper` singleton per test. Covers a real `LateUpdate` applying position and damped
  orthographic size to a real `Camera` component, the driver correctly following the resolved active
  camera across an override push and pop (including the zero-duration-default immediate transition),
  and `CameraController`'s real `Awake`-computed initial pose/`SetTargetTransform`/`ClearTarget`/`Snap`.
- **A real bug this testing caught - not by a unit test, but by the first live Play Mode run of the
  Phase11Demo sample:** the gameplay camera's Z depth silently jumped from its authored `-10` to `0`
  the moment a target was assigned, because the controller's initial pose was seeded from the
  target's own position whenever one was present (falling back to the controller's own `transform`
  only when no target was assigned) - and the target (the Player) happened to sit at Z `0`. Since Z
  was correctly configured as a *fixed* (non-followed) axis, `FollowCameraMode` then never touched it
  again on any later tick, so the wrong seed value stuck permanently. This is exactly the class of bug
  CLAUDE.md's workflow (Step 4 — Validate, "test relevant functionality") exists to catch before
  calling a feature done: every one of the 66 EditMode/8 PlayMode unit tests already passed, because
  none of them happened to combine "a target is assigned" with "an axis is deliberately not followed"
  in the same case. Fixed by always seeding the initial pose from the controller's own
  `transform.position`/`transform.rotation`, never from an assigned target (see [Camera Framework](#camera-framework)'s
  "Initial pose" remarks for the corrected design, which also replaced the old externally-supplied-pose
  `Snap(CameraPose)` with a `Snap()` that re-derives from the live mode/target through one
  `Camera.ReduceMotion`-style tick instead, so the same class of hand-rolled-axis-logic bug cannot
  recur in the reset path either) - two regression tests
  (`CameraPoseControllerTests.Snap_StaticMode_HoldsCurrentPose_NeverJumpsToTarget` and the fixed-axis
  assertion inside `FollowCameraModeTests`/`CameraControllerRuntimeTests`) now cover this directly.
  67/67 Phase 11 EditMode tests and 8/8 PlayMode tests pass; the full project suite (684 EditMode + 84
  PlayMode tests project-wide) was re-run after this phase with zero regressions, and the
  Phase11Demo sample's Follow camera (dead zone/soft zone/world bounds/damped zoom, confirmed against
  the exact expected clamped values), Boss camera override push/pop, and Phase 10 camera-shake
  composing on top were additionally verified live in Play Mode - driven through direct service calls
  via the Unity MCP `execute_code` tool rather than simulated key presses, since Play Mode input
  simulation was not available through the tooling used for this verification pass.
- **Phase 11's Cinemachine integration.** `GameFramework.Cameras.Cinemachine.Tests` (EditMode) covers
  the one genuinely new piece of math this integration adds -
  `CinemachineBoundsTranslator.TryComputeBoxBounds` (null/disabled/degenerate-width/degenerate-height
  rect → `false`; a valid rect → the exact expected center/size). `GameFramework.Cameras.Cinemachine.Tests.Runtime`
  (PlayMode) covers what genuinely needs a live Cinemachine pipeline, using the same
  reflection-field-injection technique `CameraDriverRuntimeTests` already established (bypassing
  `GameBootstrapper` entirely): activation boosting/restoring `CinemachineVirtualCamera.Priority`
  across an active-camera switch, `CameraController.Target` mirroring onto Follow/LookAt,
  `LateUpdate` writing `CameraController.ComputePose`'s zoom half to the vcam's lens and converging
  toward a requested `SetZoom` value, and `Start()` generating a `BoxCollider2D` for an assigned
  `CinemachineConfiner2D` with no shape already authored, sized exactly from the configured bounds
  rect. 5/5 EditMode and 4/4 PlayMode tests pass; the full project suite (689 EditMode + 88 PlayMode
  tests project-wide) was re-run after this integration with zero regressions.
- **Live verification, Phase11CinemachineDemo sample.** Through the same `execute_code`-driven
  approach (no Play Mode key-press simulation available): activating the gameplay camera correctly
  boosted `CinemachineVirtualCamera.Priority` (base 10 → 1010) and mirrored the Player `Transform`
  onto Follow/LookAt; `ICameraService.PushOverride`/handle `Release` correctly re-mapped priorities
  in both directions and `CinemachineBrain.ActiveVirtualCamera` genuinely switched, with the real
  `Main Camera` GameObject's `transform.position` observed actually arriving at the event camera's
  authored vantage point `(20, 10, -10)` and blending back to `(0, 0, -10)` on release -
  `CinemachineBrain` itself performing the transform write, never this framework's code;
  `CameraController.SetZoom(9f)` was observed driving `Camera.orthographicSize` from its initial `5`
  to `~9` through the real `Camera` component (`m_Lens.OrthographicSize` → Cinemachine → the render
  `Camera`); `IPresentationService.Play` on the camera-shake `FeedbackDefinition` was observed
  registering on `CameraFeedbackDriver.ActiveShakeCount` (confirming the request reached the driver)
  and the transform returning to its exact pre-shake baseline once the shake's authored 0.35s
  duration had elapsed, confirming Phase 10's non-destructive offset composition works correctly on
  top of a Cinemachine-driven transform with the zero-code-change claim above; and `ResetCamera`
  returned `true`. Zero console errors/warnings across the whole verification session (three
  incidental "type is not a supported float value" warnings from the author's own ad hoc
  `SerializedProperty` inspection script during this pass, unrelated to any product code, are not
  counted here).
- **Phase 12.** `GameFramework.UI.Navigation.Tests.Runtime` (PlayMode-only, no EditMode variant -
  the same reason `GameFramework.UI.Tests` has none: test-double `UIScreen`/`UIPopup` subclasses are
  MonoBehaviours, and `AddComponent` rejects a script compiled only for the Editor platform). Covers
  registration (duplicate/invalid id, unknown screen/popup), push/replace/reset navigation (including
  parameter delivery to `IUINavigationParameterReceiver` before `OnOpened`, and `ScreenNavigatedEvent`
  publication), the full back-navigation priority chain (popup closes first, an
  `IUINavigationBackHandler` consuming the request without a stack change, stack pop with a delivered
  result, and `BackRequestedAtRootEvent` at the root), nested popup stacks (back closes the
  topmost first, leaving the one beneath it open), `NavigationRequestOptions.PausesGameplay` acquiring
  and releasing a `GameFlow.IPauseToken` via a fake `IGameFlowService`, `IsNavigating` correctly
  blocking a concurrent request while an `IUINavigationTransitionHandler` enter-transition coroutine
  is still playing and allowing one again once it completes, `INavigationGuard` Allow/Block/Defer
  (including that it runs for back navigation too), and `RegisterEventMapping<TEvent>`/
  `UnregisterEventMapping<TEvent>` (including parameter pass-through and duplicate-mapping rejection).
  41/41 Phase 12 tests pass; the full project suite (689 EditMode + 129 PlayMode tests project-wide)
  was re-run after this phase with zero regressions.
- **Live verification, Phase12Demo sample.** Through the same `execute_code`-driven approach as
  Phase 11 (no Play Mode key-press simulation available) - but this time invoking the sample's real
  `Button.onClick` handlers rather than calling `INavigationService` directly, since the point was to
  verify the actual click-driven UI flow: Main Menu's "Play" button pushed Character Selection;
  selecting "Warrior" pushed Car Selection carrying that parameter (confirmed via the receiving
  screen's private field); selecting "Sports Car" pushed Customization; Customization's "Done" button
  first popped back to Car Selection synchronously, then - after the one-frame-deferred coroutine that
  works around the reentrancy guard described in this section's "Concurrency policy" - popped Car
  Selection too, delivering `"SportsCar"` to Character Selection's original result callback (confirmed
  via the exact expected sequence of three `Debug.Log` lines). Separately: Main Menu's "Settings"
  button opened the Settings popup; its "Exit Game" button opened a nested Confirm popup on top of it;
  a first `NavigateBack()` closed only the Confirm popup, leaving Settings open and Main Menu
  untouched (`CurrentScreenId` never changed); a second closed Settings, returning `HasOpenPopups` to
  `false`. Separately again: calling `NavigateBack()` at the Main Menu root (no popups, single-screen
  stack) returned `NavigationResultKind.NotFound` and was observed to actually publish
  `BackRequestedAtRootEvent` (subscribed from the verification code itself). Zero console
  errors/warnings across the whole verification session.

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

## UI Navigation & Menu Flow Framework

Phase 12. One new assembly, `GameFramework.UI.Navigation`, referencing `GameFramework.Core`/
`GameFramework.Runtime`/`GameFramework.UI` (hard - it orchestrates Phase 3's screen/popup stack
directly) plus `GameFramework.Input`/`GameFramework.GameFlow` (soft, resolved via `registry.TryGet`)
and `GameFramework.PlayerSystems` (only so its own `NavigationBootstrapper` can subclass
`PlayerSystemsBootstrapper` - the same hard-dependency-bootstrapper pattern `Quests.QuestsBootstrapper`
already established over `ProgressionBootstrapper`). It is not a second UI framework - Phase 3's
`IUIService`/`UIScreen`/`UIPopup`/layered canvases are still the only thing that ever instantiates,
parents, shows, hides, or destroys a screen/popup GameObject; this layer only decides *when* and
*with what data*.

**Core principle.** Game code never touches `IUIService.OpenScreen`/`OpenPopup`/`CloseScreen`
directly once Navigation is registered - it expresses intent through stable ids instead:

```csharp
INavigationService navigation = GameBootstrapper.Instance.Services.Get<INavigationService>();

// Authored once, at composition-root time:
navigation.RegisterScreen(new UIScreenId("MainMenu"), mainMenuPrefab);
navigation.RegisterPopup(new UIPopupId("Settings"), settingsPopupPrefab);

// "Go to the character selection screen, remembering how to come back":
navigation.Navigate(new UIScreenId("CharacterSelection"));

// "Replace this screen, don't keep it in history":
navigation.Replace(new UIScreenId("Results"));

// "Start a new top-level flow; back should never return to the menu":
navigation.Reset(new UIScreenId("Gameplay"));

// "Go back" - implements the full popup -> screen-handler -> stack -> app priority chain itself:
navigation.NavigateBack();
```

**The one seam added to Phase 3.** `UIScreen`/`UIPopup`'s own lifecycle (`OnOpened`/`OnHidden`/
`OnShown`/`OnClosed`) had no way for a caller to inject data between instantiation and `OnOpened`
firing. `IUIService.OpenScreen<T>`/`OpenPopup<T>` gained one small, additive, backward-compatible
overload for this - `T OpenScreen<T>(T prefab, Action<T> onBeforeOpen = null)` - with `onBeforeOpen`
defaulting to null so every existing call site is unaffected. `NavigationService` is the only caller
that ever passes a non-null `onBeforeOpen`, using it to deliver `NavigationRequestOptions.Parameters`
to a screen/popup implementing `IUINavigationParameterReceiver` before its own `OnOpened` runs. This
is the "smallest architectural change" CLAUDE.md's Phase 12 brief (section 73) asks for when a
feature genuinely doesn't fit the existing seams - everything else Navigation needs is built on top
of `IUIService`'s existing public API.

**`INavigationService`** is the one registered entry point - the Phase 12 equivalent of
`GameFlow.IGameFlowService`/`Presentation.IPresentationService`/`Cameras.ICameraService`:

- `RegisterScreen`/`UnregisterScreen`/`IsScreenRegistered` and the popup equivalents map a stable
  `UIScreenId`/`UIPopupId` (the same string-backed-id shape as `GameFlow.LevelId`/`Presentation.FeedbackId`
  - never `GetInstanceID()`, a GameObject name, or a stack position) to a prefab. Registering a
  duplicate id throws `InvalidOperationException`; an invalid (empty) id throws `ArgumentException` -
  both authoring/programmer errors caught once at startup, exactly like `IPresentationService.RegisterDefinition`.
- `Navigate`/`Replace`/`Reset` push/replace/clear-and-root the screen stack (`NavigationMode`).
  `NavigateBack` pops it, implementing the priority chain below. Every command returns a
  `NavigationResult` (`Success`/`Blocked`/`Cancelled`/`Failed`/`AlreadyActive`/`NotFound`/`Deferred`)
  instead of throwing - the same "normal flow never needs a try/catch" policy `IGameFlowService`
  already established.
- `OpenPopup`/`CloseTopPopup` manage a separate popup stack layered on top of whatever screen is
  current. `CurrentScreen`/`CurrentScreenId`/`CurrentPopup`/`CurrentPopupId`/`CanNavigateBack`/
  `HasOpenPopups` are always-current read properties.
- `AddGuard`/`RemoveGuard` register an `INavigationGuard` (Allow/Block/Defer) evaluated, in
  registration order, before every screen/popup navigation request including back navigation.
- `RegisterEventMapping<TEvent>`/`UnregisterEventMapping<TEvent>` open a popup automatically when
  `TEvent` is published through the existing `IEventService` - the same explicit, strongly-typed
  mapping `IPresentationService.RegisterMapping<TEvent>` already established, applied here to
  GameFlow-driven (or any event-driven) UI, e.g. a game-over popup reacting to
  `GameFlow.GameplaySessionFailedEvent` without Navigation ever referencing Quests/Progression/
  anything else that might also want to publish "show a popup" events.
- `GetDiagnostics()` returns a `NavigationDiagnosticsSnapshot` (current screen, both stacks,
  `IsNavigating`, registered counts) - a read-only development diagnostic, the same shape as
  `Gameplay.Pooling.GameObjectPool.Statistics`, never something gameplay code should branch on.

**Back-navigation priority** (CLAUDE.md's Phase 12 brief, section 11) is centralized in
`NavigateBack` itself, never scattered per-screen:

1. An open popup closes first (`CloseTopPopup`-equivalent, result `UIPopupResult.Cancelled`).
2. Otherwise, if the current screen implements `IUINavigationBackHandler`, it gets first refusal -
   returning `true` consumes the request with no stack change (e.g. a screen with its own internal
   tabs/sub-panels that should close on the first back press).
3. Otherwise, if more than one screen is on the stack, it pops one level.
4. Otherwise (already at the root, nothing left to pop), `BackRequestedAtRootEvent` is published
   through `IEventService` and `NavigateBack` returns `NavigationResultKind.NotFound` - a game
   subscribes to decide what "back at the root" means (a confirm-exit popup, forwarding to
   `IGameFlowService`, or `Application.Quit()`); this framework never assumes one, and never calls
   `Application.Quit()` itself.

**Android/mobile back button** (section 12) is centralized the same way input is everywhere else in
this framework: `NavigationService` creates one internal `NavigationBackButtonDriver` (a plain
`Update()`, matching CLAUDE.md's Tick Rules for "anything simple, low-count"), the only place in the
whole framework that reads `Input.GetKeyDown(KeyCode.Escape)` - Unity's documented mapping for the
Android hardware/gesture back button as well as the desktop Escape key, giving zero-setup Android
back support. If `Input.IInputService` is registered, a logical action (default name `"Cancel"`, so
a game can also bind a gamepad B button to it) is honored too; either source calls `NavigateBack()`,
which implements the full chain above.

**Concurrency policy** (section 21): a navigation command issued while `IsNavigating` is true is
rejected outright (`NavigationResultKind.AlreadyActive`) - never queued, never silently cancelled.
`IsNavigating` stays true for the duration of an `IUINavigationTransitionHandler.PlayEnterTransition`
coroutine, if the newly-entered screen/popup implements one (section 22-23) - this is what blocks a
second navigation request from interrupting a transition mid-flight, and is also why a navigation
call made *synchronously from inside a lifecycle hook this service itself just triggered*
(`OnShown`/`OnOpened`/`OnClosed`/`OnHidden`, or an `IUINavigationBackHandler` callback) is rejected
the same way - `IsNavigating` is still true for the whole outer call. This is the same deliberate
re-entrancy guard `GameFlow.GameFlowService`/`Tutorials.TutorialService` already apply to a command
issued from inside one of their own event handlers; defer such a follow-up call by one frame (a
coroutine) instead - see the Phase12Demo sample's `CarSelectionScreen` for a worked example (selecting
a car, finishing customization, and returning the chosen car as a result to the screen two levels up
the stack).

`IUINavigationTransitionHandler` is deliberately **enter-only** - Phase 3's `UIScreen`/`UIPopup`
destroy their GameObject synchronously the instant `IUIService.CloseScreen`/`ClosePopup` is called,
with no seam for this layer to defer that destruction, so an "exit transition" hook could never
actually finish playing before the object it animates is destroyed. A screen wanting a guaranteed
pre-destroy exit animation plays it synchronously inside its own `OnClosed`/`OnHidden` override
(Phase 3's existing hooks) instead. Navigation never depends on a tweening library either way -
without a transition handler, a push/pop is instant, exactly as Phase 3 already behaves.

**Parameters and results** (sections 19-20) are deliberately untyped (`object`) at the
`NavigationRequestOptions`/`IUINavigationParameterReceiver` boundary rather than a generic
`INavigationService` surface - the call site is already type-safe (the caller's own typed local is
what gets boxed), and the receiver does one explicit cast for the type it expects. A screen/popup
that wants to hand a value back to whoever navigated to it supplies `ResultCallback` at push time;
that callback fires exactly once, whenever the pushed entry (or, via a game's own deliberate chained
`NavigateBack` calls, an entry further down the stack) is eventually popped/closed, receiving
whatever `object result` that specific pop/close call supplied.

**Contexts** (section 15) were deliberately *not* implemented as independently-persisted stacks per
flow (Main Menu vs. Gameplay vs. Results each keeping their own resumable history). Phase 3's
`UIScreen` destroys its GameObject on close, so there is no existing seam to keep a hidden context's
screens alive to return to exactly as left without either duplicating Phase 3's lifecycle or
silently losing screen state anyway - and CLAUDE.md's own guidance ("do not introduce contexts merely
for abstraction... use them when they solve real lifecycle/navigation problems") argues against
building a facade that doesn't actually deliver persistence. `Reset` covers the real use case
instead: moving between top-level flows (Main Menu -> Gameplay) where back should never return to
the previous one - the same outcome, achieved with the lifecycle Phase 3 already guarantees.

**Pause/GameFlow integration** (section 26): `NavigationRequestOptions.PausesGameplay` on a popup
open acquires a `GameFlow.IPauseToken` via `IGameFlowService.PauseGameplay` (a no-op, logged once,
if `IGameFlowService` isn't registered - the same soft-dependency degradation `IPresentationService`
already established for its own channels) and releases it automatically when the popup closes, by
any path (`CloseTopPopup`, back navigation, or `Shutdown`). Navigation never touches
`Runtime.Time.ITimeService`/`Time.timeScale` directly.

**Known limitations / non-goals** (mirrors every other phase's "what this deliberately does not do"):
no visual transition/tweening engine (enter-transition hook only, see above); no persisted
navigation state (section 40 - a navigation stack, open popup, or screen parameter is never written
to `Runtime.Persistence.IPersistenceService`; Phase 13 owns save profiles); no generic workflow/queue
engine behind `NavigationGuardResult.Defer` (the guard itself is responsible for retrying); no focus/
gamepad-navigation system beyond what Phase 3's own `EventSystem` already provides (this framework
targets touch-first mobile UI, per CLAUDE.md's Phase 12 brief, section 24); and no second scene-loading
system - `Runtime.SceneManagement.ISceneService` already exists and is untouched, and Navigation's own
state naturally survives a scene load for free since `UIService`'s canvas root (and therefore every
screen/popup instantiated under it) is already `DontDestroyOnLoad`.

**Sample.** `Assets/GameFramework/Samples/Phase12Demo/` demonstrates: `Reset` establishing a Main
Menu root; `Navigate` with parameters through a Character Selection -> Car Selection -> Customization
chain; a chained multi-level `NavigateBack` returning a typed result two levels up the stack (with
the reentrancy sharp edge above worked around via a one-frame-deferred coroutine); a modal Settings
popup opening a nested Confirm popup, with back-navigation closing the topmost popup first and
leaving the one beneath it open; and `BackRequestedAtRootEvent` firing once nothing is left to pop.
Live-verified in Play Mode by invoking the sample's real `Button.onClick` handlers (not raw
`INavigationService` calls) through the Unity MCP `execute_code` tool, since Play Mode key-press
simulation is not available through that tooling (the same limitation Phase 11's own live
verification notes) - every step above was observed to produce the expected `CurrentScreenId`/
`CurrentPopupId`/`CanNavigateBack` state and console log after each click.

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
A general-purpose Tick Scheduler/Job System/ECS update graph was also deliberately not built in
Phase 4 — Phase 5's `ITickService` (see [Tick System](#tick-system)) is that general-purpose primitive,
kept forward-compatible with `IGameplayService`'s three session-scoped tick interfaces rather than
replacing them; the two coexist by design (see [Tick System](#tick-system)'s remarks on when to use
which).

Phase 5 deliberately does **not** include: a custom ECS (Entity World/Archetypes/Component
Storage/Systems World/Scheduler), a custom memory allocator (arena/slab/custom GC), a custom
resource pipeline replacing Addressables/AssetBundles, a universal thermal-management system (no
reliable cross-platform API exists for one), or an automatic device-to-quality-profile heuristic
(`IMobilePerformanceService.ApplyProfile` is the hook; picking *which* profile is a game decision).
None of Phases 0–4's public APIs were changed to make room for it — every addition is either new
(the `GameFramework.Performance` assembly) or purely additive to an existing one (`GameObjectPool`'s
`Statistics`/hardening, two new asmdef references, both one-way).

Phase 6 deliberately does **not** include: a full IAP SDK integration, cloud save, backend/
server-authoritative economy, an achievements platform, a quest system, a battle pass/season
system, a marketplace/trading/player-to-player transfer system, or an analytics SDK — `IReward`/
the result types (`SpendResult`, `UnlockResult`, `RewardClaimResult`, ...) are the seams an external
purchase-validation or analytics adapter would hook into later, not something this phase builds
itself. No concrete currency, item, level curve, unlock, or reward exists for any specific game —
[Sample](#sample) exists for validation/documentation only and is not part of the reusable
framework. Planned next:

- **Phase 7** — Objectives, Quests, Achievements & Milestones. Done — see
  [Objectives, Quests, Achievements & Milestones](#objectives-quests-achievements--milestones).
  Explicitly out of scope and left for later: daily/weekly quest scheduling, seasons/battle pass,
  cloud sync/backend validation, remote config, live ops, analytics SDK integration, advanced
  achievement UI, and quest chains — `ICondition`/`IRewardService`/`IUnlockService` are the seams a
  later phase would extend, not something this phase builds itself.
- **Phase 8** — Game Flow, Gameplay Sessions & State Orchestration. Done — see
  [Game Flow, Sessions & Checkpoints](#game-flow-sessions--checkpoints). Explicitly out of scope
  and left for later: a visual state-machine editor, a development inspector/debug HUD for the
  current flow state (`PerformanceOverlay`'s pattern would be the natural template, but nothing
  measured this as a real need yet), a level-graph/next-level-sequencing system (the framework
  deliberately never assumes levels are linear — a game calls `LoadLevel` with whatever it decides
  is next), advanced cutscene integration, a multiplayer/online-match session layer,
  server-authoritative sessions, cloud resume, an advanced replay system, and tournament/season
  flow — `IGameFlowService`/`GameplaySession`/`CheckpointSystem` are the seams a later phase would
  extend, not something this phase builds itself.
- **Phase 9** — Tutorial & Onboarding Framework. Done — see
  [Tutorial & Onboarding Framework](#tutorial--onboarding-framework). Explicitly out of scope and
  left for later (see that section's own "Non-goals"): a visual tutorial/node-graph editor, a
  hand/pointer animation system, a tweening framework, a dialogue/voice-over/cutscene framework, an
  analytics/ads/IAP SDK, remote config, multiplayer tutorials, and AI-driven tutorials —
  `ITutorialCondition`/`IEventService`/`ITutorialService.GetDefinition` are the seams a game's own
  presentation layer or a later phase would extend, not something this phase builds itself.
- **Phase 10** — Game Feel, Feedback & Presentation. Done — see
  [Game Feel, Feedback & Presentation](#game-feel-feedback--presentation). Explicitly out of scope
  and left for later (see that section's own "Non-goals"): a full camera framework/Cinemachine
  replacement (Phase 11's job, done - see below), a full post-processing or VFX/particle-system
  framework, a shader framework, a tweening/animation framework, a dialogue/cutscene system, and an
  analytics/ads/IAP SDK - `ICameraFeedbackDriver`/`IEventService`/`FeedbackDefinition`'s own
  extensibility are the seams a game's own presentation layer or a later phase would extend, not
  something this phase builds itself.
- **Phase 11** — Camera Framework, with an optional Cinemachine integration. Done — see
  [Camera Framework](#camera-framework) and [Cinemachine Integration](#cinemachine-integration) (the
  latter added once Cinemachine was installed in this project, after the pure-C# pipeline already
  existed). Explicitly out of scope and left for later (see both sections' own "Non-goals"/"Known
  limitations"): a custom camera solver/blend engine (Cinemachine already provides one, now
  integrated), cinematic director/cutscene framework, Timeline replacement, physical multi-camera
  rendering/switching (culling masks/`AudioListener`/post-processing per camera), full split-screen
  framework, minimap framework, photo mode, replay camera system, per-channel (position/rotation/zoom)
  transition durations for the non-Cinemachine path, `ICameraTargetGroup`/`CinemachineTargetGroup`
  wiring, and a custom blend-authoring UI - `ICameraMode`/`ICameraTarget`/`CameraTransitionSettings`
  (non-Cinemachine path) and `CinemachineVirtualCamera`'s own Body/Aim components plus
  `CinemachineBrain.m_CustomBlends` (Cinemachine path) are the seams a game's own camera rig or a
  later phase would extend, not something this phase builds itself. No concrete camera content (a
  specific game's authored `CameraConfiguration`/Cinemachine values, boss-fight sequencing, cutscene
  cameras) exists for any specific game - [Sample](#camera-framework) exists for validation/
  documentation only.

- **Phase 12** — UI Navigation & Menu Flow Framework. Done — see
  [UI Navigation & Menu Flow Framework](#ui-navigation--menu-flow-framework). Explicitly out of
  scope and left for later (see that section's own "Known limitations / non-goals"): a visual
  transition/tweening engine, persisted navigation state, a generic workflow/queue engine behind
  guard deferral, a gamepad-focus navigation system beyond Phase 3's own `EventSystem`, and a second
  scene-loading system — `IUINavigationTransitionHandler`/`INavigationGuard`/`RegisterEventMapping`
  are the seams a game's own presentation layer or a later phase would extend, not something this
  phase builds itself.

Candidate next phases, based on the actual architecture after Phase 12 (none committed to yet):
a save-slot/profile layer on top of Phase 2's `IPersistenceService` (multiple named save slots, not
just one envelope per key, and the natural place to persist "last screen"/"tutorial completed"/etc.
if a game ever wants that); or a first concrete game built on top of everything through Phase 12,
which would likely surface real integration gaps (e.g. an actual GameFlow<->Navigation bridge beyond
plain event mappings, a concrete need for `NavigationGuardResult.Defer` retry semantics, or a genuine
need for gamepad/keyboard UI focus navigation) faster than a thirteenth infrastructure-only phase
would.
