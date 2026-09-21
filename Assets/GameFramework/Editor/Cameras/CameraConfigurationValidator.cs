using GameFramework.Cameras;
using GameFramework.Cameras.Configuration;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Cameras
{
    /// <summary>
    /// Lightweight authoring-time checks for Phase 11 <see cref="CameraConfiguration"/> assets,
    /// following the same pattern as <c>FeedbackContentValidator</c>/<c>QuestContentValidator</c>:
    /// validate the selected asset in the Project window.
    /// </summary>
    internal static class CameraConfigurationValidator
    {
        [MenuItem("GameFramework/Cameras/Validate Selected Configuration")]
        private static void ValidateSelected()
        {
            if (Selection.activeObject is CameraConfiguration configuration)
            {
                ValidateConfiguration(configuration);
            }
            else
            {
                Debug.LogWarning("[Cameras] Select a CameraConfiguration asset in the Project window first.");
            }
        }

        private static void ValidateConfiguration(CameraConfiguration configuration)
        {
            int issues = 0;

            if (configuration.Zoom.MinOrthographicSize > configuration.Zoom.MaxOrthographicSize)
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' has MinOrthographicSize > MaxOrthographicSize.", configuration);
                issues++;
            }

            if (configuration.Zoom.DefaultOrthographicSize < configuration.Zoom.MinOrthographicSize ||
                configuration.Zoom.DefaultOrthographicSize > configuration.Zoom.MaxOrthographicSize)
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' has DefaultOrthographicSize outside its own Min/Max range.", configuration);
                issues++;
            }

            if (configuration.Zoom.MinFieldOfView > configuration.Zoom.MaxFieldOfView)
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' has MinFieldOfView > MaxFieldOfView.", configuration);
                issues++;
            }

            if (configuration.Bounds.Enabled && (configuration.Bounds.MinX > configuration.Bounds.MaxX || configuration.Bounds.MinY > configuration.Bounds.MaxY))
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' has an inverted Bounds rect (Min > Max).", configuration);
                issues++;
            }

            if (configuration.Mode == CameraModeKind.Follow && configuration.Follow.FollowAxisMask == Vector3.zero)
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' is in Follow mode but FollowAxisMask is all-zero; the camera will never move.", configuration);
                issues++;
            }

            if (configuration.Follow.UseSoftZone && (configuration.Follow.SoftZoneSize.x < 0f || configuration.Follow.SoftZoneSize.y < 0f))
            {
                Debug.LogWarning($"[Cameras] '{configuration.name}' has a negative SoftZoneSize.", configuration);
                issues++;
            }

            Debug.Log(issues == 0
                ? $"[Cameras] '{configuration.name}' validated with no issues."
                : $"[Cameras] '{configuration.name}' validation found {issues} issue(s). See warnings above.");
        }
    }
}
