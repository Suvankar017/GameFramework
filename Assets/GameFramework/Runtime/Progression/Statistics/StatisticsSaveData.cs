using System;
using System.Collections.Generic;

namespace GameFramework.Progression.Statistics
{
    /// <summary>Persisted shape for <see cref="StatisticsService"/> — parallel lists for the same
    /// reason as <c>EconomySaveData</c>. Only <see cref="StatisticDefinition.Persistent"/> statistics
    /// are ever written here; the other two value columns are unused padding for a given row but keep
    /// every list the same length, which is simpler than three independently-sized lists.</summary>
    [Serializable]
    internal sealed class StatisticsSaveData
    {
        public List<string> Ids = new List<string>();
        public List<int> IntValues = new List<int>();
        public List<float> FloatValues = new List<float>();
        public List<bool> BoolValues = new List<bool>();
    }
}
