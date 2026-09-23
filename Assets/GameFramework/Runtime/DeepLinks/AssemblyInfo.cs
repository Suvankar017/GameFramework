using System.Runtime.CompilerServices;

// Tests need access to internal simulation hooks (deterministic capture-driver injection) that are
// deliberately not part of the public IDeepLinkService surface - the same pattern
// Monetization/RemoteConfig/Notifications' test assemblies already use.
[assembly: InternalsVisibleTo("GameFramework.DeepLinks.Tests")]
