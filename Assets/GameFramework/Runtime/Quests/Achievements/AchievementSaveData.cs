using System;
using System.Collections.Generic;

namespace GameFramework.Quests.Achievements
{
    /// <summary>Persisted shape for <see cref="AchievementService"/> — parallel lists for the same
    /// reason as <c>EconomySaveData</c>. Reward claim state is not duplicated here (see
    /// <see cref="GameFramework.Quests.Quests.QuestStatus"/>'s remarks on the same pattern).</summary>
    [Serializable]
    internal sealed class AchievementSaveData
    {
        public List<string> AchievementIds = new List<string>();
        public List<bool> Completed = new List<bool>();
    }
}
