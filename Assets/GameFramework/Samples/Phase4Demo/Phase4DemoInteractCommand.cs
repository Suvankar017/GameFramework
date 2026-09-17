using GameFramework.Gameplay.Commands;
using GameFramework.Gameplay.Interaction;

namespace GameFramework.Samples.Phase4Demo
{
    /// <summary>
    /// Wraps "interact with this target" as a discrete <see cref="IGameplayCommand"/>, invoked
    /// through <see cref="GameplayCommandInvoker"/> so the CanExecute/Execute split and automatic
    /// rejection/failure logging are exercised, rather than calling
    /// <see cref="Phase4DemoTarget.Interact"/> directly.
    /// </summary>
    public sealed class Phase4DemoInteractCommand : IGameplayCommand
    {
        private readonly Phase4DemoTarget _target;
        private readonly InteractionContext _context;

        public Phase4DemoInteractCommand(Phase4DemoTarget target, InteractionContext context)
        {
            _target = target;
            _context = context;
        }

        public bool CanExecute() => _target != null && _target.CanInteract(_context);

        public CommandResult Execute()
        {
            _target.Interact(_context);
            return CommandResult.Success($"Interacted with '{_target.name}'.");
        }
    }
}
