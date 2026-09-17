using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Experience
{
    /// <summary>Default <see cref="IExperienceService"/> — see <c>EconomyService</c>'s remarks for
    /// the shared constructor-injected-config / explicit-Save-Load-with-dirty-flag pattern this
    /// mirrors. The <see cref="IProgressionCurve"/> is constructor-injected the same way.</summary>
    public sealed class ExperienceService : IExperienceService
    {
        private const string LogCategory = "Progression";
        private const string SaveKey = "GameFramework.Progression.Experience";
        private const int SaveVersion = 1;

        private readonly IProgressionCurve _curve;

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private int _level = 1;
        private int _experience;
        private bool _isDirty;

        public ExperienceService(IProgressionCurve curve)
        {
            _curve = Guard.NotNull(curve, nameof(curve));
        }

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
            Load();
        }

        public void Shutdown()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public int CurrentLevel => _level;

        public int CurrentExperience => _experience;

        public int ExperienceToNextLevel
        {
            get
            {
                int required = _curve.GetRequiredExperience(_level);
                return required > 0 ? System.Math.Max(0, required - _experience) : 0;
            }
        }

        public bool IsAtMaxLevel => _curve.GetRequiredExperience(_level) <= 0;

        public AddExperienceResult AddExperience(int amount, string reason = null)
        {
            if (amount <= 0)
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"AddExperience rejected: non-positive amount {amount}.");
                return AddExperienceResult.InvalidAmount;
            }

            if (IsAtMaxLevel)
            {
                return AddExperienceResult.AtMaxLevel;
            }

            int previousExperience = _experience;
            long remaining = (long)_experience + amount;

            while (true)
            {
                int required = _curve.GetRequiredExperience(_level);
                if (required <= 0 || remaining < required)
                {
                    break;
                }

                remaining -= required;
                int previousLevel = _level;
                _level++;
                _events.Publish(new LevelChangedEvent(previousLevel, _level));
            }

            _experience = (int)remaining;
            _isDirty = true;
            _events.Publish(new ExperienceChangedEvent(previousExperience, _experience, reason));

            return AddExperienceResult.Success;
        }

        public void SetLevel(int level, int currentExperience)
        {
            int previousLevel = _level;
            int previousExperience = _experience;

            _level = level < 1 ? 1 : level;
            _experience = currentExperience < 0 ? 0 : currentExperience;

            if (_level != previousLevel || _experience != previousExperience)
            {
                _isDirty = true;
            }

            if (_experience != previousExperience)
            {
                _events.Publish(new ExperienceChangedEvent(previousExperience, _experience, "SetLevel"));
            }

            if (_level != previousLevel)
            {
                _events.Publish(new LevelChangedEvent(previousLevel, _level));
            }
        }

        public void Save()
        {
            var data = new ExperienceSaveData { Level = _level, CurrentExperience = _experience };
            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            ExperienceSaveData data = _persistence.Load(SaveKey, SaveVersion, new ExperienceSaveData());
            _level = data.Level < 1 ? 1 : data.Level;
            _experience = data.CurrentExperience < 0 ? 0 : data.CurrentExperience;
            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            int previousLevel = _level;
            int previousExperience = _experience;
            _level = 1;
            _experience = 0;
            _isDirty = true;

            if (_experience != previousExperience)
            {
                _events.Publish(new ExperienceChangedEvent(previousExperience, _experience, "Reset"));
            }

            if (_level != previousLevel)
            {
                _events.Publish(new LevelChangedEvent(previousLevel, _level));
            }
        }
    }
}
