namespace GameFramework.Gameplay.Interaction
{
    /// <summary>Optional contract for anything that can be interacted with. Deliberately has no
    /// built-in cooldown — compose that separately (e.g. with an
    /// <see cref="Commands.GameplayCommandInvoker"/>-style guard or a plain timer) so this contract
    /// stays composable rather than growing policy every interactable must carry.</summary>
    public interface IInteractable
    {
        bool CanInteract(InteractionContext context);
        void Interact(InteractionContext context);
    }
}
