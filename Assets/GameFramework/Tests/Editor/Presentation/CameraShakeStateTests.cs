using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Presentation.Tests
{
    public class CameraShakeStateTests
    {
        [Test]
        public void Add_ZeroDuration_IsIgnored()
        {
            var state = new CameraShakeState();

            state.Add(new CameraShakeRequest(1f, 10f, 0f));

            Assert.AreEqual(0, state.ActiveCount);
        }

        [Test]
        public void Add_ZeroAmplitude_IsIgnored()
        {
            var state = new CameraShakeState();

            state.Add(new CameraShakeRequest(0f, 10f, 1f));

            Assert.AreEqual(0, state.ActiveCount);
        }

        [Test]
        public void Add_Valid_IncrementsActiveCount()
        {
            var state = new CameraShakeState();

            state.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 42));

            Assert.AreEqual(1, state.ActiveCount);
        }

        [Test]
        public void Tick_SameSeedAndElapsed_ProducesSameOffset()
        {
            var stateA = new CameraShakeState();
            var stateB = new CameraShakeState();
            stateA.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 42));
            stateB.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 42));

            Vector3 offsetA = stateA.Tick(0.1f);
            Vector3 offsetB = stateB.Tick(0.1f);

            Assert.AreEqual(offsetA, offsetB);
        }

        [Test]
        public void Tick_DifferentSeeds_ProduceDifferentOffsets()
        {
            var stateA = new CameraShakeState();
            var stateB = new CameraShakeState();
            stateA.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 1));
            stateB.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 999));

            Vector3 offsetA = stateA.Tick(0.1f);
            Vector3 offsetB = stateB.Tick(0.1f);

            Assert.AreNotEqual(offsetA, offsetB);
        }

        [Test]
        public void Tick_PastDuration_RemovesShake()
        {
            var state = new CameraShakeState();
            state.Add(new CameraShakeRequest(1f, 10f, 0.5f, seed: 1));

            state.Tick(0.6f);

            Assert.AreEqual(0, state.ActiveCount);
        }

        [Test]
        public void Tick_DefaultLinearFalloff_AverageMagnitudeDecreasesOverTime()
        {
            // Averaged over a window rather than compared at single instants: an individual Perlin
            // sample's magnitude varies with the noise itself, not just the falloff envelope, so a
            // single early-vs-single-late comparison could occasionally be misleading even though
            // the envelope genuinely decreases. Averaging over enough samples washes that out.
            var state = new CameraShakeState();
            state.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 7));

            float earlyTotal = 0f;
            for (int i = 0; i < 10; i++)
            {
                earlyTotal += state.Tick(0.01f).magnitude; // elapsed 0.01 .. 0.10, falloff ~0.99 .. ~0.90
            }

            float lateTotal = 0f;
            for (int i = 0; i < 10; i++)
            {
                lateTotal += state.Tick(0.01f).magnitude; // elapsed 0.81 .. 0.90, falloff ~0.19 .. ~0.10
            }

            Assert.Greater(earlyTotal / 10f, lateTotal / 10f);
        }

        [Test]
        public void Tick_TwoIdenticalShakes_SumToDoubleOneShakesOffset()
        {
            var single = new CameraShakeState();
            single.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 5));
            Vector3 singleOffset = single.Tick(0.05f);

            var doubled = new CameraShakeState();
            doubled.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 5));
            doubled.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 5));
            Vector3 doubledOffset = doubled.Tick(0.05f);

            Assert.AreEqual(singleOffset * 2f, doubledOffset);
        }

        [Test]
        public void Tick_ConstrainToXY_ZeroesZAxis()
        {
            var state = new CameraShakeState();
            state.Add(new CameraShakeRequest(1f, 10f, 1f, axisMask: new Vector3(1f, 1f, 0f), seed: 3));

            Vector3 offset = state.Tick(0.1f);

            Assert.AreEqual(0f, offset.z);
        }

        [Test]
        public void CancelAll_ClearsEveryActiveShake()
        {
            var state = new CameraShakeState();
            state.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 1));
            state.Add(new CameraShakeRequest(1f, 10f, 1f, seed: 2));

            state.CancelAll();

            Assert.AreEqual(0, state.ActiveCount);
        }
    }
}
