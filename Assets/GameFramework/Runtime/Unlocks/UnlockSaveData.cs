using System;
using System.Collections.Generic;

namespace GameFramework.Unlocks
{
    [Serializable]
    internal sealed class UnlockSaveData
    {
        public List<string> UnlockedIds = new List<string>();
    }
}
