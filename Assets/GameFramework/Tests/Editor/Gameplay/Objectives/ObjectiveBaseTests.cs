using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Events;
using NUnit.Framework;

namespace GameFramework.Gameplay.Tests
{
    public class ObjectiveBaseTests
    {
        private sealed class TestObjective : ObjectiveBase
        {
            public int ActivatedCount;
            public int CompletedCount;
            public int FailedCount;
            public int ResetCount;

            public TestObjective(string id, IEventService events = null) : base(id, events)
            {
            }

            protected override void OnActivated() => ActivatedCount++;
            protected override void OnCompleted() => CompletedCount++;
            protected override void OnFailed() => FailedCount++;
            protected override void OnReset() => ResetCount++;
        }

        [Test]
        public void InitialState_IsInactive()
        {
            var objective = new TestObjective("obj1");

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
        }

        [Test]
        public void Activate_FromInactive_TransitionsToActive()
        {
            var objective = new TestObjective("obj1");

            objective.Activate();

            Assert.AreEqual(ObjectiveState.Active, objective.State);
            Assert.AreEqual(1, objective.ActivatedCount);
        }

        [Test]
        public void Complete_FromActive_TransitionsToCompleted()
        {
            var objective = new TestObjective("obj1");
            objective.Activate();

            objective.Complete();

            Assert.AreEqual(ObjectiveState.Completed, objective.State);
            Assert.AreEqual(1, objective.CompletedCount);
        }

        [Test]
        public void Fail_FromActive_TransitionsToFailed()
        {
            var objective = new TestObjective("obj1");
            objective.Activate();

            objective.Fail();

            Assert.AreEqual(ObjectiveState.Failed, objective.State);
            Assert.AreEqual(1, objective.FailedCount);
        }

        [Test]
        public void Complete_WhenNotActive_IsRejectedAndStateUnchanged()
        {
            var objective = new TestObjective("obj1");

            objective.Complete();

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
            Assert.AreEqual(0, objective.CompletedCount);
        }

        [Test]
        public void Complete_CalledTwice_SecondCallIsRejected()
        {
            var objective = new TestObjective("obj1");
            objective.Activate();
            objective.Complete();

            objective.Complete();

            Assert.AreEqual(1, objective.CompletedCount);
        }

        [Test]
        public void Reset_FromCompleted_ReturnsToInactive()
        {
            var objective = new TestObjective("obj1");
            objective.Activate();
            objective.Complete();

            objective.Reset();

            Assert.AreEqual(ObjectiveState.Inactive, objective.State);
            Assert.AreEqual(1, objective.ResetCount);
        }

        [Test]
        public void Reset_FromActive_IsRejected()
        {
            var objective = new TestObjective("obj1");
            objective.Activate();

            objective.Reset();

            Assert.AreEqual(ObjectiveState.Active, objective.State);
            Assert.AreEqual(0, objective.ResetCount);
        }

        [Test]
        public void Activate_PublishesObjectiveActivatedEvent()
        {
            var events = new EventService();
            events.Initialize(new Runtime.Services.ServiceRegistry());
            ObjectiveActivatedEvent? received = null;
            events.Subscribe<ObjectiveActivatedEvent>(e => received = e);
            var objective = new TestObjective("obj1", events);

            objective.Activate();

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("obj1", received.Value.ObjectiveId);
        }

        [Test]
        public void Complete_PublishesObjectiveCompletedEvent()
        {
            var events = new EventService();
            events.Initialize(new Runtime.Services.ServiceRegistry());
            ObjectiveCompletedEvent? received = null;
            events.Subscribe<ObjectiveCompletedEvent>(e => received = e);
            var objective = new TestObjective("obj1", events);
            objective.Activate();

            objective.Complete();

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("obj1", received.Value.ObjectiveId);
        }

        [Test]
        public void NoEventService_StillTransitionsState()
        {
            var objective = new TestObjective("obj1", events: null);

            Assert.DoesNotThrow(() => objective.Activate());
            Assert.AreEqual(ObjectiveState.Active, objective.State);
        }
    }
}
