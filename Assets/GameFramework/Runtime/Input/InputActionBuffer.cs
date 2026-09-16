namespace GameFramework.Input
{
    /// <summary>
    /// Opt-in short-lived input buffering (e.g. a jump pressed slightly before landing should
    /// still fire once landing becomes possible). Not part of <see cref="IInputService"/> itself —
    /// a gameplay feature that needs this instantiates one per buffered action.
    /// </summary>
    public sealed class InputActionBuffer
    {
        private readonly float _bufferWindowSeconds;
        private float _pressedAtTime = float.NegativeInfinity;

        public InputActionBuffer(float bufferWindowSeconds)
        {
            _bufferWindowSeconds = bufferWindowSeconds;
        }

        /// <summary>Call every frame with whether the action was pressed this frame and the
        /// current unscaled/scaled time (caller decides which, via <c>ITimeService</c>).</summary>
        public void Record(bool wasPressedThisFrame, float currentTime)
        {
            if (wasPressedThisFrame)
            {
                _pressedAtTime = currentTime;
            }
        }

        /// <summary>Returns true at most once per buffered press: if a press was recorded within
        /// the buffer window, consumes it (so it cannot fire again) and returns true.</summary>
        public bool ConsumeIfBuffered(float currentTime)
        {
            if (currentTime - _pressedAtTime > _bufferWindowSeconds)
            {
                return false;
            }

            _pressedAtTime = float.NegativeInfinity;
            return true;
        }
    }
}
