using System.Collections.Generic;
using GameFramework.Progression.Statistics;

namespace GameFramework.Quests.Conditions
{
    /// <summary>
    /// Walks a condition tree (through <see cref="ICompositeCondition"/>) collecting every
    /// <see cref="StatisticId"/> it depends on. Quest/Achievement/Milestone services use this at
    /// registration time to build a statistic-to-objective index, so a <see cref="StatisticChangedEvent"/>
    /// only re-evaluates objectives that could plausibly be affected by it instead of scanning
    /// everything — the "simple indexing strategy" the framework favors over a full dependency graph
    /// (see the Performance Infrastructure rules on dirty evaluation).
    /// </summary>
    internal static class ConditionStatisticIndex
    {
        /// <summary>Collects every statistic <paramref name="condition"/> depends on into
        /// <paramref name="into"/>, and returns true only if <paramref name="condition"/> (and every
        /// descendant, for a composite) is statistic-driven. A false return means some part of the
        /// tree is a different condition type (Level/Currency/Inventory/Unlock, or a game-specific
        /// custom condition) that a <see cref="Progression.Statistics.StatisticChangedEvent"/> alone
        /// cannot fully account for — the caller should fall back to evaluating on a broader set of
        /// events for that condition instead of relying solely on the statistic index.</summary>
        public static bool CollectStatistics(ICondition condition, ISet<StatisticId> into)
        {
            if (condition is IStatisticDependency dependency)
            {
                if (dependency.Statistic.IsValid)
                {
                    into.Add(dependency.Statistic);
                }

                return true;
            }

            if (condition is ICompositeCondition composite)
            {
                bool pure = true;
                foreach (ICondition child in composite.Children)
                {
                    if (!CollectStatistics(child, into))
                    {
                        pure = false;
                    }
                }

                return pure;
            }

            return false;
        }
    }
}
