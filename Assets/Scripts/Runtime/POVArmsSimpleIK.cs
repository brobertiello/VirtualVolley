using UnityEngine;
using Unity.XR.CoreUtils;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Simple POV arms with 2-bone IK solver.
    /// Shoulder anchors connect to camera, arms use math to calculate optimal elbow positions.
    /// </summary>
    public class POVArmsSimpleIK : MonoBehaviour
    {
        [Header("Shoulder Anchors")]
        [Tooltip("Left shoulder anchor (will be created if not assigned)")]
        [SerializeField] private Transform leftShoulderAnchor;
        
        [Tooltip("Right shoulder anchor (will be created if not assigned)")]
        [SerializeField] private Transform rightShoulderAnchor;
        
        [Header("Bone References")]
        [Tooltip("Left shoulder bone (Bone.016)")]
        [SerializeField] private Transform leftShoulderBone;
        
        [Tooltip("Left elbow bone (Bone.017)")]
        [SerializeField] private Transform leftElbowBone;
        
        [Tooltip("Left hand bone (Bone.001)")]
        [SerializeField] private Transform leftHandBone;
        
        [Tooltip("Right shoulder bone (Bone.018)")]
        [SerializeField] private Transform rightShoulderBone;
        
        [Tooltip("Right elbow bone (Bone.019)")]
        [SerializeField] private Transform rightElbowBone;
        
        [Tooltip("Right hand bone (Bone.020)")]
        [SerializeField] private Transform rightHandBone;
        
        [Header("Hand Offsets")]
        [Tooltip("Position offset for left hand relative to controller")]
        public Vector3 leftHandPositionOffset = Vector3.zero;
        
        [Tooltip("Position offset for right hand relative to controller")]
        public Vector3 rightHandPositionOffset = Vector3.zero;
        
        [Tooltip("Rotation offset for left hand (Euler angles)")]
        public Vector3 leftHandRotationOffset = Vector3.zero;
        
        [Tooltip("Rotation offset for right hand (Euler angles)")]
        public Vector3 rightHandRotationOffset = Vector3.zero;
        
        [Header("Shoulder Settings")]
        [Tooltip("Shoulder position relative to camera (local space)")]
        public Vector3 leftShoulderOffset = new Vector3(-0.2f, -0.1f, 0.1f);
        
        [Tooltip("Shoulder position relative to camera (local space)")]
        public Vector3 rightShoulderOffset = new Vector3(0.2f, -0.1f, 0.1f);
        
        [Header("IK Settings")]
        [Tooltip("Preferred elbow bend direction (relative to shoulder-hand line)")]
        public Vector3 elbowBendDirection = Vector3.down;
        
        // Private fields
        private Transform leftController;
        private Transform rightController;
        private Transform cameraTransform;
        private XROrigin xrOrigin;
        
        // Parent bone transform (parent of both shoulders)
        private Transform parentBone;
        
        // Bone lengths (calculated in Awake)
        private float leftUpperArmLength;
        private float leftForearmLength;
        private float rightUpperArmLength;
        private float rightForearmLength;
        
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
            
            // Find bones first (needed for initial positions)
            FindBones();
            
            // Create shoulder anchors if not assigned
            CreateShoulderAnchors();
            
            // Calculate bone lengths (before moving shoulders)
            CalculateBoneLengths();
        }
        
        private void CreateShoulderAnchors()
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning("[POVArmsSimpleIK] Camera not found - cannot create shoulder anchors!");
                return;
            }
            
            // Create left shoulder anchor if not assigned
            if (leftShoulderAnchor == null)
            {
                // Check if it already exists as child of camera
                Transform existing = cameraTransform.Find("Left Shoulder Anchor");
                if (existing != null)
                {
                    leftShoulderAnchor = existing;
                    Debug.Log("[POVArmsSimpleIK] Found existing Left Shoulder Anchor");
                }
                else
                {
                    GameObject leftAnchor = new GameObject("Left Shoulder Anchor");
                    leftAnchor.transform.SetParent(cameraTransform);
                    leftShoulderAnchor = leftAnchor.transform;
                    leftShoulderAnchor.localPosition = leftShoulderOffset;
                    leftShoulderAnchor.localRotation = Quaternion.identity;
                    Debug.Log("[POVArmsSimpleIK] Created Left Shoulder Anchor");
                }
            }
            
            // Create right shoulder anchor if not assigned
            if (rightShoulderAnchor == null)
            {
                // Check if it already exists as child of camera
                Transform existing = cameraTransform.Find("Right Shoulder Anchor");
                if (existing != null)
                {
                    rightShoulderAnchor = existing;
                    Debug.Log("[POVArmsSimpleIK] Found existing Right Shoulder Anchor");
                }
                else
                {
                    GameObject rightAnchor = new GameObject("Right Shoulder Anchor");
                    rightAnchor.transform.SetParent(cameraTransform);
                    rightShoulderAnchor = rightAnchor.transform;
                    rightShoulderAnchor.localPosition = rightShoulderOffset;
                    rightShoulderAnchor.localRotation = Quaternion.identity;
                    Debug.Log("[POVArmsSimpleIK] Created Right Shoulder Anchor");
                }
            }
        }
        
        private void Update()
        {
            // Ensure controllers are found
            if (xrOrigin != null)
            {
                if (leftController == null)
                    leftController = FindController(xrOrigin.transform, "Left");
                if (rightController == null)
                    rightController = FindController(xrOrigin.transform, "Right");
                if (cameraTransform == null)
                    cameraTransform = xrOrigin.Camera?.transform;
            }
            
            // Update shoulder anchors relative to camera
            if (cameraTransform != null)
            {
                UpdateShoulderAnchors();
            }
            
            // Position parent bone to align shoulders with anchors
            // Since both shoulders share the same parent "Bone", we position it based on the left shoulder
            // (or average of both if needed)
            if (parentBone != null && leftShoulderBone != null && leftShoulderAnchor != null)
            {
                Vector3 leftAnchorPos = leftShoulderAnchor.position;
                Vector3 leftShoulderLocalPos = leftShoulderBone.localPosition;
                
                // Calculate where parent bone should be so left shoulder ends up at anchor
                // shoulderWorldPos = parentWorldPos + parentRotation * shoulderLocalPos
                // parentWorldPos = shoulderWorldPos - parentRotation * shoulderLocalPos
                Vector3 desiredParentPos = leftAnchorPos - parentBone.rotation * leftShoulderLocalPos;
                parentBone.position = desiredParentPos;
            }
            
            // Update left arm
            if (leftShoulderBone != null && leftElbowBone != null && leftHandBone != null && leftController != null)
            {
                UpdateArm(leftShoulderAnchor, leftShoulderBone, leftElbowBone, leftHandBone, 
                    leftController, leftHandPositionOffset, leftHandRotationOffset, 
                    leftUpperArmLength, leftForearmLength);
            }
            
            // Update right arm
            if (rightShoulderBone != null && rightElbowBone != null && rightHandBone != null && rightController != null)
            {
                UpdateArm(rightShoulderAnchor, rightShoulderBone, rightElbowBone, rightHandBone, 
                    rightController, rightHandPositionOffset, rightHandRotationOffset, 
                    rightUpperArmLength, rightForearmLength);
            }
        }
        
        private void UpdateShoulderAnchors()
        {
            // Update shoulder anchor positions relative to camera
            if (leftShoulderAnchor != null && cameraTransform != null)
            {
                // Ensure it's parented to camera
                if (leftShoulderAnchor.parent != cameraTransform)
                {
                    leftShoulderAnchor.SetParent(cameraTransform);
                }
                // Set local position (relative to camera)
                leftShoulderAnchor.localPosition = leftShoulderOffset;
                leftShoulderAnchor.localRotation = Quaternion.identity;
            }
            
            if (rightShoulderAnchor != null && cameraTransform != null)
            {
                // Ensure it's parented to camera
                if (rightShoulderAnchor.parent != cameraTransform)
                {
                    rightShoulderAnchor.SetParent(cameraTransform);
                }
                // Set local position (relative to camera)
                rightShoulderAnchor.localPosition = rightShoulderOffset;
                rightShoulderAnchor.localRotation = Quaternion.identity;
            }
        }
        
        private void UpdateArm(Transform shoulderAnchor, Transform shoulderBone, Transform elbowBone, Transform handBone,
            Transform controller, Vector3 handPositionOffset, Vector3 handRotationOffset,
            float upperArmLength, float forearmLength)
        {
            // 1. Position shoulder bone at shoulder anchor (WORLD position)
            if (shoulderAnchor == null || shoulderBone == null)
            {
                Debug.LogWarning("[POVArmsSimpleIK] Shoulder anchor or bone is null!");
                return;
            }
            
            // Get shoulder anchor world position
            Vector3 shoulderWorldPos = shoulderAnchor.position;
            
            // Note: Parent bone positioning is now handled in Update() before calling UpdateArm()
            // The shoulder bone should now be at the correct position relative to the parent
            
            // 2. Calculate hand target position (controller + offset)
            Vector3 handTargetPosition = controller.position + controller.rotation * handPositionOffset;
            Quaternion handTargetRotation = controller.rotation * Quaternion.Euler(handRotationOffset);
            
            // 3. Update hand bone
            handBone.position = handTargetPosition;
            handBone.rotation = handTargetRotation;
            
            // 4. Calculate elbow position using 2-bone IK
            Vector3 shoulderPos = shoulderBone.position;
            Vector3 handPos = handTargetPosition;
            Vector3 shoulderToHand = handPos - shoulderPos;
            float shoulderToHandDistance = shoulderToHand.magnitude;
            
            // Check if arm can reach (sum of bone lengths)
            float totalLength = upperArmLength + forearmLength;
            if (shoulderToHandDistance > totalLength)
            {
                // Arm is fully extended
                Vector3 direction = shoulderToHand.normalized;
                elbowBone.position = shoulderPos + direction * upperArmLength;
            }
            else if (shoulderToHandDistance < Mathf.Abs(upperArmLength - forearmLength))
            {
                // Arm is fully contracted
                Vector3 direction = shoulderToHand.normalized;
                elbowBone.position = shoulderPos + direction * (upperArmLength * 0.5f);
            }
            else
            {
                // Calculate elbow position using law of cosines
                // We have a triangle: shoulder -> elbow -> hand
                // We know: shoulder-elbow distance, elbow-hand distance, shoulder-hand distance
                
                float a = upperArmLength; // shoulder to elbow
                float b = forearmLength;  // elbow to hand
                float c = shoulderToHandDistance; // shoulder to hand
                
                // Angle at shoulder (using law of cosines)
                float angleAtShoulder = Mathf.Acos(Mathf.Clamp((a * a + c * c - b * b) / (2 * a * c), -1f, 1f));
                
                // Direction from shoulder to hand
                Vector3 shoulderToHandDir = shoulderToHand.normalized;
                
                // Perpendicular direction for elbow bend
                Vector3 bendDir = Vector3.Cross(shoulderToHandDir, elbowBendDirection);
                if (bendDir.magnitude < 0.01f)
                {
                    // Fallback if cross product is too small
                    bendDir = Vector3.Cross(shoulderToHandDir, Vector3.up);
                }
                bendDir.Normalize();
                
                // Rotate shoulder-to-hand direction around the bend direction
                Vector3 upperArmDir = Quaternion.AngleAxis(Mathf.Rad2Deg * angleAtShoulder, bendDir) * shoulderToHandDir;
                
                // Calculate elbow position
                elbowBone.position = shoulderPos + upperArmDir * upperArmLength;
            }
            
            // 5. Update bone rotations to point toward next bone
            // Shoulder bone rotation: point from shoulder to elbow
            Vector3 shoulderToElbow = elbowBone.position - shoulderBone.position;
            if (shoulderToElbow.magnitude > 0.01f)
            {
                // Use LookRotation but preserve the bone's up direction if possible
                // For arms, we typically want the bone to point along its forward axis
                shoulderBone.rotation = Quaternion.LookRotation(shoulderToElbow.normalized);
            }
            
            // Elbow bone rotation: point from elbow to hand
            Vector3 elbowToHand = handBone.position - elbowBone.position;
            if (elbowToHand.magnitude > 0.01f)
            {
                elbowBone.rotation = Quaternion.LookRotation(elbowToHand.normalized);
            }
            
            // Debug: Log positions occasionally
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[POVArmsSimpleIK] Arm Update: Shoulder={shoulderBone.position}, Elbow={elbowBone.position}, Hand={handBone.position}");
            }
        }
        
        private void FindBones()
        {
            Transform armature = transform.Find("Armature");
            if (armature == null)
            {
                Debug.LogWarning("[POVArmsSimpleIK] Armature not found!");
                return;
            }
            
            // Find all bones
            if (leftShoulderBone == null)
                leftShoulderBone = FindBone(armature, "Bone.016");
            if (leftElbowBone == null)
                leftElbowBone = FindBone(armature, "Bone.017");
            if (leftHandBone == null)
                leftHandBone = FindBone(armature, "Bone.001");
            
            if (rightShoulderBone == null)
                rightShoulderBone = FindBone(armature, "Bone.018");
            if (rightElbowBone == null)
                rightElbowBone = FindBone(armature, "Bone.019");
            if (rightHandBone == null)
                rightHandBone = FindBone(armature, "Bone.020");
            
            Debug.Log("[POVArmsSimpleIK] Found bones:");
            Debug.Log($"  Left: Shoulder={leftShoulderBone?.name}, Elbow={leftElbowBone?.name}, Hand={leftHandBone?.name}");
            Debug.Log($"  Right: Shoulder={rightShoulderBone?.name}, Elbow={rightElbowBone?.name}, Hand={rightHandBone?.name}");
        }
        
        private void CalculateBoneLengths()
        {
            // Calculate bone lengths from initial positions
            if (leftShoulderBone != null && leftElbowBone != null)
            {
                leftUpperArmLength = Vector3.Distance(leftShoulderBone.position, leftElbowBone.position);
            }
            if (leftElbowBone != null && leftHandBone != null)
            {
                leftForearmLength = Vector3.Distance(leftElbowBone.position, leftHandBone.position);
            }
            if (rightShoulderBone != null && rightElbowBone != null)
            {
                rightUpperArmLength = Vector3.Distance(rightShoulderBone.position, rightElbowBone.position);
            }
            if (rightElbowBone != null && rightHandBone != null)
            {
                rightForearmLength = Vector3.Distance(rightElbowBone.position, rightHandBone.position);
            }
            
            Debug.Log($"[POVArmsSimpleIK] Bone lengths:");
            Debug.Log($"  Left: Upper={leftUpperArmLength:F3}, Forearm={leftForearmLength:F3}");
            Debug.Log($"  Right: Upper={rightUpperArmLength:F3}, Forearm={rightForearmLength:F3}");
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

