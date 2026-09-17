namespace GameFramework.Unlocks
{
    /// <summary>Published by <see cref="UnlockService"/> through the Phase 2 Event System the
    /// moment an unlock actually becomes unlocked (never published for an already-unlocked or
    /// rejected <see cref="IUnlockService.TryUnlock"/> call).</summary>
    public readonly struct UnlockChangedEvent
    {
        public readonly UnlockId Unlock;
        public readonly string Reason;

        public UnlockChangedEvent(UnlockId unlock, string reason)
        {
            Unlock = unlock;
            Reason = reason;
        }
    }
}
