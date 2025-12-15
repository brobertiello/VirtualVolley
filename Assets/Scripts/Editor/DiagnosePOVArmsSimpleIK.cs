using UnityEditor;
using UnityEngine;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Diagnoses the POVArmsSimpleIK setup.
    /// </summary>
    public static class DiagnosePOVArmsSimpleIK
    {
        [MenuItem("VirtualVolley/Diagnostics/Arms/Diagnose POV Arms Simple IK")]
        public static void Diagnose()
        {
            Debug.Log("======================================== POV ARMS SIMPLE IK DIAGNOSIS ========================================\n");
            
            GameObject arms = GameObject.Find("POVArms");
            if (arms == null)
            {
                Debug.LogError("❌ POVArms not found!");
                return;
            }
            
            POVArmsSimpleIK ikScript = arms.GetComponent<POVArmsSimpleIK>();
            if (ikScript == null)
            {
                Debug.LogError("❌ POVArmsSimpleIK component not found!");
                return;
            }
            
            Debug.Log("✓ POVArmsSimpleIK component found\n");
            
            // Check shoulder anchors
            SerializedObject so = new SerializedObject(ikScript);
            SerializedProperty leftAnchorProp = so.FindProperty("leftShoulderAnchor");
            SerializedProperty rightAnchorProp = so.FindProperty("rightShoulderAnchor");
            
            Transform leftAnchor = leftAnchorProp?.objectReferenceValue as Transform;
            Transform rightAnchor = rightAnchorProp?.objectReferenceValue as Transform;
            
            Debug.Log("=== SHOULDER ANCHORS ===");
            Debug.Log($"Left Shoulder Anchor: {(leftAnchor != null ? $"✓ {leftAnchor.name}" : "❌ NOT ASSIGNED")}");
            if (leftAnchor != null)
            {
                Debug.Log($"  Position: {leftAnchor.position}");
                Debug.Log($"  Local Position: {leftAnchor.localPosition}");
                Debug.Log($"  Parent: {(leftAnchor.parent != null ? leftAnchor.parent.name : "None")}");
            }
            
            Debug.Log($"Right Shoulder Anchor: {(rightAnchor != null ? $"✓ {rightAnchor.name}" : "❌ NOT ASSIGNED")}");
            if (rightAnchor != null)
            {
                Debug.Log($"  Position: {rightAnchor.position}");
                Debug.Log($"  Local Position: {rightAnchor.localPosition}");
                Debug.Log($"  Parent: {(rightAnchor.parent != null ? rightAnchor.parent.name : "None")}");
            }
            
            // Check bones
            SerializedProperty leftShoulderBoneProp = so.FindProperty("leftShoulderBone");
            SerializedProperty leftElbowBoneProp = so.FindProperty("leftElbowBone");
            SerializedProperty leftHandBoneProp = so.FindProperty("leftHandBone");
            SerializedProperty rightShoulderBoneProp = so.FindProperty("rightShoulderBone");
            SerializedProperty rightElbowBoneProp = so.FindProperty("rightElbowBone");
            SerializedProperty rightHandBoneProp = so.FindProperty("rightHandBone");
            
            Transform leftShoulderBone = leftShoulderBoneProp?.objectReferenceValue as Transform;
            Transform leftElbowBone = leftElbowBoneProp?.objectReferenceValue as Transform;
            Transform leftHandBone = leftHandBoneProp?.objectReferenceValue as Transform;
            Transform rightShoulderBone = rightShoulderBoneProp?.objectReferenceValue as Transform;
            Transform rightElbowBone = rightElbowBoneProp?.objectReferenceValue as Transform;
            Transform rightHandBone = rightHandBoneProp?.objectReferenceValue as Transform;
            
            Debug.Log("\n=== BONES ===");
            Debug.Log($"Left Shoulder Bone (Bone.016): {(leftShoulderBone != null ? $"✓ {leftShoulderBone.name} at {leftShoulderBone.position}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"Left Elbow Bone (Bone.017): {(leftElbowBone != null ? $"✓ {leftElbowBone.name} at {leftElbowBone.position}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"Left Hand Bone (Bone.001): {(leftHandBone != null ? $"✓ {leftHandBone.name} at {leftHandBone.position}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"Right Shoulder Bone (Bone.018): {(rightShoulderBone != null ? $"✓ {rightShoulderBone.name} at {rightShoulderBone.position}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"Right Elbow Bone (Bone.019): {(rightElbowBone != null ? $"✓ {rightElbowBone.name} at {rightElbowBone.position}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"Right Hand Bone (Bone.020): {(rightHandBone != null ? $"✓ {rightHandBone.name} at {rightHandBone.position}" : "❌ NOT ASSIGNED")}");
            
            // Check if shoulder bones are at anchor positions
            if (leftShoulderBone != null && leftAnchor != null)
            {
                float distance = Vector3.Distance(leftShoulderBone.position, leftAnchor.position);
                Debug.Log($"\nLeft Shoulder Bone distance from anchor: {distance:F3} (should be ~0)");
                if (distance > 0.1f)
                {
                    Debug.LogWarning($"⚠️  Left shoulder bone is not at anchor position!");
                }
            }
            
            if (rightShoulderBone != null && rightAnchor != null)
            {
                float distance = Vector3.Distance(rightShoulderBone.position, rightAnchor.position);
                Debug.Log($"Right Shoulder Bone distance from anchor: {distance:F3} (should be ~0)");
                if (distance > 0.1f)
                {
                    Debug.LogWarning($"⚠️  Right shoulder bone is not at anchor position!");
                }
            }
            
            // Check camera
            Unity.XR.CoreUtils.XROrigin xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                Debug.Log($"\n=== CAMERA ===");
                Debug.Log($"Camera: ✓ {xrOrigin.Camera.name} at {xrOrigin.Camera.transform.position}");
                
                // Check if anchors are children of camera
                if (leftAnchor != null)
                {
                    bool isChildOfCamera = leftAnchor.IsChildOf(xrOrigin.Camera.transform);
                    Debug.Log($"Left Anchor is child of camera: {isChildOfCamera}");
                }
                if (rightAnchor != null)
                {
                    bool isChildOfCamera = rightAnchor.IsChildOf(xrOrigin.Camera.transform);
                    Debug.Log($"Right Anchor is child of camera: {isChildOfCamera}");
                }
            }
            
            Debug.Log("\n======================================== END DIAGNOSIS ========================================\n");
        }
    }
}

