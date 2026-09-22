using System;
using GameFramework.Runtime.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Platform
{
    /// <summary>
    /// Default <see cref="IPermissionService"/>, built entirely on
    /// <see cref="Application.HasUserAuthorization"/>/<see cref="Application.RequestUserAuthorization"/> -
    /// Unity's own cross-platform (Android/iOS/Editor) permission API, so no native plugin or
    /// <c>#if UNITY_ANDROID</c>/<c>UNITY_IOS</c> branching is needed here (see CLAUDE.md's Phase 14
    /// brief, sections 34-35).
    /// </summary>
    public sealed class PermissionService : IPermissionService
    {
        private PermissionRequestDriver _driver;

        public void Initialize(IServiceRegistry registry)
        {
            var driverObject = new GameObject(nameof(PermissionRequestDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<PermissionRequestDriver>();

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            if (_driver == null)
            {
                return;
            }

            GameObject driverObject = _driver.gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(driverObject);
            }
            else
            {
                Object.DestroyImmediate(driverObject);
            }

            _driver = null;
        }

        public PermissionStatus GetStatus(PlatformPermission permission)
        {
            return Application.HasUserAuthorization(ToUnity(permission))
                ? PermissionStatus.Granted
                : PermissionStatus.NotDetermined;
        }

        public bool CanRequest(PlatformPermission permission)
        {
            return GetStatus(permission) != PermissionStatus.Granted;
        }

        public void RequestPermission(PlatformPermission permission, Action<PermissionStatus> onResult)
        {
            UserAuthorization mode = ToUnity(permission);

            if (Application.HasUserAuthorization(mode))
            {
                onResult?.Invoke(PermissionStatus.Granted);
                return;
            }

            AsyncOperation operation = Application.RequestUserAuthorization(mode);
            _driver.WaitForResult(operation, () =>
                onResult?.Invoke(Application.HasUserAuthorization(mode) ? PermissionStatus.Granted : PermissionStatus.Denied));
        }

        private static UserAuthorization ToUnity(PlatformPermission permission)
        {
            return permission == PlatformPermission.Camera ? UserAuthorization.WebCam : UserAuthorization.Microphone;
        }
    }
}
