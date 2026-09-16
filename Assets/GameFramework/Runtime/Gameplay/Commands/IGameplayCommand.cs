namespace GameFramework.Gameplay.Commands
{
    /// <summary>
    /// A discrete, decoupled gameplay action (Move, Interact, Use Ability, Open Door, ...). Each
    /// concrete command carries its own strongly-typed data as constructor parameters/fields —
    /// there is deliberately no generic payload on this interface (avoid a
    /// <c>Dictionary&lt;string, object&gt;</c>-shaped command API). Not an undo/redo framework;
    /// add that on top only if a real feature needs it.
    /// </summary>
    public interface IGameplayCommand
    {
        /// <summary>Cheap, side-effect-free check. <see cref="GameplayCommandInvoker.Invoke"/> calls
        /// this before <see cref="Execute"/> and turns a false result into
        /// <see cref="CommandResultStatus.Rejected"/> without calling Execute at all.</summary>
        bool CanExecute();

        /// <summary>Performs the action. Only called after <see cref="CanExecute"/> returned true
        /// when invoked through <see cref="GameplayCommandInvoker"/>.</summary>
        CommandResult Execute();
    }
}
