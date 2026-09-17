using System.Runtime.CompilerServices;

// Lets GameFramework.Performance.Tests exercise TickService's internal TickFixed/TickLate methods
// directly (the same two phases GameBootstrapper's IUpdatableService mechanism doesn't cover),
// mirroring GameFramework.Runtime's own AssemblyInfo.cs pattern for its test assemblies.
[assembly: InternalsVisibleTo("GameFramework.Performance.Tests")]
