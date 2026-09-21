using System.Runtime.CompilerServices;

// Phase 11 camera test assemblies reuse the same BuildInitializedRegistry pattern established
// since Phase 3 (register fakes, a real EventService, MarkInitialized, call Initialize) and need
// access to internal members (the pure pose/mode/constraint/transition classes) that are
// deliberately not part of the public API - mirrors GameFramework.Presentation's AssemblyInfo.cs.
[assembly: InternalsVisibleTo("GameFramework.Cameras.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Cameras.Tests.Runtime")]
