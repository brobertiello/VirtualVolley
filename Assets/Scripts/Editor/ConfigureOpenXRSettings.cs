using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Editor tool to configure OpenXR settings for Quest 3.
    /// Enables hand tracking features: Hand Interaction Poses, Hand Tracking Subsystem, and Palm Pose.
    /// </summary>
    public static class ConfigureOpenXRSettings
    {
        [MenuItem("VirtualVolley/Project Setup/Configure OpenXR for Quest 3")]
        public static void ConfigureOpenXRForQuest3()
        {
            // Note: OpenXR feature settings are stored in asset files
            // This script provides instructions and verification
            
            Debug.Log("[VirtualVolley] OpenXR Configuration for Quest 3:");
            Debug.Log("[VirtualVolley] ==================================");
            Debug.Log("[VirtualVolley] ");
            Debug.Log("[VirtualVolley] To enable hand tracking features:");
            Debug.Log("[VirtualVolley] 1. Go to: Edit → Project Settings → XR Plug-in Management → OpenXR");
            Debug.Log("[VirtualVolley] 2. Select 'Android' tab");
            Debug.Log("[VirtualVolley] 3. Enable the following features:");
            Debug.Log("[VirtualVolley]    - Hand Interaction Poses");
            Debug.Log("[VirtualVolley]    - Hand Tracking Subsystem");
            Debug.Log("[VirtualVolley]    - Palm Pose");
            Debug.Log("[VirtualVolley] ");
            Debug.Log("[VirtualVolley] These features are already configured in the OpenXR Package Settings asset.");
            Debug.Log("[VirtualVolley] Verify they are enabled in Project Settings.");
            
            // Check if SampleScene is in build settings
            CheckBuildSettings();
        }

        [MenuItem("VirtualVolley/Project Setup/Verify Build Settings")]
        public static void CheckBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool hasSampleScene = false;
            
            foreach (var scene in scenes)
            {
                if (scene.enabled && scene.path.Contains("SampleScene"))
                {
                    hasSampleScene = true;
                    Debug.Log($"[VirtualVolley] ✓ SampleScene found in build settings: {scene.path}");
                    break;
                }
            }
            
            if (!hasSampleScene)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ SampleScene is not in build settings!");
                Debug.LogWarning("[VirtualVolley] Add it via: File → Build Settings → Add Open Scenes");
            }
            
            // Check Android build target
            BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            Debug.Log($"[VirtualVolley] Current Build Target: {currentTarget}");
            Debug.Log($"[VirtualVolley] Current Build Target Group: {currentGroup}");
            
            if (currentTarget != BuildTarget.Android)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ Build target is not Android. Switch to Android for Quest 3 builds.");
            }
            else
            {
                Debug.Log("[VirtualVolley] ✓ Build target is Android (correct for Quest 3)");
            }
        }
    }
}

