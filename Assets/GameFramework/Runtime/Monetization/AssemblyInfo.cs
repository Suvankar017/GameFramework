using System.Runtime.CompilerServices;

// Tests need access to internal simulation hooks on the mock providers (deterministic outcome
// injection) that are deliberately not part of the public IAdProvider/IPurchaseProvider surface -
// the same pattern PlayerData/Platform's test assemblies already use.
[assembly: InternalsVisibleTo("GameFramework.Monetization.Tests")]
