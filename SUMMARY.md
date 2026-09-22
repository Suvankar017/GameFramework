# Project Summary — GameFramework

A reusable Unity framework intended to be shared across multiple mobile games, providing the
foundational services (bootstrap, persistence, UI, audio, input, localization, feedback, gameplay
infrastructure, performance, progression/economy, quests/achievements, and game-flow orchestration)
that most games need, without any game-specific content.

## Status

**Phase 14 — Mobile Platform & Device Services Framework**, on top of:

- **Phase 0** — core utilities (validation, extensions).
- **Phase 1** — Bootstrap, Services, Logging, GameState, SceneManagement.
- **Phase 2** — infrastructure layer: Time, Timers, Events, Persistence, Settings.
- **Phase 3** — five player-facing systems: Input, Localization, Audio, UI Foundation,
  Feedback/Haptics.
- **Phase 4** — generic gameplay infrastructure: Gameplay Loop, Entity/Component Utilities, Object
  Lifecycle, Spawning, Pooling, Commands, Interaction/Targeting, Objectives/Checkpoints.
- **Phase 5** — cross-cutting performance layer: profiling/frame diagnostics, a centralized tick
  system, pooling hardening, a lightweight resource-loading abstraction, mobile performance
  utilities, memory diagnostics, and configurable performance budgets.
- **Phase 6** — player-progression content systems: Economy (multi-currency), Inventory (item
  ownership/quantities), Experience (levels/XP), Unlocks (composable requirement-gated content),
  Rewards (idempotent, transaction-safe grant orchestration).
