using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Reads the Input Action Asset to find XRI controller actions.
    /// </summary>
    public static class ReadInputActionAsset
    {
        [MenuItem("VirtualVolley/Diagnostics/Input/Read XRI Input Action Asset")]
        public static void Read()
        {
            Debug.Log("======================================== XRI INPUT ACTION ASSET ========================================\n");
            
            // Find the XRI Default Input Actions asset
            string[] guids = AssetDatabase.FindAssets("XRI Default Input Actions");
            if (guids.Length == 0)
            {
                Debug.LogError("❌ Could not find 'XRI Default Input Actions' asset!");
                return;
            }
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                
                if (asset != null)
                {
                    Debug.Log($"✓ Found Input Action Asset: {assetPath}\n");
                    
                    // Read all action maps
                    foreach (InputActionMap actionMap in asset.actionMaps)
                    {
                        Debug.Log($"--- Action Map: {actionMap.name} ---");
                        
                        // Look for Left and Right controller maps
                        if (actionMap.name.Contains("Left") || actionMap.name.Contains("Right"))
                        {
                            Debug.Log($"  ⭐ Found controller map: {actionMap.name}");
                            
                            foreach (InputAction action in actionMap.actions)
                            {
                                Debug.Log($"    Action: {action.name} (Type: {action.type})");
                                
                                // Check if this is Position, Rotation, or Tracking State
                                if (action.name.Contains("Position") || 
                                    action.name.Contains("Rotation") || 
                                    action.name.Contains("Tracking"))
                                {
                                    Debug.Log($"      ⭐ This is what we need: {actionMap.name}/{action.name}");
                                }
                            }
                            Debug.Log("");
                        }
                    }
                }
            }
            
            Debug.Log("======================================== END ========================================\n");
        }
    }
}

