using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>Thin wrapper over <see cref="UnityEngine.GUIUtility.systemCopyBuffer"/> - Unity's
    /// own clipboard API is already cross-platform (Editor/Windows/macOS/Android/iOS), so no
    /// platform-specific implementation is needed (see CLAUDE.md's Phase 14 brief, section 18).</summary>
    public interface IClipboardService : IGameService
    {
        bool IsSupported { get; }

        void SetText(string text);

        string GetText();

        bool HasText();
    }
}
