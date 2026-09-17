using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Conditions;
using GameFramework.Quests.Objectives;
using GameFramework.Rewards;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Quests.Milestones
{
    /// <summary>Default <see cref="IMilestoneService"/> — see <see cref="Quests.QuestService"/>'s
    /// remarks for the shared post-Initialize registration timing and reward-claim-delegated-to-
    /// <see cref="IRewardService"/> patterns this mirrors. Since a milestone's condition is always
    /// "statistic reaches threshold" (see <see cref="MilestoneDefinition"/>'s remarks), indexing by
    /// statistic is direct - no generic condition-tree walk is needed here.</summary>
    public sealed class MilestoneService : IMilestoneService
    {
        private const string LogCategory = "Milestones";
        private const string SaveKey = "GameFramework.Milestones";
        private const int SaveVersion = 1;

        private sealed class MilestoneRuntime
        {
            public MilestoneDefinition Definition;
            public ConditionObjective Objective;
            public bool IsReached;
        }

        private readonly Dictionary<MilestoneId, MilestoneRuntime> _milestones = new Dictionary<MilestoneId, MilestoneRuntime>();
        private readonly Dictionary<StatisticId, List<MilestoneId>> _statisticIndex = new Dictionary<StatisticId, List<MilestoneId>>();
        private readonly HashSet<MilestoneId> _pendingReached = new HashSet<MilestoneId>();

        private IStatisticsService _statistics;
        private IPersistenceService _persistence;
        private IEventService _events;
        private IRewardService _rewards;
        private ILoggingService _log;
        private bool _isDirty;

        public void Initialize(IServiceRegistry registry)
        {
            _statistics = registry.Get<IStatisticsService>();
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            _rewards = registry.Get<IRewardService>();
            registry.TryGet(out _log);

            _events.Subscribe<StatisticChangedEvent>(OnStatisticChanged);

            Load();
        }

        public void Shutdown()
        {
            _events.Unsubscribe<StatisticChangedEvent>(OnStatisticChanged);

            if (_isDirty)
            {
                Save();
            }
        }

        public void RegisterMilestone(MilestoneDefinition definition)
        {
            Guard.NotNull(definition, nameof(definition));

            MilestoneId id = definition.Id;
            if (!id.IsValid)
            {
                throw new ArgumentException("MilestoneDefinition has no Id assigned.", nameof(definition));
            }

            if (_milestones.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate milestone id '{id}'.");
            }

            ICondition condition = new StatisticCondition(_statistics, definition.StatisticId, definition.Threshold);
            var objective = new ConditionObjective(id.Value, condition, _events);
            var runtime = new MilestoneRuntime { Definition = definition, Objective = objective };
            _milestones.Add(id, runtime);

            if (definition.StatisticId.IsValid)
            {
                if (!_statisticIndex.TryGetValue(definition.StatisticId, out List<MilestoneId> list))
                {
                    list = new List<MilestoneId>();
                    _statisticIndex[definition.StatisticId] = list;
                }

                list.Add(id);
            }

            bool restoreReached = _pendingReached.Remove(id);

            objective.Activate();
            if (restoreReached && objective.State == ObjectiveState.Active)
            {
                objective.Complete();
                runtime.IsReached = true;
            }
            else
            {
                EvaluateMilestone(id, runtime);
            }
        }

        public bool IsRegistered(MilestoneId id) => _milestones.ContainsKey(id);

        public bool IsReached(MilestoneId id) => _milestones.TryGetValue(id, out MilestoneRuntime runtime) && runtime.IsReached;

        public bool IsClaimed(MilestoneId id)
        {
            if (!_milestones.TryGetValue(id, out MilestoneRuntime runtime) || !runtime.IsReached)
            {
                return false;
            }

            RewardId rewardId = runtime.Definition.RewardId;
            return rewardId.IsValid && _rewards.HasClaimed(rewardId);
        }

        public MilestoneProgress GetProgress(MilestoneId id)
        {
            if (!_milestones.TryGetValue(id, out MilestoneRuntime runtime))
            {
                return new MilestoneProgress(0, 0, false);
            }

            return new MilestoneProgress(runtime.Objective.CurrentValue, runtime.Objective.RequiredValue, runtime.IsReached);
        }

        public MilestoneClaimResult TryClaimReward(MilestoneId id)
        {
            if (!_milestones.TryGetValue(id, out MilestoneRuntime runtime))
            {
                return MilestoneClaimResult.InvalidId;
            }

            if (!runtime.IsReached)
            {
                return MilestoneClaimResult.NotReached;
            }

            RewardId rewardId = runtime.Definition.RewardId;
            if (!rewardId.IsValid)
            {
                return MilestoneClaimResult.NoReward;
            }

            RewardClaimResult result = _rewards.TryClaim(rewardId, $"Milestone:{id}");
            switch (result)
            {
                case RewardClaimResult.Success:
                    _events.Publish(new MilestoneRewardClaimedEvent(id, rewardId));
                    return MilestoneClaimResult.Success;
                case RewardClaimResult.AlreadyClaimed:
                    return MilestoneClaimResult.AlreadyClaimed;
                case RewardClaimResult.InvalidId:
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"TryClaimReward('{id}') rejected: reward '{rewardId}' is not registered with IRewardService.");
                    return MilestoneClaimResult.NoReward;
                default:
                    return MilestoneClaimResult.GrantFailed;
            }
        }

        public void Save()
        {
            var data = new MilestoneSaveData();
            foreach (KeyValuePair<MilestoneId, MilestoneRuntime> pair in _milestones)
            {
                if (!pair.Value.IsReached)
                {
                    continue;
                }

                data.MilestoneIds.Add(pair.Key.Value);
                data.Reached.Add(true);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            _pendingReached.Clear();

            MilestoneSaveData data = _persistence.Load(SaveKey, SaveVersion, new MilestoneSaveData());
            for (int i = 0; i < data.MilestoneIds.Count && i < data.Reached.Count; i++)
            {
                if (data.Reached[i])
                {
                    _pendingReached.Add(new MilestoneId(data.MilestoneIds[i]));
                }
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            foreach (KeyValuePair<MilestoneId, MilestoneRuntime> pair in _milestones)
            {
                MilestoneRuntime runtime = pair.Value;
                if (!runtime.IsReached)
                {
                    continue;
                }

                runtime.IsReached = false;
                if (runtime.Objective.State == ObjectiveState.Completed || runtime.Objective.State == ObjectiveState.Failed)
                {
                    runtime.Objective.Reset();
                }

                runtime.Objective.Activate();
            }

            _pendingReached.Clear();
            _isDirty = true;
        }

        private void EvaluateMilestone(MilestoneId id, MilestoneRuntime runtime)
        {
            if (runtime.IsReached || runtime.Objective.State != ObjectiveState.Active || !runtime.Objective.Condition.IsSatisfied())
            {
                return;
            }

            runtime.Objective.Evaluate();
            runtime.IsReached = true;
            _isDirty = true;
            _events.Publish(new MilestoneReachedEvent(id));
        }

        private void OnStatisticChanged(StatisticChangedEvent e)
        {
            if (!_statisticIndex.TryGetValue(e.Statistic, out List<MilestoneId> ids))
            {
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (_milestones.TryGetValue(ids[i], out MilestoneRuntime runtime))
                {
                    EvaluateMilestone(ids[i], runtime);
                }
            }
        }
    }
}
