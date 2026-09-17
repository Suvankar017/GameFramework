using GameFramework.Core.Validation;
using GameFramework.Progression.Experience;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Mirrors <see cref="GameFramework.Unlocks.LevelRequirement"/> — see
    /// <see cref="ICondition"/>'s remarks for why this domain has its own parallel condition type
    /// instead of reusing that one.</summary>
    public sealed class LevelCondition : IProgressCondition
    {
        private readonly IExperienceService _experience;
        private readonly int _minLevel;

        public LevelCondition(IExperienceService experience, int minLevel)
        {
            _experience = Guard.NotNull(experience, nameof(experience));
            _minLevel = minLevel;
        }

        public int CurrentValue => _experience.CurrentLevel;

        public int RequiredValue => _minLevel;

        public bool IsSatisfied() => CurrentValue >= _minLevel;

        public string Describe() => $"Reach level {_minLevel}";
    }
}
