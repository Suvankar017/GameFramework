namespace GameFramework.Progression.Economy
{
    /// <summary>
    /// One entry in <see cref="EconomyService"/>'s in-memory transaction log — for
    /// debugging/UI/an analytics adapter, not a persisted financial ledger (see
    /// <see cref="EconomyService"/>'s remarks on why this is intentionally ephemeral).
    /// </summary>
    public readonly struct EconomyTransaction
    {
        public readonly int TransactionId;
        public readonly CurrencyId Currency;
        public readonly int Delta;
        public readonly int BalanceAfter;
        public readonly string Reason;
        public readonly float Timestamp;

        public EconomyTransaction(int transactionId, CurrencyId currency, int delta, int balanceAfter, string reason, float timestamp)
        {
            TransactionId = transactionId;
            Currency = currency;
            Delta = delta;
            BalanceAfter = balanceAfter;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
}
