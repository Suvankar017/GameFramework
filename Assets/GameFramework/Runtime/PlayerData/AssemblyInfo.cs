using System.Runtime.CompilerServices;

// Phase 13 tests need access to internal members (PlayerProfile's mutation hooks,
// PlayerProfileService's storage-key builders, PlayerDataLifecycleDriver) that are deliberately
// not part of the public API surface - the same pattern every other phase's test assembly uses.
[assembly: InternalsVisibleTo("GameFramework.PlayerData.Tests")]
[assembly: InternalsVisibleTo("GameFramework.PlayerData.Tests.Runtime")]
