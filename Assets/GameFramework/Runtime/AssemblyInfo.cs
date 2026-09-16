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
