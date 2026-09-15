using System;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Events
{
    public class EventServiceTests
    {
        private readonly struct TestEventA
        {
            public readonly int Value;
            public TestEventA(int value) => Value = value;
        }

        private readonly struct TestEventB
        {
        }

        private EventService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new EventService();
            _service.Initialize(new ServiceRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.Publish(new TestEventA(1)));
        }

        [Test]
        public void Subscribe_ThenPublish_InvokesHandlerWithPayload()
        {
            TestEventA received = default;
            bool called = false;
            _service.Subscribe<TestEventA>(e => { received = e; called = true; });

            _service.Publish(new TestEventA(42));

            Assert.IsTrue(called);
            Assert.AreEqual(42, received.Value);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllInvoked()
        {
            int count = 0;
            _service.Subscribe<TestEventA>(_ => count++);
            _service.Subscribe<TestEventA>(_ => count++);

            _service.Publish(new TestEventA(1));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Subscribe_SameDelegateTwice_OnlyInvokedOnce()
        {
            int count = 0;
            Action<TestEventA> handler = _ => count++;
            _service.Subscribe(handler);
            _service.Subscribe(handler);

            _service.Publish(new TestEventA(1));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_StopsFurtherInvocation()
        {
            int count = 0;
            Action<TestEventA> handler = _ => count++;
            _service.Subscribe(handler);

            _service.Unsubscribe(handler);
            _service.Publish(new TestEventA(1));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Unsubscribe_UnknownHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.Unsubscribe<TestEventA>(_ => { }));
        }

        [Test]
        public void SubscriptionDispose_UnsubscribesHandler()
        {
            int count = 0;
            IEventSubscription subscription = _service.Subscribe<TestEventA>(_ => count++);

            subscription.Dispose();
            _service.Publish(new TestEventA(1));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void SubscriptionDispose_CalledTwice_IsSafe()
        {
            IEventSubscription subscription = _service.Subscribe<TestEventA>(_ => { });

            subscription.Dispose();

            Assert.DoesNotThrow(() => subscription.Dispose());
        }

        [Test]
        public void DifferentEventTypes_AreIndependent()
        {
            int aCount = 0;
            int bCount = 0;
            _service.Subscribe<TestEventA>(_ => aCount++);
            _service.Subscribe<TestEventB>(_ => bCount++);

            _service.Publish(new TestEventA(1));

            Assert.AreEqual(1, aCount);
            Assert.AreEqual(0, bCount);
        }

        [Test]
        public void SubscriberException_IsCaughtAndOtherSubscribersStillRun()
        {
            int secondCalled = 0;
            _service.Subscribe<TestEventA>(_ => throw new InvalidOperationException("boom"));
            _service.Subscribe<TestEventA>(_ => secondCalled++);

            Assert.DoesNotThrow(() => _service.Publish(new TestEventA(1)));

            Assert.AreEqual(1, secondCalled);
        }

        [Test]
        public void HandlerUnsubscribesItselfDuringPublish_StillRunsThisTime_GoneNextTime()
        {
            int callCount = 0;
            Action<TestEventA> handler = null;
            handler = _ => { callCount++; _service.Unsubscribe(handler); };
            _service.Subscribe(handler);

            _service.Publish(new TestEventA(1));
            _service.Publish(new TestEventA(1));

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void NestedPublish_SameEventType_BothLevelsRun()
        {
            var order = new List<string>();
            _service.Subscribe<TestEventA>(e =>
            {
                order.Add("outer");
                if (e.Value == 1)
                {
                    _service.Publish(new TestEventA(2));
                }
            });

            _service.Publish(new TestEventA(1));

            CollectionAssert.AreEqual(new[] { "outer", "outer" }, order);
        }

        [Test]
        public void HandlerSubscribingAnotherDuringPublish_NewHandlerNotInvokedUntilNextPublish()
        {
            int secondCallCount = 0;
            _service.Subscribe<TestEventA>(_ => { _service.Subscribe<TestEventA>(__ => secondCallCount++); });

            _service.Publish(new TestEventA(1));
            Assert.AreEqual(0, secondCallCount);

            _service.Publish(new TestEventA(1));
            Assert.AreEqual(1, secondCallCount);
        }

        [Test]
        public void Shutdown_ClearsAllSubscriptions()
        {
            int count = 0;
            _service.Subscribe<TestEventA>(_ => count++);

            _service.Shutdown();
            _service.Publish(new TestEventA(1));

            Assert.AreEqual(0, count);
        }
    }
}
