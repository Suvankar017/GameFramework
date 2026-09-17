using System;
using System.Collections.Generic;

namespace GameFramework.Quests.Milestones
{
    /// <summary>Persisted shape for <see cref="MilestoneService"/> — parallel lists for the same
    /// reason as <c>EconomySaveData</c>. Reward claim state is not duplicated here (see
    /// <see cref="GameFramework.Quests.Quests.QuestStatus"/>'s remarks on the same pattern).</summary>
    [Serializable]
    internal sealed class MilestoneSaveData
    {
        public List<string> MilestoneIds = new List<string>();
        public List<bool> Reached = new List<bool>();
    }
}
