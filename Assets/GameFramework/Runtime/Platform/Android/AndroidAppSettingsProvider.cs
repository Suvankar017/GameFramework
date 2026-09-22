#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using GameFramework.Runtime.Diagnostics;
using UnityEngine;

namespace GameFramework.Platform.Android
{
    /// <summary>
    /// The one isolated boundary in this framework that touches <see cref="AndroidJavaObject"/>/
    /// <see cref="AndroidJavaClass"/> directly (see CLAUDE.md's Phase 14 brief, sections 23-25) -
    /// opens Android's "App info" settings screen via an
    /// <c>ACTION_APPLICATION_DETAILS_SETTINGS</c> intent. Every Java object created here is used
    /// once, within this single call, and left to the garbage collector afterwards - nothing is
    /// cached or held beyond it.
    /// </summary>
    internal static class AndroidAppSettingsProvider
    {
        internal static bool OpenApplicationDetailsSettings()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                string packageName = activity.Call<string>("getPackageName");

                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null);
                using var intent = new AndroidJavaObject(
                    "android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri);

                activity.Call("startActivity", intent);
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception(exception, "Platform");
                return false;
            }
        }
    }
}
#endif
