namespace GameFramework.Rewards
{
    /// <summary>
    /// One grantable thing — currency, an item, XP, an unlock, or a <see cref="RewardBundle"/> of
    /// several. Mirrors <c>IGameplayCommand</c>'s CanExecute/Execute split deliberately:
    /// <see cref="RewardService"/> calls <see cref="CanGrant"/> on an entire reward (recursing
    /// through any bundle) before calling <see cref="Grant"/> on any part of it, so a reward is
    /// never partially applied because one piece turned out to be invalid — see
    /// <see cref="RewardService"/>'s remarks on why this validate-before-mutate approach is the
    /// framework's answer to reward transaction safety, not a true rollback engine.
    /// </summary>
    public interface IReward
    {
        bool CanGrant();

        RewardGrantResult Grant();
    }
}
