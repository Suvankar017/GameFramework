namespace GameFramework.Presentation
{
    /// <summary>
    /// Seam between <see cref="PresentationService"/> and whatever a game's own camera actually is -
    /// this framework never assumes a specific camera controller/rig (see CLAUDE.md's Phase 10
    /// brief, section 11). A game attaches <see cref="CameraFeedbackDriver"/> (or its own
    /// implementation) to its active camera and registers it via
    /// <see cref="IPresentationService.RegisterCameraDriver"/>; <see cref="CameraFeedbackConfig"/>
    /// requests are forwarded here. With no driver registered, camera feedback silently no-ops
    /// (logged once) rather than throwing - a content-authoring gap, not a framework failure.
    ///
    /// Note: this namespace is deliberately flat (<c>GameFramework.Presentation</c>, not
    /// <c>GameFramework.Presentation.Camera</c>) even though these files live under a
    /// <c>Camera/</c> folder - a nested <c>.Camera</c> namespace segment would shadow
    /// <see cref="UnityEngine.Camera"/> for any bare `Camera` reference in this file's own
    /// ancestor-namespace chain, the same class of bug documented for `GameFramework.Input` vs.
    /// `UnityEngine.Input`. The same reasoning keeps `Screen/`/`Time/` flat too (both collide with
    /// `UnityEngine.Screen`/`UnityEngine.Time`).
    /// </summary>
    public interface ICameraFeedbackDriver
    {
        /// <summary>Adds one shake to the composed set currently affecting the camera - multiple
        /// simultaneous requests sum (see <see cref="CameraShakeState"/>'s remarks), they never
        /// replace each other.</summary>
        void RequestShake(CameraShakeRequest request);

        /// <summary>Cancels every currently active shake immediately, snapping back to the driver's
        /// undisturbed base pose.</summary>
        void CancelAll();
    }
}
