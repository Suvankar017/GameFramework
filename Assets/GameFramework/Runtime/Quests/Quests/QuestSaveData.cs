using System;
using System.Collections.Generic;

namespace GameFramework.Quests.Quests
{
    /// <summary>Persisted shape for <see cref="QuestService"/> — parallel lists for the same reason as
    /// <c>EconomySaveData</c>. Only ever contains rows for a quest that reached
    /// <see cref="QuestStatus.Active"/> or <see cref="QuestStatus.Completed"/> at least once; reward
    /// claim state is not duplicated here (see <see cref="QuestStatus"/>'s remarks).</summary>
    [Serializable]
    internal sealed class QuestSaveData
    {
        public List<string> QuestIds = new List<string>();
        public List<int> Statuses = new List<int>();
        public List<int> CompletionCounts = new List<int>();
    }
}
