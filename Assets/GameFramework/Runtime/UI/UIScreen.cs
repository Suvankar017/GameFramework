using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Base type for a stack-navigable full screen (e.g. Main Menu → Settings → Language). Opened
    /// via <see cref="IUIService.OpenScreen{T}"/>, never by hand-instantiating a prefab — that's
    /// what wires up <see cref="Layer"/> parenting and stack bookkeeping.
    /// Lifecycle: Closed → Opened → (Hidden while covered) → Opened again → Closed (destroyed).
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private UILayer _layer = UILayer.Game;

        public UILayer Layer => _layer;
        public UIScreenState State { get; private set; } = UIScreenState.Closed;

        internal IUIService Owner { get; set; }

        /// <summary>Closes this screen through its owning <see cref="IUIService"/> — a no-op if it
        /// isn't the current top screen (see <see cref="IUIService.CloseScreen"/>).</summary>
        public void Close() => Owner?.CloseScreen(this);

        internal void InternalOpen()
        {
            gameObject.SetActive(true);
            State = UIScreenState.Opened;
            OnOpened();
        }

        internal void InternalHide()
        {
            State = UIScreenState.Hidden;
            OnHidden();
            gameObject.SetActive(false);
        }

        internal void InternalShow()
        {
            gameObject.SetActive(true);
            State = UIScreenState.Opened;
            OnShown();
        }

        internal void InternalClose()
        {
            State = UIScreenState.Closed;
            OnClosed();
            UIObjectUtility.DestroySafely(gameObject);
        }

        /// <summary>Called when this screen becomes the visible top of the stack for the first
        /// time. Override for one-time animation/setup — a lightweight hook, not a tweening
        /// framework: play whatever animation is needed via this MonoBehaviour's own
        /// <see cref="MonoBehaviour.StartCoroutine(System.Collections.IEnumerator)"/>.</summary>
        protected virtual void OnOpened()
        {
        }

        /// <summary>Called when a screen is pushed on top of this one.</summary>
        protected virtual void OnHidden()
        {
        }

        /// <summary>Called when the screen above this one is popped, making this one visible again.</summary>
        protected virtual void OnShown()
        {
        }

        /// <summary>Called when this screen is popped and about to be destroyed.</summary>
        protected virtual void OnClosed()
        {
        }
    }
}
