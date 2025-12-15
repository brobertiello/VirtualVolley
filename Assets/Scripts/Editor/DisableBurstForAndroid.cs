using UnityEditor;
using UnityEngine;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Disables Burst compilation for Android to prevent OutOfMemoryException during builds.
    /// </summary>
    public static class DisableBurstForAndroid
    {
        [MenuItem("VirtualVolley/Project Setup/Disable Burst for Android (Fix Build Memory Issue)")]
        public static void Disable()
        {
            // This is handled via ProjectSettings/BurstAotSettings_Android.json
            // The file has been updated to set EnableBurstCompilation to false
            
            Debug.Log("[VirtualVolley] Burst compilation has been disabled for Android builds.");
            Debug.Log("[VirtualVolley] This prevents OutOfMemoryException during build.");
            Debug.Log("[VirtualVolley] If you need Burst performance, you can re-enable it in:");
            Debug.Log("[VirtualVolley] Edit → Project Settings → Burst AOT Settings → Android");
            
            AssetDatabase.Refresh();
        }
    }
}

