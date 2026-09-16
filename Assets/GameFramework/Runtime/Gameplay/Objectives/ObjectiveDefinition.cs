using UnityEngine;

namespace GameFramework.Gameplay.Objectives
{
    /// <summary>Pure authoring data for an objective — id/display name/description only, no
    /// completion logic. A game's own <see cref="ObjectiveBase"/> subclass reads this for its
    /// <see cref="IObjective.Id"/> and UI-facing text; the framework does not hard-code what the
    /// objective actually checks.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Gameplay/Objective Definition", fileName = "ObjectiveDefinition")]
    public sealed class ObjectiveDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[ObjectiveDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
