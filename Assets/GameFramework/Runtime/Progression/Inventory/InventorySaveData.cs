using System;
using System.Collections.Generic;

namespace GameFramework.Progression.Inventory
{
    /// <summary>Persisted shape for <see cref="InventoryService"/> — parallel lists for the same
    /// <see cref="JsonUtility"/>-cannot-serialize-a-Dictionary reason as <c>EconomySaveData</c>.</summary>
    [Serializable]
    internal sealed class InventorySaveData
    {
        public List<string> ItemIds = new List<string>();
        public List<int> Quantities = new List<int>();
    }
}
