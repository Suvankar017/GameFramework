using System;
using System.Collections.Generic;

namespace GameFramework.DeepLinks.Tests
{
    /// <summary>Fully controllable <see cref="IDeepLinkHandler"/> test double.</summary>
    internal sealed class FakeDeepLinkHandler : IDeepLinkHandler
    {
        public Func<DeepLink, bool> CanHandlePredicate { get; set; } = _ => true;
        public DeepLinkHandlerResult ResultToReturn { get; set; } = DeepLinkHandlerResult.Handled;
        public Exception ThrowOnHandle { get; set; }
        public List<DeepLink> HandledLinks { get; } = new List<DeepLink>();
        public int CanHandleCallCount { get; private set; }
        public int HandleCallCount { get; private set; }

        public bool CanHandle(DeepLink link)
        {
            CanHandleCallCount++;
            return CanHandlePredicate(link);
        }

        public DeepLinkHandlerResult Handle(DeepLink link)
        {
            HandleCallCount++;
            HandledLinks.Add(link);

            if (ThrowOnHandle != null)
            {
                throw ThrowOnHandle;
            }

            return ResultToReturn;
        }
    }
}
