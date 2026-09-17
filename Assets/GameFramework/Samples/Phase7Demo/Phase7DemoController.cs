using System.Collections;
using System.Collections.Generic;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Milestones;
using GameFramework.Quests.Quests;
using GameFramework.Rewards;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Samples.Phase7Demo
{
    /// <summary>
    /// Drives the Phase 7 demonstration scene: reports a "race completed" gameplay event as a
    /// statistic increment, and shows that single increment flow into a quest objective, an
    /// achievement, and a milestone simultaneously - exactly the reusable composition Phase 7 is
    /// meant to enable. Uses the three content assets under <c>Content/</c> (the framework's own
    /// <see cref="QuestsBootstrapper"/> only registers the services with static content - see this
    /// class for the game-specific step of registering actual quest/achievement/milestone content,
    /// done here after Ready for the same reason documented on <c>ProgressionBootstrapper</c>). Not
    /// part of the reusable framework - sample/demo content only, kept in its own assembly and scene.
    /// </summary>
    public sealed class Phase7DemoController : MonoBehaviour
    {
        private static readonly StatisticId RacesCompleted = new StatisticId("RacesCompleted");
        private static readonly CurrencyId Coins = new CurrencyId("Coins");
        private static readonly QuestId FirstRace = new QuestId("FirstRace");
        private static readonly AchievementId ExperiencedDriver = new AchievementId("ExperiencedDriver");
        private static readonly MilestoneId RaceMaster = new MilestoneId("RaceMaster");

        [Tooltip("Assigned from Content/CompleteRaceObjective.asset.")]
        [SerializeField] private ObjectiveDefinition _completeRaceObjective;

        [Tooltip("Assigned from Content/FirstRaceQuest.asset.")]
        [SerializeField] private QuestDefinition _firstRaceQuest;

        [Tooltip("Assigned from Content/ExperiencedDriverAchievement.asset.")]
        [SerializeField] private AchievementDefinition _experiencedDriverAchievement;

        [Tooltip("Assigned from Content/RaceMasterMilestone.asset.")]
        [SerializeField] private MilestoneDefinition _raceMasterMilestone;

        [Tooltip("Reward definitions matching each content asset's RewardId, assigned from Content/*Reward.asset.")]
        [SerializeField] private RewardDefinition _firstRaceRewardDefinition;
        [SerializeField] private RewardDefinition _experiencedDriverRewardDefinition;
        [SerializeField] private RewardDefinition _raceMasterRewardDefinition;

        private IStatisticsService _statistics;
        private IEconomyService _economy;
        private IRewardService _rewards;
        private IQuestService _quests;
        private IAchievementService _achievements;
        private IMilestoneService _milestones;
        private bool _ready;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _statistics = services.Get<IStatisticsService>();
            _economy = services.Get<IEconomyService>();
            _rewards = services.Get<IRewardService>();
            _quests = services.Get<IQuestService>();
            _achievements = services.Get<IAchievementService>();
            _milestones = services.Get<IMilestoneService>();

            RegisterDemoContent();

            _ready = true;
            Debug.Log("[Phase7Demo] Ready. Keys: 1 = start First Race quest, Space = report a completed race " +
                "(increments RacesCompleted), 2 = claim First Race reward, 3 = claim Experienced Driver reward, " +
                "4 = claim Race Master reward, R = full status report.");
            LogStatus();
        }

        private void RegisterDemoContent()
        {
            _rewards.RegisterReward(_firstRaceRewardDefinition, new CurrencyReward(_economy, Coins, 100));
            _rewards.RegisterReward(_experiencedDriverRewardDefinition, new CurrencyReward(_economy, Coins, 500));
            _rewards.RegisterReward(_raceMasterRewardDefinition, new CurrencyReward(_economy, Coins, 1000));

            _quests.RegisterQuest(_firstRaceQuest, availability: null, objectives: new List<QuestObjectiveEntry>
            {
                new QuestObjectiveEntry(_completeRaceObjective, new StatisticCondition(_statistics, RacesCompleted, 1))
            });

            _achievements.RegisterAchievement(_experiencedDriverAchievement, new StatisticCondition(_statistics, RacesCompleted, 10));

            _milestones.RegisterMilestone(_raceMasterMilestone);
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                QuestStartResult result = _quests.Start(FirstRace);
                Debug.Log($"[Phase7Demo] Start(FirstRace) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _statistics.Increment(RacesCompleted, 1, "Demo");
                Debug.Log($"[Phase7Demo] Reported a completed race. RacesCompleted = {_statistics.Get(RacesCompleted)}.");
                LogStatus();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                QuestClaimResult result = _quests.TryClaimReward(FirstRace);
                Debug.Log($"[Phase7Demo] TryClaimReward(FirstRace) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                AchievementClaimResult result = _achievements.TryClaimReward(ExperiencedDriver);
                Debug.Log($"[Phase7Demo] TryClaimReward(ExperiencedDriver) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                MilestoneClaimResult result = _milestones.TryClaimReward(RaceMaster);
                Debug.Log($"[Phase7Demo] TryClaimReward(RaceMaster) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                LogStatus();
            }
        }

        private void LogStatus()
        {
            AchievementProgress achievementProgress = _achievements.GetProgress(ExperiencedDriver);
            MilestoneProgress milestoneProgress = _milestones.GetProgress(RaceMaster);

            Debug.Log($"[Phase7Demo] Status: Coins={_economy.GetBalance(Coins)}, " +
                $"RacesCompleted={_statistics.Get(RacesCompleted)}, " +
                $"FirstRaceQuest={_quests.GetStatus(FirstRace)}, " +
                $"ExperiencedDriver={achievementProgress.CurrentValue}/{achievementProgress.RequiredValue} " +
                $"(Completed={achievementProgress.IsCompleted}), " +
                $"RaceMaster={milestoneProgress.CurrentValue}/{milestoneProgress.RequiredValue} " +
                $"(Reached={milestoneProgress.IsReached}).");
        }
    }
}
