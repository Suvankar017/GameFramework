using UnityEngine;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Pure authoring data for one level/stage - id, display text, the scene to load, and an
    /// optional free-form game mode - no runtime completion/unlock state (that belongs to
    /// Phase 6/7 progression, not here; see <see cref="GameFlowService"/>'s remarks on not
    /// duplicating it). The framework does not assume levels form a linear 1-2-3 sequence - a game
    /// decides what "next" means and calls <see cref="IGameFlowService.LoadLevel"/> with whatever
    /// <see cref="LevelDefinition"/> it chooses.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/GameFlow/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _sceneName;
        [SerializeField] private string _mode;

        public LevelId Id => new LevelId(_id);
        public string DisplayName => _displayName;

        /// <summary>Scene name as it appears in Build Settings - passed directly to
        /// <see cref="Runtime.SceneManagement.ISceneService"/>, which already validates it exists.</summary>
        public string SceneName => _sceneName;

        /// <summary>Optional free-form game mode (Campaign/TimeTrial/Practice/...) - context/data
        /// only, never a second state machine.</summary>
        public string Mode => _mode;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[LevelDefinition] '{name}' has no Id assigned.", this);
            }

            if (string.IsNullOrEmpty(_sceneName))
            {
                Debug.LogWarning($"[LevelDefinition] '{name}' has no Scene Name assigned.", this);
            }
        }
#endif
    }
}
