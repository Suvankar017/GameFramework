using UnityEngine;

namespace GameFramework.Rewards
{
    /// <summary>
    /// Pure authoring data for one reward — id/display text/claim policy only. What the reward
    /// actually grants is composed in code as an <see cref="IReward"/> and associated with this
    /// definition's <see cref="Id"/> via <see cref="IRewardService.RegisterReward"/>, the same
    /// register-in-code pattern <see cref="Unlocks.UnlockDefinition"/> uses and for the same reason
    /// (avoids a <c>[SerializeReference]</c> custom-drawer just to author reward contents in the
    /// Inspector).
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Rewards/Reward Definition", fileName = "RewardDefinition")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private RewardClaimPolicy _claimPolicy = RewardClaimPolicy.Once;

        public RewardId Id => new RewardId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public RewardClaimPolicy ClaimPolicy => _claimPolicy;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[RewardDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
