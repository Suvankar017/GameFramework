using GameFramework.UI;

namespace GameFramework.UI.Navigation.Tests
{
    internal sealed class TestNavPopup : UIPopup, IUINavigationParameterReceiver
    {
        public int OpenedCount;
        public object ReceivedParameters;

        protected override void OnOpened() => OpenedCount++;

        public void OnNavigationParameters(object parameters) => ReceivedParameters = parameters;
    }
}
