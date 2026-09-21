using System.Runtime.CompilerServices;

// Lets the Cinemachine integration's own test assemblies exercise its internal helpers directly,
// the same InternalsVisibleTo pattern every other phase's test assembly already uses (see
// GameFramework.Cameras' own AssemblyInfo.cs).
[assembly: InternalsVisibleTo("GameFramework.Cameras.Cinemachine.Tests")]
[assembly: InternalsVisibleTo("GameFramework.Cameras.Cinemachine.Tests.Runtime")]
