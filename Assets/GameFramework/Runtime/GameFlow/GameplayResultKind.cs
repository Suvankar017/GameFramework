namespace GameFramework.GameFlow
{
    public enum GameplayResultKind
    {
        /// <summary>The default value of <see cref="GameplayResult"/> - "no result supplied by the
        /// caller." <see cref="GameFlowService.CompleteLevel"/>/<see cref="GameFlowService.FailLevel"/>
        /// substitute their own obvious default (Success/Failure respectively) when they see this,
        /// so a caller that doesn't care about the reason can just omit the argument.</summary>
        Unspecified,

        Success,
        Failure,
        Aborted,
        Cancelled
    }
}
