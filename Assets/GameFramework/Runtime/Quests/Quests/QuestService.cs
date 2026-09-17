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
using UnityEngine;

namespace GameFramework.Quests.Quests
{
    /// <summary>
    /// Default <see cref="IQuestService"/>. Like <c>UnlockService</c>/<c>RewardService</c>, quests are
    /// registered via <see cref="RegisterQuest"/> after <see cref="Initialize"/> (a condition
    /// typically needs another already-initialized service), so <see cref="Load"/> stages persisted
    /// state into <see cref="_pendingRestore"/> and each <see cref="RegisterQuest"/> call applies its
    /// own entry once the quest exists to apply it to.
    ///
    /// Evaluation is event-driven, never polled: a <see cref="StatisticChangedEvent"/> only
    /// re-evaluates quests indexed against that exact statistic (see <see cref="ConditionStatisticIndex"/>);
    /// quests with any non-statistic dependency (Level/Currency/Inventory/Unlock, or a game-specific
    /// custom condition) fall back to re-evaluating on the corresponding broader change event instead.
    /// Reward claim idempotency is fully delegated to <see cref="IRewardService"/> - a quest's
    /// "claimed" state is never tracked here (see <see cref="QuestStatus"/>'s remarks).
    /// </summary>
    public sealed class QuestService : IQuestService
    {
        private const string LogCategory = "Quests";
        private const string SaveKey = "GameFramework.Quests";
        private const int SaveVersion = 1;

        private sealed class QuestRuntime
        {
            public QuestDefinition Definition;
            public ICondition Availability;
            public ConditionObjective[] Objectives;
            public ObjectiveDefinition[] ObjectiveDefinitions;
            public bool HasStarted;
            public bool HasCompleted;
            public int CompletionCount;
        }

        private readonly Dictionary<QuestId, QuestRuntime> _quests = new Dictionary<QuestId, QuestRuntime>();
        private readonly Dictionary<StatisticId, List<QuestId>> _statisticIndex = new Dictionary<StatisticId, List<QuestId>>();
        private readonly List<QuestId> _fallbackQuests = new List<QuestId>();
        private readonly HashSet<QuestId> _announcedAvailable = new HashSet<QuestId>();
        private readonly Dictionary<QuestId, (QuestStatus Status, int CompletionCount)> _pendingRestore =
            new Dictionary<QuestId, (QuestStatus, int)>();
        private readonly List<QuestObjectiveProgress> _progressBuffer = new List<QuestObjectiveProgress>();

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

        public void RegisterQuest(QuestDefinition definition, ICondition availability, IReadOnlyList<QuestObjectiveEntry> objectives)
        {
            Guard.NotNull(definition, nameof(definition));
            Guard.NotNull(objectives, nameof(objectives));

            QuestId id = definition.Id;
            if (!id.IsValid)
            {
                throw new ArgumentException("QuestDefinition has no Id assigned.", nameof(definition));
            }

            if (_quests.ContainsKey(id))
            {
                throw new InvalidOperationException($"Duplicate quest id '{id}'.");
            }

            var objectiveInstances = new ConditionObjective[objectives.Count];
            var objectiveDefinitions = new ObjectiveDefinition[objectives.Count];

            for (int i = 0; i < objectives.Count; i++)
            {
                QuestObjectiveEntry entry = objectives[i];
                Guard.NotNull(entry.Definition, "objectives[" + i + "].Definition");
                Guard.NotNull(entry.Condition, "objectives[" + i + "].Condition");

                objectiveDefinitions[i] = entry.Definition;
                objectiveInstances[i] = new ConditionObjective(entry.Definition.Id, entry.Condition, _events);
            }

            var runtime = new QuestRuntime
            {
                Definition = definition,
                Availability = availability,
                Objectives = objectiveInstances,
                ObjectiveDefinitions = objectiveDefinitions
            };

            _quests.Add(id, runtime);
            IndexConditions(id, availability, objectiveInstances);
            ApplyPendingRestore(id, runtime);
        }

        public bool IsRegistered(QuestId id) => _quests.ContainsKey(id);

