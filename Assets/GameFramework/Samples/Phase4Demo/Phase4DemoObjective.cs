using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;

namespace GameFramework.Samples.Phase4Demo
{
    /// <summary>
    /// "Collect N targets" - a game-defined objective built on <see cref="ObjectiveBase"/>. The
    /// framework itself defines no concrete objectives; this is the demo's own subclass, exactly
    /// as a real game would write one.
    /// </summary>
    public sealed class Phase4DemoObjective : ObjectiveBase
    {
        private const string LogCategory = "Phase4Demo";

        private readonly int _targetCount;
        private int _collectedCount;

        public Phase4DemoObjective(string id, int targetCount, IEventService events = null) : base(id, events)
        {
            _targetCount = targetCount;
        }

        public int CollectedCount => _collectedCount;

        public void ReportCollected()
        {
            _collectedCount++;
            Log.Info(LogCategory, $"Objective '{Id}' progress: {_collectedCount}/{_targetCount}.");

            if (_collectedCount >= _targetCount)
            {
                Complete();
            }
        }
    }
}
