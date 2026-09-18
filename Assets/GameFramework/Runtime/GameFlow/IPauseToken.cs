namespace GameFramework.GameFlow
{
    /// <summary>
    /// A single outstanding gameplay-pause request, obtained from
    /// <see cref="IGameFlowService.PauseGameplay"/>. The underlying pause is fully owned by
    /// <see cref="Runtime.Time.ITimeService"/>'s existing reference-counted Pause/Resume (see that
    /// interface's remarks) - this token is a labeled, one-shot handle over exactly one
    /// Pause/Resume pair, so multiple independent owners (a tutorial, a system dialog, ...) compose
    /// correctly: releasing one token never resumes gameplay while another is still outstanding.
    /// </summary>
    public interface IPauseToken
    {
        /// <summary>The free-form reason this pause was requested for - see
        /// <see cref="IGameFlowService.ActivePauseReasons"/>.</summary>
        string Reason { get; }

        bool IsActive { get; }

        /// <summary>Releases this pause request. Safe to call more than once.</summary>
        void Release();
    }
}
