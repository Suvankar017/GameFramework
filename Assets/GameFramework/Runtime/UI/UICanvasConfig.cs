using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Configuration for the canvases <see cref="UIService"/> builds for every <see cref="UILayer"/>.
    /// Passed to <see cref="UIService"/>'s constructor; the default values reproduce the framework's
    /// original zero-setup Screen Space - Overlay behavior exactly, so existing games that never
    /// pass one are unaffected.
    /// </summary>
    public sealed class UICanvasConfig
    {
        public UIRenderMode RenderMode = UIRenderMode.ScreenSpaceOverlay;

        /// <summary>Required when <see cref="RenderMode"/> is
        /// <see cref="UIRenderMode.ScreenSpaceCamera"/> — the camera every layer's <see cref="Canvas"/>
        /// renders through. Ignored in <see cref="UIRenderMode.ScreenSpaceOverlay"/> mode.</summary>
        public Camera WorldCamera;

        /// <summary><see cref="UIRenderMode.ScreenSpaceCamera"/> only: distance from the camera at
        /// which each canvas plane sits. Layers still sort correctly via
        /// <see cref="Canvas.sortingOrder"/> regardless of this value — it only needs to stay within
        /// <see cref="WorldCamera"/>'s near/far clip planes.</summary>
        public float PlaneDistance = 100f;
    }
}
