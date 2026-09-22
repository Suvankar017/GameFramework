using System;
using System.Collections.Generic;

namespace GameFramework.PlayerData.Tests
{
    [Serializable]
    internal sealed class TestProgressionData
    {
        public int Integer;
        public float Float;
        public string String = string.Empty;
        public bool Bool;
        public List<string> List = new List<string>();
        public NestedData Nested = new NestedData();

        [Serializable]
        public sealed class NestedData
        {
            public int Depth;
        }
    }

    /// <summary>Section-under-test with domain methods, mirroring how a real game would use
    /// <see cref="PlayerDataSection{TData}"/> - see CLAUDE.md's Phase 13 brief, section 54.</summary>
    internal sealed class TestProgressionSection : PlayerDataSection<TestProgressionData>
    {
        public override string Id => "TestProgression";
        public override int Version => 1;

        public int Integer => Data.Integer;
        public IReadOnlyList<string> Items => Data.List;

        public void SetInteger(int value)
        {
            if (Data.Integer != value)
            {
                Data.Integer = value;
                MarkDirty();
            }
        }

        public void AddItem(string item)
        {
            Data.List.Add(item);
            MarkDirty();
        }

        public int ValidateCallCount;

        public override void Validate()
        {
            ValidateCallCount++;
            if (Data.Integer < 0)
            {
                Data.Integer = 0;
            }
        }
    }

    [Serializable]
    internal sealed class TestSettingsDataV1
    {
        public int Difficulty;
    }

    [Serializable]
    internal sealed class TestSettingsData
    {
        public int Difficulty;
        public bool TutorialsEnabled = true;
    }

    internal sealed class TestSettingsSection : PlayerDataSection<TestSettingsData>
    {
        public override string Id => "TestSettings";
        public override int Version => 2;

        public int Difficulty => Data.Difficulty;

        public void SetDifficulty(int value)
        {
            Data.Difficulty = value;
            MarkDirty();
        }
    }

    /// <summary>A section whose <see cref="Validate"/> always throws - used to exercise
    /// <see cref="ProfileOperationResultKind.Corrupted"/>/save-failure handling.</summary>
    internal sealed class ThrowingSection : PlayerDataSection<TestProgressionData>
    {
        public override string Id => "Throwing";
        public override int Version => 1;
        public bool ThrowOnValidate;

        public void ForceDirty()
        {
            Data.Integer++;
            MarkDirty();
        }

        public override void Validate()
        {
            if (ThrowOnValidate)
            {
                throw new InvalidOperationException("Simulated unrecoverable validation failure.");
            }
        }
    }
}
