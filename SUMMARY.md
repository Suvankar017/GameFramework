# Project Summary — GameFramework

A reusable Unity framework intended to be shared across multiple mobile games, providing the
foundational services (bootstrap, persistence, UI, audio, input, localization, feedback, gameplay
infrastructure, performance, progression/economy, quests/achievements, and game-flow orchestration)
that most games need, without any game-specific content.

## Status

**Phase 20 — Build, Release & Store Pipeline**, on top of:

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
- **Phase 15** — a game-facing Monetization layer, Core/Runtime + a hard dependency on Rewards (for
  reward/entitlement granting) only: `IAdsService` (Banner/Interstitial/Rewarded, placement-based
  cooldown/session-limit policy, entitlement-based suppression rules, a rewarded ad that only ever
  reports `RewardedAdResult.RewardEarned` — it never grants a reward itself), `IPurchaseService`
  (consumable/non-consumable/subscription products, idempotent transaction processing that survives
  an application restart, automatic entitlement/reward granting on a completed or restored purchase),
  and `IEntitlementService` (what the player currently owns, separate from purchase transactions,
  persisted independently of `PlayerData`). No ad or IAP SDK is installed in this project — only the
  provider seam (`IAdProvider`/`IPurchaseProvider`) plus deterministic mock providers exist; see
  `Framework.md`'s Phase 15 section for exactly what a real Google Mobile Ads/Unity IAP adapter would
  require.
