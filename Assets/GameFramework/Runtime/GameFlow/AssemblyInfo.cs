using System.Runtime.CompilerServices;

// Phase 8 flow/session test assembly reuses the same BuildInitializedRegistry pattern established
// since Phase 3 (register a fake ITimeService/a real EventService, MarkInitialized, call Initialize)
// to test GameFlowService in isolation, and needs access to internal members
// (LevelFlowStateMachine, GameplaySession's mutation methods) that are deliberately not part of the
// public API surface.
[assembly: InternalsVisibleTo("GameFramework.GameFlow.Tests")]
