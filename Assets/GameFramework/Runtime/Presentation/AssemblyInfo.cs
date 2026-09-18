using System.Runtime.CompilerServices;

// Phase 10 presentation test assembly reuses the same BuildInitializedRegistry pattern established
// since Phase 3 (register fakes, a real EventService, MarkInitialized, call Initialize) to test
// PresentationService in isolation, and needs access to internal members (CameraShakeState,
// ScreenEffectController, TimeFeedbackController) that are deliberately not part of the public API.
[assembly: InternalsVisibleTo("GameFramework.Presentation.Tests")]
