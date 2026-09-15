namespace GameFramework.Runtime.Services
{
    /// <summary>
    /// Opt-in for a service that needs a per-frame callback (e.g. the Timer service). This is a
    /// minimal internal driver — <see cref="Bootstrap.GameBootstrapper"/> calls <see cref="Tick"/>
    /// on every initialized service that implements this, in registration order, from its own
    /// Update(). It is deliberately not a general-purpose scheduling/ordering/tick-group system;
    /// that is out of Phase 2's scope.
    /// </summary>
    public interface IUpdatableService : IGameService
    {
        void Tick();
    }
}
