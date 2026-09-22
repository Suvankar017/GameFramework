using System.Runtime.CompilerServices;

// Tests need access to internal simulation hooks (deterministic failure injection on the Fake
// providers) that are deliberately not part of the public IAnalyticsProvider/ICrashReportingProvider
// surface - the same pattern Monetization/PlayerData/Platform's test assemblies already use.
[assembly: InternalsVisibleTo("GameFramework.Analytics.Tests")]