        public QuestStatus GetStatus(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                return QuestStatus.Locked;
            }

            if (runtime.HasCompleted)
            {
                RewardId rewardId = runtime.Definition.RewardId;
                bool claimed = rewardId.IsValid && _rewards.HasClaimed(rewardId);
                return claimed ? QuestStatus.Claimed : QuestStatus.Completed;
            }

            if (runtime.HasStarted)
            {
                return QuestStatus.Active;
            }

            bool available = runtime.Availability == null || runtime.Availability.IsSatisfied();
            return available ? QuestStatus.Available : QuestStatus.Locked;
        }

        public bool IsAvailable(QuestId id) => GetStatus(id) == QuestStatus.Available;

        public bool IsCompleted(QuestId id) => _quests.TryGetValue(id, out QuestRuntime runtime) && runtime.HasCompleted;

        public bool IsClaimed(QuestId id) => GetStatus(id) == QuestStatus.Claimed;

        public QuestStartResult Start(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                return QuestStartResult.InvalidId;
            }

            if (runtime.HasCompleted)
            {
                return QuestStartResult.AlreadyCompleted;
            }

            if (runtime.HasStarted)
            {
                return QuestStartResult.AlreadyActive;
            }

            if (runtime.Availability != null && !runtime.Availability.IsSatisfied())
            {
                return QuestStartResult.NotAvailable;
            }

