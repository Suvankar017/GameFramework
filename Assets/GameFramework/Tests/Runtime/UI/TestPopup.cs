namespace GameFramework.UI.Tests
{
    internal sealed class TestPopup : UIPopup
    {
        public int OpenedCount;
        public UIPopupResult LastResult = UIPopupResult.None;
        public bool WasClosed;

        protected override void OnOpened() => OpenedCount++;

        protected override void OnClosed(UIPopupResult result)
        {
            LastResult = result;
            WasClosed = true;
        }
    }
}
