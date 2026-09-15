using System;
using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Base type for a popup/modal opened via <see cref="IUIService.OpenPopup{T}"/>. A modal popup
    /// (<see cref="IsModal"/>) automatically blocks UI input to everything below it — see
    /// <see cref="UIService"/>'s modal blocker — with no per-caller input-disabling required.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class UIPopup : MonoBehaviour
    {
        [SerializeField] private bool _isModal = true;

        public bool IsModal => _isModal;
        public bool IsOpen { get; private set; }

        public event Action<UIPopupResult> Closed;

        internal IUIService Owner { get; set; }

        public void Close(UIPopupResult result = UIPopupResult.None) => Owner?.ClosePopup(this, result);

        internal void InternalOpen()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            OnOpened();
        }

        internal void InternalClose(UIPopupResult result)
        {
            IsOpen = false;
            OnClosed(result);
            Closed?.Invoke(result);
            UIObjectUtility.DestroySafely(gameObject);
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnClosed(UIPopupResult result)
        {
        }
    }
}
