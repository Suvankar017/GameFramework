using GameFramework.Gameplay.Lifecycle;
using UnityEngine;

namespace GameFramework.Samples.Phase4Demo
{
    /// <summary>
    /// Standalone (non-pooled) demonstration of <see cref="GameplayObjectLifecycleRunner"/>'s
    /// phase-ordering guard - the pattern <see cref="GameFramework.Gameplay.Pooling.GameObjectPool"/>
    /// itself does not use (it drives <see cref="IGameplayObjectLifecycle"/> directly; see
    /// <see cref="Phase4DemoTarget"/>'s remarks). This is the "plain reusable object with no pool"
    /// case the Runner exists for: an object that wants Initialize to fire only once no matter how
    /// many times a caller mistakenly re-invokes the phases.
    /// </summary>
    public sealed class Phase4DemoLifecycleProbe : IGameplayObjectLifecycle
    {
        public void Initialize() => Debug.Log("[Phase4Demo] LifecycleProbe.Initialize (once, ever).");
        public void Activate() => Debug.Log("[Phase4Demo] LifecycleProbe.Activate.");
        public void Deactivate() => Debug.Log("[Phase4Demo] LifecycleProbe.Deactivate.");
        public void Dispose() => Debug.Log("[Phase4Demo] LifecycleProbe.Dispose (once, ever).");

        public static void RunDemoSequence()
        {
            var probe = new Phase4DemoLifecycleProbe();
            var runner = new GameplayObjectLifecycleRunner(probe);

            Debug.Log("[Phase4Demo] --- GameplayObjectLifecycleRunner guard demo ---");
            runner.Initialize();
            runner.Initialize(); // Guarded: logs a warning instead of re-running setup.
            runner.Activate();
            runner.Activate();   // Guarded: Active -> Active is not a valid transition.
            runner.Deactivate();
            runner.Activate();   // Valid: Deactivated -> Active is a legitimate reuse.
            runner.Dispose();
            runner.Dispose();    // Guarded: idempotent, no second Dispose call reaches the probe.
            Debug.Log("[Phase4Demo] --- end guard demo (warnings above are expected) ---");
        }
    }
}
