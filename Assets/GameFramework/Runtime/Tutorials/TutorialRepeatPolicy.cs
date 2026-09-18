namespace GameFramework.Tutorials
{
    /// <summary>Whether a completed tutorial can be started again - see <see cref="ITutorialService.Start"/>.</summary>
    public enum TutorialRepeatPolicy
    {
        /// <summary>Once completed (normally or via skip), never starts again - the persisted
        /// completion flag blocks it across application restarts too.</summary>
        Once,

        /// <summary>Always startable again once the previous run finished.</summary>
        Repeatable,

        /// <summary>Startable once per application session - completing it blocks further starts
        /// only until the application restarts, regardless of what is persisted.</summary>
        OncePerSession
    }
}
