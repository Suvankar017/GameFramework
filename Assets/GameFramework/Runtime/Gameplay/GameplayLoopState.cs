namespace GameFramework.Gameplay
{
    /// <summary>Current state of <see cref="IGameplayService"/>'s gameplay session.</summary>
    public enum GameplayLoopState
    {
        /// <summary>No session is active. The loop calls no tickables and no lifecycle phase past
        /// <see cref="IGameplayLifecycle.OnGameplayInitialize"/> has fired for new registrants.</summary>
        Inactive,

        /// <summary>A session is active and not paused — tickables are ticked every frame.</summary>
        Playing,

        /// <summary>A session is active but <see cref="Runtime.Time.ITimeService.IsPaused"/> is
        /// true — tickables are not ticked; the session resumes automatically once unpaused.</summary>
        Paused
    }
}
