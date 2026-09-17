using System.Collections.Generic;
using GameFramework.Quests.Conditions;
using GameFramework.Runtime.Services;

namespace GameFramework.Quests.Quests
{
    /// <summary>
    /// Generic quest lifecycle: availability, activation, objective tracking, completion and reward
    /// claiming. The framework defines no concrete quests - a game registers a
    /// <see cref="QuestDefinition"/> with the availability condition and objectives it should track.
    /// </summary>
    public interface IQuestService : IGameService
    {
        /// <summary>Registers a quest definition with its availability condition (null = always
        /// available) and objectives. Call once per quest at composition-root time.</summary>
        void RegisterQuest(QuestDefinition definition, ICondition availability, IReadOnlyList<QuestObjectiveEntry> objectives);

        bool IsRegistered(QuestId id);

        /// <summary>Current lifecycle stage — see <see cref="QuestStatus"/>'s remarks for what is
        /// persisted versus derived.</summary>
        QuestStatus GetStatus(QuestId id);

        /// <summary>True if not already started/completed and the availability condition (if any) is
        /// currently satisfied.</summary>
        bool IsAvailable(QuestId id);

        /// <summary>Activates every objective and moves the quest to <see cref="QuestStatus.Active"/>.</summary>
        QuestStartResult Start(QuestId id);

        /// <summary>Per-objective progress snapshot, in registration order.</summary>
        IReadOnlyList<QuestObjectiveProgress> GetObjectiveProgress(QuestId id);

        /// <summary>Overall completion progress in [0, 1] — objectives completed divided by objectives
        /// required by the quest's <see cref="QuestCompletionRule"/>.</summary>
        float GetProgress(QuestId id);

        bool IsCompleted(QuestId id);

        /// <summary>True only once the quest's reward (if any) has been claimed — derives from
        /// <see cref="GameFramework.Rewards.IRewardService.HasClaimed"/>, never tracked separately.</summary>
        bool IsClaimed(QuestId id);

        /// <summary>Claims the quest's reward via <see cref="GameFramework.Rewards.IRewardService"/>.
        /// Idempotent - a second call after a successful claim always returns
        /// <see cref="QuestClaimResult.AlreadyClaimed"/>.</summary>
        QuestClaimResult TryClaimReward(QuestId id);

        /// <summary>Resets a completed, repeatable quest back to startable - see
        /// <see cref="QuestRepeatPolicy"/>. Rejected for a one-time quest or one that has reached its
        /// repeat limit.</summary>
        QuestResetResult TryReset(QuestId id);

        /// <summary>Forces a quest back to its not-started state regardless of repeat policy.
        /// Development/testing use — see the framework's reset-support conventions.</summary>
        void ForceReset(QuestId id);

        void Save();
        void Load();

        /// <summary>Clears every quest's runtime state (not its content) without touching saved data
        /// on disk until <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
