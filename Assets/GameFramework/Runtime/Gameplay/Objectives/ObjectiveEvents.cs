namespace GameFramework.Gameplay.Objectives
{
    /// <summary>Published by <see cref="ObjectiveBase"/> through the Phase 2 Event System — no
    /// separate notification mechanism is introduced for this.</summary>
    public readonly struct ObjectiveActivatedEvent
    {
        public readonly string ObjectiveId;
        public ObjectiveActivatedEvent(string objectiveId) => ObjectiveId = objectiveId;
    }

    public readonly struct ObjectiveCompletedEvent
    {
        public readonly string ObjectiveId;
        public ObjectiveCompletedEvent(string objectiveId) => ObjectiveId = objectiveId;
    }

    public readonly struct ObjectiveFailedEvent
    {
        public readonly string ObjectiveId;
        public ObjectiveFailedEvent(string objectiveId) => ObjectiveId = objectiveId;
    }
}
