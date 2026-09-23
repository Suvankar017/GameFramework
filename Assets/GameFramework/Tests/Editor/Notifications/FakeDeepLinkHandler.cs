using System;
using System.Collections.Generic;
using GameFramework.DeepLinks;

namespace GameFramework.Notifications.Tests
{
    /// <summary>See <c>DeepLinks.Tests.FakeDeepLinkHandler</c>'s remarks - same small test double,
    /// duplicated per test assembly rather than referencing another assembly's test-only internals.</summary>
    internal sealed class FakeDeepLinkHandler : IDeepLinkHandler
    {
        public Func<DeepLink, bool> CanHandlePredicate { get; set; } = _ => true;
        public DeepLinkHandlerResult ResultToReturn { get; set; } = DeepLinkHandlerResult.Handled;
        public List<DeepLink> HandledLinks { get; } = new List<DeepLink>();
        public int HandleCallCount { get; private set; }

        public bool CanHandle(DeepLink link) => CanHandlePredicate(link);

        public DeepLinkHandlerResult Handle(DeepLink link)
        {
            HandleCallCount++;
            HandledLinks.Add(link);
            return ResultToReturn;
        }
    }
}
