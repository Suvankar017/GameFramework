using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    /// <summary>Sets an <see cref="Image"/>'s sprite from a per-language asset entry (e.g. a flag
    /// icon, or art containing text) — see <see cref="ILocalizationService.TryGetAsset{TAsset}"/>.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class LocalizedImage : MonoBehaviour
    {
        [SerializeField] private string _key;

        private Image _image;
        private Image ImageComponent => _image != null ? _image : (_image = GetComponent<Image>());
        private IEventSubscription _subscription;

        private void OnEnable()
        {
            Refresh();

            if (IsBootstrapReady() && GameBootstrapper.Instance.Services.TryGet(out IEventService events))
            {
                _subscription = events.Subscribe<LanguageChangedEvent>(_ => Refresh());
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_key) || !IsBootstrapReady())
            {
                return;
            }

            if (GameBootstrapper.Instance.Services.TryGet(out ILocalizationService localization) &&
                localization.TryGetAsset(_key, out Sprite sprite))
            {
                ImageComponent.sprite = sprite;
            }
        }

        private static bool IsBootstrapReady() =>
            GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready;
    }
}
