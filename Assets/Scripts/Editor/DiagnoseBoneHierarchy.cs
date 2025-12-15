using UnityEditor;
using UnityEngine;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Diagnoses the bone hierarchy to understand the structure.
    /// </summary>
    public static class DiagnoseBoneHierarchy
    {
        [MenuItem("VirtualVolley/Diagnostics/Arms/Diagnose Bone Hierarchy")]
        public static void Diagnose()
        {
            Debug.Log("======================================== BONE HIERARCHY DIAGNOSIS ========================================\n");
            
            GameObject arms = GameObject.Find("POVArms");
            if (arms == null)
            {
                Debug.LogError("❌ POVArms not found!");
                return;
            }
            
            Transform armature = arms.transform.Find("Armature");
            if (armature == null)
            {
                Debug.LogError("❌ Armature not found!");
                return;
            }
            
            // Find shoulder bones
            Transform leftShoulder = FindBone(armature, "Bone.016");
            Transform rightShoulder = FindBone(armature, "Bone.018");
            
            if (leftShoulder != null)
            {
                Debug.Log("=== LEFT SHOULDER (Bone.016) ===");
                Debug.Log($"  Position: {leftShoulder.position}");
                Debug.Log($"  Local Position: {leftShoulder.localPosition}");
                Debug.Log($"  Parent: {leftShoulder.parent?.name}");
                Debug.Log($"  Root: {leftShoulder.root.name}");
                Debug.Log($"  Is Child of Armature: {leftShoulder.IsChildOf(armature)}");
                
                // Check if it's constrained
                if (leftShoulder.parent != null)
                {
                    Debug.Log($"  Parent Position: {leftShoulder.parent.position}");
                    Debug.Log($"  Parent Local Position: {leftShoulder.parent.localPosition}");
                }
            }
            
            if (rightShoulder != null)
            {
                Debug.Log("\n=== RIGHT SHOULDER (Bone.018) ===");
                Debug.Log($"  Position: {rightShoulder.position}");
                Debug.Log($"  Local Position: {rightShoulder.localPosition}");
                Debug.Log($"  Parent: {rightShoulder.parent?.name}");
                Debug.Log($"  Root: {rightShoulder.root.name}");
                Debug.Log($"  Is Child of Armature: {rightShoulder.IsChildOf(armature)}");
                
                if (rightShoulder.parent != null)
                {
                    Debug.Log($"  Parent Position: {rightShoulder.parent.position}");
                    Debug.Log($"  Parent Local Position: {rightShoulder.parent.localPosition}");
                }
            }
            
            Debug.Log("\n=== ARMATURE STRUCTURE ===");
            PrintHierarchy(armature, 0);
            
            Debug.Log("\n======================================== END DIAGNOSIS ========================================\n");
        }
        
        private static void PrintHierarchy(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}- {parent.name} (Pos: {parent.position}, LocalPos: {parent.localPosition})");
            
            foreach (Transform child in parent)
            {
                PrintHierarchy(child, depth + 1);
            }
        }
        
        private static Transform FindBone(Transform parent, string boneName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == boneName)
                    return child;
                
                Transform found = FindBone(child, boneName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}

