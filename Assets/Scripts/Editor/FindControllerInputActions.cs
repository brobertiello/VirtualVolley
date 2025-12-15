using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SpatialTracking;
using Unity.XR.CoreUtils;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Finds what Input Action References the controllers are actually using.
    /// </summary>
    public static class FindControllerInputActions
    {
        [MenuItem("VirtualVolley/Diagnostics/Input/Find Controller Input Action References")]
        public static void Find()
        {
            Debug.Log("======================================== CONTROLLER INPUT ACTIONS ========================================\n");
            
            XROrigin xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ XR Origin not found!");
                return;
            }
            
            // Find controllers
            Transform leftController = FindController(xrOrigin.transform, "Left");
            Transform rightController = FindController(xrOrigin.transform, "Right");
            
            if (leftController != null)
            {
                Debug.Log($"--- Left Controller: {leftController.name} ---");
                CheckControllerInputActions(leftController);
            }
            
            if (rightController != null)
            {
                Debug.Log($"\n--- Right Controller: {rightController.name} ---");
                CheckControllerInputActions(rightController);
            }
            
            Debug.Log("\n======================================== END ========================================\n");
        }
        
        private static Transform FindController(Transform parent, string hand)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(hand) && child.name.Contains("Controller"))
                    return child;
                
                Transform found = FindController(child, hand);
                if (found != null)
                    return found;
            }
            return null;
        }
        
        private static void CheckControllerInputActions(Transform controller)
        {
            // Check for TrackedPoseDriver (might be on controller or children)
            TrackedPoseDriver poseDriver = controller.GetComponent<TrackedPoseDriver>();
            if (poseDriver == null)
            {
                poseDriver = controller.GetComponentInChildren<TrackedPoseDriver>();
            }
            
            if (poseDriver != null)
            {
                Debug.Log("✓ Has TrackedPoseDriver component");
                
                // Use SerializedObject to read the fields (most reliable method)
                SerializedObject so = new SerializedObject(poseDriver);
                SerializedProperty posProp = so.FindProperty("m_PositionInput");
                SerializedProperty rotProp = so.FindProperty("m_RotationInput");
                SerializedProperty trackProp = so.FindProperty("m_TrackingStateInput");
                
                if (posProp != null)
                {
                    InputActionReference posRef = posProp.objectReferenceValue as InputActionReference;
                    Debug.Log($"  Position Input: {(posRef != null ? $"✓ {posRef.name} ({GetActionPath(posRef)})" : "❌ Not assigned")}");
                }
                else
                {
                    Debug.LogWarning("  Could not find m_PositionInput property");
                }
                
                if (rotProp != null)
                {
                    InputActionReference rotRef = rotProp.objectReferenceValue as InputActionReference;
                    Debug.Log($"  Rotation Input: {(rotRef != null ? $"✓ {rotRef.name} ({GetActionPath(rotRef)})" : "❌ Not assigned")}");
                }
                else
                {
                    Debug.LogWarning("  Could not find m_RotationInput property");
                }
                
                if (trackProp != null)
                {
                    InputActionReference trackRef = trackProp.objectReferenceValue as InputActionReference;
                    Debug.Log($"  Tracking State Input: {(trackRef != null ? $"✓ {trackRef.name} ({GetActionPath(trackRef)})" : "❌ Not assigned")}");
                }
                else
                {
                    Debug.LogWarning("  Could not find m_TrackingStateInput property");
                }
            }
            else
            {
                Debug.LogWarning("❌ No TrackedPoseDriver found on controller");
                Debug.LogWarning("  Checking for other components that might use Input Actions...");
                
                // Check all components on the controller
                Component[] components = controller.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    if (comp == null) continue;
                    Debug.Log($"  Component: {comp.GetType().Name}");
                }
            }
        }
        
        private static string GetActionPath(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return "null";
            string map = reference.action.actionMap?.name ?? "Unknown";
            string action = reference.action.name ?? "Unknown";
            return $"{map}/{action}";
        }
    }
}

