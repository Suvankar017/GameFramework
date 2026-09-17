using UnityEngine;

namespace GameFramework.Quests.Quests
{
    /// <summary>
    /// Pure authoring data for one quest — id/display text/completion policy/reward reference only.
    /// Its objectives and availability condition are composed in code and associated with this
    /// definition's <see cref="Id"/> via <see cref="IQuestService.RegisterQuest"/>, the same
    /// register-in-code pattern <see cref="GameFramework.Unlocks.UnlockDefinition"/>/
    /// <see cref="GameFramework.Rewards.RewardDefinition"/> use and for the same reason.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Quests/Quest Definition", fileName = "QuestDefinition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        [SerializeField] private QuestCompletionRule _completionRule = QuestCompletionRule.All;

        [Tooltip("Only used when Completion Rule is Count.")]
        [SerializeField] private int _requiredObjectiveCount = 1;

        [SerializeField] private QuestRepeatPolicy _repeatPolicy = QuestRepeatPolicy.OneTime;

        [Tooltip("Only used when Repeat Policy is Limited Repeats.")]
        [SerializeField] private int _maxRepeats = 1;

        [Tooltip("RewardDefinition id to claim on completion. Empty = no reward.")]
        [SerializeField] private string _rewardId;

        public QuestId Id => new QuestId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public QuestCompletionRule CompletionRule => _completionRule;
        public int RequiredObjectiveCount => _requiredObjectiveCount;
        public QuestRepeatPolicy RepeatPolicy => _repeatPolicy;
        public int MaxRepeats => _maxRepeats;
        public Rewards.RewardId RewardId => new Rewards.RewardId(_rewardId);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[QuestDefinition] '{name}' has no Id assigned.", this);
            }

            if (_completionRule == QuestCompletionRule.Count && _requiredObjectiveCount < 1)
            {
                Debug.LogWarning($"[QuestDefinition] '{name}' uses Count completion but RequiredObjectiveCount < 1.", this);
            }

            if (_repeatPolicy == QuestRepeatPolicy.LimitedRepeats && _maxRepeats < 1)
            {
                Debug.LogWarning($"[QuestDefinition] '{name}' uses LimitedRepeats but MaxRepeats < 1.", this);
            }
        }
#endif
    }
}
