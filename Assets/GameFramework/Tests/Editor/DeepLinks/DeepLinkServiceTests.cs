using System;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.DeepLinks.Tests
{
    public class DeepLinkServiceTests
    {
        private EventService _events;
        private DeepLinkService _service;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out _events);
            _service = new DeepLinkService();
            _service.Initialize(registry);
        }

        [TearDown]
        public void TearDown() => _service.Shutdown();

        [Test]
        public void Process_NotReady_DefersAndReturnsPending()
        {
            DeepLinkResult result = _service.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Deferred, result.Kind);
            Assert.AreEqual("mygame://daily-reward", _service.PendingRawUri);
        }

        [Test]
        public void SetReady_WithPendingLink_DispatchesExactlyOnce()
        {
            var handler = new FakeDeepLinkHandler();
            _service.RegisterHandler(handler);

            _service.Process("mygame://daily-reward");
            _service.SetReady(true);

            Assert.AreEqual(1, handler.HandleCallCount);
            Assert.IsNull(_service.PendingRawUri);

            // Toggling ready again must not re-dispatch the same (now-consumed) pending link.
            _service.SetReady(false);
            _service.SetReady(true);
            Assert.AreEqual(1, handler.HandleCallCount);
        }

        [Test]
        public void Process_Ready_DispatchesImmediately()
        {
            _service.SetReady(true);
            var handler = new FakeDeepLinkHandler();
            _service.RegisterHandler(handler);

            DeepLinkResult result = _service.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Handled, result.Kind);
            Assert.AreEqual(1, handler.HandleCallCount);
        }

        [Test]
        public void Process_HandlerPriority_HigherPriorityTriedFirst()
        {
            _service.SetReady(true);
            var low = new FakeDeepLinkHandler();
            var high = new FakeDeepLinkHandler();
            _service.RegisterHandler(low, priority: 0);
            _service.RegisterHandler(high, priority: 10);

            _service.Process("mygame://daily-reward");

            Assert.AreEqual(1, high.HandleCallCount);
            Assert.AreEqual(0, low.HandleCallCount);
        }

        [Test]
        public void Process_FirstHandlerNotApplicable_FallsThroughToNext()
        {
            _service.SetReady(true);
            var first = new FakeDeepLinkHandler { CanHandlePredicate = _ => false };
            var second = new FakeDeepLinkHandler();
            _service.RegisterHandler(first, priority: 10);
            _service.RegisterHandler(second, priority: 0);

            DeepLinkResult result = _service.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Handled, result.Kind);
            Assert.AreEqual(0, first.HandleCallCount);
            Assert.AreEqual(1, second.HandleCallCount);
        }

        [Test]
        public void Process_NoHandlerClaimsLink_ReturnsNoHandlerFound()
        {
            _service.SetReady(true);
            _service.RegisterHandler(new FakeDeepLinkHandler { CanHandlePredicate = _ => false });

            DeepLinkResult result = _service.Process("mygame://unknown-route");

            Assert.AreEqual(DeepLinkResultKind.NoHandlerFound, result.Kind);
        }

        [Test]
        public void Process_HandlerThrows_TreatedAsFailedAndDoesNotPropagate()
        {
            _service.SetReady(true);
            var handler = new FakeDeepLinkHandler { ThrowOnHandle = new InvalidOperationException("boom") };
            _service.RegisterHandler(handler);

            DeepLinkResult result = _service.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.NoHandlerFound, result.Kind);
        }

        [Test]
        public void Process_MalformedUri_ReturnsRejected()
        {
            _service.SetReady(true);

            DeepLinkResult result = _service.Process("not a uri");

            Assert.AreEqual(DeepLinkResultKind.Rejected, result.Kind);
        }

        [Test]
        public void Process_IdenticalConsecutiveUri_ReturnsDuplicate()
        {
            _service.SetReady(true);
            _service.RegisterHandler(new FakeDeepLinkHandler());

            _service.Process("mygame://daily-reward");
            DeepLinkResult second = _service.Process("mygame://daily-reward");

            Assert.AreEqual(DeepLinkResultKind.Duplicate, second.Kind);
        }

        [Test]
        public void Process_DifferentUriAfterDuplicateWindow_IsProcessedNormally()
        {
            _service.SetReady(true);
            var handler = new FakeDeepLinkHandler();
            _service.RegisterHandler(handler);

            _service.Process("mygame://daily-reward");
            _service.Process("mygame://weekly-reward");

            Assert.AreEqual(2, handler.HandleCallCount);
        }

        [Test]
        public void Process_PublishesDeepLinkReceivedAndHandledEvents()
        {
            _service.SetReady(true);
            _service.RegisterHandler(new FakeDeepLinkHandler());

            bool receivedPublished = false;
            bool handledPublished = false;
            _events.Subscribe<DeepLinkReceivedEvent>(_ => receivedPublished = true);
            _events.Subscribe<DeepLinkHandledEvent>(_ => handledPublished = true);

            _service.Process("mygame://daily-reward");

            Assert.IsTrue(receivedPublished);
            Assert.IsTrue(handledPublished);
        }

        [Test]
        public void Process_Rejected_PublishesDeepLinkRejectedEvent()
        {
            _service.SetReady(true);

            string rejectedReason = null;
            _events.Subscribe<DeepLinkRejectedEvent>(evt => rejectedReason = evt.Reason);

            _service.Process("not a uri");

            Assert.IsNotNull(rejectedReason);
        }

        [Test]
        public void UnregisterHandler_StopsReceivingDispatches()
        {
            _service.SetReady(true);
            var handler = new FakeDeepLinkHandler();
            _service.RegisterHandler(handler);
            _service.UnregisterHandler(handler);

            _service.Process("mygame://daily-reward");

            Assert.AreEqual(0, handler.HandleCallCount);
        }
    }
}
