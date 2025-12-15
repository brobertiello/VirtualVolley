using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Lists all Input Action References in the project.
    /// </summary>
    public static class ListAllInputActionReferences
    {
        [MenuItem("VirtualVolley/Diagnostics/Input/List All Input Action References")]
        public static void List()
        {
            Debug.Log("======================================== ALL INPUT ACTION REFERENCES ========================================\n");
            
            string[] guids = AssetDatabase.FindAssets("t:InputActionReference");
            Debug.Log($"Found {guids.Length} Input Action Reference(s) in project:\n");
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(assetPath);
                if (reference != null && reference.action != null)
                {
                    string map = reference.action.actionMap?.name ?? "Unknown";
                    string action = reference.action.name ?? "Unknown";
                    string fullPath = $"{map}/{action}";
                    
                    Debug.Log($"  {reference.name}");
                    Debug.Log($"    Path: {assetPath}");
                    Debug.Log($"    Full Action Path: {fullPath}");
                    
                    // Highlight XRI-related ones
                    if (map.Contains("XRI") || action.Contains("Position") || action.Contains("Rotation"))
                    {
                        Debug.Log($"    ⭐ This might be what we need!");
                    }
                    Debug.Log("");
                }
            }
            
            Debug.Log("======================================== END ========================================\n");
        }
    }
}

