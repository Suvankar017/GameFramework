using UnityEngine;

namespace GameFramework.Unlocks
{
    /// <summary>
    /// Pure authoring data for one unlockable thing — id/display text only, no requirement logic.
    /// A requirement is composed in code and associated with this definition's <see cref="Id"/> via
    /// <see cref="IUnlockService.RegisterUnlock"/> at composition-root time (the same
    /// register-in-code pattern <c>ISettingsService.Register</c> already established), rather than
    /// authored as polymorphic data on the asset itself — keeps this asset simple and avoids a
    /// <c>[SerializeReference]</c> custom-drawer just to author AND/OR requirement trees in the
    /// Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Unlocks/Unlock Definition", fileName = "UnlockDefinition")]
    public sealed class UnlockDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        public UnlockId Id => new UnlockId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[UnlockDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
