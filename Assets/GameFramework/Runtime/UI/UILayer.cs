namespace GameFramework.UI
{
    /// <summary>
    /// UI rendering/interaction layers, back-to-front. The underlying int is used directly as the
    /// layer's <see cref="UnityEngine.Canvas.sortingOrder"/> — higher layers always render, and
    /// receive raycasts, above lower ones.
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Game = 1,
        HUD = 2,
        Overlay = 3,
        Popup = 4,
        Modal = 5,
        Debug = 6
    }
}
