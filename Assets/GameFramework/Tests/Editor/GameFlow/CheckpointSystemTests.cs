using GameFramework.GameFlow.Checkpoints;
using GameFramework.Gameplay.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.GameFlow.Tests
{
    public class CheckpointSystemTests
    {
        [Test]
        public void NewSystem_HasNoCurrentCheckpoint()
        {
            var checkpoints = new CheckpointSystem();

            Assert.IsFalse(checkpoints.HasCurrent);
            Assert.IsFalse(checkpoints.TryGetCurrent(out _));
        }

        [Test]
        public void Activate_UnregisteredId_ReturnsFalse_AndDoesNotSetCurrent()
        {
            var checkpoints = new CheckpointSystem();

            bool result = checkpoints.Activate("Missing");

            Assert.IsFalse(result);
            Assert.IsFalse(checkpoints.HasCurrent);
        }

        [Test]
        public void RegisterThenActivate_MakesItCurrent()
        {
            var checkpoints = new CheckpointSystem();
            var transform = new CheckpointData { Id = "CP1", Position = new Vector3(1, 2, 3), Rotation = Quaternion.identity };

            checkpoints.Register("CP1", transform);
            bool activated = checkpoints.Activate("CP1");

            Assert.IsTrue(activated);
            Assert.IsTrue(checkpoints.HasCurrent);
            Assert.AreEqual("CP1", checkpoints.CurrentId);
            Assert.IsTrue(checkpoints.TryGetCurrent(out CheckpointRecord record));
            Assert.AreEqual(new Vector3(1, 2, 3), record.Transform.Value.Position);
        }

        [Test]
        public void ActivatingASecondCheckpoint_ReplacesTheFirst()
        {
            var checkpoints = new CheckpointSystem();
            checkpoints.Register("CP1");
            checkpoints.Register("CP2");

            checkpoints.Activate("CP1");
            checkpoints.Activate("CP2");

            Assert.AreEqual("CP2", checkpoints.CurrentId);
        }

        [Test]
        public void Reset_ClearsCurrent_ButKeepsRegistry()
        {
            var checkpoints = new CheckpointSystem();
            checkpoints.Register("CP1");
            checkpoints.Activate("CP1");

            checkpoints.Reset();

            Assert.IsFalse(checkpoints.HasCurrent);
            Assert.IsTrue(checkpoints.Activate("CP1")); // still registered
        }

        [Test]
        public void Clear_RemovesRegistryEntirely()
        {
            var checkpoints = new CheckpointSystem();
            checkpoints.Register("CP1");
            checkpoints.Activate("CP1");

            checkpoints.Clear();

            Assert.IsFalse(checkpoints.HasCurrent);
            Assert.IsFalse(checkpoints.Activate("CP1")); // no longer registered
        }
    }
}
