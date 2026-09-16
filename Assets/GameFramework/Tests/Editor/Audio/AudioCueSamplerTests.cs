using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Audio.Tests
{
    public class AudioCueSamplerTests
    {
        private AudioCueAsset _cue;

        [SetUp]
        public void SetUp()
        {
            _cue = ScriptableObject.CreateInstance<AudioCueAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cue);
        }

        [Test]
        public void TrySelect_NoClips_ReturnsFalse()
        {
            bool result = AudioCueSampler.TrySelect(_cue, new FakeRandomSource(), out AudioClip clip, out _, out _);

            Assert.IsFalse(result);
            Assert.IsNull(clip);
        }

        [Test]
        public void TrySelect_NullCue_ReturnsFalse()
        {
            Assert.IsFalse(AudioCueSampler.TrySelect(null, new FakeRandomSource(), out _, out _, out _));
        }

        [Test]
        public void TrySelect_SingleClip_DoesNotConsultRandomForClipIndex()
        {
            AudioClip clip = AudioClip.Create("test", 100, 1, 44100, false);
            _cue.Clips.Add(clip);
            _cue.MinVolume = 1f;
            _cue.MaxVolume = 1f;

            bool result = AudioCueSampler.TrySelect(
                _cue, new FakeRandomSource { FixedInt = 999, FixedFloat = 1f }, out AudioClip selected, out float volume, out _);

            Assert.IsTrue(result);
            Assert.AreEqual(clip, selected);
            Assert.AreEqual(1f, volume, 0.0001f);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void TrySelect_MultipleClips_UsesRandomSourceIndex()
        {
            AudioClip clipA = AudioClip.Create("a", 100, 1, 44100, false);
            AudioClip clipB = AudioClip.Create("b", 100, 1, 44100, false);
            _cue.Clips.Add(clipA);
            _cue.Clips.Add(clipB);

            bool result = AudioCueSampler.TrySelect(_cue, new FakeRandomSource { FixedInt = 1 }, out AudioClip selected, out _, out _);

            Assert.IsTrue(result);
            Assert.AreEqual(clipB, selected);
            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
        }

        [Test]
        public void TrySelect_VolumeAndPitch_ComeFromRandomSourceWithinCueRange()
        {
            AudioClip clip = AudioClip.Create("test", 100, 1, 44100, false);
            _cue.Clips.Add(clip);
            _cue.MinVolume = 0.2f;
            _cue.MaxVolume = 0.8f;
            _cue.MinPitch = 0.9f;
            _cue.MaxPitch = 1.1f;
            var random = new FakeRandomSource { FixedFloat = 0.5f };

            AudioCueSampler.TrySelect(_cue, random, out _, out float volume, out float pitch);

            Assert.AreEqual(0.5f, volume, 0.0001f);
            Assert.AreEqual(0.5f, pitch, 0.0001f);
            Object.DestroyImmediate(clip);
        }
    }
}