            runtime.HasStarted = true;
            _isDirty = true;
            _announcedAvailable.Remove(id);

            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                runtime.Objectives[i].Activate();
            }

            _events.Publish(new QuestStartedEvent(id));

            EvaluateObjectives(id, runtime);

            return QuestStartResult.Success;
        }

        public IReadOnlyList<QuestObjectiveProgress> GetObjectiveProgress(QuestId id)
        {
            _progressBuffer.Clear();

            if (!_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"GetObjectiveProgress('{id}') rejected: unregistered quest.");
                return _progressBuffer;
            }

            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                ConditionObjective objective = runtime.Objectives[i];
                ObjectiveDefinition definition = runtime.ObjectiveDefinitions[i];
                _progressBuffer.Add(new QuestObjectiveProgress(
                    objective.Id,
                    definition.DisplayName,
                    objective.CurrentValue,
                    objective.RequiredValue,
                    objective.State == ObjectiveState.Completed));
            }

            return _progressBuffer;
        }

        public float GetProgress(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime) || runtime.Objectives.Length == 0)
            {
                return 0f;
            }

            int completed = 0;
            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                if (runtime.Objectives[i].State == ObjectiveState.Completed)
                {
                    completed++;
                }
            }

            int required = RequiredCompletionCount(runtime);
            return required <= 0 ? 0f : Mathf.Clamp01((float)completed / required);
        }

        public QuestClaimResult TryClaimReward(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                return QuestClaimResult.InvalidId;
            }

            if (!runtime.HasCompleted)
            {
                return QuestClaimResult.NotCompleted;
            }

            RewardId rewardId = runtime.Definition.RewardId;
            if (!rewardId.IsValid)
            {
                return QuestClaimResult.NoReward;
            }

            RewardClaimResult result = _rewards.TryClaim(rewardId, $"Quest:{id}");
            switch (result)
            {
                case RewardClaimResult.Success:
                    _events.Publish(new QuestRewardClaimedEvent(id, rewardId));
                    return QuestClaimResult.Success;
                case RewardClaimResult.AlreadyClaimed:
                    return QuestClaimResult.AlreadyClaimed;
                case RewardClaimResult.InvalidId:
                    _log?.Log(LogLevel.Warning, LogCategory,
                        $"TryClaimReward('{id}') rejected: reward '{rewardId}' is not registered with IRewardService.");
                    return QuestClaimResult.NoReward;
                default:
                    return QuestClaimResult.GrantFailed;
            }
        }

        public QuestResetResult TryReset(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                return QuestResetResult.InvalidId;
            }

            if (!runtime.HasCompleted)
            {
                return QuestResetResult.NotCompleted;
            }

            if (runtime.Definition.RepeatPolicy == QuestRepeatPolicy.OneTime)
            {
                return QuestResetResult.NotRepeatable;
            }

            if (runtime.Definition.RepeatPolicy == QuestRepeatPolicy.LimitedRepeats &&
                runtime.CompletionCount >= runtime.Definition.MaxRepeats)
            {
                return QuestResetResult.RepeatLimitReached;
            }

            runtime.CompletionCount++;
            ResetRuntime(runtime);
            _isDirty = true;
            return QuestResetResult.Success;
        }

        public void ForceReset(QuestId id)
        {
            if (_quests.TryGetValue(id, out QuestRuntime runtime))
            {
                ResetRuntime(runtime);
                _isDirty = true;
            }
        }

        public void Save()
        {
            var data = new QuestSaveData();
            foreach (KeyValuePair<QuestId, QuestRuntime> pair in _quests)
            {
                QuestRuntime runtime = pair.Value;
                if (!runtime.HasStarted && !runtime.HasCompleted)
                {
                    continue;
                }

                data.QuestIds.Add(pair.Key.Value);
                data.Statuses.Add((int)(runtime.HasCompleted ? QuestStatus.Completed : QuestStatus.Active));
                data.CompletionCounts.Add(runtime.CompletionCount);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            _pendingRestore.Clear();

            QuestSaveData data = _persistence.Load(SaveKey, SaveVersion, new QuestSaveData());
            for (int i = 0; i < data.QuestIds.Count && i < data.Statuses.Count; i++)
            {
                var id = new QuestId(data.QuestIds[i]);
                var status = (QuestStatus)data.Statuses[i];
                if (status != QuestStatus.Active && status != QuestStatus.Completed)
                {
                    continue; // corrupted/unexpected value - ignore rather than corrupt runtime state
                }

                int completionCount = i < data.CompletionCounts.Count ? data.CompletionCounts[i] : 0;
                _pendingRestore[id] = (status, completionCount);
            }

            _isDirty = false;

            // RegisterQuest normally runs after Load (see this class's remarks); this only matters for
            // an explicit re-Load() call after quests already exist.
            foreach (KeyValuePair<QuestId, QuestRuntime> pair in _quests)
            {
                ApplyPendingRestore(pair.Key, pair.Value);
            }
        }

        public void ResetToDefaults()
        {
            foreach (KeyValuePair<QuestId, QuestRuntime> pair in _quests)
            {
                ResetRuntime(pair.Value);
                pair.Value.CompletionCount = 0;
            }

            _pendingRestore.Clear();
            _isDirty = true;
        }

        private void IndexConditions(QuestId id, ICondition availability, ConditionObjective[] objectives)
        {
            var statisticIds = new HashSet<StatisticId>();
            bool pure = true;

            if (availability != null)
            {
                pure &= ConditionStatisticIndex.CollectStatistics(availability, statisticIds);
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                pure &= ConditionStatisticIndex.CollectStatistics(objectives[i].Condition, statisticIds);
            }

            foreach (StatisticId statisticId in statisticIds)
            {
                if (!_statisticIndex.TryGetValue(statisticId, out List<QuestId> list))
                {
                    list = new List<QuestId>();
                    _statisticIndex[statisticId] = list;
                }

                list.Add(id);
            }

            if (!pure || statisticIds.Count == 0)
            {
                _fallbackQuests.Add(id);
            }
        }

        private void ApplyPendingRestore(QuestId id, QuestRuntime runtime)
        {
            if (!_pendingRestore.TryGetValue(id, out (QuestStatus Status, int CompletionCount) persisted))
            {
                return;
            }

            runtime.CompletionCount = persisted.CompletionCount;

            if (persisted.Status == QuestStatus.Completed)
            {
                runtime.HasStarted = true;
                runtime.HasCompleted = true;

                for (int i = 0; i < runtime.Objectives.Length; i++)
                {
                    ConditionObjective objective = runtime.Objectives[i];
                    objective.Activate();
                    if (objective.State == ObjectiveState.Active)
                    {
                        objective.Complete();
                    }
                }
            }
            else if (persisted.Status == QuestStatus.Active)
            {
                runtime.HasStarted = true;

                for (int i = 0; i < runtime.Objectives.Length; i++)
                {
                    runtime.Objectives[i].Activate();
                }

                EvaluateObjectives(id, runtime);
            }

            _pendingRestore.Remove(id);
        }

        private void ResetRuntime(QuestRuntime runtime)
        {
            runtime.HasStarted = false;
            runtime.HasCompleted = false;

            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                ObjectiveState state = runtime.Objectives[i].State;
                if (state == ObjectiveState.Completed || state == ObjectiveState.Failed)
                {
                    runtime.Objectives[i].Reset();
                }
            }

            _announcedAvailable.Remove(runtime.Definition.Id);
        }

        private void HandleQuestPossiblyChanged(QuestId id)
        {
            if (!_quests.TryGetValue(id, out QuestRuntime runtime) || runtime.HasCompleted)
            {
                return;
            }

            if (runtime.HasStarted)
            {
                EvaluateObjectives(id, runtime);
            }
            else
            {
                EvaluateAvailability(id, runtime);
            }
        }

        private void EvaluateAvailability(QuestId id, QuestRuntime runtime)
        {
            bool isAvailableNow = runtime.Availability == null || runtime.Availability.IsSatisfied();
            if (isAvailableNow)
            {
                if (_announcedAvailable.Add(id))
                {
                    _events.Publish(new QuestAvailableEvent(id));
                }
            }
            else
            {
                _announcedAvailable.Remove(id);
            }
        }

        private void EvaluateObjectives(QuestId id, QuestRuntime runtime)
        {
            bool anyNewlyCompleted = false;

            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                ConditionObjective objective = runtime.Objectives[i];
                if (objective.State != ObjectiveState.Active || !objective.Condition.IsSatisfied())
                {
                    continue;
                }

                objective.Evaluate();
                anyNewlyCompleted = true;
                _events.Publish(new QuestObjectiveCompletedEvent(id, objective.Id));
            }

            if (anyNewlyCompleted && !runtime.HasCompleted && IsCompletionRuleSatisfied(runtime))
            {
                runtime.HasCompleted = true;
                _isDirty = true;
                _events.Publish(new QuestCompletedEvent(id));
            }
        }

        private static bool IsCompletionRuleSatisfied(QuestRuntime runtime)
        {
            int completedCount = 0;
            for (int i = 0; i < runtime.Objectives.Length; i++)
            {
                if (runtime.Objectives[i].State == ObjectiveState.Completed)
                {
                    completedCount++;
                }
            }

            return completedCount >= RequiredCompletionCount(runtime);
        }

        private static int RequiredCompletionCount(QuestRuntime runtime)
        {
            switch (runtime.Definition.CompletionRule)
            {
                case QuestCompletionRule.All: return runtime.Objectives.Length;
                case QuestCompletionRule.Any: return runtime.Objectives.Length > 0 ? 1 : 0;
                case QuestCompletionRule.Count: return runtime.Definition.RequiredObjectiveCount;
                default: return runtime.Objectives.Length;
            }
        }

        private void OnStatisticChanged(StatisticChangedEvent e)
        {
            if (_statisticIndex.TryGetValue(e.Statistic, out List<QuestId> ids))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    HandleQuestPossiblyChanged(ids[i]);
                }
            }
        }

        private void OnLevelChanged(LevelChangedEvent e) => EvaluateFallbackQuests();
        private void OnCurrencyChanged(CurrencyChangedEvent e) => EvaluateFallbackQuests();
        private void OnItemChanged(ItemChangedEvent e) => EvaluateFallbackQuests();
        private void OnUnlockChanged(UnlockChangedEvent e) => EvaluateFallbackQuests();

        private void EvaluateFallbackQuests()
        {
            for (int i = 0; i < _fallbackQuests.Count; i++)
            {
                HandleQuestPossiblyChanged(_fallbackQuests[i]);
            }
        }
    }
}
