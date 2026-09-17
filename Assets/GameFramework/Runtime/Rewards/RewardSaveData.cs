using System;
using System.Collections.Generic;

namespace GameFramework.Rewards
{
    [Serializable]
    internal sealed class RewardSaveData
    {
        public List<string> ClaimedRewardIds = new List<string>();
    }
}
