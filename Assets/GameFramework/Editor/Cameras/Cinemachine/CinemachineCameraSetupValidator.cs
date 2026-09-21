using System.Collections.Generic;
using Cinemachine;
using GameFramework.Cameras;
using GameFramework.Cameras.CinemachineIntegration;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Cameras.CinemachineIntegration
{
    /// <summary>
    /// Authoring-time checks for a scene's Cinemachine-backed cameras, following the same pattern as
    /// <c>Cameras.CameraConfigurationValidator</c>: validate the currently open scene from a menu
    /// item (CLAUDE.md's Phase 11 Cinemachine brief, section 48). Deliberately a plain console
    /// validator, not a UI Toolkit window - matches the existing Camera Configuration validator's own
    /// modest scope rather than introducing a new tooling paradigm for one small check (section 47:
    /// "do not build a giant custom camera editor").
    /// </summary>
    internal static class CinemachineCameraSetupValidator
    {
        [MenuItem("GameFramework/Cameras/Validate Cinemachine Setup In Scene")]
        private static void ValidateScene()
        {
            int issues = 0;

            var backends = Object.FindObjectsOfType<CinemachineCameraBackend>(includeInactive: true);
            if (backends.Length == 0)
            {
                Debug.LogWarning("[Cameras.Cinemachine] No CinemachineCameraBackend found in the open " +
                    "scene - Cinemachine-backed cameras will never activate. If this scene only uses " +
                    "the non-Cinemachine CameraDriver path, this is expected.");
                issues++;
            }
            else if (backends.Length > 1)
            {
                Debug.LogWarning($"[Cameras.Cinemachine] {backends.Length} CinemachineCameraBackend " +
                    "components found in the open scene - exactly one is expected per scene, mirroring " +
                    "CameraDriver's own cardinality.");
                issues++;
            }

            foreach (CinemachineCameraBackend backend in backends)
            {
                if (backend.GetComponent<CinemachineBrain>() == null)
                {
                    Debug.LogError($"[Cameras.Cinemachine] '{backend.name}' has no CinemachineBrain - " +
                        "this should be impossible ([RequireComponent]); the component may be missing " +
                        "due to a prefab override.", backend);
                    issues++;
                }
            }

            var adapters = Object.FindObjectsOfType<CinemachineCameraAdapter>(includeInactive: true);
            var controllerOwners = new Dictionary<CameraController, CinemachineCameraAdapter>();

            foreach (CinemachineCameraAdapter adapter in adapters)
            {
                if (adapter.Controller == null)
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' has no Controller " +
                        "assigned - it cannot register a CameraId and will disable itself at runtime.", adapter);
                    issues++;
                }
                else if (controllerOwners.TryGetValue(adapter.Controller, out CinemachineCameraAdapter existing))
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' and '{existing.name}' both " +
                        $"reference the same CameraController ('{adapter.Controller.name}') - only one " +
                        "adapter per controller is supported.", adapter);
                    issues++;
                }
                else
                {
                    controllerOwners.Add(adapter.Controller, adapter);
                }

                if (adapter.VirtualCamera == null)
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' has no Virtual Camera " +
                        "assigned - it cannot register a CameraId and will disable itself at runtime.", adapter);
                    issues++;
                }

                if (adapter.Backend == null)
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' has no Backend assigned - " +
                        "it will never self-register, so it will never become active regardless of " +
                        "ICameraService.Activate/PushOverride calls.", adapter);
                    issues++;
                }
                else if (backends.Length > 0 && System.Array.IndexOf(backends, adapter.Backend) < 0)
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' references a Backend that " +
                        "is not in the open scene.", adapter);
                    issues++;
                }

                if (adapter.Confiner != null && adapter.Confiner.m_BoundingShape2D == null &&
                    (adapter.Controller == null || adapter.Controller.Configuration == null || !adapter.Controller.Configuration.Bounds.Enabled))
                {
                    Debug.LogWarning($"[Cameras.Cinemachine] '{adapter.name}' has a Confiner assigned but " +
                        "no Bounding Shape 2D, and its CameraConfiguration's Bounds are not enabled - no " +
                        "bounds will be generated, so the confiner will not constrain the camera.", adapter);
                    issues++;
                }
            }

            Debug.Log(issues == 0
                ? "[Cameras.Cinemachine] Scene validated with no issues."
                : $"[Cameras.Cinemachine] Scene validation found {issues} issue(s). See warnings above.");
        }
    }
}
