using System.Runtime.CompilerServices;

// Lets the Input test assembly construct an InputService with an injected fake sampler and
// exercise its action-resolution logic directly, without needing a live device.
[assembly: InternalsVisibleTo("GameFramework.Input.Tests")]
