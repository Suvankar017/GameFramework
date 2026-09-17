namespace GameFramework.Progression.Economy
{
    /// <summary>Outcome of <see cref="IEconomyService.TrySpend"/> — an expected gameplay failure
    /// (insufficient funds) is a normal result, never an exception.</summary>
    public enum SpendResult
    {
        Success,
        InvalidCurrency,
        InvalidAmount,
        InsufficientFunds
    }
}
