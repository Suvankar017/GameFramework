using GameFramework.UI;

namespace GameFramework.UI.Navigation.Tests
{
    internal sealed class TestNavScreen : UIScreen, IUINavigationParameterReceiver
    {
        public int OpenedCount;
        public object ReceivedParameters;
        public bool HasReceivedParameters;

        protected override void OnOpened() => OpenedCount++;

        public void OnNavigationParameters(object parameters)
        {
            ReceivedParameters = parameters;
            HasReceivedParameters = true;
        }
    }
}
