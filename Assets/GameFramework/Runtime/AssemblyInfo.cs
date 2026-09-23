using System.Runtime.CompilerServices;

// Lets the test assemblies exercise ServiceRegistry's internal orchestration members
// (RegistrationOrder, GetInstance, MarkInitialized, Clear) directly, so bootstrap-style
// sequencing can be unit tested without making those members part of the public API surface.
[assembly: InternalsVisibleTo("GameFramework.Runtime.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Runtime.Tests.Runtime")]

// Phase 3 player-systems test assemblies reuse the same BuildInitializedRegistry pattern
// (register a dependency, MarkInitialized it, call its Initialize) to construct a minimal,
// already-initialized ServiceRegistry for testing a service in isolation.
[assembly: InternalsVisibleTo("GameFramework.Localization.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Audio.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Audio.Tests.Runtime")]
[assembly: InternalsVisibleTo("GameFramework.Feedback.Tests")]
[assembly: InternalsVisibleTo("GameFramework.UI.Tests.Runtime")]

// Phase 4 gameplay test assemblies reuse the same pattern to build a minimal, already-initialized
// ServiceRegistry (e.g. with a fake ITimeService) for testing GameplayService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Gameplay.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Gameplay.Tests.Runtime")]

// Phase 5 performance test assembly reuses the same pattern to build a minimal,
// already-initialized ServiceRegistry (e.g. with a fake ITimeService) for testing TickService and
// PerformanceMonitorService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Performance.Tests")]

// Phase 6 progression test assemblies reuse the same pattern to build a minimal,
// already-initialized ServiceRegistry (a real EventService/PersistenceService-over-
// InMemoryPersistenceStorage plus a fake ITimeService) for testing Economy/Inventory/Experience/
// Unlock/Reward services in isolation.
[assembly: InternalsVisibleTo("GameFramework.Progression.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Unlocks.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Rewards.Tests")]

// Phase 7 quest/achievement/milestone test assembly reuses the same pattern to build a minimal,
// already-initialized ServiceRegistry (a real EventService/PersistenceService-over-
// InMemoryPersistenceStorage plus a fake ITimeService) for testing Statistics/Quest/Achievement/
// Milestone services in isolation.
[assembly: InternalsVisibleTo("GameFramework.Quests.Tests")]

// Phase 8 game-flow test assembly reuses the same pattern to build a minimal, already-initialized
// ServiceRegistry (fake ITimeService/ISceneService, a real EventService/PersistenceService-over-
// InMemoryPersistenceStorage) for testing GameFlowService in isolation.
[assembly: InternalsVisibleTo("GameFramework.GameFlow.Tests")]

// Phase 9 tutorial test assembly reuses the same pattern to build a minimal, already-initialized
// ServiceRegistry (fake ITimeService/IInputService, a real EventService/PersistenceService-over-
// InMemoryPersistenceStorage) for testing TutorialService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Tutorials.Tests")]

// Phase 10 presentation test assemblies reuse the same pattern to build a minimal, already-
// initialized ServiceRegistry (fake ITimeService, a real EventService/SettingsService-over-
// InMemoryPersistenceStorage) for testing PresentationService in isolation. The PlayMode variant
// additionally needs a real PoolService/GameObjectPool and Object.Destroy, which Unity only allows
// in Play Mode - see PresentationServiceRuntimeTests's remarks.
[assembly: InternalsVisibleTo("GameFramework.Presentation.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Presentation.Tests.Runtime")]

// Phase 11 camera test assemblies reuse the same pattern to build a minimal, already-initialized
// ServiceRegistry (a real EventService/SettingsService-over-InMemoryPersistenceStorage) for testing
// CameraService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Cameras.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Cameras.Tests.Runtime")]

// Phase 11's Cinemachine integration test assemblies reuse the same pattern to build a minimal,
// already-initialized ServiceRegistry (a real EventService) for testing CinemachineCameraBackend in
// isolation.
[assembly: InternalsVisibleTo("GameFramework.Cameras.Cinemachine.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Cameras.Cinemachine.Tests.Runtime")]

// Phase 12 UI navigation test assembly reuses the same pattern to build a minimal,
// already-initialized ServiceRegistry (a real UIService/EventService, fake IInputService/
// IGameFlowService) for testing NavigationService in isolation.
[assembly: InternalsVisibleTo("GameFramework.UI.Navigation.Tests.Runtime")]

// Phase 13 player-data test assemblies reuse the same pattern to build a minimal,
// already-initialized ServiceRegistry (fake ITimeService, a real EventService/TimerService/
// PersistenceService-over-InMemoryPersistenceStorage) for testing PlayerProfileService in isolation.
[assembly: InternalsVisibleTo("GameFramework.PlayerData.Tests")]
[assembly: InternalsVisibleTo("GameFramework.PlayerData.Tests.Runtime")]

// Phase 15 monetization test assembly reuses the same pattern to build a minimal, already-
// initialized ServiceRegistry (fake ITimeService, a real EventService/TimerService/
// PersistenceService-over-InMemoryPersistenceStorage) for testing AdsService/PurchaseService/
// EntitlementService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Monetization.Tests")]

// Phase 16 analytics test assembly reuses the same pattern to build a minimal, already-initialized
// ServiceRegistry (a real EventService/PersistenceService-over-InMemoryPersistenceStorage - neither
// AnalyticsService nor DiagnosticsService needs a fake ITimeService, since session timing uses
// wall-clock DateTime.UtcNow directly) for testing AnalyticsService/DiagnosticsService in isolation.
[assembly: InternalsVisibleTo("GameFramework.Analytics.Tests")]

// Phase 17 remote config test assembly reuses the same pattern to build a minimal, already-
// initialized ServiceRegistry (fake ITimeService, a real EventService/TimerService/
// PersistenceService-over-InMemoryPersistenceStorage) for testing RemoteConfigService/
// FeatureFlagService/LiveOpsService in isolation.
[assembly: InternalsVisibleTo("GameFramework.RemoteConfig.Tests")]
