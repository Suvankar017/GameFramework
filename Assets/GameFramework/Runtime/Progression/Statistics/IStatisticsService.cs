using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Statistics
{
    /// <summary>
    /// Generic gameplay statistic/counter tracking — usable for any "N of something" or true/false
    /// progression fact (races completed, coins collected, distance driven, tutorial completed, ...).
    /// The framework defines no concrete statistics; a game supplies its own
    /// <see cref="StatisticDefinition"/> assets and refers to them by <see cref="StatisticId"/>.
    /// Integer is the primary, fully-featured type (see <see cref="StatisticValueType"/>'s remarks);
    /// Float and Boolean have their own focused Get/Set.
    /// </summary>
    public interface IStatisticsService : IGameService
    {
        bool IsRegistered(StatisticId id);

        StatisticValueType GetValueType(StatisticId id);

        int Get(StatisticId id);

        /// <summary>False for an unregistered id or one whose <see cref="StatisticValueType"/> isn't
        /// <see cref="StatisticValueType.Integer"/>.</summary>
        bool TryGet(StatisticId id, out int value);

        /// <summary>Sets the value directly (e.g. save-data restoration, debug tooling), clamped to
        /// [MinValue, MaxValue]. Rejected with a logged warning if <paramref name="id"/> is not an
        /// Integer statistic, or is monotonic and <paramref name="value"/> is lower than the current
        /// value.</summary>
        void Set(StatisticId id, int value, string reason = null);

        /// <summary>Adds <paramref name="amount"/> (must be &gt; 0) to the value, clamped to MaxValue
        /// if one is configured. Publishes <see cref="StatisticChangedEvent"/> if the value actually
        /// changed.</summary>
        void Increment(StatisticId id, int amount = 1, string reason = null);

        /// <summary>Subtracts <paramref name="amount"/> (must be &gt; 0) from the value, clamped to
        /// MinValue if one is configured. Rejected with a logged warning for a monotonic statistic —
        /// see <see cref="StatisticDefinition.IsMonotonic"/>.</summary>
        void Decrement(StatisticId id, int amount = 1, string reason = null);

        float GetFloat(StatisticId id);

        void SetFloat(StatisticId id, float value, string reason = null);

        bool GetBool(StatisticId id);

        void SetBool(StatisticId id, bool value, string reason = null);

        void Save();
        void Load();

        /// <summary>Resets every registered statistic to its default value without touching saved
        /// data on disk until <see cref="Save"/> is called. Bypasses monotonic protection —
        /// development/testing use, same as every other Phase 6/7 service's ResetToDefaults.</summary>
        void ResetToDefaults();
    }
}
