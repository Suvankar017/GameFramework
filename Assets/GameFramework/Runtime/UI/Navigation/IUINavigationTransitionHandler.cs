using System.Collections;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Optional hook a <see cref="UIScreen"/>/<see cref="UIPopup"/> subclass implements to play a
    /// visual transition after it becomes the new current entry (CLAUDE.md's Phase 12 brief, section
    /// 22). <see cref="INavigationService"/> runs this coroutine on its own internal driver and
    /// keeps <see cref="INavigationService.IsNavigating"/> true until it finishes, which is what
    /// blocks a second navigation request from interrupting a transition mid-flight (section 23).
    ///
    /// Deliberately enter-only: Phase 3's <see cref="UIScreen"/>/<see cref="UIPopup"/> destroy their
    /// GameObject synchronously the moment <c>IUIService.CloseScreen</c>/<c>ClosePopup</c> is called,
    /// with no seam for this layer to defer that destruction - an "exit transition" hook here could
    /// not actually finish playing before the object it's animating is destroyed, so it isn't
    /// offered. A screen wanting a guaranteed pre-destroy exit animation should play it synchronously
    /// inside its own <see cref="UIScreen.OnClosed"/>/<see cref="UIScreen.OnHidden"/> override
    /// (Phase 3's existing hooks) instead.
    /// </summary>
    public interface IUINavigationTransitionHandler
    {
        IEnumerator PlayEnterTransition();
    }
}
