namespace GameFramework.UI
{
    /// <summary>
    /// How <see cref="UIService"/>'s layer canvases render. Deliberately mirrors only the two
    /// full-screen modes of <see cref="UnityEngine.RenderMode"/> — <c>WorldSpace</c> isn't exposed,
    /// since it doesn't fit a stack of full-screen layered canvases the way <see cref="UILayer"/>
    /// models it; a screen/popup that genuinely needs world-space UI is better served by its own
    /// plain <see cref="UnityEngine.Canvas"/>, outside this service.
    /// </summary>
    public enum UIRenderMode
    {
        /// <summary>Renders on top of everything with no camera dependency — the framework's
        /// original zero-setup default. Unaffected by camera zoom, shake, or post-processing.</summary>
        ScreenSpaceOverlay,

        /// <summary>Renders through <see cref="UICanvasConfig.WorldCamera"/>
        /// (<see cref="UnityEngine.RenderMode.ScreenSpaceCamera"/>) — for 2D games where UI must
        /// share a camera stack with the world (URP camera stacking, camera-driven zoom/shake or
        /// post-processing that should also affect UI). Falls back to
        /// <see cref="ScreenSpaceOverlay"/>, with a warning, if no camera is provided.</summary>
        ScreenSpaceCamera
    }
}
