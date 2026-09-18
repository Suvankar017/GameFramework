using System;

namespace GameFramework.GameFlow
{
    internal sealed class PauseToken : IPauseToken
    {
        private readonly Action<PauseToken> _onRelease;

        internal PauseToken(string reason, Action<PauseToken> onRelease)
        {
            Reason = reason;
            _onRelease = onRelease;
            IsActive = true;
        }

        public string Reason { get; }
        public bool IsActive { get; private set; }

        public void Release()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _onRelease?.Invoke(this);
        }
    }
}
