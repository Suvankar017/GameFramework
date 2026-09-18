using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Pure authoring data for one tutorial - id/localized display keys/prerequisites/policies
    /// only. Step content is never embedded here: exactly like <see cref="Quests.Quests.QuestDefinition"/>
    /// leaves objectives to be composed in code and passed to <c>RegisterQuest</c>, a tutorial's
    /// actual <see cref="ITutorialStep"/> sequence is composed in code and passed to
    /// <see cref="ITutorialService.RegisterTutorial"/> - this asset only carries what a game/UI
    /// layer needs to *describe* the tutorial (see <see cref="TutorialStepDefinition"/> for the
    /// per-step equivalent), never how it actually progresses.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Tutorials/Tutorial Definition", fileName = "TutorialDefinition")]
    public sealed class TutorialDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayLocalizationKey;
        [SerializeField] private string _descriptionLocalizationKey;

        [Tooltip("TutorialId values that must be completed before this tutorial can start.")]
        [SerializeField] private string[] _prerequisites = System.Array.Empty<string>();

        [SerializeField] private TutorialRepeatPolicy _repeatPolicy = TutorialRepeatPolicy.Once;
        [SerializeField] private TutorialSkipPolicy _skipPolicy = TutorialSkipPolicy.Skippable;
        [SerializeField] private TutorialPausePolicy _pausePolicy = TutorialPausePolicy.DoesNotPauseGameplay;
        [SerializeField] private TutorialPersistencePolicy _persistencePolicy = TutorialPersistencePolicy.CompletionOnly;

        [Tooltip("Free-form reason passed to ITimeService.Pause() when Pause Policy is PausesGameplay. Defaults to the tutorial's Id when left empty.")]
        [SerializeField] private string _pauseReason;

        public TutorialId Id => new TutorialId(_id);
        public string DisplayLocalizationKey => _displayLocalizationKey;
        public string DescriptionLocalizationKey => _descriptionLocalizationKey;

        // Not cached: prerequisite lists are tiny (a handful of entries at most) and this asset can
        // be edited live in the Inspector while referenced - caching by array length alone would
        // silently go stale if an existing entry's text changed without the array being resized.
        public IReadOnlyList<TutorialId> Prerequisites
        {
            get
            {
                var ids = new TutorialId[_prerequisites.Length];
                for (int i = 0; i < _prerequisites.Length; i++)
                {
                    ids[i] = new TutorialId(_prerequisites[i]);
                }

                return ids;
            }
        }

        public TutorialRepeatPolicy RepeatPolicy => _repeatPolicy;
        public TutorialSkipPolicy SkipPolicy => _skipPolicy;
        public TutorialPausePolicy PausePolicy => _pausePolicy;
        public TutorialPersistencePolicy PersistencePolicy => _persistencePolicy;

        public string PauseReason => string.IsNullOrEmpty(_pauseReason) ? _id : _pauseReason;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[TutorialDefinition] '{name}' has no Id assigned.", this);
            }

            for (int i = 0; i < _prerequisites.Length; i++)
            {
                if (!string.IsNullOrEmpty(_prerequisites[i]) && string.Equals(_prerequisites[i], _id, System.StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[TutorialDefinition] '{name}' lists itself as its own prerequisite.", this);
                }
            }
        }
#endif
    }
}
