using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>Optional hook a <see cref="UIScreen"/>/<see cref="UIPopup"/> subclass implements to
    /// receive <see cref="NavigationRequestOptions.Parameters"/> - see that field's remarks for why
    /// this is untyped <see cref="object"/> rather than a generic interface.</summary>
    public interface IUINavigationParameterReceiver
    {
        /// <summary>Called once, immediately after instantiation but before the screen/popup's own
        /// <c>OnOpened</c> - see <see cref="IUIService.OpenScreen{T}"/>'s <c>onBeforeOpen</c> remarks.</summary>
        void OnNavigationParameters(object parameters);
    }
}
