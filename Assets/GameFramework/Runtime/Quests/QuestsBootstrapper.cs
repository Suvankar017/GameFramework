using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Milestones;
using GameFramework.Quests.Quests;
using GameFramework.Rewards;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Quests
{
    /// <summary>
    /// Drop-in <see cref="GameFramework.Runtime.Bootstrap.GameBootstrapper"/> that additionally
    /// registers Phase 7's four services (<see cref="IStatisticsService"/>, <see cref="IQuestService"/>,
    /// <see cref="IAchievementService"/>, <see cref="IMilestoneService"/>) on top of
    /// <see cref="ProgressionBootstrapper"/>'s five. Extends <see cref="ProgressionBootstrapper"/>
    /// (rather than being a flat sibling of it, the way <c>PerformanceBootstrapper</c>/
    /// <c>GameplayBootstrapper</c> are) because this assembly already has a genuine compile-time
    /// dependency on <see cref="GameFramework.Rewards"/> (quests/achievements/milestones claim
    /// through <see cref="IRewardService"/>) - unlike Performance/Gameplay/PlayerSystems, which are
    /// mutually independent and must be combined by composition instead.
    ///
    /// Only registers the four services with their static content (statistic definitions) - like
    /// <see cref="ProgressionBootstrapper"/>, it deliberately does <b>not</b> register any
    /// <see cref="QuestDefinition"/>/<see cref="AchievementDefinition"/>/<see cref="MilestoneDefinition"/>
    /// content, since which quests/achievements/milestones exist and what they require is
    /// game-specific content, wired up separately once <see cref="GameFramework.Runtime.Bootstrap.GameBootstrapper.State"/>
    /// reaches Ready.
    /// </summary>
    public class QuestsBootstrapper : ProgressionBootstrapper
    {
        [Header("Quest Content")]
        [SerializeField] private StatisticDefinition[] _statistics = System.Array.Empty<StatisticDefinition>();

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IStatisticsService>(new StatisticsService(_statistics));
            registry.Register<IQuestService>(new QuestService());
            registry.Register<IAchievementService>(new AchievementService());
            registry.Register<IMilestoneService>(new MilestoneService());
        }
    }
}