- **Phase 16** — a provider-independent Analytics + Crash/Diagnostics layer, Core/Runtime + hard
  references to Performance (lifecycle events) and Platform (diagnostic device context) only:
  `IAnalyticsService` (validated/sanitized custom + well-known events, consent-gated
  buffering, anonymous identity persisted independently of `PlayerData`, session lifecycle built on
  Phase 14's application-lifecycle events, screen-view tracking) and `IDiagnosticsService`
  (breadcrumbs, diagnostic context/tags, exception/error recording, a crash-reporting provider
  boundary with a structural no-recursive-diagnostics guarantee). A third, optional assembly,
  `GameFramework.Analytics.Integration`, holds four opt-in bridges (GameFlow/UI Navigation/Tutorial/
  Monetization → Analytics), never registered automatically — a game constructs the ones it wants.
  No analytics or crash-reporting SDK is installed in this project — only the two provider seams
  (`IAnalyticsProvider`/`ICrashReportingProvider`) plus deterministic No-Op/Mock providers exist; see
  `Framework.md`'s Phase 16 section for exactly what a real Firebase/Sentry/Crashlytics-style adapter
  would require.
- **Phase 17** — a provider-independent Remote Config + Live Operations layer, Core/Runtime + a hard
  reference to Performance (application-resume lifecycle events) only: `IRemoteConfigService` (typed
  Get*, local defaults, atomic validated snapshot activation — an invalid/out-of-range/newer-schema
  payload never partially applies, cache persisted separately from player-save data, stale/expired
  cache policy, a fetch generation counter that safely ignores a late provider callback after its own
  fetch-timeout already fired), `IFeatureFlagService` (a thin typed-bool layer over
  `IRemoteConfigService` with change notifications diffed per declared flag), and `ILiveOpsService`
  (a flat UTC start/end event schedule with an optional per-event remote override, a pluggable
  `ILiveOpsClock` for server-time-aware/testable scheduling, and a low-frequency `ITimerService` poll —
  never a per-frame `Update` — to detect a pure time-based state transition between configuration
  activations). A fifth opt-in `Analytics.Integration` bridge
  (`RemoteConfigAnalyticsIntegration`) forwards Phase 17's own published events into Phase 16, the
  same non-bootstrapper-registered pattern the existing four bridges use. No Firebase Remote
  Config/Unity Remote Config/PlayFab SDK is installed in this project — only the provider seam
  (`IRemoteConfigProvider`) plus deterministic NoOp/Mock providers exist; see `Framework.md`'s Phase
  17 section for exactly what a real backend adapter would require.
- **Phase 18** — two provider-independent, Core/Runtime-only layers plus one optional glue assembly:
  `GameFramework.Notifications` (a hard reference to Performance for the resume-lifecycle event type,
  and to Localization so a notification's title/body can be declared as a localization key/params
  pair and resolved once at schedule time, since the OS may display a local notification while the
  game process isn't even running) provides `INotificationService` — game-supplied, never
  auto-generated `NotificationId`s (an id is the cancel/replace key), scheduling that replaces an
  already-scheduled id rather than erroring, a provider-independent
  `NotificationPermissionStatus`, and cold/warm/hot notification-open reporting, all fully
  synchronous (the same reasoning `IPersistenceService` already gives for not being `Task`-based).
  `GameFramework.DeepLinks` (Core/Runtime/Performance only - deliberately *not* referencing UI
  Navigation, see below) provides `IDeepLinkService` — cold/warm/hot capture via
  `Application.absoluteURL`/`deepLinkActivated` (genuinely cross-platform Unity APIs, no native
  plugin needed), URI parsing independent of route matching/dispatch, a priority-ordered
  `IDeepLinkHandler` chain, a single-slot deferred-link gate a game releases explicitly via
  `SetReady()`, and a documented "identical to the immediately-previous URI" duplicate-OS-redelivery
  guard. Neither assembly hard-references `UI.Navigation` - routing is a generic `IDeepLinkHandler`
  extension point; the optional `GameFramework.Notifications.Integration` assembly supplies a
  ready-made `NavigationDeepLinkHandler` (route pattern → `INavigationService.Navigate`) and
  `NotificationDeepLinkBridge` (a tapped notification's payload → a synthetic URI → the same
  `IDeepLinkService.Process` pipeline a real inbound link goes through), both opt-in and
  game-constructed. A sixth opt-in `Analytics.Integration` bridge
  (`NotificationsAnalyticsIntegration`) forwards both phases' published events into Phase 16. No
  notification SDK (Unity Mobile Notifications/Firebase/OneSignal) is installed in this project —
  only the provider seam (`INotificationProvider`) plus a deterministic NoOp/Mock provider exist; see
  `Framework.md`'s Phase 18 section for exactly what a real adapter would require, and for why
  Android/iOS-specific code was not written this phase (deep links need none - both capture APIs are
  already cross-platform; notifications have no shipped real provider to adapt against).
- **Phase 19** — production hardening at existing trust boundaries, with
  no new assembly or registered service:
  - **Persistence:** atomic temp → flush → verify → `File.Replace` save writes, a SHA-256 envelope
    checksum (corruption detection, explicitly not tamper-proofing), rejection of newer-than-supported
    and broken-migration saves, and a structured `IPersistenceService.TryLoad`/`PersistenceLoadStatus`.
  - **PlayerData:** primary → `.bak` → defaults recovery that also covers post-load `Validate()`
    failures, backups written by older builds, metadata, and the profile index; re-adoption of on-disk
    profiles so a lost index can never wipe progress; structured `LastLoadRecoveries`.
  - **Monetization:** product-id-matched purchase validation, provider-exception isolation, and
    `IEntitlementService.IsVerifiedThisSession` (local cache ≠ proof of payment).
  - **Remote config:** rejects NaN/oversized/unsupported values and tampered caches.
  - **Inbound input:** `DeepLinkValidationOptions` (size limits, optional scheme allowlist) and
    `NotificationPayloadValidator`.
  - **Redaction:** `SensitiveDataRedactor` at diagnostic/log boundaries.
  - **Mock providers:** `BuildEnvironment` + `DevelopmentProviderGuard`, so mocks are refused in
    release builds. `MonetizationBootstrapper` previously registered an always-succeed mock purchase
    provider unconditionally.
  - **Bootstrap:** per-service init/shutdown failure isolation (`InitializationFailures`).
  - **Editor:** a read-only UI Toolkit **Production Preflight** (mock toggles, committed-secret scan,
    duplicate ids, build profile).

  See `Framework.md`'s "Security, Data Integrity & Production Hardening" section for the threat model
  and what client-side hardening cannot do.
- **Phase 20** — Editor-only build and release orchestration on Unity's own `BuildPipeline`
  (`GameFramework.Editor.Build`):
  - **Profiles:** `FrameworkBuildProfile` assets, one per target × environment. The environment is
    `DeploymentEnvironment`, which extends Phase 19's `BuildEnvironment`. One `GAMEFRAMEWORK_ENV_*`
    define is injected per build through `extraScriptingDefines`, never written to Player Settings.
  - **Versioning:** a validated `MAJOR[.MINOR[.PATCH]]` application version, plus a configurable
    platform build-number scheme.
  - **Preflight validators:** project, scenes (read as text YAML), Player Settings, Android
    (IL2CPP/ARM64, AAB, env-var-only release signing), iOS (external-signing boundary), release safety
    (forbidden defines, mock providers, the Phase 19 secret scan, Git), and framework integration
    (remote-config environment, store product ids).
  - **Temporary settings:** applied and then restored in `finally`.
  - **Output:** deterministic artifact names under a git-ignored `Builds/`, and post-build validation.
  - **Files per build:** `build.json`, `release-manifest.json`, and `build-report.json`.
  - **Entry points:** a provider-neutral CLI (`CommandLineBuild.Build`, exit codes 0–5) and a UI
    Toolkit window.
  - **Real builds:** Windows64 and Android development builds were built for real in this project. The
    first standalone build exposed and fixed a pre-existing compile error in `MobileHapticProvider`.

  The next planned phase is **Phase 21 — Framework Validation Game / Vertical Slice**.

No player character, enemy AI, weapons, real ad network/store integration, a real analytics/crash
SDK, remote config backend, a real notification SDK, or multiplayer exists yet, and no concrete
currency/item/level/unlock/reward/quest/achievement/tutorial-content/feedback-content/ad-placement/
product/remote-config-key/live-event/notification-content/deep-link-route is defined for any specific
game — those consume this infrastructure, they don't live in it. See
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

`GameFramework.Analytics` (Phase 16) is a tenth sibling — it references only Core/Runtime plus
Performance (hard, for application-lifecycle event types only, the same reasoning
`GameFramework.Monetization` already established for the identical reference) and Platform (hard,
for diagnostic device/platform context only). `GameFramework.Analytics.Integration` is a *separate,
optional* eleventh assembly layered on top — the only one in the framework whose entire purpose is
cross-system event forwarding, so it references `GameFramework.Analytics` plus GameFlow/UI.Navigation/
Tutorials/Monetization/RemoteConfig all at once; a game that doesn't need any of that instrumentation
never references it, and Analytics itself is fully functional without it. Its `AnalyticsBootstrapper`
is a `GameBootstrapper` subclass, not a `PlayerSystemsBootstrapper`/`ProgressionBootstrapper`
subclass — nothing it registers has a hard dependency on Input/UI/Rewards.

`GameFramework.RemoteConfig` (Phase 17) is a twelfth sibling — it references only Core/Runtime plus
Performance (hard, for the application-resume lifecycle event type only, the same reasoning
`GameFramework.Monetization`/`GameFramework.Analytics` already established for an identical
reference). Its `RemoteConfigBootstrapper` is a `GameBootstrapper` subclass, not a
`PlayerSystemsBootstrapper`/`ProgressionBootstrapper` subclass — none of its three services
(`IRemoteConfigService`/`IFeatureFlagService`/`ILiveOpsService`) has a hard dependency on Input/UI/
Rewards. `Analytics.Integration`'s bridge count grows to five with the addition of
`RemoteConfigAnalyticsIntegration`, which references `GameFramework.RemoteConfig` the same
opt-in, non-bootstrapper-registered way its other four bridges reference their own source phase.

`GameFramework.Notifications` and `GameFramework.DeepLinks` (Phase 18) are a thirteenth and
fourteenth sibling. `Notifications` references only Core/Runtime plus Performance (hard, resume-
lifecycle event type) and Localization (hard, so a notification's content can be declared as a
localization key and resolved once at schedule time); `DeepLinks` references only Core/Runtime plus
Performance. Neither references `GameFramework.UI.Navigation`, unlike `NavigationBootstrapper`'s own
genuine hard dependency on `IUIService` - routing an incoming link to a screen is a generic
`IDeepLinkHandler` extension point a game (or the optional glue below) fulfills, not something either
core assembly does itself. Their bootstrappers (`NotificationsBootstrapper`/`DeepLinksBootstrapper`)
are both bare `GameBootstrapper` subclasses. A fifteenth, optional assembly,
`GameFramework.Notifications.Integration`, references `Notifications` + `DeepLinks` +
`UI.Navigation` together to supply `NavigationDeepLinkHandler` (route → `INavigationService.Navigate`)
and `NotificationDeepLinkBridge` (a tapped notification's payload → the same `IDeepLinkService.Process`
pipeline a real inbound link goes through) - both opt-in, game-constructed, never registered by any
bootstrapper. `Analytics.Integration`'s bridge count grows to six with
`NotificationsAnalyticsIntegration`, referencing `GameFramework.Notifications` + `GameFramework.DeepLinks`
the same way.

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
`IAppSettingsService`, (via `MonetizationBootstrapper`, which extends `ProgressionBootstrapper`
directly since it has a real compile-time dependency on Rewards) `IEntitlementService` →
`IAdsService` → `IPurchaseService` (the latter two resolve `IEntitlementService` softly during their
own `Initialize`, so it is registered first), and (via `AnalyticsBootstrapper`, or a game's own
combined subclass) `IDiagnosticsService` → `IAnalyticsService` (the latter resolves the former softly
during its own `Initialize`, so it is registered first), and (via `RemoteConfigBootstrapper`, or a
game's own combined subclass) `IRemoteConfigService` → `IFeatureFlagService` → `ILiveOpsService`
(the latter two resolve `IRemoteConfigService` during their own `Initialize`, so it is registered
first), and (via `NotificationsBootstrapper`/`DeepLinksBootstrapper`, or a game's own combined
subclass) `INotificationService`/`IDeepLinkService` (independent of each other and of every other
phase - either can be registered alone). `Analytics.Integration`'s six bridges and
`Notifications.Integration`'s two glue classes are not registered by any bootstrapper — a game
constructs the ones it wants after every service it observes is registered and initialized.

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
| `GameFramework.Monetization` | Ads (`IAdsService`: Banner/Interstitial/Rewarded, placement policy/frequency caps, entitlement-based suppression), Purchases (`IPurchaseService`: consumable/non-consumable/subscription products, idempotent transaction processing, restore), Entitlements (`IEntitlementService`) (Phase 15). Composition root: `MonetizationBootstrapper` (extends `ProgressionBootstrapper`). References `GameFramework.Progression`/`.Unlocks`/`.Rewards` (hard) + `GameFramework.Performance` (hard, for lifecycle event types only). Ships only deterministic mock providers — no ad/IAP SDK is installed in this project. |
| `GameFramework.Analytics` | `IAnalyticsService` (validated/sanitized events, consent-gated buffering, anonymous identity, session lifecycle, screen tracking) + `IDiagnosticsService`/`GameFramework.Analytics.Diagnostics` (breadcrumbs, context/tags, exception/error recording, crash-reporting provider boundary, no-recursive-diagnostics guarantee) (Phase 16). Composition root: `AnalyticsBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow`/`Tutorials`/`Cameras`/`PlayerData`/`Platform` — references only Core/Runtime + `GameFramework.Performance` (hard, lifecycle event types) + `GameFramework.Platform` (hard, diagnostic device context). Ships only deterministic No-Op/Mock providers — no analytics/crash SDK is installed in this project. |
| `GameFramework.Analytics.Integration` | Six opt-in bridges (`GameFlowAnalyticsIntegration`/`NavigationAnalyticsIntegration`/`TutorialAnalyticsIntegration`/`MonetizationAnalyticsIntegration`/`RemoteConfigAnalyticsIntegration`/`NotificationsAnalyticsIntegration`) forwarding another phase's published events into Analytics (Phases 16-18). Not a composition root — plain `IDisposable` classes a game constructs itself, never registered by `AnalyticsBootstrapper`. References `GameFramework.Analytics` + `GameFramework.GameFlow`/`.UI.Navigation`/`.Tutorials`/`.Monetization`/`.RemoteConfig`/`.Notifications`/`.DeepLinks` — the one assembly in the framework whose entire purpose is cross-system event forwarding, deliberately kept separate from the core `GameFramework.Analytics` assembly so a game that wants none of this instrumentation never pulls those references in. |
| `GameFramework.RemoteConfig` | `IRemoteConfigService` (typed Get*, local defaults, atomic validated snapshot activation, cache with stale/expiration policy), `IFeatureFlagService` (thin typed-bool layer with diffed change notifications), `ILiveOpsService` (UTC start/end event schedules with an optional per-event remote override, pluggable `ILiveOpsClock`) (Phase 17). Composition root: `RemoteConfigBootstrapper`. Sibling of `PlayerSystems`/`Gameplay`/`Performance`/`Progression`/`GameFlow`/`Tutorials`/`Cameras`/`PlayerData`/`Platform`/`Analytics` — references only Core/Runtime + `GameFramework.Performance` (hard, application-resume lifecycle event type only). Ships only a deterministic NoOp/Mock provider — no remote config SDK is installed in this project. |
| `GameFramework.Notifications` | `INotificationService` (game-supplied `NotificationId`s that replace-on-duplicate-schedule, provider-independent `NotificationPermissionStatus`, cold/warm/hot notification-open reporting, schedule-time localization resolution) (Phase 18). Composition root: `NotificationsBootstrapper`. Sibling — references only Core/Runtime + `GameFramework.Performance` (hard, resume lifecycle) + `GameFramework.Localization` (hard, schedule-time content resolution). Ships only a deterministic NoOp/Mock provider — no notification SDK is installed in this project. |
| `GameFramework.DeepLinks` | `IDeepLinkService` (cold/warm/hot URI capture via `Application.absoluteURL`/`deepLinkActivated`, parsing independent of routing, a priority-ordered `IDeepLinkHandler` chain, a single-slot deferred-link gate, documented duplicate-URI protection) (Phase 18). Composition root: `DeepLinksBootstrapper`. Sibling — references only Core/Runtime + `GameFramework.Performance`. Deliberately does not reference `GameFramework.UI.Navigation` - see `GameFramework.Notifications.Integration`. |
| `GameFramework.Notifications.Integration` | Optional glue: `NavigationDeepLinkHandler` (a ready-made `IDeepLinkHandler` mapping a route pattern to `INavigationService.Navigate`) and `NotificationDeepLinkBridge` (a tapped notification's payload → the same `IDeepLinkService.Process` pipeline a real inbound link goes through) (Phase 18). Not a composition root — never registered by any bootstrapper. References `GameFramework.Notifications` + `.DeepLinks` + `.UI.Navigation`. |
| `GameFramework.Editor` | Editor-only; localization table validation, Gameplay config validation, Quest content validation, Tutorial content validation, Feedback content validation, Camera Configuration validation, UI Navigation Catalog validation, Player Data diagnostics, Platform diagnostics, Monetization configuration validation/diagnostics menu items, Analytics diagnostics/event-simulator tooling, Remote Config/Live Ops content validation/diagnostics menu items, and a Notifications/DeepLinks UI Toolkit debug window + diagnostics menu item. |
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
- **Monetization** (`IAdsService`/`IPurchaseService`/`IEntitlementService`, Phase 15) — three
  separate interfaces, not one giant service: an entitlement is what the player currently owns,
  distinct from the purchase transaction that granted it. `IAdsService` covers Banner/Interstitial/
  Rewarded via a stable `AdPlacementId`, mechanism-level `CanShow` policy (cooldown/session-limit/
  entitlement suppression, all authored per placement, never hard-coded), and a rewarded ad that only
  ever reports `RewardedAdResult.RewardEarned` after the provider confirms it — it never grants a
  reward itself; that stays the game's own code, optionally via the small opt-in
  `AdPlacementRewardBridge`. `IPurchaseService` covers Consumable/NonConsumable/Subscription products
  via a stable `ProductId`, with idempotent transaction processing (a persisted processed-transaction
  set survives an application restart) and *automatic* entitlement/reward granting on a completed or
  restored purchase (unlike Ads, a product's grant is fixed authored data, so `PurchaseService` calls
  `Rewards.IRewardService`/`IEntitlementService` directly). `IEntitlementService` persists directly
  through `IPersistenceService` (like `RewardService`/`UnlockService`, not a `PlayerData` profile
  section) and reconciles with a store restore via `SyncFromProvider` without revoking anything the
  restore simply didn't mention. No ad or IAP SDK is installed in this project — only
  `Providers.Mock.MockAdProvider`/`MockPurchaseProvider` exist behind the `IAdProvider`/
  `IPurchaseProvider` seam; a client-side purchase check is explicitly not treated as secure
  validation — see Framework.md's Phase 15 section for the full design and provider status.
- **Analytics & Diagnostics** (`IAnalyticsService`/`IDiagnosticsService`, Phase 16) — two separate
  interfaces, not one giant service: Analytics answers "what happened in the game," Diagnostics
  answers "what went wrong technically." `Track`/`TrackScreenView`/`SetUserProperty` never throw —
  an invalid event name or unsupported parameter type is logged and dropped/sanitized, and every
  provider call is wrapped in a try/catch so a provider exception can never break gameplay. Consent
  (`Unknown`/`Granted`/`Denied`) gates dispatch: `Denied` always drops immediately, `Unknown` buffers
  (bounded, drop-oldest) or drops per the configured `ConsentPolicy`, and denying consent after
  buffering clears the buffer without ever sending it. Anonymous identity is an application-generated
  GUID persisted directly via `IPersistenceService` (not retrofitted onto `PlayerData`). Session
  lifecycle is built on Phase 14's application-lifecycle events using wall-clock `DateTime.UtcNow`
  (not `Time.realtimeSinceStartup`, which doesn't advance while a mobile process is suspended).
  `IDiagnosticsService` adds a bounded breadcrumb ring buffer, persistent context/tags, and
  `RecordException`/`RecordError`, forwarding to an `ICrashReportingProvider` with a *structural*
  no-recursive-diagnostics guarantee (a provider failure is logged only through the plain logger,
  never re-reported through itself) — `UnhandledExceptionDriver` is the one place in the framework
  that reads `Application.logMessageReceived`. No analytics/crash SDK is installed in this project —
  only `NoOpAnalyticsProvider`/`NoOpCrashReportingProvider` (the `AnalyticsBootstrapper` defaults) and
  `Providers.Mock.MockAnalyticsProvider`/`Diagnostics.Mock.MockCrashReportingProvider` (opt-in,
  Editor/testing only) exist behind the `IAnalyticsProvider`/`ICrashReportingProvider` seams. Four
  opt-in bridges in the separate `GameFramework.Analytics.Integration` assembly forward GameFlow/UI
  Navigation/Tutorial/Monetization events into Analytics — never registered automatically, since
  Analytics observes the game and never controls it; see Framework.md's Phase 16 section for the
  full design and provider status.
- **Remote Config + Live Operations** (`IRemoteConfigService`/`IFeatureFlagService`/`ILiveOpsService`,
  Phase 17) — configuration, not authority: every declared `RemoteConfigDefinition` has a safe local
  default, and every candidate snapshot (a cached payload or a fresh fetch, always rebuilt from a
  fresh copy of the defaults rather than layered onto whatever was previously active) is fully
  validated — type match, numeric range, schema version — before it is ever activated; a single
  invalid key rejects the whole snapshot rather than partially applying it. A fetch generation counter
  makes a slow provider's callback arriving after the service's own fetch-timeout already resolved
  that attempt a safe no-op. `IFeatureFlagService` is a thin typed-bool wrapper that additionally
  diffs each declared flag across an activation to raise a change notification only when its resolved
  value actually changes. `ILiveOpsService` evaluates a flat, UTC-only event schedule
  (Upcoming/Active/Ended/Disabled/Invalid) against a pluggable `ILiveOpsClock` (local time adjusted by
  a provider-supplied server-time offset when available, explicitly local-time-only otherwise), with
  three reserved per-event remote config keys (`liveops.{id}.enabled`/`start_utc`/`end_utc`) letting a
  backend move or disable an authored event without a new build; a low-frequency `ITimerService` poll
  (never a per-frame `Update`) detects a pure time-based transition between configuration activations.
  No Firebase Remote Config/Unity Remote Config/PlayFab SDK is installed in this project — only
  `NoOpRemoteConfigProvider` (the `RemoteConfigBootstrapper` default) and
  `Providers.Mock.MockRemoteConfigProvider` (opt-in, Editor/testing only) exist behind the
  `IRemoteConfigProvider` seam. A fifth opt-in bridge in `GameFramework.Analytics.Integration`
  (`RemoteConfigAnalyticsIntegration`) forwards fetch/activation/flag/live-event events into Analytics
  — never registered automatically; see Framework.md's Phase 17 section for the full design and
  provider status.
- **Notifications & Deep Links** (`INotificationService`/`IDeepLinkService`, Phase 18) — a
  game-supplied `NotificationId` is the cancel/replace key (never auto-generated, since an
  uncontrolled id would make cancellation impossible); scheduling an already-scheduled id replaces it
  rather than erroring. Every `Schedule`/`Cancel` call is synchronous, not `Task`-based — the same
  reasoning `IPersistenceService` already gives for its own synchronous Save/Load. A notification's
  title/body can be a raw string or a Phase 3 localization key, resolved once at `Schedule` time (not
  display time) since the OS may show a local notification while the game process isn't running at
  all. `IDeepLinkService` captures cold/warm/hot links via `Application.absoluteURL`/
  `deepLinkActivated` (genuinely cross-platform Unity APIs — no native plugin needed), keeps parsing
  (`DeepLinkParser`, built on `System.Uri`) strictly separate from routing (a priority-ordered
  `IDeepLinkHandler` chain a game populates), holds at most one link in a `SetReady()`-gated deferred
  slot for a cold start before UI/GameFlow is ready, and dedupes an identical-to-the-immediately-
  previous raw URI (a documented, deterministic strategy — not a time-boxed cache) against repeated OS
  redelivery. Neither core assembly references `UI.Navigation` — routing to a screen is an opt-in
  `NavigationDeepLinkHandler`/`NotificationDeepLinkBridge` pair in the separate
  `GameFramework.Notifications.Integration` assembly, so a game not using UI Navigation still gets
  full Notifications/DeepLinks functionality. No notification SDK (Unity Mobile Notifications/
  Firebase/OneSignal) is installed in this project — only `NoOpNotificationProvider` (the
  `NotificationsBootstrapper` default) and `Providers.Mock.MockNotificationProvider` (opt-in, Editor/
  testing only) exist behind the `INotificationProvider` seam; deep links need no such seam at all,
  since Unity's own capture APIs are already cross-platform. A sixth opt-in
  `Analytics.Integration` bridge (`NotificationsAnalyticsIntegration`) forwards both phases' events
  into Analytics; see Framework.md's Phase 18 section for the full design and provider status.

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
- Phase 15: `GameFramework.Monetization.Tests` (EditMode) reuses the same `TestRegistryFactory`/
  `TestDefinitions` pattern, plus two fully-controllable test doubles (`FakeAdProvider`/
  `FakePurchaseProvider`, distinct from the shipped `MockAdProvider`/`MockPurchaseProvider`, which get
  their own `MockProviderTests` coverage) so multi-step scenarios (a fullscreen ad already showing, a
  duplicate transaction callback, a deferred purchase resolving later) are deterministic. Covers Ads
  init/load/show/hide, cooldown/session-limit policy, entitlement suppression (including "Rewarded is
  not suppressed unless explicitly configured"), rewarded success/failure/closed-without-reward/
  already-showing, load-retry backoff, and pause/resume banner hide/reshow; Purchases product
  loading, success/cancelled/failed/pending-then-resolved, already-owned short-circuiting the
  provider, validation failure, duplicate-transaction idempotency, and restore idempotency;
  Entitlements grant/revoke/expiration/sync/persistence round-trip; and integration flows (Remove Ads
  suppressing Banner/Interstitial but not Rewarded end-to-end, a consumable purchase's reward
  surviving a simulated application restart without double-granting, and `AdPlacementRewardBridge`
  claiming its mapped reward). 53/53 Phase 15 tests pass; the full project suite (787 EditMode + 171
  PlayMode tests) was re-run after this phase with zero regressions.
- Phase 16: `GameFramework.Analytics.Tests` (EditMode) reuses the same `TestRegistryFactory` pattern
  (only `EventService`/`PersistenceService` — neither service needs a fake `ITimeService`, since
  session timing uses wall-clock `DateTime.UtcNow` directly) plus a reflection-based `TestDefinitions`
  helper and two fully-controllable test doubles (`FakeAnalyticsProvider`/`FakeCrashReportingProvider`,
  distinct from the shipped `MockAnalyticsProvider`/`MockCrashReportingProvider`, covered in their own
  right by `MockProviderTests`). Covers event validation/sanitization (invalid names, parameter-count/
  key-length/value-length limits, unsupported parameter types), disabled-analytics no-op, all three
  consent transitions (buffer-then-flush, drop-until-granted, denial clearing a buffer), provider
  failure isolation, anonymous identity persisting across two independent service instances sharing
  one `InMemoryPersistenceStorage` (simulating an application restart) plus `ResetIdentity`, screen-view
  tracking, user properties, and session start/continue/rollover driven through real
  `Performance.Mobile.ApplicationPausedEvent`/`ApplicationResumedEvent` publishes; breadcrumb bounding,
  context/tags round-tripping into a report, exception/error recording gated by `Enabled`, and —
  verified directly — a provider failure during `Report` never triggers a second report (no recursive
  diagnostics); and `Analytics.Integration`'s four bridges forwarding a hand-published GameFlow/UI
  Navigation/Tutorial/Monetization event into a normalized `Track` call, plus `Dispose` correctly
  unsubscribing. 36/36 Phase 16 tests pass.
- Phase 17: `GameFramework.RemoteConfig.Tests` (EditMode) reuses the same `TestRegistryFactory`/
  reflection-based `TestDefinitions` pattern, plus a fully-controllable `FakeRemoteConfigProvider`
  (distinct from the shipped `MockRemoteConfigProvider`, covered in its own right by
  `MockProviderTests`) so multi-step scenarios (a suppressed callback simulating a hung fetch, an
  exact invalid/out-of-range/schema-mismatched payload) are deterministic. Covers declared-default
  resolution, successful fetch/activation with published events, atomicity (an invalid key rejects a
  payload's otherwise-valid keys too), range/schema-version rejection, provider-unavailable fallback,
  `AlreadyInProgress` re-entrancy, a late provider callback after the service's own fetch-timeout
  already resolved being ignored, a key a later fetch omits reverting to its default rather than
  lingering; cache fresh/stale/expired/corrupt(unsupported schema)/missing/cross-environment behavior
  via two independent `RemoteConfigService` instances sharing one `InMemoryPersistenceStorage`
  (simulating an application restart); `FeatureFlagService` default resolution and change-notification
  diffing (only on an actual value change); `LiveOpsService` Upcoming/Active/Ended state resolution
  and start/end transition events driven through a `ManualLiveOpsClock`, plus a per-event remote
  override disabling an authored event; and the shipped `MockRemoteConfigProvider`'s four simulation
  modes. 30/30 Phase 17 tests pass. The full project suite (853 EditMode + 171 PlayMode tests,
  measured directly via the Unity Test Runner after this phase) passed with zero regressions.
- Phase 18: `GameFramework.DeepLinks.Tests` (EditMode, 28 tests) covers `DeepLinkParser` (custom-
  scheme host-into-path normalization, https host/path separation, multiple/encoded query parameters,
  fragments, malformed/empty input), `DeepLinkRoutePattern` (literal/path-parameter/case-insensitive/
  segment-count-mismatch matching), and `DeepLinkService` (deferred-until-ready, exactly-once dispatch
  on `SetReady`, priority ordering, fall-through on `NotApplicable`, `NoHandlerFound`, a handler
  exception treated as `Failed` without propagating, malformed-URI rejection, identical-consecutive-
  URI dedup vs. a genuinely different URI processing normally, and `DeepLinkReceived`/`Handled`/
  `Rejected` events). `GameFramework.Notifications.Tests` (EditMode, 38 tests) reuses the same
  `TestRegistryFactory` pattern plus `FakeLocalizationService`/`FakeNavigationService`/
  `FakeDeepLinkHandler` (avoiding a real `LocalizationConfigAsset`/UGUI screen stack, which would
  otherwise require PlayMode - see `UI.Navigation.Tests`'s own remarks) and the shipped
  `MockNotificationProvider` (covered separately by `MockNotificationProviderTests`). Covers
  `NotificationService` schedule validation (past-time and empty-id rejection, a null request
  throwing), permission gating (`PermissionDenied`/`Unsupported`), duplicate-id replace, cancel/
  cancel-all, schedule-time localization resolution (including the no-`ILocalizationService`-
  registered fallback to raw key text), a cold-start launch notification raising
  `NotificationOpened` during `Initialize`, Editor/QA simulation always marked
  `WasSimulated = true`, and permission request/changed events; `NavigationDeepLinkHandler` (route
  matching, path-parameter pass-through, a navigation-guard rejection reported as `Failed`);
  `NotificationDeepLinkBridge` (payload → URI construction including query-parameter encoding, no-
  route no-op, `Dispose` unsubscribing); and two explicit end-to-end integration scenarios required by
  this phase's own brief - `FullChainIntegrationTests` (schedule → simulated tap → payload → deep
  link → `INavigationService.Navigate` call, with path/query parameters intact) and
  `LifecyclePendingDeepLinkTests` (a link arriving before `SetReady` never navigates early; the
  readiness signal - standing in for a game's own GameFlow/UI-ready callback - dispatches the held
  link to Navigation exactly once). 66/66 Phase 18 tests pass. The full project suite (919 EditMode +
  171 PlayMode tests, measured directly via the Unity Test Runner after this phase) passed with zero
  regressions.
- Phase 19: 128 new EditMode tests:
  - `GameFramework.Runtime.Tests`:
    - `PersistenceIntegrityTests`: checksum, truncation, legacy envelope, future version, migration
      missing/throwing/null/non-advancing, and the `Unreadable` status.
    - `FilePersistenceStorageAtomicityTests`: interrupted-replace recovery, leftover temp files, and
      unsafe keys.
    - `SensitiveDataRedactorTests`
    - `DevelopmentProviderGuardTests`: includes the release-build refusal path.
    - `BootstrapFailureIsolationTests`
  - `RecoveryHardeningTests` (PlayerData)
  - `MonetizationHardeningTests`
  - `RemoteConfigHardeningTests`
  - `DeepLinkValidationTests`
  - `PayloadValidationTests` (Notifications)
  - `DiagnosticsRedactionTests` (Analytics)
  - `FrameworkPreflightTests`: in the new `GameFramework.Editor.Tests` assembly.

  The full suite after this phase, measured via the Unity Test Runner, was 1047 EditMode + 171
  PlayMode tests, all passing, with no regressions.
- Phase 20: 118 new EditMode tests in `GameFramework.Editor.Tests` (`Preflight/Build/`):
  - `VersioningAndNamingTests`: versions, build numbers, artifact names, and defines.
  - `CommandLineTests`: parsing, assertions, Unity target aliases, and exit codes.
  - `BuildValidationTests`: report promotion and text, resolution, each validator, the scanner, the
    pipeline's validate-only behavior, and custom/throwing validators.
  - `MetadataTests`: metadata, manifest, UTC, Git, signing-variable names, and debug outputs.

  The measured full suite (1165 EditMode + 171 PlayMode) passes. Real Windows64 and Android
  development builds were executed through the pipeline; iOS was validated only.

## Packages / Dependencies

- Universal RP 14.0.12, TextMeshPro 3.0.9, UGUI 1.0.0, Input System 1.14.2 (used directly by
  `GameFramework.Input` for New Input System bindings), Device Simulator, 2D feature set, standard
  Unity modules.
- Cinemachine 2.10.7 — used only by the optional `GameFramework.Cameras.Cinemachine`/
  `.Cinemachine.Editor` assemblies (Phase 11's Cinemachine integration); every other assembly in the
  framework, including `GameFramework.Cameras` itself, has no reference to it and works without it
  installed.
- `com.coplaydev.unity-mcp` — Unity MCP integration for AI-tool-driven editor control.
- No ad network SDK (e.g. Google Mobile Ads) or `com.unity.purchasing` (Unity IAP) is installed —
  Phase 15's `GameFramework.Monetization` ships only the `IAdProvider`/`IPurchaseProvider` seam and
  deterministic mock providers behind it; see `Framework.md`'s Phase 15 section, "Provider status."
- No analytics SDK (Firebase Analytics, GameAnalytics, Unity Analytics, ...) or crash-reporting SDK
  (Crashlytics, Sentry, ...) is installed — Phase 16's `GameFramework.Analytics` ships only the
  `IAnalyticsProvider`/`ICrashReportingProvider` seams and deterministic No-Op/Mock providers behind
  them; see `Framework.md`'s Phase 16 section, "Provider status."
- No remote config SDK (Firebase Remote Config, Unity Remote Config, PlayFab, ...) is installed —
  Phase 17's `GameFramework.RemoteConfig` ships only the `IRemoteConfigProvider` seam and a
  deterministic NoOp/Mock provider behind it; see `Framework.md`'s Phase 17 section, "Provider status."
- No notification SDK (`com.unity.mobile.notifications`, Firebase Cloud Messaging, OneSignal, ...) is
  installed — Phase 18's `GameFramework.Notifications` ships only the `INotificationProvider` seam and
  a deterministic NoOp/Mock provider behind it; see `Framework.md`'s Phase 18 section, "Provider
  status." `GameFramework.DeepLinks` needs no such seam - cold/warm/hot link capture is built entirely
  on `Application.absoluteURL`/`Application.deepLinkActivated`, genuinely cross-platform Unity engine
  APIs with no package/native-plugin dependency.

## Folder Layout

```text
Assets/GameFramework/
├── Runtime/            One folder + .asmdef per system (Core, Bootstrap, Services, Diagnostics,
│                        State, SceneManagement, Time, Timers, Events, Persistence, Settings,
│                        Input, Localization, Audio, Feedback, UI, PlayerSystems, Gameplay,
│                        Performance, Progression, Unlocks, Rewards, Quests, GameFlow, Tutorials,
│                        Presentation, Cameras, PlayerData, Platform, Monetization); Cameras/Integration/
│                        Cinemachine/ holds the optional GameFramework.Cameras.Cinemachine assembly;
│                        UI/Navigation/ holds the GameFramework.UI.Navigation assembly (Phase 12);
│                        PlayerData/ holds the GameFramework.PlayerData assembly (Phase 13); Platform/
│                        holds the GameFramework.Platform assembly (Phase 14), with Android/ isolating
│                        the one AndroidJavaObject boundary; Monetization/ holds the
│                        GameFramework.Monetization assembly (Phase 15), with Ads/, Purchases/,
│                        Entitlements/, Providers/Mock/, and Integration/ subfolders; Analytics/ holds
│                        the GameFramework.Analytics assembly (Phase 16), with Core/, Consent/,
│                        Configuration/, Events/, Providers/(Mock/), Diagnostics/(Mock/), and
│                        Integration/ (the separate, optional GameFramework.Analytics.Integration
│                        assembly, whose fifth/sixth bridges, RemoteConfigAnalyticsIntegration/
│                        NotificationsAnalyticsIntegration, are Phase 17's/Phase 18's) subfolders;
│                        RemoteConfig/ holds the GameFramework.RemoteConfig assembly (Phase 17), with
│                        Providers/(Mock/), FeatureFlags/, and LiveOps/ subfolders; Notifications/
│                        holds the GameFramework.Notifications assembly (Phase 18), with Providers/
│                        (Mock/) and Integration/ (the separate, optional
│                        GameFramework.Notifications.Integration assembly) subfolders; DeepLinks/
│                        holds the GameFramework.DeepLinks assembly (Phase 18)
├── Editor/              Editor-only tooling (Localization, Gameplay config, Quest content,
│                        Tutorial content, Feedback content, Camera Configuration, UI Navigation
│                        Catalog, Player Data diagnostics, Platform diagnostics, Monetization
│                        configuration validation/diagnostics, Analytics diagnostics/event-
│                        simulator tooling, Remote Config/Live Ops content validation/diagnostics, and
│                        a Notifications/DeepLinks UI Toolkit debug window + diagnostics menu item);
│                        Cameras/Cinemachine/ holds the optional .Cinemachine.Editor assembly
├── Samples/              Phase0Demo/ … Phase4Demo/ (one per phase), Phase5Benchmark/,
│                         Phase6Demo/, Phase7Demo/, Phase9Demo/, Phase10Demo/, Phase11Demo/, Phase12Demo/
│                         (each with authored Content/ ScriptableObject/prefab assets; Phase11Demo/
│                         additionally has a second scene, Phase11CinemachineDemo.unity, for the
│                         Cinemachine integration) — Phase 8, Phase 13, Phase 14, Phase 15, Phase 16,
│                         Phase 17, and Phase 18 intentionally have no sample yet (Phase 8: see
│                         Framework.md's Phase 8 section for why; Phase 13/14/15/16/17/18: each
│                         phase's infrastructure is fully exercised by its own test suite instead)
│                         (Phase 19 adds Runtime/Security/ - redaction, build environment,
│                         development-provider guard - and Editor/Security/ - Production Preflight)
│                         (Phase 20 adds Editor/Build/ - build pipeline, validators, CLI, UI Toolkit
│                         window - and Samples/BuildProfiles/ - generic sample profiles)
├── Tests/               Editor/ (EditMode) and Runtime/ (PlayMode) tests, mirroring Runtime/
└── Documentation/       Framework.md — full authoritative reference
```

## Where to Look Next

- Full architecture/API reference: `Assets/GameFramework/Documentation/Framework.md`
- Engineering rules and workflow this project's changes must follow: `CLAUDE.md`
