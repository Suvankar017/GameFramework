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
