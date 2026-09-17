using GameFramework.Progression.Statistics;
using UnityEngine;

namespace GameFramework.Quests.Milestones
{
    /// <summary>
    /// Pure authoring data for one threshold milestone - always "statistic reaches threshold" (the
    /// one fixed shape every example in the design brief uses), so unlike Quests/Achievements a
    /// milestone needs no code-composed condition; <see cref="MilestoneService"/> builds its own
    /// <see cref="Conditions.StatisticCondition"/> internally from <see cref="StatisticId"/> and
    /// <see cref="Threshold"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Quests/Milestone Definition", fileName = "MilestoneDefinition")]
    public sealed class MilestoneDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private string _statisticId;
        [SerializeField] private int _threshold = 1;

        [Tooltip("RewardDefinition id to claim on reaching the threshold. Empty = no reward.")]
        [SerializeField] private string _rewardId;

        public MilestoneId Id => new MilestoneId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public StatisticId StatisticId => new StatisticId(_statisticId);
        public int Threshold => _threshold;
        public Rewards.RewardId RewardId => new Rewards.RewardId(_rewardId);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[MilestoneDefinition] '{name}' has no Id assigned.", this);
            }

            if (string.IsNullOrEmpty(_statisticId))
            {
                Debug.LogWarning($"[MilestoneDefinition] '{name}' has no Statistic Id assigned.", this);
            }

            if (_threshold <= 0)
            {
                Debug.LogWarning($"[MilestoneDefinition] '{name}' has a non-positive Threshold.", this);
            }
        }
#endif
    }
}
