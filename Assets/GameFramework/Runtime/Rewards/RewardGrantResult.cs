namespace GameFramework.Rewards
{
    /// <summary>Outcome of one <see cref="IReward.Grant"/> call. <see cref="FailureDetail"/> is a
    /// plain string rather than an enum since the range of "invalid" varies per concrete reward
    /// type (invalid currency, unregistered item, unregistered unlock, ...) - it is diagnostic
    /// text, not something calling code should branch on (branch on <see cref="Success"/> instead).</summary>
    public readonly struct RewardGrantResult
    {
        public readonly bool Success;
        public readonly string FailureDetail;

        private RewardGrantResult(bool success, string failureDetail)
        {
            Success = success;
            FailureDetail = failureDetail;
        }

        public static RewardGrantResult Ok() => new RewardGrantResult(true, null);
        public static RewardGrantResult Fail(string detail) => new RewardGrantResult(false, detail);
    }
}
