using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Objectives;
using GameFramework.Rewards;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Unlocks;

namespace GameFramework.Quests.Achievements
{
    /// <summary>Default <see cref="IAchievementService"/> — see <see cref="Quests.QuestService"/>'s
    /// remarks for the shared post-Initialize registration timing, statistic-indexed event-driven
    /// evaluation, and reward-claim-delegated-to-<see cref="IRewardService"/> patterns this mirrors.
    /// An achievement has no "not started" concept - its <see cref="ConditionObjective"/> is activated
    /// the moment it is registered.</summary>
    public sealed class AchievementService : IAchievementService
    {
        private const string LogCategory = "Achievements";
        private const string SaveKey = "GameFramework.Achievements";
        private const int SaveVersion = 1;

        private sealed class AchievementRuntime
        {
            public AchievementDefinition Definition;
            public ConditionObjective Objective;
            public bool HasCompleted;
        }

        private readonly Dictionary<AchievementId, AchievementRuntime> _achievements = new Dictionary<AchievementId, AchievementRuntime>();
        private readonly Dictionary<StatisticId, List<AchievementId>> _statisticIndex = new Dictionary<StatisticId, List<AchievementId>>();
        private readonly List<AchievementId> _fallbackAchievements = new List<AchievementId>();
        private readonly HashSet<AchievementId> _pendingCompleted = new HashSet<AchievementId>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private IRewardService _rewards;
        private ILoggingService _log;
        private bool _isDirty;

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            _rewards = registry.Get<IRewardService>();
            registry.TryGet(out _log);

            _events.Subscribe<StatisticChangedEvent>(OnStatisticChanged);
            _events.Subscribe<LevelChangedEvent>(OnLevelChanged);
            _events.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _events.Subscribe<ItemChangedEvent>(OnItemChanged);
            _events.Subscribe<UnlockChangedEvent>(OnUnlockChanged);

            Load();
        }

        public void Shutdown()
        {
            _events.Unsubscribe<StatisticChangedEvent>(OnStatisticChanged);
            _events.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
            _events.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _events.Unsubscribe<ItemChangedEvent>(OnItemChanged);
            _events.Unsubscribe<UnlockChangedEvent>(OnUnlockChanged);

            if (_isDirty)
            {
                Save();
            }
        }

        public void RegisterAchievement(AchievementDefinition definition, ICondition condition)
        {
            Guard.NotNull(definition, nameof(definition));
            Guard.NotNull(condition, nameof(condition));

            AchievementId id = definition.Id;
            if (!id.IsValid)
            {
                throw new ArgumentException("AchievementDefinition has no Id assigned.", nameof(definition));
            }

            if (_achievements.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate achievement id '{id}'.");
            }

            var objective = new ConditionObjective(id.Value, condition, _events);
            var runtime = new AchievementRuntime { Definition = definition, Objective = objective };
            _achievements.Add(id, runtime);

            var statisticIds = new HashSet<StatisticId>();
            bool pure = ConditionStatisticIndex.CollectStatistics(condition, statisticIds);

            foreach (StatisticId statisticId in statisticIds)
            {
                if (!_statisticIndex.TryGetValue(statisticId, out List<AchievementId> list))
                {
                    list = new List<AchievementId>();
                    _statisticIndex[statisticId] = list;
                }

                list.Add(id);
            }

            if (!pure || statisticIds.Count == 0)
            {
                _fallbackAchievements.Add(id);
            }

            bool restoreCompleted = _pendingCompleted.Remove(id);

            objective.Activate();
            if (restoreCompleted && objective.State == ObjectiveState.Active)
            {
                objective.Complete();
                runtime.HasCompleted = true;
            }
            else
            {
                EvaluateAchievement(id, runtime);
            }
        }

        public bool IsRegistered(AchievementId id) => _achievements.ContainsKey(id);

        public bool IsCompleted(AchievementId id) => _achievements.TryGetValue(id, out AchievementRuntime runtime) && runtime.HasCompleted;

        public bool IsClaimed(AchievementId id)
        {
            if (!_achievements.TryGetValue(id, out AchievementRuntime runtime) || !runtime.HasCompleted)
            {
                return false;
            }

            RewardId rewardId = runtime.Definition.RewardId;
            return rewardId.IsValid && _rewards.HasClaimed(rewardId);
        }

