namespace GameFramework.UI
{
    public enum UIScreenState
    {
        /// <summary>Not on the screen stack (either never opened, or closed and destroyed).</summary>
        Closed,

        /// <summary>On the stack and the current top — visible and interactive.</summary>
        Opened,

        /// <summary>On the stack but covered by a screen pushed on top of it — not visible, still
        /// instantiated.</summary>
        Hidden
    }
}
