using System.Runtime.CompilerServices;

// Lets the Audio test assembly exercise AudioService's internal voice pool/limiting logic and the
// pure AudioCueSampler selection helper directly.
[assembly: InternalsVisibleTo("GameFramework.Audio.Tests")]
