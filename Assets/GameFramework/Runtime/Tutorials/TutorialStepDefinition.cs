using UnityEngine;

namespace GameFramework.Tutorials
{
    /// <summary>Pure authoring data for one tutorial step - id and a localization key only, no
    /// completion logic. Mirrors <see cref="Gameplay.Objectives.ObjectiveDefinition"/>'s
    /// role/shape exactly (id/display text, no behavior) - the runtime behavior for the step
    /// (input/event/condition/manual) is a separately constructed <see cref="ITutorialStep"/>,
    /// paired with this asset via <see cref="TutorialStepEntry"/> when the tutorial is registered.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Tutorials/Tutorial Step Definition", fileName = "TutorialStepDefinition")]
    public sealed class TutorialStepDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _localizationKey;

        public string Id => _id;

        /// <summary>Key a game's localization system resolves to the instruction text shown for
        /// this step. The framework never resolves this itself - see CLAUDE.md's Phase 9 brief,
        /// section 24.</summary>
        public string LocalizationKey => _localizationKey;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[TutorialStepDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
