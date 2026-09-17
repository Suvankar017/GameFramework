using System;
using System.Collections;
using GameFramework.Core.Extensions;
using GameFramework.Core.Validation;
using UnityEngine;

namespace GameFramework.Samples.Phase0Demo
{
    /// <summary>
    /// Drives the Phase 0 demonstration scene: exercises <see cref="Guard"/> and the Core
    /// extension methods in isolation. Phase 0 has no services and no Bootstrap, so unlike the
    /// later phase demos this one needs neither — it runs standalone from <see cref="Start"/>.
    /// Not part of the reusable framework — sample/demo content only, kept in its own assembly and
    /// scene, separate from any production game content.
    /// </summary>
    public sealed class Phase0DemoController : MonoBehaviour
    {
        private IEnumerator Start()
        {
            Debug.Log("[Phase0Demo] Guard - exercising pass and fail cases for each check.");
            DemoGuardNotNull();
            DemoGuardNotNullOrEmpty();
            DemoGuardInRange();
            DemoGuardIsTrue();

            Debug.Log("[Phase0Demo] Extensions - GetOrAddComponent, IsNullOrDestroyed, DestroyAllChildren.");
            DemoGetOrAddComponent();
            yield return DemoDestroyedObjectExtensions();
            yield return DemoDestroyAllChildren();

            Debug.Log("[Phase0Demo] Done - see the log above for each check's outcome.");
        }

        private static void DemoGuardNotNull()
        {
            try
            {
                Guard.NotNull<object>(null, "argument");
                Debug.LogError("[Phase0Demo] FAIL: Guard.NotNull did not throw for a null argument.");
            }
            catch (ArgumentNullException ex)
            {
                Debug.Log($"[Phase0Demo] PASS: Guard.NotNull rejected null ({ex.GetType().Name}).");
            }

            var value = new object();
            Debug.Log($"[Phase0Demo] PASS: Guard.NotNull passed a valid value through: {ReferenceEquals(Guard.NotNull(value, "argument"), value)}");
        }

        private static void DemoGuardNotNullOrEmpty()
        {
            try
            {
                Guard.NotNullOrEmpty(string.Empty, "argument");
                Debug.LogError("[Phase0Demo] FAIL: Guard.NotNullOrEmpty did not throw for an empty string.");
            }
            catch (ArgumentException ex)
            {
                Debug.Log($"[Phase0Demo] PASS: Guard.NotNullOrEmpty rejected an empty string ({ex.GetType().Name}).");
            }
        }

        private static void DemoGuardInRange()
        {
            try
            {
                Guard.InRange(150, 0, 100, "argument");
                Debug.LogError("[Phase0Demo] FAIL: Guard.InRange did not throw for an out-of-range value.");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.Log($"[Phase0Demo] PASS: Guard.InRange rejected 150 outside [0, 100] ({ex.GetType().Name}).");
            }

            int inRange = Guard.InRange(42, 0, 100, "argument");
            Debug.Log($"[Phase0Demo] PASS: Guard.InRange passed a valid value through: {inRange}.");
        }

        private static void DemoGuardIsTrue()
        {
            try
            {
                Guard.IsTrue(false, "argument", "Demo condition was false.");
                Debug.LogError("[Phase0Demo] FAIL: Guard.IsTrue did not throw for a false condition.");
            }
            catch (ArgumentException ex)
            {
                Debug.Log($"[Phase0Demo] PASS: Guard.IsTrue rejected a false condition ({ex.Message}).");
            }
        }

        private static void DemoGetOrAddComponent()
        {
            var probe = new GameObject("Phase0Demo_ComponentProbe");

            BoxCollider added = probe.GetOrAddComponent<BoxCollider>();
            BoxCollider fetched = probe.GetOrAddComponent<BoxCollider>();
            Debug.Log($"[Phase0Demo] PASS: GameObjectExtensions.GetOrAddComponent added once, then returned the " +
                $"same instance on a second call: {ReferenceEquals(added, fetched)}.");

            Destroy(probe);
        }

        private IEnumerator DemoDestroyedObjectExtensions()
        {
            var probe = new GameObject("Phase0Demo_LifetimeProbe");
            Destroy(probe);

            // Unity's overloaded == only resolves for an expression statically typed as
            // UnityEngine.Object (or a subclass) - inside this generic, T-constrained-to-class
            // method, the compiler cannot know that, so it falls back to a plain reference check.
            // That's the exact gotcha UnityObjectExtensions exists to close.
            bool genericNullCheckSeesIt = LooksNullGeneric(probe);
            Debug.Log($"[Phase0Demo] Generic '== null' check on a Destroy()-pending object: {genericNullCheckSeesIt} " +
                "(false is expected here - the reference is not yet gone, it's just marked for destruction).");

            // Destruction actually happens at end of frame - wait for it, then compare the two checks.
            yield return null;

            bool isNullOrDestroyed = probe.IsNullOrDestroyed();
            Debug.Log($"[Phase0Demo] PASS: UnityObjectExtensions.IsNullOrDestroyed correctly reports " +
                $"the destroyed probe as gone: {isNullOrDestroyed}.");
        }

        private static bool LooksNullGeneric<T>(T value) where T : class
        {
            return value == null;
        }

        private IEnumerator DemoDestroyAllChildren()
        {
            var parent = new GameObject("Phase0Demo_DestroyAllChildrenProbe");
            for (int i = 0; i < 3; i++)
            {
                var child = new GameObject($"Child{i}");
                child.transform.SetParent(parent.transform);
            }

            Debug.Log($"[Phase0Demo] Parent has {parent.transform.childCount} children before DestroyAllChildren.");
            parent.transform.DestroyAllChildren();

            yield return null; // Destroy() is deferred to end of frame.

            Debug.Log($"[Phase0Demo] PASS: Parent has {parent.transform.childCount} children after DestroyAllChildren.");
            Destroy(parent);
        }
    }
}
