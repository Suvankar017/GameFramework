using GameFramework.Core.Validation;
using GameFramework.Progression.Experience;

namespace GameFramework.Unlocks
{
    public sealed class LevelRequirement : IUnlockRequirement
    {
        private readonly IExperienceService _experience;
        private readonly int _minLevel;

        public LevelRequirement(IExperienceService experience, int minLevel)
        {
            _experience = Guard.NotNull(experience, nameof(experience));
            _minLevel = minLevel;
        }

        public bool IsSatisfied() => _experience.CurrentLevel >= _minLevel;

        public string Describe() => $"Reach level {_minLevel}";
    }
}