- **Phase 7** — Statistics, a composable Condition system, condition-backed Objectives (extending
  Phase 4's objective state machine), Quests, Achievements, and threshold Milestones — all claiming
  rewards through Phase 6's existing idempotent `IRewardService`.
- **Phase 8** — the reusable game-flow/orchestration layer: one state machine
  (`LevelFlowState`) covering level-load lifecycle and gameplay-session state together, a generic
  `GameplaySession` (attempt tracking, elapsed time, result), session-scoped checkpoints/respawn,
  reference-counted pause with labeled tokens (built entirely on Phase 2's `ITimeService`), and
  restart/retry flows — one registered `IGameFlowService` that never references Progression/
  Unlocks/Rewards/Quests directly, only ever notifying them via events.
- **Phase 9** — a reusable tutorial/onboarding execution layer: a lifecycle-managed
  `ITutorialService` (one active tutorial at a time), a minimal sequential step model
  (Instruction/Wait/Input/Event/Condition), prerequisites/repeat/skip/persistence policies, and
  optional gameplay pause/input-context gating — all built on Phase 2's `ITimeService`/
  `IEventService`/`IPersistenceService` and Phase 3's `IInputService` only; it never references
  GameFlow/Gameplay/Progression/Unlocks/Rewards/Quests, so it stays usable by a game that has none
  of those systems.
- **Phase 10** — a coordinated game-feel/presentation orchestration layer: one registered
  `IPresentationService` gameplay code calls (`Play(feedbackId, ...)`) to dispatch a
  `FeedbackDefinition`'s enabled channels (Audio/Haptics/Camera/Visual/Screen/UI/Time) without
  knowing how any one is implemented — it orchestrates Phase 3's `IAudioService`/`IFeedbackService`
  and Phase 4/5's pooling/UI layer roots, never duplicates them; every one of those dependencies is
  resolved softly, so it degrades channel by channel rather than requiring all of them registered.
- **Phase 11** — a reusable camera orchestration layer: one registered `ICameraService` owns camera
  registration/selection (base activation plus a temporary override stack) while a pure,
  Unity-lifecycle-free pipeline (`ICameraMode` → zoom → world bounds) computes each registered
  `CameraController`'s pose; exactly one `CameraDriver` per scene applies whichever camera is
  currently active to the real `UnityEngine.Camera`. Built-in modes cover Follow (dead zone/soft
  zone, per-axis lock, damping), Static, TargetLook, and Manual (external pose intent with no direct
  `Camera`/`Transform` access). Composes with Phase 10's existing `Presentation.CameraFeedbackDriver`
  through transform-write ordering alone (`[DefaultExecutionOrder(-100)]`/`[DefaultExecutionOrder(100)]`),
  never a compile-time reference in either direction.
  - **Cinemachine integration** (`GameFramework.Cameras.Cinemachine`, new sibling assembly, added
    once Cinemachine 2.10.7 was installed in this project) — an *alternative* driver for the exact
    same `ICameraService`/`CameraController` orchestration: `CinemachineCameraAdapter` +
    `CinemachineCameraBackend` map GameFramework's activation/target/zoom decisions onto a real
    `CinemachineVirtualCamera`/`CinemachineBrain`, letting Cinemachine perform the actual follow,
    framing/dead-zone, confinement, and blending instead of `CameraDriver`'s own pure-C# pipeline -
    see Framework.md's "Cinemachine Integration" section for the full design and which of the two
    drivers owns what.
- **Phase 12** — a reusable UI navigation/menu-flow orchestration layer on top of Phase 3's UI
  Foundation: one registered `INavigationService` owns stable-id screen/popup registration, a
  navigation stack independent of Unity's own scene history (`Navigate`/`Replace`/`Reset`/
  `NavigateBack`), a centralized back-navigation priority chain (open popup → a screen's own
  `IUINavigationBackHandler` → the stack → `BackRequestedAtRootEvent` for the game/GameFlow/app to
  decide), typed parameters/results delivered through the one small additive seam added to
  `IUIService.OpenScreen`/`OpenPopup` (an optional `onBeforeOpen` callback), `INavigationGuard`s,
  optional enter-transition coroutine hooks, and centralized Android/back-button routing. It never
  duplicates Phase 3's screen/popup instantiation, destruction, or layered-canvas machinery - every
  screen/popup it manages is still opened/closed exclusively through `IUIService`.
- **Phase 13** — a player-profile and player-data orchestration layer on top of Phase 2's
  `IPersistenceService`: one registered `IPlayerProfileService` owns profile identity/lifecycle
  (create/load/unload/switch/delete, a convenience default profile), modular `PlayerDataSection<TData>`
  data sections a game registers itself (dirty-tracked, independently versioned/migratable, each its
  own already-existing `IPersistenceService` key), autosave composed from
  `ApplicationPause`/`FocusLost`/`SceneTransition`/`ProfileUnload`/debounced-dirty triggers (built on
  Phase 2's `ITimerService`, not a new update loop), and corruption/backup recovery (a `.bak`
  companion key per section, restored automatically if the primary fails to load) - all without
  introducing a second storage/serialization system. References only Core/Runtime, so it stays usable
  by any game regardless of which other phases it also uses; does not retrofit Phase 3/6/9's six
  existing self-persisting systems (Settings/Economy/Inventory/Experience/Statistics/Tutorials) onto
  profile-scoped keys — see `Framework.md`'s Phase 13 section for that known limitation.
- **Phase 14** — a platform/device services layer, Core/Runtime only: one registered
  `IPlatformService` (platform identity), `IDeviceInfoService` (cached device snapshot + capability
  queries), `IScreenService` (safe area/orientation, polled since Unity has no change callback for
  either), `IClipboardService`, `IPlatformUrlService`, `IAppStoreService` (game-supplied store
  identifiers), `INetworkReachabilityService`, `IPermissionService` (Camera/Microphone only — the two
  permissions Unity's own `Application.HasUserAuthorization` genuinely supports cross-platform), and
  `IAppSettingsService` (Android only, via the framework's one isolated `AndroidJavaObject`
  boundary). Deliberately does not re-implement application lifecycle (already
  `Performance.Mobile.IApplicationLifecycleService`) or haptics (already `Feedback.IHapticProvider`) —
  see `Framework.md`'s Phase 14 section for the full non-goal list.

No player character, enemy AI, weapons, ads, analytics, IAP, remote config, or multiplayer exists
yet, and no concrete currency/item/level/unlock/reward/quest/achievement/tutorial-content/feedback-
content is defined for any specific game — those consume this infrastructure, they don't live in
it. See `Assets/GameFramework/Documentation/Framework.md`'s Roadmap section for what each phase explicitly
left out and the seams a later phase would extend.

## Target Environment

- Unity **2022.3.62f3**, C# 9.0-compatible syntax only (no newer .NET/C# language features or
  post-2022.3 Unity APIs).
- Mobile-first (Android/iOS).
- Render pipeline: Universal RP (URP) 14.0.12.
- Input: Active Input Handling is set to **Both** — the framework's `GameFramework.Input` system
  supports the legacy Input Manager and the New Input System (keyboard/mouse/gamepad, via
  `com.unity.inputsystem`) side by side, per action binding.
- Notable BCL gotcha documented in-repo: the 3-argument `File.Move(src, dst, overwrite: true)`
  does not compile in this project despite the API compatibility level being .NET Standard 2.1 —
  Unity's reference assemblies are a curated subset, not the full BCL.

## Architecture

Strict one-way dependency direction (a lower layer never references a higher one):

```text
Game / Game Features
        ↓                    ↓                    ↓                  ↓
GameFramework.PlayerSystems  GameFramework.Gameplay  GameFramework.Quests  GameFramework.GameFlow
(registers Input/Loc/         (Loop, Entities,        (Conditions,          (LevelFlowState,
 Audio/UI/Feedback)            Lifecycle, Spawning,     Objectives, Quests,   GameplaySession,
        ↓                      Pooling, Commands,       Achievements,         Checkpoints, Pause)
GameFramework.UI →              Interaction,            Milestones)                ↓
 Localization, Audio,           Objectives)                  ↓             (references only
 Feedback                          ↓                  GameFramework.Rewards  Core/Runtime/Gameplay;
        ↓                          │                  GameFramework.Unlocks  soft-depends on
GameFramework.Input                │                  GameFramework.Progression  IGameplayService)
        ↓                          ↓                          ↓
GameFramework.Runtime   (Bootstrap, Services, Diagnostics, State, SceneManagement,
        ↓                Time, Timers, Events, Persistence, Settings)
GameFramework.Core       (Validation, Extensions — zero dependencies)
        ↓
Unity / .NET
```

`GameFramework.Gameplay` is a compile-time-enforced **sibling** of `GameFramework.PlayerSystems` —
it references only Core/Runtime/Performance, never Input/UI/Audio/Feedback, so Gameplay
Infrastructure stays independent of Player Experience. `GameFramework.Performance` (Phase 5) is a
third sibling, referencing only Core/Runtime, so it stays usable regardless of which of the other
two a game also uses; `GameFramework.Gameplay`'s one-way reference *on* it (for pooling's profiling
markers/statistics) is the only new cross-assembly edge Phase 5 added. `GameFramework.Progression`
(Phase 6, housing Economy/Inventory/Experience, plus Phase 7's Statistics namespace) is a fourth
sibling, also Core/Runtime only; `GameFramework.Unlocks` sits above it (requirement types reference
Progression's read APIs) and `GameFramework.Rewards` sits above both (orchestrates all four) — a
real one-way chain, the same reasoning Phase 3 used to split UI from Localization/Audio/Feedback.
`GameFramework.Quests` (Phase 7) sits above Rewards/Unlocks/Progression *and* references
`GameFramework.Gameplay` (for `IObjective`/`ObjectiveBase`) — the one assembly in the framework that
legitimately spans both chains, since nothing downstream needs to. `GameFramework.GameFlow`
(Phase 8) is a fifth sibling of PlayerSystems/Gameplay/Performance/Progression — it references only
Core/Runtime/Gameplay (a soft `TryGet` dependency on `IGameplayService`, never a hard one) and
deliberately never references Progression/Unlocks/Rewards/Quests at all; completion/failure/restart
are only ever published as events for those systems (or a game's own code) to react to.
`GameFramework.Tutorials` (Phase 9) is a sixth sibling — it references only Core/Runtime/Input,
deliberately not GameFlow/Gameplay/Performance/Progression/Unlocks/Rewards/Quests/UI/Localization/
Audio/Feedback, so tutorial infrastructure stays usable by a game with none of the framework's other
higher-level systems; it reuses `IInputService`'s existing context stack for input gating and
`ITimeService`'s existing `Pause`/`Resume` for optional gameplay pause, with no new abstraction for
either.
`GameFramework.Presentation` (Phase 10) breaks the "minimal sibling" pattern deliberately: it
references `GameFramework.Core`/`GameFramework.Runtime`/`GameFramework.Audio`/`GameFramework.Feedback`/
`GameFramework.Gameplay`/`GameFramework.Performance`/`GameFramework.UI` — a genuine orchestration
layer over several existing lower ones, not a new cycle (none of those seven reference each other in
a loop, and nothing in them ever references Presentation). Every one of those references is still
resolved *softly* at runtime (`registry.TryGet`), so a game can register Presentation alone and it
still works, just with each channel missing its backing service logging one warning instead of
executing. It deliberately never references GameFlow/Tutorials/Progression/Unlocks/Rewards/Quests/
Localization/Input at all, and is explicitly not a replacement for `GameFramework.Feedback`'s
existing haptics+audio-preset coordination — it orchestrates it.
`GameFramework.Cameras` (Phase 11) is a seventh sibling of PlayerSystems/Gameplay/Performance/
Progression/GameFlow/Tutorials — it references only Core/Runtime/Performance (the last one for
`ProfileScope`/`ProfilingCategory.Cameras` only, the same reason `GameFramework.Gameplay` references
`GameFramework.Performance`). It deliberately has **no** reference to `GameFramework.Presentation`
in either direction: Phase 10's `Presentation.CameraFeedbackDriver` already composes with whatever
wrote a camera's transform earlier in the frame (subtract-previous-offset-then-add-new, see that
class's remarks), so `Cameras.CameraDriver` only needs to run *before* it — guaranteed by
`[DefaultExecutionOrder(-100)]`, a Unity script-ordering primitive, not a compile-time dependency.
This is the same "base pose vs. feedback layered on top, zero coupling" seam Phase 10 already
designed for, just exercised from the other side for the first time.
`GameFramework.Cameras.Cinemachine` is an **optional eighth assembly**, added after Cinemachine
2.10.7 was installed in this project - it references `GameFramework.Cameras` (one-way) plus the
`Cinemachine` package, and is the *only* place in the whole framework that references Cinemachine
at all. `GameFramework.Cameras` itself was not changed to depend on Cinemachine - it stays usable
exactly as before for a game that doesn't have the package installed. `CameraFeedbackDriver` picked
up one small, backward-compatible change for this integration: `[DefaultExecutionOrder(100)]`, so it
also runs after `CinemachineBrain`'s own (default-order) transform write, not only after
`CameraDriver`'s - it still needs no reference to Cinemachine or to the new assembly to do so.
`GameFramework.UI.Navigation` (Phase 12) breaks the minimal-sibling pattern deliberately, the same
way Presentation did: it references `GameFramework.UI` as a genuine **hard** dependency (it
orchestrates Phase 3's screen/popup stack directly via `IUIService.OpenScreen`/`OpenPopup`/
`CloseScreen`/`ClosePopup`, never merely soft-looks-up an optional channel), plus
`GameFramework.Input`/`GameFramework.GameFlow` as *soft* runtime dependencies (`registry.TryGet`,
for Android back-button routing and popup pause-token acquisition respectively) and
`GameFramework.PlayerSystems` so its own `NavigationBootstrapper` can subclass
`PlayerSystemsBootstrapper` directly - the same reasoning `Quests.QuestsBootstrapper` already
established for its own hard dependency on Rewards. The only change to an existing assembly this
phase makes is additive/backward-compatible: `UI.IUIService.OpenScreen<T>`/`OpenPopup<T>` gained an
optional `Action<T> onBeforeOpen = null` parameter (default null, every existing call site
unaffected) - the one seam Navigation needed to deliver typed parameters to a screen/popup before its
own `OnOpened` lifecycle hook fires.

`GameFramework.Platform` (Phase 14) is a ninth sibling of PlayerSystems/Gameplay/Performance/
Progression/GameFlow/Tutorials/Cameras — it references only Core/Runtime, deliberately not
Performance (its `DeviceInfoService` independently queries `SystemInfo` rather than depending on
`Performance.Mobile.DeviceInfo`, the same "usable by any game regardless of which other systems it
also uses" independence Phase 5/Phase 13 already established) and not Feedback (it does not
re-implement haptics; `DeviceInfoService.Supports(DeviceCapability.Haptics)` mirrors
`MobileHapticProvider.IsSupported`'s check rather than depending on `GameFramework.Feedback` to reuse
it). Its `PlatformBootstrapper` is a `GameBootstrapper` subclass, not a `PlayerSystemsBootstrapper`
subclass — nothing it registers has a hard dependency on Input/UI.

Everything is wired together by `GameBootstrapper`, which registers and initializes services in a
load-bearing order: Logging → GameState → Scene → Time → Timer → Event → Persistence → Settings,
then (via `PlayerSystemsBootstrapper`) Input → Localization → Audio → UI → Feedback, (via
`GameplayBootstrapper`, or a game's own combined subclass) `IGameplayService` → `IPoolService`, (via
`PerformanceBootstrapper`) `ITickService` → `IPerformanceMonitorService` →
`IApplicationLifecycleService` → `IMobilePerformanceService`, (via `ProgressionBootstrapper`)
`IEconomyService` → `IInventoryService` → `IExperienceService` → `IUnlockService` →
`IRewardService`, (via `QuestsBootstrapper`, which extends `ProgressionBootstrapper` directly since
it has a real compile-time dependency on Rewards) `IStatisticsService` → `IQuestService` →
`IAchievementService` → `IMilestoneService`, (via `GameFlowBootstrapper`, or a game's own
combined subclass, registered after `IGameplayService` so its soft lookup succeeds)
`IGameFlowService`, (via `TutorialBootstrapper`, or a game's own combined subclass, registered
after `IInputService` so its soft lookup succeeds) `ITutorialService`, (via
`PresentationBootstrapper`, or a game's own combined subclass, registered after whichever of Audio/
Feedback/Gameplay/Performance/UI are wanted so its soft lookups succeed) `IPresentationService`,
(via `CameraBootstrapper`, or a game's own combined subclass) `ICameraService`, and (via
`NavigationBootstrapper`, which extends `PlayerSystemsBootstrapper` directly since it has a real
compile-time dependency on UI, or a game's own combined subclass, registered after `IInputService`/
`IGameFlowService` so their soft lookups succeed) `INavigationService`, and (via `PlatformBootstrapper`, or a game's own combined subclass)
`IPlatformService` → `IDeviceInfoService` → `IScreenService` → `IClipboardService` →
`IPlatformUrlService` → `IAppStoreService` → `INetworkReachabilityService` → `IPermissionService` →
`IAppSettingsService`.

## Assemblies

| Assembly | Purpose |
|---|---|
| `GameFramework.Core` | Zero-dependency utilities: `Guard` validation, Unity object/component/transform extensions. |
| `GameFramework.Runtime` | Bootstrap, service registry, logging, game state, scene management, time, timers, events, persistence, settings. |
| `GameFramework.Input` | Physical→logical input mapping, contexts, pointer/touch, gestures. |
| `GameFramework.Localization` | Language tables, lookup/fallback, per-language fonts/assets. |
| `GameFramework.Audio` | Pooled `AudioSource` playback, categories/volumes, cues, music crossfade. |
| `GameFramework.Feedback` | Haptics abstraction + presets that coordinate haptics with an audio cue. |
| `GameFramework.UI` | Layered canvases, screen stack, popups/modals, localized/audio/haptic UI components. |
| `GameFramework.PlayerSystems` | Composition root (`PlayerSystemsBootstrapper`) wiring the five Phase 3 services in. |
| `GameFramework.Gameplay` | Gameplay loop, entity/component utilities, object lifecycle, spawning, pooling, commands, interaction/targeting, objectives/checkpoints. Composition root: `GameplayBootstrapper`. Sibling of `PlayerSystems` (Core/Runtime/Performance). |
| `GameFramework.Performance` | Profiling markers/frame diagnostics, centralized tick system, memory diagnostics, resource-loading abstraction, mobile performance utilities, performance budgets. Composition root: `PerformanceBootstrapper`. Sibling of `PlayerSystems`/`Gameplay` (Core/Runtime only). |
| `GameFramework.Progression` | Economy (multi-currency balances), Inventory (item ownership/quantities), Experience (levels/XP curves), Statistics (Phase 7 — generic named counters). Core/Runtime only. |
| `GameFramework.Unlocks` | Composable `IUnlockRequirement` (Level/Currency/Item/Prerequisite, AND/OR) + `IUnlockService`. References `GameFramework.Progression`. |
| `GameFramework.Rewards` | `IReward` (Currency/Item/Experience/Unlock/Bundle) + `IRewardService` (idempotent claim). Composition root: `ProgressionBootstrapper`. References `GameFramework.Progression` + `.Unlocks`. |
| `GameFramework.Quests` | Conditions (composable, statistic-driven), condition-backed Objectives, Quests, Achievements, threshold Milestones (Phase 7). Composition root: `QuestsBootstrapper` (extends `ProgressionBootstrapper`). References Progression/Unlocks/Rewards/Gameplay. |
| `GameFramework.GameFlow` | Level-load/gameplay-session state machine (`LevelFlowState`), `GameplaySession`, session-scoped checkpoints/respawn, reference-counted pause tokens, restart/retry (Phase 8). Composition root: `GameFlowBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression` — references only Core/Runtime/Gameplay. |
| `GameFramework.Tutorials` | Tutorial lifecycle/state machine, sequential steps (Instruction/Wait/Input/Event/Condition), prerequisites/repeat/skip/persistence policies, optional gameplay pause and input-context gating (Phase 9). Composition root: `TutorialBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow` — references only Core/Runtime/Input. |
| `GameFramework.Presentation` | Coordinated feedback/presentation orchestration: `FeedbackDefinition` bundles Audio/Haptic/Camera/Visual/Screen/UI/Time channels behind one `IPresentationService.Play` call (Phase 10). Composition root: `PresentationBootstrapper`. References Audio/Feedback/Gameplay/Performance/UI, every one resolved softly at runtime. |
| `GameFramework.Cameras` | Camera orchestration: `ICameraService` (registration, base activation, override stack), `ICameraMode` (Follow/Static/TargetLook/Manual), world bounds, damped zoom, transitions (Phase 11). Composition root: `CameraBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow`/`Tutorials` — references only Core/Runtime/Performance; composes with `GameFramework.Presentation`'s camera feedback through script execution order only, no assembly reference either way. |
| `GameFramework.Cameras.Cinemachine` | Optional alternative driver for the same `ICameraService`/`CameraController` orchestration, backed by a real Cinemachine virtual camera instead of `CameraDriver`'s pure-C# pipeline (Phase 11, added once Cinemachine was installed). `CinemachineCameraAdapter` + `CinemachineCameraBackend`; no composition root/service of its own — plain scene composition. References `GameFramework.Cameras` + `Cinemachine` only; the only assembly in the framework that references Cinemachine. |
| `GameFramework.UI.Navigation` | UI navigation/menu-flow orchestration on top of `IUIService`: stable-id screen/popup registration, a navigation stack independent of Unity's scene history (`Navigate`/`Replace`/`Reset`/`NavigateBack`), back-navigation priority, typed parameters/results, guards, event-driven popups, and centralized Android/back-button routing (Phase 12). Composition root: `NavigationBootstrapper` (extends `PlayerSystemsBootstrapper`). References `GameFramework.UI` (hard) + `GameFramework.Input`/`GameFramework.GameFlow` (soft, `registry.TryGet`). |
| `GameFramework.PlayerData` | Player-profile/player-data orchestration on top of `IPersistenceService`: profile identity/lifecycle, `PlayerDataSection<TData>` data sections, autosave, corruption/backup recovery (Phase 13). Composition root: `PlayerDataBootstrapper`. Sibling — references only Core/Runtime. |
| `GameFramework.Platform` | Platform identity, device information/capabilities, screen/orientation/safe-area, clipboard, URL opening, app-store linking, network reachability, and Camera/Microphone permissions (Phase 14). Composition root: `PlatformBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow`/`Tutorials`/`Cameras`/`PlayerData` — references only Core/Runtime; deliberately does not re-implement Phase 5's application lifecycle or Phase 3's haptics. |
| `GameFramework.Editor` | Editor-only; localization table validation, Gameplay config validation, Quest content validation, Tutorial content validation, Feedback content validation, Camera Configuration validation, UI Navigation Catalog validation, Player Data diagnostics, and Platform diagnostics menu items. |
| `GameFramework.Cameras.Cinemachine.Editor` | Editor-only; validates a scene's Cinemachine-backed cameras (missing Brain/Controller/Virtual Camera/Backend references, duplicate controller ownership, an assigned Confiner with nothing to confine against). Separate from `GameFramework.Editor` specifically so that assembly stays Cinemachine-free. |
| Matching `*.Tests` / `*.Tests.Runtime` assemblies | EditMode/PlayMode test coverage per system (see Testing below). |

Full per-assembly reference tables and namespace listings live in
`Assets/GameFramework/Documentation/Framework.md`.

## Core Systems (brief)

- **Bootstrap** — `GameBootstrapper`: `Created → Initializing → Ready → ShuttingDown → Shutdown`,
  singleton, `DontDestroyOnLoad`, drives per-frame `IUpdatableService.Tick()`.
- **Services** — `IServiceRegistry`: simple `Type → instance` lookup, no reflection/scene search.
- **Time** — `ITimeService` wraps `UnityEngine.Time` with explicit scaled/unscaled naming;
  reference-counted `Pause`/`Resume`.
- **Timers** — `ITimerService` (one-shot, delay, repeating, countdown), owner-lifetime-aware,
  zero-allocation `Tick()`.
- **Events** — `IEventService`: strongly-typed pub/sub, allocation-free steady-state publish via
  `ArrayPool`.
- **Persistence** — `IPersistenceService` → `IPersistenceSerializer` (JSON via `JsonUtility`) →
  `IPersistenceStorage` (file or in-memory); versioned envelopes with migration chains; atomic-ish
  writes.
- **Settings** — built entirely on Persistence + Events; typed `Get`/`Set`, dirty-flag save policy.
- **Input** — physical → mapped logical actions, a context stack for gating input, gesture/buffer
  helpers kept outside the core service as opt-in utilities. Each action can bind legacy Input
  Manager sources, New Input System sources (keyboard/mouse/gamepad), or both — whichever is
  physically active satisfies the action; axis/vector2 actions combine legacy and gamepad values by
  taking whichever has the larger magnitude each frame.
- **Localization** — key → table lookup with language/default fallback, asset/font overrides,
  editor validation for duplicate/missing keys. RTL is explicitly *not* implemented.
- **Audio** — fixed-size voice pool + dedicated music slots, per-category/master volume, cue
  randomization, application-focus pause/resume.
- **UI Foundation** — auto-built layered canvas hierarchy, screen stack, modal popups with
  automatic input blocking; not a full UI framework (no navigation graph, no tweening engine).
  Render mode (Screen Space Overlay, the zero-setup default, or Screen Space Camera for 2D games
  that need UI in the same camera stack as the world) is configurable via `UICanvasConfig`, exposed
  as Inspector fields on `PlayerSystemsBootstrapper`.
- **Feedback/Haptics** — thin coordinator over `IHapticProvider` (mobile vs. no-op) plus an
  optional audio cue via a soft dependency on Audio.
- **Gameplay Loop** (`IGameplayService`) — Initialize/BeginPlay/Pause/Resume/Shutdown lifecycle
  hooks (`IGameplayLifecycle`) plus three opt-in tick interfaces
  (`IGameplayTickable`/`IGameplayFixedTickable`/`IGameplayLateTickable`), so participants aren't
  forced into per-object `Update()`. Pause/Resume are detected by polling `ITimeService.IsPaused`
  every tick — nothing ever writes `Time.timeScale` directly.
- **Entities** — `EntityId` (a process-unique monotonic counter, explicitly not
  `GetInstanceID()`), optional `EntityIdentity` component, `ComponentLookup.RequireInParent/InChildren`
  for the one gap `[RequireComponent]` doesn't cover.
- **Object Lifecycle** — `IGameplayObjectLifecycle` (Initialize/Activate/Deactivate/Dispose) driven
  by composition via `GameplayObjectLifecycleRunner`, which guarantees Initialize fires only once
  regardless of pool reuse.
- **Spawning** — `Spawner` (scene/object-lifetime) answers what/where/when/how-many; delegates
  creation to a swappable `ISpawnProvider` (`InstantiateSpawnProvider` or `PooledSpawnProvider`) so
  gameplay code never changes when pooling is turned on. Optional active/total limits + cooldown.
- **Pooling** — `GameObjectPool` wraps Unity's built-in `UnityEngine.Pool.ObjectPool<T>` (2021.1+)
  rather than reimplementing a free-list; adds GameObject activation, an optional
  `IGameplayObjectLifecycle` hook per instance, max-size excess destruction, duplicate-release
  detection, transparent replacement of an externally-destroyed pooled instance, foreign-object
  release rejection, and a `PoolStatistics` snapshot (Phase 5 hardening).
- **Commands** — `IGameplayCommand` (CanExecute/Execute) with a typed `CommandResult`
  (Success/Failure/Rejected), invoked via `GameplayCommandInvoker`; no undo/redo, no
  `Dictionary<string,object>` payload; an optional `GameplayCommandQueue` for the uncommon
  deferred-execution case.
- **Interaction/Targeting** — `IInteractable` + a deliberately minimal `InteractionContext`;
  `TargetingUtility` provides non-allocating 3D/2D overlap queries, closest-target selection, and a
  cheap view-cone check. Not an AI targeting system; never references `GameFramework.Input`.
- **Objectives/Checkpoints (Phase 4)** — `ObjectiveBase` owns a generic
  Inactive→Active→Completed/Failed→Inactive state machine and publishes events through the Phase 2
  `IEventService`; `Checkpoint`/`CheckpointData` are data-only (no auto-respawn, no second save
  system). No concrete objective or progression layer is defined by this phase.
- **Profiling** (`ProfileScope`, `IPerformanceMonitorService`) — categorized `ProfilerMarker`
  wrapper gated by `PerformanceSettings.Mode` (zero-cost when `Disabled`); rolling frame-time
  stats/spike detection/named budgets sampled via `IUpdatableService.Tick()`. An optional
  development-only `OnGUI` overlay (`PerformanceOverlay`) shows FPS/frame time/memory.
- **Tick System** (`ITickService`) — general-purpose `ITickable`/`IFixedTickable`/`ILateTickable`
  registration, replacing per-object `Update()` where useful. Unlike `IGameplayTickable`, it ticks
  every frame regardless of gameplay pause (deltaTime is just 0 while paused) — a lower-level
  primitive, not a replacement for the Phase 4 gameplay loop. Allocation-free steady-state via a
  snapshot-before-invoke pattern (same technique as `IEventService.Publish`); safe to
  register/unregister mid-tick.
- **Resource Management** (`IAssetProvider`/`ResourcesAssetProvider`) — reference-counted
  `Resources.Load`/`LoadAsync` wrapper returning release-owning handles; not Addressables (the
  project doesn't use it) and not a registered service (lifetime is caller-owned).
- **Mobile Utilities** — `IMobilePerformanceService` (target frame rate/quality level/resolution
  scale, via configurable `PerformanceProfile`s), `DeviceInfo` (read-once `SystemInfo`/`Screen`
  hints), `IApplicationLifecycleService` (centralizes pause/focus/quit, republishes as Phase 2
  events, pauses `ITimeService` while backgrounded).
- **Memory Diagnostics** (`MemoryDiagnostics`) — static managed-heap/Unity-allocated-memory sample;
  clearly flags when Unity's own memory counters aren't available (non-development release builds).
- **Economy** (`IEconomyService`) — arbitrary currencies via `CurrencyId` (never hard-coded
  "Coins"/"Gems"); `TryAdd`/`TrySpend`/`SetBalance` validate and clamp to `CurrencyDefinition`'s
  Min/MaxBalance, widening to `long` before clamping to avoid overflow. A 50-entry in-memory
  transaction log for debugging/UI/analytics, not a persisted ledger.
- **Inventory** (`IInventoryService`) — quantity-based item ownership; `TryAdd` never silently loses
  items past `ItemDefinition.MaxStack` — it returns exactly how much was applied and exactly how
  much wasn't (`InventoryOperationResult.Remainder`). `TryRemove` is all-or-nothing.
- **Experience** (`IExperienceService`) — `IProgressionCurve` (Linear formula or explicit Table)
  decides XP-per-level; `AddExperience` applies every level crossed in one deterministic
  integer-math pass and publishes one `LevelChangedEvent` per level (never only the final one).
  `IsAtMaxLevel`/`AddExperienceResult.AtMaxLevel` make hitting the curve's cap an explicit, not
  silent, state.
- **Unlocks** (`IUnlockService`) — composable `IUnlockRequirement` (`LevelRequirement`/
  `CurrencyRequirement`/`ItemRequirement`/`PrerequisiteUnlockRequirement`, combined via
  `AllRequirement`/`AnyRequirement`); requirements are registered in code per `UnlockId`, the same
  pattern `ISettingsService.Register` established. `PrerequisiteUnlockRequirement` checks the
  already-unlocked flag (never re-evaluates), which is what makes a requirement cycle a safe,
  detectable content deadlock rather than infinite recursion — `ValidateNoCycles()` is the
  authoring-time check.
- **Rewards** (`IRewardService`) — `IReward` (`CanGrant`/`Grant`) mirrors Phase 4's
  `IGameplayCommand`; a `RewardBundle`'s `CanGrant()` recurses through every child before
  `RewardService.TryClaim` calls `Grant()` on any of them, so an invalid reward never partially
  mutates player state. A persisted claimed-id set makes `RewardClaimPolicy.Once` idempotent by
  construction — repeated `TryClaim` calls after the first always return `AlreadyClaimed`.
- **Statistics** (`IStatisticsService`, Phase 7) — mirrors `EconomyService`'s shape: constructor-
  injected `StatisticDefinition[]`, explicit Save/Load with a dirty-flag-on-Shutdown policy.
  Integer is the primary type (Get/Set/Increment/Decrement); Float/Boolean get focused accessors
  only. `IsMonotonic` statistics reject decreases; `Persistent = false` statistics are session-only.
- **Conditions** (`ICondition`, Phase 7) — composable, mirrors `IUnlockRequirement`'s shape but adds
  progress reporting (`IProgressCondition`). `AllCondition`/`AnyCondition`/`NotCondition`;
  `StatisticCondition` is the primary building block. Never polled — `ConditionStatisticIndex`
  collects which statistics a condition tree depends on at registration time so only a relevant
  `StatisticChangedEvent` re-evaluates it; non-statistic conditions fall back to their own broader
  event (level/currency/item/unlock changed).
- **Objectives/Quests/Achievements/Milestones** (Phase 7) — `ConditionObjective : ObjectiveBase`
  extends Phase 4's bare state machine with a condition + progress. `IQuestService` composes
  objectives + an optional availability condition + a completion rule (All/Any/Count) + a repeat
  policy; `QuestStatus` is mostly derived, not persisted. `IAchievementService` is a simpler,
  always-active single-condition case. `IMilestoneService` is the one place needing no
  code-composed condition — always "statistic reaches threshold." All three claim rewards through
  the existing `IRewardService` rather than tracking their own claimed flag.
- **Game Flow / Level State Machine** (`IGameFlowService`, Phase 8) — one state machine
  (`LevelFlowState`: Unloaded→Loading→Initializing→Ready→Playing⇄Paused→Completing/Failing→
  Completed/Failed→Restarting→Exiting) covering both level-load lifecycle and gameplay-session
  state. Scene-load completion is detected via `ISceneService.SceneLoaded` (not by polling
  `AsyncOperation`, which can't be driven manually in tests). Every command returns a result
  (`TransitionResult`/`LevelLoadResult`/`RespawnResult`) instead of throwing, and every command
  wraps its transition *and* every event it publishes in one re-entrancy guard.
- **Gameplay Sessions** (`GameplaySession`, Phase 8) — one active attempt: `SessionId`, `LevelId`,
  `AttemptNumber`, `ElapsedGameplayTime` (accumulated only while Active), `PauseCount`,
  `RespawnCount`, `Result`, and an owned `CheckpointSystem`. Mutation is internal — exactly one
  session is ever active, enforced by construction. `Retry` always creates a new session; `Respawn`
  is the one operation that continues the *same* attempt instead.
- **Checkpoints/Respawn** (`CheckpointSystem`, Phase 8) — session-scoped registry
  (Register/Activate/GetCurrent/Reset/Clear), composing with Phase 4's `Checkpoint` marker for
  transform data plus an optional `IGameplaySnapshot` for game-defined extra state (never
  inspected/persisted by the framework). `Respawn()` never leaves gameplay half-restored — it only
  touches its own state when a valid checkpoint exists and publishes `PlayerRespawnedEvent` for a
  game's own player controller to react to (GameFlow does not know what "the player" is). Only the
  built-in transform is ever optionally persisted (`IGameFlowService.PersistCheckpoints`), never an
  `IGameplaySnapshot`'s contents.
- **Pause Tokens** (`IPauseToken`, Phase 8) — `PauseGameplay(reason)` returns a labeled token
  wrapping exactly one `ITimeService.Pause()`/`Resume()` pair, so multiple independent owners (a
  tutorial, a dialog) compose correctly without duplicating `ITimeService`'s existing
  reference-counting. `LevelFlowState` reactively mirrors `ITimeService.IsPaused` every tick — the
  same reactive-pause pattern `GameplayService` already established in Phase 4.
- **Tutorial Framework** (`ITutorialService`, Phase 9) — one active tutorial at a time;
  `TutorialState` (`Inactive→Starting→Running⇄Paused→Completing→Completed`, plus
  `Running/Paused→Cancelling→Cancelled`) auto-returns to `Inactive` after every outcome, freeing the
  slot without a separate reset call. Five built-in `ITutorialStep` types (`InstructionStep`/
  `WaitStep`/`InputStep`/`EventStep<TEvent>`/`ConditionStep`) cover manual acknowledgement, timed
  waits, logical-input actions (`IInputService`), framework events (`IEventService`), and a small
  local `ITutorialCondition` (mirrors `GameFramework.Quests.Conditions.ICondition`'s shape without
  the assembly reference). Step *instances* are reused across every run of a tutorial, so
  `ITutorialStep.Reset()` returns a step to `NotStarted` before each new run — the bug this exact gap
  caused (a second run silently short-circuiting through already-completed step objects) was caught
  by the first real test run, see Testing below. Optional gameplay pause reuses `ITimeService.Pause`/
  `Resume` directly (no new token type, since unlike Phase 8's `IPauseToken` nothing external needs
  to hold this pause); optional input gating reuses `IInputService`'s existing context stack.
  `TutorialRepeatPolicy` (Once/Repeatable/OncePerSession)/`TutorialSkipPolicy`/
  `TutorialPersistencePolicy` (None/CompletionOnly/ResumeProgress) are per-tutorial authored policies.
- **Game Feel / Presentation** (`IPresentationService`, Phase 10) — `Play(feedbackId, ...)` dispatches
  a `FeedbackDefinition`'s enabled channels without gameplay code knowing how any one is implemented:
  Audio (`IAudioService.Play`, intensity scales volume only below full intensity so a cue's own
  randomized volume is never clobbered at 1.0), Haptics (`Feedback.IFeedbackService.TriggerHaptic`),
  Camera shake (`ICameraFeedbackDriver` + `CameraShakeState`, a pure, seeded, composable
  Perlin-noise accumulator a game's own `CameraFeedbackDriver` applies non-destructively on top of
  whatever wrote the camera's transform that frame), Visual effects (spawned through
  `Gameplay.Pooling.IPoolService` when registered, else a plain `Instantiate`/`Destroy`), Screen
  flash/fade (one `Image` this service creates lazily under the existing `UI.IUIService` layer root,
  driven by pure `ScreenEffectController`), UI reactions (`UIFeedbackRequestedEvent` only — never a
  direct `UIScreen`/`UIPopup` call), and Time/hit-stop (`ITimeService.SetTimeScale`/`ResetTimeScale`
  only, never `Pause`/`Resume`, via pure `TimeFeedbackController`). Screen and Time are the two
  *exclusive* channels — a lower-priority request while one is active is dropped, equal-or-higher
  replaces it; every other channel is fire-and-forget composable. Every one of Audio/Feedback/
  Gameplay(pooling)/UI is a soft (`registry.TryGet`) dependency, so the service degrades channel by
  channel rather than requiring all of them registered. Settings-integrated: a master
  "Presentation.Enabled" switch, one enable toggle per channel, and a global
  "Presentation.IntensityScale" accessibility multiplier.
- **Camera Framework** (`ICameraService`, Phase 11) — camera orchestration, not a monolithic
  controller: `RegisterCamera`/`UnregisterCamera` assign a stable `CameraId` (a monotonic counter,
  never `GetInstanceID()`) to a `CameraController`; `Activate` sets the base camera, `PushOverride`/
  `ICameraOverrideHandle.Release` maintain a temporary-override stack on top of it (nested overrides
  can be released out of order without corrupting the stack). Pose computation is a pure,
  Unity-lifecycle-free pipeline owned per-controller: `ICameraMode.ComputePose` (Follow — per-axis
  lock, dead zone/soft zone "rubber band," `SmoothDamp` — or Static/TargetLook/Manual) → damped zoom
  (`CameraZoomController`, orthographic size or field of view, whichever the camera's own
  `Camera.orthographic` says) → world bounds (`CameraBoundsConstraint`, exact for orthographic,
  center-only for perspective). `CameraDriver` is the one MonoBehaviour per scene that actually
  writes to the real `Camera`/`Transform` each `LateUpdate` (plain Unity lifecycle, not
  `Performance.Ticking.ITickService` — CLAUDE.md's own Tick Rules call for plain `Update`/`LateUpdate`
  for "anything simple, low-count, or already working," the same reasoning `Presentation.CameraFeedbackDriver`
  already used for itself), blending through `CameraTransitionRunner` whenever the active camera
  changes. `[DefaultExecutionOrder(-100)]` guarantees this runs before `CameraFeedbackDriver`'s own
  (default-order) `LateUpdate`, so Phase 10's shake always layers on top of this frame's freshly
  written base pose — achieved with zero compile-time coupling between the two assemblies in either
  direction. A missing/destroyed target holds the last valid pose rather than snapping or throwing.
  `Camera.ReduceMotion` (off by default) is the one accessibility setting this layer owns — every
  built-in mode/zoom skips damping entirely when it's on, deliberately separate from Presentation's
  existing "Presentation.Channel.Camera" shake toggle (an unrelated concern already owned by Phase 10).
- **UI Navigation / Menu Flow** (`INavigationService`, Phase 12) — orchestrates Phase 3's
  `IUIService` screen/popup stack rather than duplicating it: `RegisterScreen`/`RegisterPopup` map a
  stable `UIScreenId`/`UIPopupId` to a prefab; `Navigate`/`Replace`/`Reset` push/replace/clear-and-root
  the screen stack; `NavigateBack` implements a centralized priority chain (an open popup closes
  first, then the current screen's own `IUINavigationBackHandler` gets first refusal, then the stack
  pops, then `BackRequestedAtRootEvent` publishes for the game/GameFlow/app to decide what "back at
  the root" means — this framework never assumes one). Typed parameters/results flow through
  `NavigationRequestOptions` and a small additive seam added to `IUIService.OpenScreen`/`OpenPopup`
  (`onBeforeOpen`, default null). `INavigationGuard`s can Allow/Block/Defer any request, including
  back navigation. `IUINavigationTransitionHandler` offers an enter-only visual-transition coroutine
  hook (exit transitions aren't offered — Phase 3 destroys a closing screen/popup synchronously, so
  there is no seam to defer that destruction for one). Android/mobile back button is centralized in
  one internal `NavigationBackButtonDriver` (`KeyCode.Escape`, Unity's documented Android-back
  mapping, plus an optional `IInputService` logical action) — the only place in the framework that
  reads it. A navigation command issued while `IsNavigating` is true (including synchronously from
  inside a lifecycle hook this service itself just triggered) is rejected, never queued — the same
  deliberate re-entrancy guard `GameFlowService`/`TutorialService` already apply to their own event
  handlers; a legitimate follow-up call from a hook must be deferred one frame (see the sample).
  Popups can optionally acquire a `GameFlow.IPauseToken` for as long as they're open, released
  automatically on close, resolved softly so this works with or without GameFlow registered.
- **Save Profiles & Player Data** (`IPlayerProfileService`, Phase 13) — orchestrates Phase 2's
  `IPersistenceService` rather than duplicating storage/serialization: `RegisterSection<TSection>`
  registers a factory for a game's own `PlayerDataSection<TData>` subclass (each with a stable id,
  independent schema version, and its own `Validate`); `CreateProfile`/`LoadProfile`/
  `LoadDefaultProfile`/`SwitchProfile`/`UnloadActiveProfile`/`DeleteProfile` never throw, returning a
  `ProfileOperationResult` instead, and reject a concurrent call with `AlreadyActive` rather than
  queuing it. Each section's data is its own `"GameFramework.PlayerData.{profileId}.{sectionId}"`
  persistence key, so Phase 2's existing versioning/migration machinery applies per section with no
  new envelope format; `RegisterMigration(sectionId, migration)` is queued and forwarded to the
  concrete key the first time a profile using that section loads. Autosave composes
  `ApplicationPause`/`FocusLost`/`SceneTransition`/`ProfileUnload`/debounced-dirty triggers
  (`AutosavePolicy`) — the debounce timer coalesces rapid mutations into one write via `ITimerService`,
  never a new update loop; mobile pause/focus/quit are forwarded by a private
  `PlayerDataLifecycleDriver`, deliberately independent of Phase 5's `IApplicationLifecycleService` so
  a game doesn't need all of Performance just for autosave. Before overwriting a section, its
  currently-stored data is copied to a `.bak` companion key; on load, a genuine deserialize failure
  (detected the same way `PersistenceServiceTests` already verifies Phase 2's own `.corrupt` marker
  behavior) triggers a `.bak` restore attempt, then falls back to that section's defaults — the
  profile still loads (`ProfileOperationResultKind.Corrupted`), nothing is silently destroyed.
- **Mobile Platform & Device Services** (`IPlatformService`, Phase 14) — platform identity
  (`PlatformType`, `IsMobile`/`IsAndroid`/`IsIOS`/`IsDesktop`), `IDeviceInfoService` (a cached
  `PlatformDeviceInfo` snapshot plus `Supports(DeviceCapability)`), `IScreenService` (safe area/
  orientation, an internal `ScreenSignalDriver` polling once per frame since Unity has no change
  callback for either), `IClipboardService`, `IPlatformUrlService` (validated `Application.OpenURL`),
  `IAppStoreService` (game-supplied `AppStoreConfig`, builds `market://`/`itms-apps://` URLs with no
  native code), `INetworkReachabilityService` (`Application.internetReachability` passthrough),
  `IPermissionService` (Camera/Microphone only, built on `Application.HasUserAuthorization`/
  `RequestUserAuthorization`), and `IAppSettingsService` (Android only, via the framework's one
  isolated `AndroidJavaObject` boundary, `Platform/Android/AndroidAppSettingsProvider`). References
  only Core/Runtime; deliberately does not duplicate Phase 5's `IApplicationLifecycleService` or
  Phase 3's `IHapticProvider` — see Framework.md's Phase 14 section for the full non-goal list.

Complete API examples, edge cases, and design rationale for every system are documented in
`Assets/GameFramework/Documentation/Framework.md` — treat that file as the authoritative reference.

## Testing

- EditMode tests cover pure logic via injected fakes (`IInputSampler`, `IRandomSource`,
  `IHapticProvider`, fake `ITimeService`/`ISceneService`), so behavior is deterministic without a
  live device, a real scene load, or an engine tick.
- PlayMode tests cover behavior that genuinely needs a running engine (real `AudioSource`
  playback/fades, `UIScreen`/`UIPopup` `AddComponent` + `Destroy`, full `GameBootstrapper.Awake`
  wiring of all services).
- `GameFramework.UI.Tests` intentionally has no EditMode variant — Unity's `AddComponent` rejects
  scripts from an Editor-only assembly, so all UI tests are PlayMode-only.
- Phase 4: EditMode (`GameFramework.Gameplay.Tests`) covers lifecycle/commands/objectives/entity-id
  logic and the gameplay loop (fake `ITimeService`) with no real GameObject dependency; PlayMode
  (`.Tests.Runtime`) covers pooling, spawning, and Physics/Physics2D-based targeting, which need
  real GameObjects/colliders.
- Phase 5: EditMode (`GameFramework.Performance.Tests`) covers `TickService` (registration,
  duplicate calls, priority, mid-tick modification, exception isolation, fixed/late phases) and
  `PerformanceMonitorService` (frame sampling, budgets) against a fake `ITimeService`; the new
  pooling-hardening tests live alongside the existing `GameObjectPoolTests` in
  `GameFramework.Gameplay.Tests.Runtime` (PlayMode, since they need real GameObjects).
- `Assets/GameFramework/Samples/Phase5Benchmark/` is an isolated, non-Build-Settings benchmark scene
  (Stopwatch-timed tick/pool benchmarks, a memory sample, a combined development report) — see
  Framework.md's Performance Test Scene section and this project's Phase 5 completion report for
  the actual measurements one run produced.
- Phase 6: three EditMode assemblies (`GameFramework.Progression.Tests`, `.Unlocks.Tests`,
  `.Rewards.Tests`), each with its own `TestRegistryFactory` (fake `ITimeService` + real
  `EventService` + real `PersistenceService`-over-`InMemoryPersistenceStorage`) and a
  reflection-based `TestDefinitions` helper for populating definition ScriptableObjects' private
  fields. Covers each service's business rules in isolation, requirement composition (AND/OR/
  prerequisite cycles), reward idempotency, and cross-system integration flows (XP → level-up →
  unlock becomes available; claim → save → reload → replay-claim stays idempotent). A real bug
  (persisted unlocks/claims being silently dropped on load, since `RegisterUnlock`/`RegisterReward`
  necessarily run after `Load()`) was caught by the first real test run and fixed — see
  Framework.md's Testing section.
- Phase 7: `GameFramework.Quests.Tests` (EditMode) reuses the Phase 6 `TestRegistryFactory`/
  `TestDefinitions` pattern. Covers statistic increment/decrement/monotonic rejection/overflow
  clamping, condition composition (including vacuous AND/OR identities), the objective lifecycle,
  quest availability/completion rules/repeat policy/claim idempotency, achievement/milestone
  completion+auto-claim, and cross-system integration flows (statistic → objective → quest;
  statistic → achievement → reward; XP → level-up → unlock+achievement together;
  quest-complete → save → simulated restart → load → still claimed).
- Phase 8: `GameFramework.GameFlow.Tests` (EditMode) fakes both `ITimeService` and `ISceneService`
  (a real `AsyncOperation` cannot be driven manually outside an actual engine scene load).
  `LevelFlowStateMachineTests` covers the transition table directly. `GameFlowServiceTests` covers
  load/ready/start, reactive pause/resume (including two independent pause-token owners),
  complete/fail, respawn vs. retry, checkpoint persistence round-trips, a re-entrant command from
  inside an event handler being blocked, and cancelling a load in progress. `IntegrationTests`
  covers five named end-to-end flows. 48/48 Phase 8 tests pass; the full project suite (478
  EditMode + 74 PlayMode tests) was re-run after this phase with zero regressions.
- Phase 9: `GameFramework.Tutorials.Tests` (EditMode) reuses the same `TestRegistryFactory` pattern
  with a fake `ITimeService`/`IInputService` plus a real `EventService`/`PersistenceService`-over-
  `InMemoryPersistenceStorage`. Covers the lifecycle state machine, each built-in step type in
  isolation, start/complete/skip/cancel/restart, all three repeat policies, prerequisites, pause
  (including a tutorial not mistaking its own `PausesGameplay` acquisition for an external pause),
  input-context gating, re-entrancy (a command issued from inside an event handler being blocked),
  cleanup, all three persistence policies (including registration-after-Load ordering and corrupted
  data), and end-to-end integration flows across all five step types. A real bug (the same
  `ITutorialStep` instances being reused across every run of a tutorial, but with no way to reset a
  Completed/Cancelled step back to NotStarted, so a second run silently short-circuited) was caught
  by the first real test run and fixed with `ITutorialStep.Reset()` — see Framework.md's Testing
  section. 93/93 Phase 9 tests pass; the full project suite (571 EditMode + 74 PlayMode tests) was
  re-run after this phase with zero regressions, and the Phase9Demo sample's full step sequence
  (including a `Repeatable` second run) was additionally verified live in Play Mode.
- Phase 10: `GameFramework.Presentation.Tests` (EditMode) covers the pure-logic pieces
  (`CameraShakeState`, `ScreenEffectController`, `TimeFeedbackController`) directly, plus
  `PresentationServiceTests` against the same `TestRegistryFactory` pattern with small fakes for
  `IAudioService`/`IFeedbackService`/`IUIService`/`ICameraFeedbackDriver` (registered per test, since
  every one is a soft dependency). Covers registration, the master/per-channel/intensity-scale
  settings, every channel executing when its backing service is present and silently no-op-logging
  once when it isn't, event publication, camera-driver registration safety, event-to-feedback
  mappings, and `Shutdown` resetting an active time effect. `GameFramework.Presentation.Tests.Runtime`
  (PlayMode) covers visual-effect spawning (both the pooled and plain-`Instantiate` paths), since the
  non-pooled fallback's `Object.Destroy(instance, lifetime)` is refused outside Play Mode. A real bug
  (`CameraShakeState`'s original noise sampling could land different seeds on the same Perlin-noise
  integer lattice point, producing identical offsets) was caught by
  `Tick_DifferentSeeds_ProduceDifferentOffsets` on the first real run and fixed by offsetting every
  axis with distinct non-integer constants — see Framework.md's Testing section. 46/46 Phase 10
  EditMode tests and 2/2 PlayMode tests pass; the full project suite (617 EditMode + 76 PlayMode
  tests) was re-run after this phase with zero regressions, and the Phase10Demo sample's
  "HeavyImpact" (six channels) and "Reward" definitions, plus the master enable/disable setting, were
  additionally verified live in Play Mode.
- Phase 11: `GameFramework.Cameras.Tests` (EditMode) covers every pure-logic piece directly and in
  isolation - `CameraDeadZone` (inside/at-edge/beyond dead zone, soft-zone easing), `FollowCameraMode`/
  `TargetLookCameraMode`/`StaticCameraMode`/`ManualCameraMode` (missing/invalid target fallback, axis
  lock, damping, `Camera.ReduceMotion` bypassing damping), `CameraBoundsConstraint` (orthographic
  half-extent clamp, perspective center-only clamp, bounds narrower than the camera's own view
  centering instead of an inverted clamp), `CameraZoomController`, `CameraTransitionRunner`
  (immediate/blending/cancel), `CameraPoseController` (mode→zoom→bounds composition, a
  destroyed-mid-follow target holding the last pose), and `CameraService` (duplicate registration
  idempotency, activation, nested override push/pop including out-of-order release, unregistering an
  active/overriding camera falling back correctly, `GetState`). `GameFramework.Cameras.Tests.Runtime`
  (PlayMode) covers what genuinely needs a running engine - `CameraDriver.LateUpdate` actually firing
  and applying a pose/zoom to a real `Camera` component, resolving the active camera correctly across
  a push/pop, and `CameraController`'s real `Awake`/`SetTarget`/`Snap` lifecycle - using the same
  reflection-field-injection technique `Presentation.Tests.PresentationServiceRuntimeTests` already
  established, so these don't need a live `GameBootstrapper` singleton. A real bug (the initial pose
  seeded a fixed/non-followed axis, e.g. camera depth, from the assigned target's raw position
  instead of this controller's own authored transform, silently corrupting that axis the first time a
  target was assigned) was caught live in the Phase11Demo sample, not by a test, and fixed by always
  seeding from `transform.position`/`transform.rotation` and implementing `Snap()` as one
  `reduceMotion` tick through the normal mode/zoom/bounds pipeline instead of a hand-rolled shortcut -
  see Framework.md's Testing section. 67/67 Phase 11 EditMode tests and 8/8 PlayMode tests pass; the
  full project suite (684 EditMode + 84 PlayMode tests) was re-run after this phase with zero
  regressions, and the Phase11Demo sample's Follow/dead-zone/soft-zone/bounds/zoom, Boss camera
  override push/pop, and Phase 10 camera-shake integration were additionally verified live in Play
  Mode (via direct service calls, since Play Mode key-press simulation is not available through the
  Unity MCP tooling used for this verification).
- Phase 11 Cinemachine integration: `GameFramework.Cameras.Cinemachine.Tests` (EditMode) covers the
  one genuinely new piece of math this integration adds -
  `CinemachineBoundsTranslator.TryComputeBoxBounds` (disabled/null/degenerate-width/valid rect →
  `BoxCollider2D` center+size). `GameFramework.Cameras.Cinemachine.Tests.Runtime` (PlayMode) covers
  what needs a live Cinemachine pipeline - activation boosting/restoring `CinemachineVirtualCamera.Priority`,
  target mirroring onto Follow/LookAt via `CameraController.Target`, `LateUpdate` writing
  `CameraController.ComputePose`'s zoom half to the vcam's lens, and `Start()` generating a
  `BoxCollider2D` for an assigned `CinemachineConfiner2D` with no shape already authored - using the
  same reflection-field-injection technique `CameraDriverRuntimeTests` already established. 5/5
  EditMode and 4/4 PlayMode tests pass; the full project suite (689 EditMode + 88 PlayMode tests) was
  re-run after this integration with zero regressions. Live-verified in Play Mode via the new
  `Phase11CinemachineDemo` sample scene and direct service/reflection calls through the Unity MCP
  `execute_code` tool (again, no key-press simulation available): activating the gameplay camera
  correctly boosted its vcam's `Priority` and mirrored the Player `Transform` onto Follow/LookAt;
  `PushOverride`/handle `Release` correctly re-mapped priorities and `CinemachineBrain.ActiveVirtualCamera`
  genuinely switched cameras with the real `Main Camera` transform ending at the event camera's
  authored vantage point, then blending back; `CameraController.SetZoom` was observed driving the
  vcam's `m_Lens.OrthographicSize` toward the requested value through the real `Camera` component;
  `IPresentationService.Play` on the camera-shake `FeedbackDefinition` was observed registering on
  `CameraFeedbackDriver.ActiveShakeCount` and the transform returning to its exact pre-shake baseline
  once the (0.35s) shake decayed - confirming Phase 10's shake composes correctly on top of a
  Cinemachine-driven camera with **zero code changes to `GameFramework.Presentation` itself** (only
  `CameraFeedbackDriver` picking up `[DefaultExecutionOrder(100)]`, described above); and
  `ResetCamera` returned `true` with no console errors throughout the whole session.
- Phase 12: `GameFramework.UI.Navigation.Tests.Runtime` (PlayMode-only - no EditMode variant, the
  same reason `GameFramework.UI.Tests` has none: test-double `UIScreen`/`UIPopup` subclasses are
  MonoBehaviours, and `AddComponent` rejects a script compiled only for the Editor platform). Covers
  registration (duplicate/invalid id, unknown screen/popup), push/replace/reset navigation (including
  parameter delivery before `OnOpened` and `ScreenNavigatedEvent` publication), the full back-priority
  chain (popup-first, `IUINavigationBackHandler` consuming a request, stack pop with a delivered
  result, `BackRequestedAtRootEvent` at the root), nested popup stacks (back closes the topmost
  first), pause-token acquisition/release against a fake `IGameFlowService`, `IsNavigating` correctly
  blocking a concurrent request during an `IUINavigationTransitionHandler` enter transition and
  allowing one again once it completes, `INavigationGuard` Allow/Block/Defer (including for back
  navigation), and `RegisterEventMapping<TEvent>`/`UnregisterEventMapping<TEvent>`. 41/41 Phase 12
  tests pass; the full project suite (689 EditMode + 129 PlayMode tests) was re-run after this phase
  with zero regressions. Live-verified in Play Mode via the new Phase12Demo sample scene, this time by
  invoking the sample's real `Button.onClick` handlers (not raw `INavigationService` calls) through
  the Unity MCP `execute_code` tool: Main Menu → Character Selection → Car Selection → Customization
  worked end-to-end with parameters flowing forward at each step; Customization's "Done" button
  triggered a two-level chained back-navigation (worked around the reentrancy guard above via a
  one-frame-deferred coroutine) that correctly delivered the chosen car as a typed result to Character
  Selection's original callback; a nested Settings → Confirm popup stack closed topmost-first on
  successive `NavigateBack()` calls, leaving Main Menu untouched throughout; and `NavigateBack()` at
  the root published `BackRequestedAtRootEvent` as expected. Zero console errors/warnings throughout.
- Phase 13: `GameFramework.PlayerData.Tests` (EditMode) reuses the same `TestRegistryFactory` pattern
  with a fake `ITimeService` plus real `EventService`/`TimerService`/`PersistenceService`-over-
  `InMemoryPersistenceStorage`. Covers profile create/load/unload/switch/delete/default-profile and
  every `ProfileOperationResultKind`, section registration (duplicate type/id, locked out once a
  profile is active), dirty tracking/`Validate` call points, save/load round-tripping, migration
  forwarding to a profile's concrete key (including a section added in a later build loading cleanly
  at defaults), corruption detection and `.bak` recovery across two independent
  `PlayerProfileService` instances sharing one `InMemoryPersistenceStorage` (simulating an
  application restart), a `Validate`-throwing section's save failure staying isolated from a healthy
  section's, debounced/coalesced autosave driven through a real `TimerService.Tick()`, and event
  ordering (`IEventService` publishes plus the mirrored direct C# events). `GameFramework.
  PlayerData.Tests.Runtime` (PlayMode) covers `PlayerDataBootstrapper` reaching `Ready` alongside the
  base eight services and the simulated application-pause flush (Unity doesn't allow a test to invoke
  `OnApplicationPause` directly, so this calls the same internal handler `PlayerDataLifecycleDriver`
  forwards to). 51/51 Phase 13 tests pass (45 EditMode + 6 PlayMode); the full project suite (734
  EditMode + 135 PlayMode tests) was re-run after this phase with zero regressions.
- Phase 14: `GameFramework.Platform.Tests.Runtime` (PlayMode-only - `PlatformBootstrapper.Awake` calls
  `DontDestroyOnLoad`, and `ScreenService`/`PermissionService` each create their own
  `DontDestroyOnLoad` driver GameObject, both only legal in Play Mode). Covers bootstrap registration
  of all nine services, `PlatformService` identity in the Editor, `DeviceInfoService.Current`'s
  snapshot values and `Supports` not throwing for any capability, `ScreenService` exposing live
  values without throwing, `AppStoreService`/`PlatformUrlService`/`NetworkReachabilityService` pure
  logic (URL validation, store URL construction for Android/iOS/missing-config/unsupported-platform,
  reachability passthrough) via `public static` pure methods with no `Application.OpenURL` side
  effect, `ClipboardService` set/get/has round-trips, and `PermissionService.RequestPermission`
  invoking its callback exactly once. 36/36 Phase 14 tests pass; the full project suite (734 EditMode
  + 171 PlayMode tests) was re-run after this phase with zero regressions.

## Packages / Dependencies

- Universal RP 14.0.12, TextMeshPro 3.0.9, UGUI 1.0.0, Input System 1.14.2 (used directly by
  `GameFramework.Input` for New Input System bindings), Device Simulator, 2D feature set, standard
  Unity modules.
- Cinemachine 2.10.7 — used only by the optional `GameFramework.Cameras.Cinemachine`/
  `.Cinemachine.Editor` assemblies (Phase 11's Cinemachine integration); every other assembly in the
  framework, including `GameFramework.Cameras` itself, has no reference to it and works without it
  installed.
- `com.coplaydev.unity-mcp` — Unity MCP integration for AI-tool-driven editor control.

## Folder Layout

```text
Assets/GameFramework/
├── Runtime/            One folder + .asmdef per system (Core, Bootstrap, Services, Diagnostics,
│                        State, SceneManagement, Time, Timers, Events, Persistence, Settings,
│                        Input, Localization, Audio, Feedback, UI, PlayerSystems, Gameplay,
│                        Performance, Progression, Unlocks, Rewards, Quests, GameFlow, Tutorials,
│                        Presentation, Cameras, PlayerData, Platform); Cameras/Integration/Cinemachine/
│                        holds the optional GameFramework.Cameras.Cinemachine assembly; UI/Navigation/
│                        holds the GameFramework.UI.Navigation assembly (Phase 12); PlayerData/
│                        holds the GameFramework.PlayerData assembly (Phase 13); Platform/ holds the
│                        GameFramework.Platform assembly (Phase 14), with Android/ isolating the one
│                        AndroidJavaObject boundary
├── Editor/              Editor-only tooling (Localization, Gameplay config, Quest content,
│                        Tutorial content, Feedback content, Camera Configuration, UI Navigation
│                        Catalog, Player Data diagnostics, and Platform diagnostics); Cameras/Cinemachine/
│                        holds the optional .Cinemachine.Editor assembly
├── Samples/              Phase0Demo/ … Phase4Demo/ (one per phase), Phase5Benchmark/,
│                         Phase6Demo/, Phase7Demo/, Phase9Demo/, Phase10Demo/, Phase11Demo/, Phase12Demo/
│                         (each with authored Content/ ScriptableObject/prefab assets; Phase11Demo/
│                         additionally has a second scene, Phase11CinemachineDemo.unity, for the
│                         Cinemachine integration) — Phase 8, Phase 13, and Phase 14 intentionally have
│                         no sample yet (Phase 8: see Framework.md's Phase 8 section for why; Phase 13/
│                         14: each phase's infrastructure is fully exercised by its own test suite
│                         instead)
├── Tests/               Editor/ (EditMode) and Runtime/ (PlayMode) tests, mirroring Runtime/
└── Documentation/       Framework.md — full authoritative reference
```

## Where to Look Next

- Full architecture/API reference: `Assets/GameFramework/Documentation/Framework.md`
- Engineering rules and workflow this project's changes must follow: `CLAUDE.md`
