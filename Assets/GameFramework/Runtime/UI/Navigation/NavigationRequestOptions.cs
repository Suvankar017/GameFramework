using System;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Optional per-call data for a navigation command (CLAUDE.md's Phase 12 brief, section 13).
    /// Kept as one small struct rather than a growing list of optional method parameters - default
    /// (all fields null/false) is exactly "no parameters, no result callback, don't pause".
    /// </summary>
    public readonly struct NavigationRequestOptions
    {
        /// <summary>Delivered to the opened screen/popup, if it implements
        /// <see cref="IUINavigationParameterReceiver"/>, before its own <c>OnOpened</c> fires. Kept
        /// as <see cref="object"/> rather than a generic parameter - the call site is already
        /// type-safe (it's the caller's own typed local being boxed here), and the receiver does one
        /// explicit cast for the type it expects (CLAUDE.md's Phase 12 brief, section 19: "type-safe
        /// where practical" - not a generic <see cref="INavigationService"/> API surface for it).</summary>
        public readonly object Parameters;

        /// <summary>Invoked exactly once, when the opened screen/popup is later popped/closed
        /// (including via back navigation) - the "Screen Return Results" mechanism (CLAUDE.md's
        /// Phase 12 brief, section 20). Receives whatever result value the pop/close call supplied,
        /// or null if none was.</summary>
        public readonly Action<object> ResultCallback;

        /// <summary>Popups only: when true and <c>GameFlow.IGameFlowService</c> is registered,
        /// acquires a <c>GameFlow.IPauseToken</c> for as long as this popup stays open, releasing it
        /// automatically on close - reuses Phase 8's existing reference-counted pause rather than
        /// touching <c>Runtime.Time.ITimeService</c> directly (CLAUDE.md's Phase 12 brief, section 26).</summary>
        public readonly bool PausesGameplay;

        /// <summary>Free-form reason passed to <c>IGameFlowService.PauseGameplay</c> - defaults to
        /// <c>"UI.Popup:{id}"</c> when omitted.</summary>
        public readonly string PauseReason;

        public NavigationRequestOptions(
            object parameters = null,
            Action<object> resultCallback = null,
            bool pausesGameplay = false,
            string pauseReason = null)
        {
            Parameters = parameters;
            ResultCallback = resultCallback;
            PausesGameplay = pausesGameplay;
            PauseReason = pauseReason;
        }
    }
}
