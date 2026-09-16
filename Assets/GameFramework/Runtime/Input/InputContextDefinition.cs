using System;

namespace GameFramework.Input
{
    /// <summary>
    /// One entry on <see cref="IInputService"/>'s context stack (see <see cref="IInputService.PushContext"/>).
    /// Only the top-of-stack context's allowed actions are enabled — a popup pushed above gameplay
    /// suppresses gameplay actions without gameplay code ever checking "is a popup open".
    /// </summary>
    public readonly struct InputContextDefinition
    {
        public readonly string Name;
        public readonly bool AllowAllActions;
        public readonly string[] AllowedActions;

        public InputContextDefinition(string name, bool allowAllActions, string[] allowedActions = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Context name cannot be null or empty.", nameof(name));
            }

            Name = name;
            AllowAllActions = allowAllActions;
            AllowedActions = allowedActions ?? Array.Empty<string>();
        }

        /// <summary>A context that enables every registered action — the default "Gameplay" context.</summary>
        public static InputContextDefinition AllowAll(string name) => new InputContextDefinition(name, true);

        /// <summary>A context that enables only the named actions (e.g. a pause menu allowing
        /// "Navigate"/"Confirm"/"Cancel" but not "Move"/"Jump").</summary>
        public static InputContextDefinition Restricted(string name, params string[] allowedActions) =>
            new InputContextDefinition(name, false, allowedActions);

        public bool Allows(string actionName)
        {
            if (AllowAllActions)
            {
                return true;
            }

            for (int i = 0; i < AllowedActions.Length; i++)
            {
                if (string.Equals(AllowedActions[i], actionName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
