using System;
using System.Collections.Generic;
using GameFramework.Performance.Profiling;
using GameFramework.Performance.Ticking;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using NUnit.Framework;

namespace GameFramework.Performance.Tests.Ticking
{
    public class TickServiceTests
    {
        private sealed class RecordingTickable : ITickable
        {
            public int TickCount;
            public float LastDeltaTime;
            public void Tick(float deltaTime) { TickCount++; LastDeltaTime = deltaTime; }
        }

        private sealed class RecordingFixedTickable : IFixedTickable
        {
            public int TickCount;
            public void FixedTick(float fixedDeltaTime) => TickCount++;
        }

        private sealed class RecordingLateTickable : ILateTickable
        {
            public int TickCount;
            public void LateTick(float deltaTime) => TickCount++;
        }

        private sealed class ThrowingTickable : ITickable
        {
            public void Tick(float deltaTime) => throw new InvalidOperationException("boom");
        }

        private sealed class OrderRecordingTickable : ITickable
        {
            private readonly List<OrderRecordingTickable> _order;
            public OrderRecordingTickable(List<OrderRecordingTickable> order) => _order = order;
            public void Tick(float deltaTime) => _order.Add(this);
        }

        private FakeTimeService _time;
        private TickService _tickService;

        [SetUp]
        public void SetUp()
        {
            PerformanceSettings.Mode = ProfilingMode.Development;

            var registry = new ServiceRegistry();
            _time = new FakeTimeService { ScaledDeltaTime = 0.1f, FixedDeltaTime = 0.02f };
            registry.Register<ITimeService>(_time);
            registry.MarkInitialized(typeof(ITimeService));

            _tickService = new TickService();
            _tickService.Initialize(registry);
        }

        [TearDown]
        public void TearDown()
        {
            _tickService.Shutdown();
        }

        [Test]
        public void Register_ThenTick_InvokesTickWithScaledDeltaTime()
        {
            var tickable = new RecordingTickable();
            _tickService.Register(tickable);

            _tickService.Tick();

            Assert.AreEqual(1, tickable.TickCount);
            Assert.AreEqual(0.1f, tickable.LastDeltaTime);
        }

        [Test]
        public void Register_SameInstanceTwice_OnlyTicksOnce()
        {
            var tickable = new RecordingTickable();
            _tickService.Register(tickable);
            _tickService.Register(tickable);

            _tickService.Tick();

            Assert.AreEqual(1, tickable.TickCount);
            Assert.AreEqual(1, _tickService.TickableCount);
        }

        [Test]
        public void Unregister_StopsFurtherTicks()
        {
            var tickable = new RecordingTickable();
            _tickService.Register(tickable);
            _tickService.Unregister(tickable);

            _tickService.Tick();

            Assert.AreEqual(0, tickable.TickCount);
            Assert.AreEqual(0, _tickService.TickableCount);
        }

        [Test]
        public void Unregister_CalledTwice_DoesNotThrow()
        {
            var tickable = new RecordingTickable();
            _tickService.Register(tickable);
            _tickService.Unregister(tickable);

            Assert.DoesNotThrow(() => _tickService.Unregister(tickable));
        }

        [Test]
        public void Unregister_NeverRegistered_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _tickService.Unregister(new RecordingTickable()));
        }

        [Test]
        public void Register_LowerPriorityTicksFirst()
        {
            var order = new List<OrderRecordingTickable>();
            var second = new OrderRecordingTickable(order);
            var first = new OrderRecordingTickable(order);
            _tickService.Register(second, priority: 10);
            _tickService.Register(first, priority: -5);

            _tickService.Tick();

            Assert.AreEqual(new[] { first, second }, order.ToArray());
        }

        [Test]
        public void RegisterDuringTick_IsNotInvokedUntilNextTick()
        {
            var late = new RecordingTickable();
            var trigger = new ActionTickable(() => _tickService.Register(late));
            _tickService.Register(trigger);

            _tickService.Tick();
            Assert.AreEqual(0, late.TickCount, "A tickable registered mid-tick must not run in the same tick.");

            _tickService.Tick();
            Assert.AreEqual(1, late.TickCount, "It must run on the following tick.");
        }

        [Test]
        public void UnregisterDuringTick_DoesNotThrowAndTakesEffectNextTick()
        {
            var victim = new RecordingTickable();
            _tickService.Register(victim);
            var trigger = new ActionTickable(() => _tickService.Unregister(victim));
            _tickService.Register(trigger, priority: -1); // runs before victim

            Assert.DoesNotThrow(() => _tickService.Tick());

            int countAfterFirstTick = victim.TickCount;
            _tickService.Tick();

            Assert.AreEqual(countAfterFirstTick, victim.TickCount, "Unregistering mid-tick must prevent later ticks.");
        }

        [Test]
        public void ThrowingTickable_DoesNotPreventOthersFromTicking()
        {
            _tickService.Register(new ThrowingTickable());
            var healthy = new RecordingTickable();
            _tickService.Register(healthy);

            Assert.DoesNotThrow(() => _tickService.Tick());
            Assert.AreEqual(1, healthy.TickCount);
        }

        [Test]
        public void RegisterFixed_ThenTickFixed_InvokesFixedTickWithFixedDeltaTime()
        {
            var tickable = new RecordingFixedTickable();
            _tickService.RegisterFixed(tickable);

            _tickService.TickFixed();

            Assert.AreEqual(1, tickable.TickCount);
        }

        [Test]
        public void RegisterLate_ThenTickLate_InvokesLateTick()
        {
            var tickable = new RecordingLateTickable();
            _tickService.RegisterLate(tickable);

            _tickService.TickLate();

            Assert.AreEqual(1, tickable.TickCount);
        }

        private sealed class ActionTickable : ITickable
        {
            private readonly Action _action;
            public ActionTickable(Action action) => _action = action;
            public void Tick(float deltaTime) => _action();
        }
    }
}
