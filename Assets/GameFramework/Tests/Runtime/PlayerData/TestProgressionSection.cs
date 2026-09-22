using System;

namespace GameFramework.PlayerData.Tests
{
    /// <summary>Duplicated from the EditMode test assembly's own <c>TestProgressionSection</c> -
    /// same reasoning as every other phase's per-assembly test fakes (e.g. <c>FakeTimeService</c>):
    /// the Editor-only EditMode test assembly isn't referenceable from this Runtime one.</summary>
    [Serializable]
    internal sealed class TestProgressionData
    {
        public int Integer;
    }

    internal sealed class TestProgressionSection : PlayerDataSection<TestProgressionData>
    {
        public override string Id => "TestProgression";
        public override int Version => 1;

        public int Integer => Data.Integer;

        public void SetInteger(int value)
        {
            if (Data.Integer != value)
            {
                Data.Integer = value;
                MarkDirty();
            }
        }
    }
}
