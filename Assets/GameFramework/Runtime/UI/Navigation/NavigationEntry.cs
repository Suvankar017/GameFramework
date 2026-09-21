using System;
using GameFramework.UI;

namespace GameFramework.UI.Navigation
{
    /// <summary>One entry on <see cref="NavigationService"/>'s own screen stack - id + live instance
    /// + the result callback supplied when it was pushed. Internal bookkeeping only.</summary>
    internal sealed class NavigationEntry
    {
        public UIScreenId Id;
        public UIScreen Screen;
        public Action<object> ResultCallback;
    }
}
