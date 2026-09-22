using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    public sealed class ClipboardService : IClipboardService
    {
        public bool IsSupported => true;

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void SetText(string text)
        {
            GUIUtility.systemCopyBuffer = text ?? string.Empty;
        }

        public string GetText()
        {
            return GUIUtility.systemCopyBuffer;
        }

        public bool HasText()
        {
            return !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);
        }
    }
}
