using System;
using System.Collections.Generic;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Persisted list of every profile id that exists - stored under a single well-known key (see
    /// <see cref="PlayerProfileService"/>'s remarks). Required because
    /// <see cref="Runtime.Persistence.IPersistenceStorage"/> has no "list all keys" capability -
    /// this is the only way <see cref="IPlayerProfileService.ListProfiles"/> can work without
    /// scanning the filesystem directly (which would only work for <c>FilePersistenceStorage</c>,
    /// not <c>InMemoryPersistenceStorage</c> or a future custom backend).
    /// </summary>
    [Serializable]
    internal sealed class PlayerProfileIndexData
    {
        public List<string> ProfileIds = new List<string>();
    }
}
