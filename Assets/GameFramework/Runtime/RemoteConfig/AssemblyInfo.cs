using System.Runtime.CompilerServices;

// Tests need access to internal simulation hooks on the mock provider (deterministic outcome
// injection) that are deliberately not part of the public IRemoteConfigProvider surface - the same
// pattern Monetization/Platform's test assemblies already use.
[assembly: InternalsVisibleTo("GameFramework.RemoteConfig.Tests")]
