using System.Runtime.CompilerServices;

// Phase 9 tutorial test assembly reuses the same BuildInitializedRegistry pattern established
// since Phase 3 (register fake ITimeService/IInputService, a real EventService, MarkInitialized,
// call Initialize) to test TutorialService in isolation, and needs access to internal members
// (TutorialLifecycleStateMachine, TutorialService's persistence internals) that are deliberately
// not part of the public API surface.
[assembly: InternalsVisibleTo("GameFramework.Tutorials.Tests")]
