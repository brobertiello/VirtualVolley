using UnityEngine;
using UnityEditor;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Updates ball launcher speeds - increases service line and free ball launchers by 2.5x.
    /// </summary>
    public static class UpdateLauncherSpeeds
    {
        [MenuItem("VirtualVolley/Launchers/Update Launcher Speeds (2.5x Service & Free Balls)")]
        public static void Update()
        {
            Debug.Log("[VirtualVolley] ===== Updating Launcher Speeds =====\n");
            
            // Find all ball launchers in the scene
            BallLauncher[] allLaunchers = Object.FindObjectsOfType<BallLauncher>();
            
            if (allLaunchers == null || allLaunchers.Length == 0)
            {
                Debug.LogWarning("[VirtualVolley] No ball launchers found in scene!");
                return;
            }
            
            int updatedCount = 0;
            
            foreach (BallLauncher launcher in allLaunchers)
            {
                if (launcher == null) continue;
                
                SerializedObject so = new SerializedObject(launcher);
                SerializedProperty launcherTypeProp = so.FindProperty("launcherType");
                SerializedProperty baseHorizontalSpeedProp = so.FindProperty("baseHorizontalSpeed");
                
                if (launcherTypeProp == null || baseHorizontalSpeedProp == null)
                {
                    Debug.LogWarning($"[VirtualVolley] Could not find properties on {launcher.name}");
                    continue;
                }
                
                // Check if this is an Arc type launcher (service line or opponent court)
                int launcherType = launcherTypeProp.intValue;
                if (launcherType == (int)BallLauncher.LauncherType.Arc)
                {
                    float currentSpeed = baseHorizontalSpeedProp.floatValue;
                    float newSpeed = currentSpeed * 2.5f;
                    baseHorizontalSpeedProp.floatValue = newSpeed;
                    so.ApplyModifiedProperties();
                    
                    Debug.Log($"[VirtualVolley] Updated {launcher.name}: {currentSpeed} -> {newSpeed}");
                    updatedCount++;
                }
            }
            
            Debug.Log($"[VirtualVolley] ✓ Updated {updatedCount} launcher(s)!");
            Debug.Log("[VirtualVolley] Service line and free ball launchers are now 2.5x faster\n");
        }
    }
}

