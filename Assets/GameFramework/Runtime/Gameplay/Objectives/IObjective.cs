namespace GameFramework.Gameplay.Objectives
{
    /// <summary>Generic objective state machine contract. The framework defines no concrete
    /// objectives (no "Collect 10 Coins") — a game subclasses <see cref="ObjectiveBase"/> and calls
    /// <see cref="Complete"/>/<see cref="Fail"/> when its own condition is met.</summary>
    public interface IObjective
    {
        string Id { get; }
        ObjectiveState State { get; }

        void Activate();
        void Complete();
        void Fail();

        /// <summary>Returns a Completed/Failed objective to Inactive so it can be activated again.</summary>
        void Reset();
    }
}
