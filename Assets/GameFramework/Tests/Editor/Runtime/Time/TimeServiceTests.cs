using GameFramework.Runtime.Time;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Time
{
    public class TimeServiceTests
    {
        private TimeService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new TimeService();
            _service.Initialize(null);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
        }

        [Test]
        public void Initialize_SetsTimeScaleToOneAndNotPaused()
        {
            Assert.AreEqual(1f, _service.TimeScale);
            Assert.IsFalse(_service.IsPaused);
        }

        [Test]
        public void SetTimeScale_ChangesTimeScale()
        {
            _service.SetTimeScale(0.5f);

            Assert.AreEqual(0.5f, _service.TimeScale);
            Assert.AreEqual(0.5f, UnityEngine.Time.timeScale);
        }

        [Test]
        public void SetTimeScale_NegativeValue_ClampsToZero()
        {
            _service.SetTimeScale(-5f);

            Assert.AreEqual(0f, _service.TimeScale);
        }

        [Test]
        public void ResetTimeScale_RestoresOne()
        {
            _service.SetTimeScale(3f);

            _service.ResetTimeScale();

            Assert.AreEqual(1f, _service.TimeScale);
        }

        [Test]
        public void Pause_ForcesTimeScaleToZero()
        {
            _service.SetTimeScale(2f);

            _service.Pause();

            Assert.AreEqual(0f, _service.TimeScale);
            Assert.IsTrue(_service.IsPaused);
        }

        [Test]
        public void Resume_AfterSinglePause_RestoresDesiredScale()
        {
            _service.SetTimeScale(2f);
            _service.Pause();

            _service.Resume();

            Assert.AreEqual(2f, _service.TimeScale);
            Assert.IsFalse(_service.IsPaused);
        }

        [Test]
        public void Pause_IsReferenceCounted_SingleResumeDoesNotUnpause()
        {
            _service.Pause();
            _service.Pause();

            _service.Resume();

            Assert.IsTrue(_service.IsPaused);
            Assert.AreEqual(0f, _service.TimeScale);
        }

        [Test]
        public void Pause_IsReferenceCounted_MatchingResumesFullyUnpause()
        {
            _service.Pause();
            _service.Pause();

            _service.Resume();
            _service.Resume();

            Assert.IsFalse(_service.IsPaused);
        }

        [Test]
        public void Resume_WithoutOutstandingPause_IsNoOp()
        {
            Assert.DoesNotThrow(() => _service.Resume());
            Assert.IsFalse(_service.IsPaused);
        }

        [Test]
        public void SetTimeScale_WhilePaused_DoesNotApplyUntilFullyResumed()
        {
            _service.Pause();

            _service.SetTimeScale(5f);
            Assert.AreEqual(0f, _service.TimeScale);

            _service.Resume();
            Assert.AreEqual(5f, _service.TimeScale);
        }

        [Test]
        public void Shutdown_RestoresTimeScaleToOneAndClearsPause()
        {
            _service.SetTimeScale(3f);
            _service.Pause();

            _service.Shutdown();

            Assert.AreEqual(1f, UnityEngine.Time.timeScale);
            Assert.IsFalse(_service.IsPaused);
        }

        [Test]
        public void TimeProperties_DelegateToUnityTime()
        {
            Assert.AreEqual(UnityEngine.Time.deltaTime, _service.ScaledDeltaTime);
            Assert.AreEqual(UnityEngine.Time.unscaledDeltaTime, _service.UnscaledDeltaTime);
            Assert.AreEqual(UnityEngine.Time.fixedDeltaTime, _service.FixedDeltaTime);
            Assert.AreEqual(UnityEngine.Time.fixedUnscaledDeltaTime, _service.UnscaledFixedDeltaTime);
            Assert.AreEqual(UnityEngine.Time.time, _service.ScaledTime);
            Assert.AreEqual(UnityEngine.Time.unscaledTime, _service.UnscaledTime);
            // realtimeSinceStartup is a live wall-clock read, not frame-cached like the others
            // above, so two separate reads of it (the assertion's and the service's) can
            // legitimately differ by a hair — assert closeness, not exact equality.
            Assert.That(_service.Realtime, Is.EqualTo(UnityEngine.Time.realtimeSinceStartup).Within(0.05f));
        }
    }
}
