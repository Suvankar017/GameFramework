namespace GameFramework.Rewards
{
    /// <summary>Outcome of <see cref="IRewardService.TryClaim"/> — an expected gameplay failure is
    /// a normal result, never an exception.</summary>
    public enum RewardClaimResult
    {
        Success,

        /// <summary>Only possible for <see cref="RewardClaimPolicy.Once"/> — this is the idempotency
        /// guard: a second <see cref="IRewardService.TryClaim"/> call never grants twice.</summary>
        AlreadyClaimed,

        InvalidId,

        /// <summary>The reward's content reported it could not (or did not) grant successfully —
        /// see <see cref="RewardGrantResult.FailureDetail"/> in the log for specifics.</summary>
        GrantFailed
    }
}
