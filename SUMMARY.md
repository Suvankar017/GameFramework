# Project Summary — GameFramework

A reusable Unity framework intended to be shared across multiple mobile games, providing the
foundational services (bootstrap, persistence, UI, audio, input, localization, feedback) that most
games need, without any game-specific content.

## Status

**Phase 5 — Performance, Optimization & Runtime Management**, on top of:

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

No player character, enemy AI, weapons, inventory, currency, economy, quests, progression,
rewards, tutorial, ads, analytics, IAP, remote config, or multiplayer exists yet — those consume
this infrastructure, they don't live in it. **Phase 6 (Progression, Rewards, Economy & Inventory)**
is next on the roadmap.

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
        ↓                                      ↓
GameFramework.PlayerSystems             GameFramework.Gameplay
(registers Input/Localization/          (Loop, Entities, Lifecycle, Spawning, Pooling,
 Audio/UI/Feedback)                      Commands, Interaction, Objectives)
        ↓                                      ↓
GameFramework.UI  →  GameFramework.Localization, GameFramework.Audio, GameFramework.Feedback
        ↓                                      │
GameFramework.Input                            │
        ↓                                      ↓
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
markers/statistics) is the only new cross-assembly edge Phase 5 added. Everything is wired together
by `GameBootstrapper`, which registers and initializes services in a load-bearing order (Logging →
GameState → Scene → Time → Timer → Event → Persistence → Settings), then (via
`PlayerSystemsBootstrapper`) Input → Localization → Audio → UI → Feedback, (via
`GameplayBootstrapper`, or a game's own combined subclass) `IGameplayService` → `IPoolService`, and
(via `PerformanceBootstrapper`, or a game's own combined subclass) `ITickService` →
`IPerformanceMonitorService` → `IApplicationLifecycleService` → `IMobilePerformanceService`.

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
| `GameFramework.Editor` | Editor-only; localization table validation and Gameplay config validation (duplicate objective IDs, missing prefabs) menu items. |
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
  detection, and transparent replacement of an externally-destroyed pooled instance.
- **Commands** — `IGameplayCommand` (CanExecute/Execute) with a typed `CommandResult`
  (Success/Failure/Rejected), invoked via `GameplayCommandInvoker`; no undo/redo, no
  `Dictionary<string,object>` payload; an optional `GameplayCommandQueue` for the uncommon
  deferred-execution case.
- **Interaction/Targeting** — `IInteractable` + a deliberately minimal `InteractionContext`;
  `TargetingUtility` provides non-allocating 3D/2D overlap queries, closest-target selection, and a
  cheap view-cone check. Not an AI targeting system; never references `GameFramework.Input`.
- **Objectives/Checkpoints** — `ObjectiveBase` owns a generic
  Inactive→Active→Completed/Failed→Inactive state machine and publishes events through the Phase 2
  `IEventService`; `Checkpoint`/`CheckpointData` are data-only (no auto-respawn, no second save
  system). No concrete objective or progression layer is defined by the framework.
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
- **Pooling Hardening** — `GameObjectPool` gained foreign/duplicate-release rejection (via O(1)
  active-instance tracking), a `PoolStatistics` snapshot (get/release/miss/peak/total-created
  counts), dispose-while-active warnings, and `ProfileScope`-wrapped `Get`/`Release`.
- **Resource Management** (`IAssetProvider`/`ResourcesAssetProvider`) — reference-counted
  `Resources.Load`/`LoadAsync` wrapper returning release-owning handles; not Addressables (the
  project doesn't use it) and not a registered service (lifetime is caller-owned).
- **Mobile Utilities** — `IMobilePerformanceService` (target frame rate/quality level/resolution
  scale, via configurable `PerformanceProfile`s), `DeviceInfo` (read-once `SystemInfo`/`Screen`
  hints), `IApplicationLifecycleService` (centralizes pause/focus/quit, republishes as Phase 2
  events, pauses `ITimeService` while backgrounded).
- **Memory Diagnostics** (`MemoryDiagnostics`) — static managed-heap/Unity-allocated-memory sample;
  clearly flags when Unity's own memory counters aren't available (non-development release builds).

Complete API examples, edge cases, and design rationale for every system are documented in
`Assets/GameFramework/Documentation/Framework.md` — treat that file as the authoritative reference.

## Testing

- EditMode tests cover pure logic via injected fakes (`IInputSampler`, `IRandomSource`,
  `IHapticProvider`, fake `ITimeService`), so behavior is deterministic without a live device or
  engine tick.
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

## Packages / Dependencies

- Universal RP 14.0.12, TextMeshPro 3.0.9, UGUI 1.0.0, Input System 1.14.2 (used directly by
  `GameFramework.Input` for New Input System bindings), Device Simulator, 2D feature set, standard
  Unity modules.
- `com.coplaydev.unity-mcp` — Unity MCP integration for AI-tool-driven editor control.

## Folder Layout

```text
Assets/GameFramework/
├── Runtime/            One folder + .asmdef per system (Core, Bootstrap, Services, Diagnostics,
│                        State, SceneManagement, Time, Timers, Events, Persistence, Settings,
│                        Input, Localization, Audio, Feedback, UI, PlayerSystems, Gameplay,
│                        Performance)
├── Editor/              Editor-only tooling (Localization validator, Gameplay config validator)
├── Samples/              Phase0Demo/ … Phase4Demo/ (one per phase) plus Phase5Benchmark/
├── Tests/               Editor/ (EditMode) and Runtime/ (PlayMode) tests, mirroring Runtime/
└── Documentation/       Framework.md — full authoritative reference
```

## Where to Look Next

- Full architecture/API reference: `Assets/GameFramework/Documentation/Framework.md`
- Engineering rules and workflow this project's changes must follow: `CLAUDE.md`
