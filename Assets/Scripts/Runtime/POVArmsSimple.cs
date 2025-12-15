using UnityEngine;
using Unity.XR.CoreUtils;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Simple POV arms system - directly moves hand bones to controller positions.
    /// No IK, no complex systems - just simple transform updates.
    /// </summary>
    public class POVArmsSimple : MonoBehaviour
    {
        [Header("Hand Bones")]
        [Tooltip("Left hand bone transform (will be auto-found if not assigned)")]
        [SerializeField] private Transform leftHand;
        
        [Tooltip("Right hand bone transform (will be auto-found if not assigned)")]
        [SerializeField] private Transform rightHand;
        
        [Header("Offsets")]
        [Tooltip("Position offset for left hand relative to controller")]
        public Vector3 leftPositionOffset = Vector3.zero;
        
        [Tooltip("Position offset for right hand relative to controller")]
        public Vector3 rightPositionOffset = Vector3.zero;
        
        [Tooltip("Rotation offset for left hand (Euler angles)")]
        public Vector3 leftRotationOffset = Vector3.zero;
        
        [Tooltip("Rotation offset for right hand (Euler angles)")]
        public Vector3 rightRotationOffset = Vector3.zero;
        
        [Header("Settings")]
        [Tooltip("If true, arms will be positioned relative to camera")]
        [SerializeField] private bool positionRelativeToCamera = true;
        
        // Private fields
        private Transform leftController;
        private Transform rightController;
        private Transform cameraTransform;
        private XROrigin xrOrigin;
        
        private void Awake()
        {
            // Find XR Origin and Camera
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                cameraTransform = xrOrigin.Camera?.transform;
                leftController = FindController(xrOrigin.transform, "Left");
                rightController = FindController(xrOrigin.transform, "Right");
            }
            
            // Find hand bones if not assigned
            if (leftHand == null || rightHand == null)
            {
                FindHandBones();
            }
        }
        
        private void Update()
        {
            // Ensure controllers are found (they might not be available in Awake)
            if (xrOrigin != null)
            {
                if (leftController == null)
                    leftController = FindController(xrOrigin.transform, "Left");
                if (rightController == null)
                    rightController = FindController(xrOrigin.transform, "Right");
                if (cameraTransform == null)
                    cameraTransform = xrOrigin.Camera?.transform;
            }
            
            // Update left hand
            if (leftHand != null && leftController != null)
            {
                UpdateHand(leftHand, leftController, leftPositionOffset, leftRotationOffset);
            }
            
            // Update right hand
            if (rightHand != null && rightController != null)
            {
                UpdateHand(rightHand, rightController, rightPositionOffset, rightRotationOffset);
            }
        }
        
        private void UpdateHand(Transform hand, Transform controller, Vector3 positionOffset, Vector3 rotationOffset)
        {
            // Get controller world position and rotation
            Vector3 controllerPosition = controller.position;
            Quaternion controllerRotation = controller.rotation;
            
            // Apply position offset in controller's local space
            Vector3 offsetPosition = controllerPosition + controllerRotation * positionOffset;
            
            // Apply rotation offset
            Quaternion offsetRotation = controllerRotation * Quaternion.Euler(rotationOffset);
            
            // Update hand transform
            hand.position = offsetPosition;
            hand.rotation = offsetRotation;
        }
        
        private void FindHandBones()
        {
            Transform armature = transform.Find("Armature");
            if (armature == null)
            {
                Debug.LogWarning("[POVArmsSimple] Armature not found! Please assign hand bones manually in Inspector.");
                return;
            }
            
            // Find hand bones (wrists)
            // Bone structure:
            // - Bone.016 = Left shoulder, Bone.017 = Left elbow, Bone.001 = Left wrist/hand
            // - Bone.018 = Right shoulder, Bone.019 = Right elbow, Bone.020 = Right wrist/hand
            Transform bone001 = FindBone(armature, "Bone.001"); // Left wrist/hand
            Transform bone020 = FindBone(armature, "Bone.020"); // Right wrist/hand
            
            if (bone001 != null && bone020 != null)
            {
                // Bone.001 is always left, Bone.020 is always right
                leftHand = bone001;
                rightHand = bone020;
                
                Debug.Log($"[POVArmsSimple] Auto-found hands: Left={leftHand.name} (Bone.001), Right={rightHand.name} (Bone.020)");
            }
            else
            {
                Debug.LogWarning("[POVArmsSimple] Could not find hand bones!");
                Debug.LogWarning("  Expected: Bone.001 (Left wrist/hand) and Bone.020 (Right wrist/hand)");
                Debug.LogWarning("  Please assign manually in Inspector.");
            }
        }
        
        private Transform FindController(Transform parent, string hand)
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
        
        private Transform FindBone(Transform parent, string boneName)
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

