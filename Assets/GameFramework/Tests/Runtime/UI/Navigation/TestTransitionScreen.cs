using System.Collections;
using GameFramework.UI;

namespace GameFramework.UI.Navigation.Tests
{
    internal sealed class TestTransitionScreen : UIScreen, IUINavigationTransitionHandler
    {
        public int FramesToWait = 2;
        public bool TransitionCompleted;

        public IEnumerator PlayEnterTransition()
        {
            for (int i = 0; i < FramesToWait; i++)
            {
                yield return null;
            }

            TransitionCompleted = true;
        }
    }
}
