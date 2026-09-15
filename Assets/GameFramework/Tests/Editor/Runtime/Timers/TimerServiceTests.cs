using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Runtime.Tests.Timers
{
    public class TimerServiceTests
    {
        private FakeTimeService _fakeTime;
        private TimerService _service;

        [SetUp]
        public void SetUp()
        {
            _fakeTime = new FakeTimeService();

            var registry = new ServiceRegistry();
            registry.Register<ITimeService>(_fakeTime);
            registry.MarkInitialized(typeof(ITimeService));

            _service = new TimerService();
            _service.Initialize(registry);
        }

        [Test]
        public void StartOneShot_DoesNotFireBeforeDurationElapsed()
        {
            int fireCount = 0;
            _service.StartOneShot(1f, () => fireCount++);

            _fakeTime.Advance(0.5f);
            _service.Tick();

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void StartOneShot_FiresOnceDurationElapsed()
        {
            int fireCount = 0;
            ITimerHandle handle = _service.StartOneShot(1f, () => fireCount++);

            _fakeTime.Advance(0.6f);
            _service.Tick();
            _fakeTime.Advance(0.6f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
            Assert.IsTrue(handle.IsCompleted);
        }

        [Test]
        public void StartOneShot_ZeroDuration_DoesNotFireSynchronouslyDuringStart()
        {
            int fireCount = 0;
            _service.StartOneShot(0f, () => fireCount++);

            Assert.AreEqual(0, fireCount);

            _fakeTime.Advance(0.001f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void StartDelay_BehavesIdenticallyToOneShot()
        {
            int fireCount = 0;
            _service.StartDelay(1f, () => fireCount++);

            _fakeTime.Advance(1.1f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void StartRepeating_FiresEveryInterval()
        {
            int tickCount = 0;
            _service.StartRepeating(1f, () => tickCount++);

            for (int i = 0; i < 3; i++)
            {
                _fakeTime.Advance(1f);
                _service.Tick();
            }

            Assert.AreEqual(3, tickCount);
        }

        [Test]
        public void StartRepeating_WithRepeatCount_CompletesAfterCount()
        {
            int tickCount = 0;
            ITimerHandle handle = _service.StartRepeating(1f, () => tickCount++, repeatCount: 2);

            for (int i = 0; i < 3; i++)
            {
                _fakeTime.Advance(1f);
                _service.Tick();
            }

            Assert.AreEqual(2, tickCount);
            Assert.IsTrue(handle.IsCompleted);
        }

        [Test]
        public void Cancel_PreventsFurtherCallbacks()
        {
            int fireCount = 0;
            ITimerHandle handle = _service.StartOneShot(1f, () => fireCount++);

            handle.Cancel();
            _fakeTime.Advance(2f);
            _service.Tick();

            Assert.AreEqual(0, fireCount);
            Assert.IsTrue(handle.IsCancelled);
        }

        [Test]
        public void Cancel_CalledTwice_IsSafe()
        {
            ITimerHandle handle = _service.StartOneShot(1f, () => { });

            handle.Cancel();

            Assert.DoesNotThrow(() => handle.Cancel());
        }

        [Test]
        public void Pause_StopsElapsedFromAdvancing()
        {
            ITimerHandle handle = _service.StartOneShot(1f, () => { });

            handle.Pause();
            _fakeTime.Advance(5f);
            _service.Tick();

            Assert.AreEqual(0f, handle.Elapsed);
            Assert.IsTrue(handle.IsPaused);
        }

        [Test]
        public void Resume_ContinuesFromWherePaused()
        {
            ITimerHandle handle = _service.StartOneShot(1f, () => { });

            _fakeTime.Advance(0.3f);
            _service.Tick();

            handle.Pause();
            _fakeTime.Advance(10f);
            _service.Tick();

            handle.Resume();
            _fakeTime.Advance(0.3f);
            _service.Tick();

            Assert.That(handle.Elapsed, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void ScaledMode_IgnoresUnscaledDeltaTime()
        {
            int fireCount = 0;
            _service.StartOneShot(1f, () => fireCount++, TimerTimeMode.Scaled);

            _fakeTime.ScaledDeltaTime = 0f;
            _fakeTime.UnscaledDeltaTime = 5f;
            _service.Tick();

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void UnscaledMode_IgnoresScaledDeltaTime()
        {
            int fireCount = 0;
            _service.StartOneShot(1f, () => fireCount++, TimerTimeMode.Unscaled);

            _fakeTime.ScaledDeltaTime = 0f;
            _fakeTime.UnscaledDeltaTime = 2f;
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void Progress_AndRemaining_ReflectElapsedOverDuration()
        {
            ITimerHandle handle = _service.StartOneShot(2f, () => { });

            _fakeTime.Advance(0.5f);
            _service.Tick();

            Assert.That(handle.Progress, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(handle.Remaining, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void StartCountdown_InvokesOnTickEveryTickWithRemainingTime()
        {
            var remainingValues = new List<float>();
            _service.StartCountdown(1f, remaining => remainingValues.Add(remaining), () => { });

            _fakeTime.Advance(0.4f);
            _service.Tick();
            _fakeTime.Advance(0.4f);
            _service.Tick();

            Assert.AreEqual(2, remainingValues.Count);
            Assert.That(remainingValues[0], Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(remainingValues[1], Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void StartCountdown_CallsOnCompleteWhenDurationReached()
        {
            bool completed = false;
            _service.StartCountdown(1f, _ => { }, () => completed = true);

            _fakeTime.Advance(1.5f);
            _service.Tick();

            Assert.IsTrue(completed);
        }

        [Test]
        public void CallbackCancelsItself_StopsFurtherFiring()
        {
            ITimerHandle handle = null;
            int fireCount = 0;
            handle = _service.StartRepeating(1f, () =>
            {
                fireCount++;
                handle.Cancel();
            });

            _fakeTime.Advance(1f);
            Assert.DoesNotThrow(() => _service.Tick());

            _fakeTime.Advance(1f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void CallbackStartingAnotherTimer_NewTimerNotProcessedInSameTick()
        {
            int innerFireCount = 0;
            _service.StartOneShot(1f, () => { _service.StartOneShot(0f, () => innerFireCount++); });

            _fakeTime.Advance(1f);
            _service.Tick();

            Assert.AreEqual(0, innerFireCount);

            _fakeTime.Advance(0.1f);
            _service.Tick();

            Assert.AreEqual(1, innerFireCount);
        }

        [Test]
        public void ExceptionInCallback_DoesNotStopOtherTimers()
        {
            int secondFired = 0;
            _service.StartOneShot(1f, () => throw new InvalidOperationException("boom"));
            _service.StartOneShot(1f, () => secondFired++);

            _fakeTime.Advance(1.1f);
            Assert.DoesNotThrow(() => _service.Tick());

            Assert.AreEqual(1, secondFired);
        }

        [Test]
        public void Owner_DestroyedBeforeCompletion_CancelsWithoutInvokingCallback()
        {
            var owner = new GameObject("Owner");
            int fireCount = 0;
            ITimerHandle handle = _service.StartOneShot(1f, () => fireCount++, owner: owner);

            Object.DestroyImmediate(owner);
            _fakeTime.Advance(2f);
            _service.Tick();

            Assert.AreEqual(0, fireCount);
            Assert.IsTrue(handle.IsCancelled);
        }

        [Test]
        public void NoOwner_TimerIsUnaffectedByOwnerCheck()
        {
            int fireCount = 0;
            _service.StartOneShot(1f, () => fireCount++);

            _fakeTime.Advance(1.1f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void CancelAll_CancelsEveryActiveTimer()
        {
            ITimerHandle a = _service.StartOneShot(1f, () => { });
            ITimerHandle b = _service.StartRepeating(1f, () => { });

            _service.CancelAll();

            Assert.IsTrue(a.IsCancelled);
            Assert.IsTrue(b.IsCancelled);
        }

        [Test]
        public void Shutdown_CancelsAllTimers()
        {
            ITimerHandle handle = _service.StartOneShot(1f, () => { });

            _service.Shutdown();

            Assert.IsTrue(handle.IsCancelled);
        }

        [Test]
        public void VerySmallDuration_EventuallyFires()
        {
            int fireCount = 0;
            _service.StartOneShot(0.0001f, () => fireCount++);

            _fakeTime.Advance(0.001f);
            _service.Tick();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void LargeDuration_DoesNotFireEarly()
        {
            int fireCount = 0;
            _service.StartOneShot(1000f, () => fireCount++);

            _fakeTime.Advance(1f);
            _service.Tick();

            Assert.AreEqual(0, fireCount);
        }

        [Test]
        public void MultipleTimers_EachTrackedIndependently()
        {
            int aFired = 0, bFired = 0;
            _service.StartOneShot(1f, () => aFired++);
            _service.StartOneShot(2f, () => bFired++);

            _fakeTime.Advance(1.5f);
            _service.Tick();

            Assert.AreEqual(1, aFired);
            Assert.AreEqual(0, bFired);

            _fakeTime.Advance(1f);
            _service.Tick();

            Assert.AreEqual(1, bFired);
        }
    }
}
