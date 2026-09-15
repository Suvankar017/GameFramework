using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// Authoring data for a set of logical actions and their physical bindings. Contains no
    /// gameplay-specific behavior — a game defines its own asset instance(s) (which action names
    /// exist, e.g. "Move"/"Jump"/"Interact", is game content, not framework content) and passes
    /// them to <see cref="IInputService.RegisterActionMap"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Input/Input Action Map", fileName = "InputActionMap")]
    public sealed class InputActionMapAsset : ScriptableObject
    {
        public List<InputActionBindingDefinition> Actions = new List<InputActionBindingDefinition>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (InputActionBindingDefinition action in Actions)
            {
                if (string.IsNullOrEmpty(action.ActionName))
                {
                    Debug.LogWarning($"[InputActionMapAsset] '{name}' has an action with an empty name.", this);
                    continue;
                }

                if (!seen.Add(action.ActionName))
                {
                    Debug.LogWarning(
                        $"[InputActionMapAsset] '{name}' has a duplicate action name '{action.ActionName}'.", this);
                }
            }
        }
#endif
        public bool TryGetAction(string actionName, out InputActionBindingDefinition action)
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                if (string.Equals(Actions[i].ActionName, actionName, System.StringComparison.Ordinal))
                {
                    action = Actions[i];
                    return true;
                }
            }

            action = null;
            return false;
        }
    }
}
