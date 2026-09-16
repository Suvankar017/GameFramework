using GameFramework.Gameplay.Lifecycle;
using UnityEngine;

namespace GameFramework.Gameplay.Tests
{
    /// <summary>Test double recording each lifecycle call it receives while pooled.</summary>
    internal sealed class RecordingPoolable : MonoBehaviour, IGameplayObjectLifecycle
    {
        public int InitializeCount;
        public int ActivateCount;
        public int DeactivateCount;
        public int DisposeCount;

        public void Initialize() => InitializeCount++;
        public void Activate() => ActivateCount++;
        public void Deactivate() => DeactivateCount++;
        public void Dispose() => DisposeCount++;
    }
}
