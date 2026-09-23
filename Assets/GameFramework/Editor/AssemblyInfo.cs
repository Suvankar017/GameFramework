using System.Runtime.CompilerServices;

// Lets the Editor test assembly set FrameworkBuildProfile's serialized fields through their internal
// setters (instead of SerializedObject string paths) when building in-memory profiles for tests.
[assembly: InternalsVisibleTo("GameFramework.Editor.Tests")]
