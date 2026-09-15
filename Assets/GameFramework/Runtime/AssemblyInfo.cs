using System.Runtime.CompilerServices;

// Lets the test assemblies exercise ServiceRegistry's internal orchestration members
// (RegistrationOrder, GetInstance, MarkInitialized, Clear) directly, so bootstrap-style
// sequencing can be unit tested without making those members part of the public API surface.
[assembly: InternalsVisibleTo("GameFramework.Runtime.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Runtime.Tests.Runtime")]
