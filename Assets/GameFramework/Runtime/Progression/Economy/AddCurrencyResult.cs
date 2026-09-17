namespace GameFramework.Progression.Economy
{
    /// <summary>Outcome of <see cref="IEconomyService.TryAdd"/>. <see cref="ClampedToMax"/> is a
    /// success with a caveat — the balance changed, but by less than requested because
    /// <see cref="CurrencyDefinition.MaxBalance"/> was reached; the caller can check
    /// <see cref="CurrencyChangedEvent.Delta"/> (published either way) to see exactly how much was
    /// actually added.</summary>
    public enum AddCurrencyResult
    {
        Success,
        InvalidCurrency,
        InvalidAmount,
        ClampedToMax
    }
}
