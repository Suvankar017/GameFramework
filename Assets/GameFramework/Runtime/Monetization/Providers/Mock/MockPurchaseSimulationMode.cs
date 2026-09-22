namespace GameFramework.Monetization.Providers.Mock
{
    /// <summary>Deterministic behavior for <see cref="MockPurchaseProvider"/> - see CLAUDE.md's
    /// Phase 15 brief, sections 39/44/67.</summary>
    public enum MockPurchaseSimulationMode
    {
        AlwaysSucceed,
        AlwaysCancel,
        AlwaysFail,
        AlwaysPending
    }
}
