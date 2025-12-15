using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Finds and displays information about the XRI Input Action Asset.
    /// </summary>
    public static class FindXRIInputActionAsset
    {
        [MenuItem("VirtualVolley/Diagnostics/Input/Find XRI Input Action Asset")]
        public static void Find()
        {
            Debug.Log("======================================== XRI INPUT ACTION ASSET ========================================\n");
            
            // Find all InputActionAssets
            var allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            Debug.Log($"Found {allAssets.Length} InputActionAsset(s):\n");
            
            InputActionAsset xriAsset = null;
            foreach (var asset in allAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                Debug.Log($"  - {asset.name}");
                Debug.Log($"    Path: {assetPath}");
                
                if (asset.name.Contains("XRI") || asset.name.Contains("XR Interaction") || asset.name.Contains("XR"))
                {
                    xriAsset = asset;
                    Debug.Log($"    ✓ This appears to be the XRI asset!");
                }
                
                Debug.Log("");
            }
            
            if (xriAsset == null)
            {
                Debug.LogError("❌ No XRI Input Action Asset found!");
                Debug.LogError("\nTo create one:");
                Debug.LogError("1. Right-click in Project window > Create > Input Actions");
                Debug.LogError("2. Name it 'XRI Default Input Actions' or similar");
                Debug.LogError("3. Double-click to open the Input Actions window");
                Debug.LogError("4. Add action map 'XRI Left Interaction'");
                Debug.LogError("5. Add action 'Menu' to that map");
                return;
            }
            
            Debug.Log($"\n=== Using Asset: {xriAsset.name} ===\n");
            Debug.Log($"Path: {AssetDatabase.GetAssetPath(xriAsset)}\n");
            
            // List all action maps
            Debug.Log("Action Maps:");
            foreach (var map in xriAsset.actionMaps)
            {
                Debug.Log($"  - {map.name}");
                
                // List actions in each map
                Debug.Log($"    Actions:");
                foreach (var action in map.actions)
                {
                    Debug.Log($"      - {action.name}");
                }
                Debug.Log("");
            }
            
            // Check for XRI Left Interaction
            var leftInteractionMap = xriAsset.FindActionMap("XRI Left Interaction");
            if (leftInteractionMap == null)
            {
                Debug.LogWarning("⚠ XRI Left Interaction action map NOT FOUND!");
                Debug.LogWarning("\nTo add it:");
                Debug.LogWarning($"1. Open the Input Actions asset: {AssetDatabase.GetAssetPath(xriAsset)}");
                Debug.LogWarning("2. Click '+' to add a new Action Map");
                Debug.LogWarning("3. Name it 'XRI Left Interaction'");
                Debug.LogWarning("4. Click '+' under that map to add an Action");
                Debug.LogWarning("5. Name the action 'Menu'");
                Debug.LogWarning("6. Set the binding to your left controller menu button");
            }
            else
            {
                Debug.Log("✓ XRI Left Interaction action map found!");
                var menuAction = leftInteractionMap.FindAction("Menu");
                if (menuAction == null)
                {
                    Debug.LogWarning("⚠ 'Menu' action NOT FOUND in XRI Left Interaction!");
                    Debug.LogWarning("Available actions:");
                    foreach (var action in leftInteractionMap.actions)
                    {
                        Debug.LogWarning($"  - {action.name}");
                    }
                }
                else
                {
                    Debug.Log("✓ 'Menu' action found!");
                }
            }
            
            Debug.Log("\n======================================== END ========================================\n");
            
            // Select the asset in the project window
            if (xriAsset != null)
            {
                Selection.activeObject = xriAsset;
                EditorUtility.FocusProjectWindow();
            }
        }
    }
}