        public AchievementProgress GetProgress(AchievementId id)
        {
            if (!_achievements.TryGetValue(id, out AchievementRuntime runtime))
            {
                return new AchievementProgress(0, 0, false);
            }

            return new AchievementProgress(runtime.Objective.CurrentValue, runtime.Objective.RequiredValue, runtime.HasCompleted);
        }

        public AchievementClaimResult TryClaimReward(AchievementId id)
        {
            if (!_achievements.TryGetValue(id, out AchievementRuntime runtime))
            {
                return AchievementClaimResult.InvalidId;
            }

            if (!runtime.HasCompleted)
            {
                return AchievementClaimResult.NotCompleted;
            }

            return ClaimReward(id, runtime);
        }

        public void Save()
        {
            var data = new AchievementSaveData();
            foreach (KeyValuePair<AchievementId, AchievementRuntime> pair in _achievements)
            {
                if (!pair.Value.HasCompleted)
                {
                    continue;
                }

                data.AchievementIds.Add(pair.Key.Value);
                data.Completed.Add(true);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            _pendingCompleted.Clear();

            AchievementSaveData data = _persistence.Load(SaveKey, SaveVersion, new AchievementSaveData());
            for (int i = 0; i < data.AchievementIds.Count && i < data.Completed.Count; i++)
            {
                if (data.Completed[i])
                {
                    _pendingCompleted.Add(new AchievementId(data.AchievementIds[i]));
                }
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            foreach (KeyValuePair<AchievementId, AchievementRuntime> pair in _achievements)
            {
                AchievementRuntime runtime = pair.Value;
                if (!runtime.HasCompleted)
                {
                    continue;
                }

                runtime.HasCompleted = false;
                if (runtime.Objective.State == ObjectiveState.Completed || runtime.Objective.State == ObjectiveState.Failed)
                {
                    runtime.Objective.Reset();
                }

                runtime.Objective.Activate();
            }

            _pendingCompleted.Clear();
            _isDirty = true;
        }

        private AchievementClaimResult ClaimReward(AchievementId id, AchievementRuntime runtime)
        {
            RewardId rewardId = runtime.Definition.RewardId;
            if (!rewardId.IsValid)
            {
                return AchievementClaimResult.NoReward;
            }

            RewardClaimResult result = _rewards.TryClaim(rewardId, $"Achievement:{id}");
            switch (result)
            {
                case RewardClaimResult.Success:
                    _events.Publish(new AchievementRewardClaimedEvent(id, rewardId));
                    return AchievementClaimResult.Success;
                case RewardClaimResult.AlreadyClaimed:
                    return AchievementClaimResult.AlreadyClaimed;
                case RewardClaimResult.InvalidId:
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"TryClaimReward('{id}') rejected: reward '{rewardId}' is not registered with IRewardService.");
                    return AchievementClaimResult.NoReward;
                default:
                    return AchievementClaimResult.GrantFailed;
            }
        }

        private void EvaluateAchievement(AchievementId id, AchievementRuntime runtime)
        {
            if (runtime.HasCompleted || runtime.Objective.State != ObjectiveState.Active || !runtime.Objective.Condition.IsSatisfied())
            {
                return;
            }

            runtime.Objective.Evaluate();
            runtime.HasCompleted = true;
            _isDirty = true;
            _events.Publish(new AchievementCompletedEvent(id));

            if (runtime.Definition.AutoClaimReward)
            {
                ClaimReward(id, runtime);
            }
        }

        private void HandleAchievementPossiblyChanged(AchievementId id)
        {
            if (_achievements.TryGetValue(id, out AchievementRuntime runtime))
            {
                EvaluateAchievement(id, runtime);
            }
        }

        private void OnStatisticChanged(StatisticChangedEvent e)
        {
            if (_statisticIndex.TryGetValue(e.Statistic, out List<AchievementId> ids))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    HandleAchievementPossiblyChanged(ids[i]);
                }
            }
        }

        private void OnLevelChanged(LevelChangedEvent e) => EvaluateFallbackAchievements();
        private void OnCurrencyChanged(CurrencyChangedEvent e) => EvaluateFallbackAchievements();
        private void OnItemChanged(ItemChangedEvent e) => EvaluateFallbackAchievements();
        private void OnUnlockChanged(UnlockChangedEvent e) => EvaluateFallbackAchievements();

        private void EvaluateFallbackAchievements()
        {
            for (int i = 0; i < _fallbackAchievements.Count; i++)
            {
                HandleAchievementPossiblyChanged(_fallbackAchievements[i]);
            }
        }
    }
}
