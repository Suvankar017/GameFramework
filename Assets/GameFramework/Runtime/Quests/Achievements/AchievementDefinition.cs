using UnityEngine;

namespace GameFramework.Quests.Achievements
{
    /// <summary>
    /// Pure authoring data for one achievement — id/display text/reward reference only. Its
    /// completion condition is composed in code and associated with this definition's
    /// <see cref="Id"/> via <see cref="IAchievementService.RegisterAchievement"/>, the same
    /// register-in-code pattern used throughout Phase 6/7 content.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Quests/Achievement Definition", fileName = "AchievementDefinition")]
    public sealed class AchievementDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        [Tooltip("RewardDefinition id to claim on completion. Empty = no reward.")]
        [SerializeField] private string _rewardId;

        [Tooltip("If true, the reward (if any) is claimed automatically the moment this achievement " +
            "completes. If false, a game claims it explicitly (e.g. after showing a popup) via " +
            "IAchievementService.TryClaimReward.")]
        [SerializeField] private bool _autoClaimReward;

        public AchievementId Id => new AchievementId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public Rewards.RewardId RewardId => new Rewards.RewardId(_rewardId);
        public bool AutoClaimReward => _autoClaimReward;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[AchievementDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
