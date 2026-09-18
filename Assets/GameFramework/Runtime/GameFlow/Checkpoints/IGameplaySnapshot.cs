namespace GameFramework.GameFlow.Checkpoints
{
    /// <summary>
    /// Marker contract for a game-defined checkpoint state snapshot (health, ammo, objective
    /// progress, whatever a specific game's checkpoint needs to restore beyond position/rotation).
    /// <see cref="CheckpointSystem"/> stores an implementation of this opaquely - it never inspects,
    /// serializes, or persists its contents; the framework does not build a universal serializer for
    /// arbitrary Unity/game state. Only the built-in transform data
    /// (<see cref="Gameplay.Objectives.CheckpointData"/>) is ever persisted by
    /// <see cref="GameFlowService"/> - a game that needs its own snapshot content to survive a
    /// session boundary persists it itself.
    /// </summary>
    public interface IGameplaySnapshot
    {
    }
}
