namespace GameFramework.Progression.Economy
{
    /// <summary>Published by <see cref="EconomyService"/> through the Phase 2 Event System whenever
    /// a currency's balance actually changes (a no-op Add/Spend of 0, or one rejected by
    /// validation, never publishes). Carries enough information that a listener never needs to
    /// query the service just to know what happened.</summary>
    public readonly struct CurrencyChangedEvent
    {
        public readonly CurrencyId Currency;
        public readonly int PreviousBalance;
        public readonly int NewBalance;
        public readonly int Delta;
        public readonly string Reason;

        public CurrencyChangedEvent(CurrencyId currency, int previousBalance, int newBalance, string reason)
        {
            Currency = currency;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
            Delta = newBalance - previousBalance;
            Reason = reason;
        }
    }
}
