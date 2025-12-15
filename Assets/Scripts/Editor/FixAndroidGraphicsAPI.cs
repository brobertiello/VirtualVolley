using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Editor tool to fix Android Graphics API settings for Quest 3.
    /// Sets OpenGL ES 3.0 as the only graphics API to prevent shader compilation errors.
    /// </summary>
    public static class FixAndroidGraphicsAPI
    {
        private const int OPENGL_ES_3_0 = 1; // GraphicsDeviceType.OpenGLES3

        [MenuItem("VirtualVolley/Project Setup/Fix Android Graphics API (OpenGL ES 3.0 Only)")]
        public static void FixAndroidGraphicsAPISettings()
        {
            // Get the Android build target
            BuildTarget androidTarget = BuildTarget.Android;
            
            // Set graphics APIs to OpenGL ES 3.0 only
            GraphicsDeviceType[] apis = { GraphicsDeviceType.OpenGLES3 };
            PlayerSettings.SetGraphicsAPIs(androidTarget, apis);
            
            // Disable automatic API selection
            PlayerSettings.SetUseDefaultGraphicsAPIs(androidTarget, false);
            
            // Get current APIs to verify
            GraphicsDeviceType[] currentAPIs = PlayerSettings.GetGraphicsAPIs(androidTarget);
            
            Debug.Log($"[VirtualVolley] Android Graphics API configured:");
            Debug.Log($"[VirtualVolley] - APIs: {string.Join(", ", currentAPIs)}");
            Debug.Log($"[VirtualVolley] - Automatic: {PlayerSettings.GetUseDefaultGraphicsAPIs(androidTarget)}");
            Debug.Log($"[VirtualVolley] ✓ Android Graphics API set to OpenGL ES 3.0 ONLY (Quest 3 compatible)");
            
            // Force asset database refresh
            AssetDatabase.Refresh();
        }

        [MenuItem("VirtualVolley/Project Setup/Check Android Graphics API Settings")]
        public static void CheckAndroidGraphicsAPISettings()
        {
            BuildTarget androidTarget = BuildTarget.Android;
            GraphicsDeviceType[] currentAPIs = PlayerSettings.GetGraphicsAPIs(androidTarget);
            bool isAutomatic = PlayerSettings.GetUseDefaultGraphicsAPIs(androidTarget);
            
            Debug.Log($"[VirtualVolley] Current Android Graphics API Settings:");
            Debug.Log($"[VirtualVolley] - APIs: {string.Join(", ", currentAPIs)}");
            Debug.Log($"[VirtualVolley] - Automatic Selection: {isAutomatic}");
            
            // Check if Vulkan is in the list (should not be)
            bool hasVulkan = System.Array.Exists(currentAPIs, api => api == GraphicsDeviceType.Vulkan);
            bool hasOpenGLES3 = System.Array.Exists(currentAPIs, api => api == GraphicsDeviceType.OpenGLES3);
            
            if (hasVulkan)
            {
                Debug.LogWarning($"[VirtualVolley] ⚠ WARNING: Vulkan is enabled! This can cause shader errors on Quest 3.");
                Debug.LogWarning($"[VirtualVolley] Run 'Fix Android Graphics API (OpenGL ES 3.0 Only)' to fix this.");
            }
            
            if (!hasOpenGLES3)
            {
                Debug.LogWarning($"[VirtualVolley] ⚠ WARNING: OpenGL ES 3.0 is not enabled!");
            }
            
            if (!hasVulkan && hasOpenGLES3 && currentAPIs.Length == 1)
            {
                Debug.Log($"[VirtualVolley] ✓ Settings are correct for Quest 3!");
            }
        }
    }
}

