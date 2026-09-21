using GameFramework.UI.Navigation;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>Well-known stable ids for this sample's screens/popups - content, not framework.</summary>
    public static class Phase12ScreenIds
    {
        public static readonly UIScreenId MainMenu = new UIScreenId("MainMenu");
        public static readonly UIScreenId CharacterSelection = new UIScreenId("CharacterSelection");
        public static readonly UIScreenId CarSelection = new UIScreenId("CarSelection");
        public static readonly UIScreenId Customization = new UIScreenId("Customization");

        public static readonly UIPopupId Settings = new UIPopupId("Settings");
        public static readonly UIPopupId ConfirmExit = new UIPopupId("ConfirmExit");
    }
}
