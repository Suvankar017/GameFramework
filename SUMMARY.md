# Project Summary — GameFramework

A reusable Unity framework intended to be shared across multiple mobile games, providing the
foundational services (bootstrap, persistence, UI, audio, input, localization, feedback, gameplay
infrastructure, performance, progression/economy, quests/achievements, and game-flow orchestration)
that most games need, without any game-specific content.

## Status

**Phase 8 — Game Flow, Gameplay Sessions & State Orchestration**, on top of:

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

No player character, enemy AI, weapons, tutorial, ads, analytics, IAP, remote config, or
multiplayer exists yet, and no concrete currency/item/level/unlock/reward/quest/achievement is
defined for any specific game — those consume this infrastructure, they don't live in it. See
`Assets/GameFramework/Documentation/Framework.md`'s Roadmap section for what each phase explicitly
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

Everything is wired together by `GameBootstrapper`, which registers and initializes services in a
load-bearing order: Logging → GameState → Scene → Time → Timer → Event → Persistence → Settings,
then (via `PlayerSystemsBootstrapper`) Input → Localization → Audio → UI → Feedback, (via
`GameplayBootstrapper`, or a game's own combined subclass) `IGameplayService` → `IPoolService`, (via
`PerformanceBootstrapper`) `ITickService` → `IPerformanceMonitorService` →
`IApplicationLifecycleService` → `IMobilePerformanceService`, (via `ProgressionBootstrapper`)
`IEconomyService` → `IInventoryService` → `IExperienceService` → `IUnlockService` →
`IRewardService`, (via `QuestsBootstrapper`, which extends `ProgressionBootstrapper` directly since
it has a real compile-time dependency on Rewards) `IStatisticsService` → `IQuestService` →
`IAchievementService` → `IMilestoneService`, and (via `GameFlowBootstrapper`, or a game's own
combined subclass, registered after `IGameplayService` so its soft lookup succeeds)
`IGameFlowService`.

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
| `GameFramework.Editor` | Editor-only; localization table validation, Gameplay config validation, and Quest content validation menu items. |
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
│                        Performance, Progression, Unlocks, Rewards, Quests, GameFlow)
├── Editor/              Editor-only tooling (Localization, Gameplay config, and Quest content
│                        validators)
├── Samples/              Phase0Demo/ … Phase4Demo/ (one per phase), Phase5Benchmark/,
│                         Phase6Demo/, Phase7Demo/ (both with authored Content/ ScriptableObject
│                         assets) — Phase 8 intentionally has no sample yet (see Framework.md's
│                         Phase 8 section for why)
├── Tests/               Editor/ (EditMode) and Runtime/ (PlayMode) tests, mirroring Runtime/
└── Documentation/       Framework.md — full authoritative reference
```

## Where to Look Next

- Full architecture/API reference: `Assets/GameFramework/Documentation/Framework.md`
- Engineering rules and workflow this project's changes must follow: `CLAUDE.md`
