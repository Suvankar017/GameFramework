using GameFramework.UI;

namespace GameFramework.UI.Navigation.Tests
{
    internal sealed class TestBackHandlerScreen : UIScreen, IUINavigationBackHandler
    {
        public bool ConsumeBack;
        public int BackRequestedCount;

        public bool OnBackRequested()
        {
            BackRequestedCount++;
            return ConsumeBack;
        }
    }
}
