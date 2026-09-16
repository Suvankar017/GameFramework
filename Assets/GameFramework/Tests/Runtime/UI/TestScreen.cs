namespace GameFramework.UI.Tests
{
    internal sealed class TestScreen : UIScreen
    {
        public int OpenedCount;
        public int HiddenCount;
        public int ShownCount;
        public int ClosedCount;

        protected override void OnOpened() => OpenedCount++;
        protected override void OnHidden() => HiddenCount++;
        protected override void OnShown() => ShownCount++;
        protected override void OnClosed() => ClosedCount++;
    }
}
