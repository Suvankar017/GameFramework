using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using TMPro;
using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Refreshes its displayed text (and, for the TMP subclass, font) from
    /// <see cref="ILocalizationService"/> on enable and whenever <see cref="LanguageChangedEvent"/>
    /// fires — no screen needs to poll the current language itself. A no-op (not an error) if the
    /// bootstrap isn't <see cref="BootstrapState.Ready"/> yet, e.g. a prefab previewed in the
    /// Editor outside Play Mode.
    /// </summary>
    public abstract class LocalizedTextBase : MonoBehaviour
    {
        [SerializeField] private string _key;

        private IEventSubscription _subscription;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Refresh();
            }
        }

        protected virtual void OnEnable()
        {
            Refresh();

            if (IsBootstrapReady() && GameBootstrapper.Instance.Services.TryGet(out IEventService events))
            {
                _subscription = events.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            }
        }

        protected virtual void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnLanguageChanged(LanguageChangedEvent evt) => Refresh();

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_key) || !IsBootstrapReady())
            {
                return;
            }

            if (!GameBootstrapper.Instance.Services.TryGet(out ILocalizationService localization))
            {
                return;
            }

            ApplyText(localization.GetString(_key));
            ApplyFont(localization.CurrentFontAsset);
        }

        private static bool IsBootstrapReady() =>
            GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready;

        protected abstract void ApplyText(string value);

        /// <summary>No-op by default; only the TextMeshPro subclass has a font to swap.</summary>
        protected virtual void ApplyFont(TMP_FontAsset font)
        {
        }
    }
}
