using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Simple POV arms using Unity primitives (spheres for hands, cylinders for arms).
    /// Much simpler than dealing with rigged models!
    /// </summary>
    public class POVArmsPrimitives : MonoBehaviour
    {
        [Header("Shoulder Anchors")]
        [Tooltip("Left shoulder anchor (child of camera)")]
        [SerializeField] private Transform leftShoulderAnchor;
        
        [Tooltip("Right shoulder anchor (child of camera)")]
        [SerializeField] private Transform rightShoulderAnchor;
        
        [Header("Arm Dimensions")]
        [Tooltip("Upper arm length (shoulder to elbow)")]
        public float upperArmLength = 0.3f;
        
        [Tooltip("Forearm length (elbow to hand)")]
        public float forearmLength = 0.3f;
        
        [Tooltip("Arm thickness (cylinder radius)")]
        public float armThickness = 0.02f;
        
        [Tooltip("Hand size (sphere radius)")]
        public float handSize = 0.03f;
        
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
        [Tooltip("Left shoulder position relative to camera (local space)")]
        public Vector3 leftShoulderOffset = new Vector3(-0.2f, -0.1f, 0.1f);
        
        [Tooltip("Right shoulder position relative to camera (local space)")]
        public Vector3 rightShoulderOffset = new Vector3(0.2f, -0.1f, 0.1f);
        
        [Header("Materials")]
        [Tooltip("Material for arms (cylinders)")]
        public Material armMaterial;
        
        [Tooltip("Material for hands (spheres)")]
        public Material handMaterial;
        
        [Tooltip("Default material for arms (stored on initialization)")]
        private Material defaultArmMaterial;
        [Tooltip("Default material for hands (stored on initialization)")]
        private Material defaultHandMaterial;
        
        [Header("Collision Settings")]
        [Tooltip("Enable collision with volleyball")]
        public bool enableVolleyballCollision = true;
        
        [Tooltip("Time after ball release to ignore collisions (prevents immediate re-grab)")]
        public float collisionIgnoreTimeAfterRelease = 0.3f;
        
        // Private fields
        private Transform leftController;
        private Transform rightController;
        private Transform cameraTransform;
        private XROrigin xrOrigin;
        
        // Left arm primitives
        private GameObject leftUpperArm;
        private GameObject leftForearm;
        private GameObject leftHand;
        private Transform leftElbow;
        
        // Right arm primitives
        private GameObject rightUpperArm;
        private GameObject rightForearm;
        private GameObject rightHand;
        private Transform rightElbow;
        
        // Volleyball collision tracking
        private GameObject volleyball;
        private XRGrabInteractable volleyballGrab;
        private float lastReleaseTime = -1f;
        private bool volleyballWasGrabbed = false;
        
        // Colliders for arms
        private Collider[] armColliders;
        
        // Velocity tracking for dynamic bounce
        private Dictionary<GameObject, Vector3> previousPositions = new Dictionary<GameObject, Vector3>();
        private Dictionary<GameObject, Vector3> armVelocities = new Dictionary<GameObject, Vector3>();
        
        // Public getters for ReceivePlatform
        public Transform LeftElbow => leftElbow;
        public Transform RightElbow => rightElbow;
        public GameObject LeftHand => leftHand;
        public GameObject RightHand => rightHand;
        public Transform LeftShoulderAnchor => leftShoulderAnchor;
        public Transform RightShoulderAnchor => rightShoulderAnchor;
        
        // Arm parts for color changes
        public GameObject LeftUpperArm => leftUpperArm;
        public GameObject LeftForearm => leftForearm;
        public GameObject RightUpperArm => rightUpperArm;
        public GameObject RightForearm => rightForearm;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            // Also initialize in Start in case Awake didn't run
            if (leftUpperArm == null || rightUpperArm == null)
            {
                Initialize();
            }
        }
        
        private void Initialize()
        {
            // Find XR Origin and Camera
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                cameraTransform = xrOrigin.Camera?.transform;
                leftController = FindController(xrOrigin.transform, "Left");
                rightController = FindController(xrOrigin.transform, "Right");
            }
            
            // Create shoulder anchors if not assigned
            CreateShoulderAnchors();
            
            // Create arm primitives if they don't exist
            if (leftUpperArm == null || rightUpperArm == null)
            {
                CreateArmPrimitives();
            }
        }
        
        private void CreateShoulderAnchors()
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning("[POVArmsPrimitives] Camera not found - cannot create shoulder anchors!");
                return;
            }
            
            // Create left shoulder anchor if not assigned
            if (leftShoulderAnchor == null)
            {
                Transform existing = cameraTransform.Find("Left Shoulder Anchor");
                if (existing != null)
                {
                    leftShoulderAnchor = existing;
                }
                else
                {
                    GameObject leftAnchor = new GameObject("Left Shoulder Anchor");
                    leftAnchor.transform.SetParent(cameraTransform);
                    leftShoulderAnchor = leftAnchor.transform;
                    leftShoulderAnchor.localPosition = leftShoulderOffset;
                    leftShoulderAnchor.localRotation = Quaternion.identity;
                }
            }
            
            // Create right shoulder anchor if not assigned
            if (rightShoulderAnchor == null)
            {
                Transform existing = cameraTransform.Find("Right Shoulder Anchor");
                if (existing != null)
                {
                    rightShoulderAnchor = existing;
                }
                else
                {
                    GameObject rightAnchor = new GameObject("Right Shoulder Anchor");
                    rightAnchor.transform.SetParent(cameraTransform);
                    rightShoulderAnchor = rightAnchor.transform;
                    rightShoulderAnchor.localPosition = rightShoulderOffset;
                    rightShoulderAnchor.localRotation = Quaternion.identity;
                }
            }
        }
        
        public void CreateArmPrimitives()
        {
            // Create left arm
            leftUpperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftUpperArm.name = "Left Upper Arm";
            leftUpperArm.transform.SetParent(transform);
            leftUpperArm.transform.localScale = new Vector3(armThickness * 2, upperArmLength / 2, armThickness * 2);
            
            leftForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftForearm.name = "Left Forearm";
            leftForearm.transform.SetParent(transform);
            leftForearm.transform.localScale = new Vector3(armThickness * 2, forearmLength / 2, armThickness * 2);
            
            leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftHand.name = "Left Hand";
            leftHand.transform.SetParent(transform);
            leftHand.transform.localScale = Vector3.one * handSize * 2;
            
            // Create invisible elbow point
            GameObject leftElbowObj = new GameObject("Left Elbow");
            leftElbowObj.transform.SetParent(transform);
            leftElbow = leftElbowObj.transform;
            
            // Create right arm
            rightUpperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightUpperArm.name = "Right Upper Arm";
            rightUpperArm.transform.SetParent(transform);
            rightUpperArm.transform.localScale = new Vector3(armThickness * 2, upperArmLength / 2, armThickness * 2);
            
            rightForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightForearm.name = "Right Forearm";
            rightForearm.transform.SetParent(transform);
            rightForearm.transform.localScale = new Vector3(armThickness * 2, forearmLength / 2, armThickness * 2);
            
            rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHand.name = "Right Hand";
            rightHand.transform.SetParent(transform);
            rightHand.transform.localScale = Vector3.one * handSize * 2;
            
            // Create invisible elbow point
            GameObject rightElbowObj = new GameObject("Right Elbow");
            rightElbowObj.transform.SetParent(transform);
            rightElbow = rightElbowObj.transform;
            
            // Apply materials if provided
            if (armMaterial != null)
            {
                leftUpperArm.GetComponent<Renderer>().material = armMaterial;
                leftForearm.GetComponent<Renderer>().material = armMaterial;
                rightUpperArm.GetComponent<Renderer>().material = armMaterial;
                rightForearm.GetComponent<Renderer>().material = armMaterial;
                defaultArmMaterial = armMaterial;
            }
            
            if (handMaterial != null)
            {
                leftHand.GetComponent<Renderer>().material = handMaterial;
                rightHand.GetComponent<Renderer>().material = handMaterial;
                defaultHandMaterial = handMaterial;
            }
            
            // Keep colliders for volleyball collision, but make them triggers initially
            // We'll enable/disable collision with volleyball based on grab state
            armColliders = new Collider[]
            {
                leftUpperArm.GetComponent<Collider>(),
                leftForearm.GetComponent<Collider>(),
                leftHand.GetComponent<Collider>(),
                rightUpperArm.GetComponent<Collider>(),
                rightForearm.GetComponent<Collider>(),
                rightHand.GetComponent<Collider>()
            };
            
            // Initialize velocity tracking for all arm parts
            InitializeVelocityTracking();
            
            // Add Rigidbody components (kinematic) for proper collision
            foreach (var col in armColliders)
            {
                if (col != null)
                {
                    Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = col.gameObject.AddComponent<Rigidbody>();
                    }
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    
                    // Apply volleyball bounce physics material
                    PhysicMaterial bounceMaterial = null;
                    
                    // Try to load from Resources first
                    bounceMaterial = Resources.Load<PhysicMaterial>("VolleyballBouncyPhysics");
                    
                    // If not found, try to load from asset path (runtime won't work, but try anyway)
                    if (bounceMaterial == null)
                    {
                        // Try loading from common asset path
                        #if UNITY_EDITOR
                        bounceMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/Data/Materials/VolleyballBouncyPhysics.mat");
                        #endif
                    }
                    
                    // If still not found, create new material with volleyball bounce properties
                    if (bounceMaterial == null)
                    {
                        bounceMaterial = new PhysicMaterial("VolleyballBouncyPhysics");
                        bounceMaterial.bounciness = 0.8f; // High bounciness (0-1 range)
                        bounceMaterial.bounceCombine = PhysicMaterialCombine.Maximum; // Use maximum bounciness when colliding
                        bounceMaterial.frictionCombine = PhysicMaterialCombine.Minimum; // Low friction
                        bounceMaterial.staticFriction = 0.3f;
                        bounceMaterial.dynamicFriction = 0.3f;
                    }
                    
                    // Apply the material to the collider
                    col.material = bounceMaterial;
                    
                    // Make sure collider is NOT a trigger (needed for OnCollisionEnter)
                    col.isTrigger = false;
                    
                    // Ensure the rigidbody can detect collisions even when kinematic
                    rb.detectCollisions = true;
                    
                    // Add collision handler component to track collisions and apply velocity
                    var helper = col.gameObject.AddComponent<ArmCollisionHelper>();
                    helper.SetParentScript(this, col.gameObject);
                }
            }
            
            // Ignore collision between arms and player body (XR Origin)
            IgnorePlayerBodyCollision();
            
            // Find volleyball
            FindVolleyball();
        }
        
        private void InitializeVelocityTracking()
        {
            // Initialize velocity tracking for all arm parts
            GameObject[] armParts = { leftUpperArm, leftForearm, leftHand, rightUpperArm, rightForearm, rightHand };
            
            foreach (var part in armParts)
            {
                if (part != null)
                {
                    previousPositions[part] = part.transform.position;
                    armVelocities[part] = Vector3.zero;
                }
            }
        }
        
        private void IgnorePlayerBodyCollision()
        {
            // Find XR Origin (player body)
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XR Origin");
            }
            
            if (xrOrigin == null)
            {
                Unity.XR.CoreUtils.XROrigin xrOriginComp = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOriginComp != null)
                {
                    xrOrigin = xrOriginComp.gameObject;
                }
            }
            
            if (xrOrigin != null && armColliders != null)
            {
                // Get all colliders on XR Origin and its children
                Collider[] bodyColliders = xrOrigin.GetComponentsInChildren<Collider>();
                
                foreach (var armCol in armColliders)
                {
                    if (armCol != null)
                    {
                        foreach (var bodyCol in bodyColliders)
                        {
                            if (bodyCol != null && bodyCol != armCol)
                            {
                                Physics.IgnoreCollision(armCol, bodyCol, true);
                            }
                        }
                    }
                }
                
                Debug.Log($"[POVArmsPrimitives] Ignored collision between arms and {bodyColliders.Length} player body collider(s)");
            }
        }
        
        private void FindVolleyball()
        {
            GameObject volleyBall = GameObject.Find("volleyball");
            if (volleyBall == null)
            {
                volleyBall = GameObject.Find("Volleyball");
            }
            
            if (volleyBall != null)
            {
                volleyball = volleyBall;
                volleyballGrab = volleyBall.GetComponent<XRGrabInteractable>();
                if (volleyballGrab != null)
                {
                    // Subscribe to grab events
                    volleyballGrab.selectEntered.AddListener(OnVolleyballGrabbed);
                    volleyballGrab.selectExited.AddListener(OnVolleyballReleased);
                }
                Debug.Log("[POVArmsPrimitives] Found volleyball for collision");
            }
            else
            {
                Debug.LogWarning("[POVArmsPrimitives] Volleyball not found - collision will not work");
            }
        }
        
        private void OnVolleyballGrabbed(SelectEnterEventArgs args)
        {
            volleyballWasGrabbed = true;
            UpdateVolleyballCollision();
        }
        
        private void OnVolleyballReleased(SelectExitEventArgs args)
        {
            volleyballWasGrabbed = false;
            lastReleaseTime = Time.time;
            UpdateVolleyballCollision();
        }
        
        private void UpdateVolleyballCollision()
        {
            if (!enableVolleyballCollision || volleyball == null || armColliders == null)
                return;
            
            Collider volleyballCollider = volleyball.GetComponent<Collider>();
            if (volleyballCollider == null)
                return;
            
            bool shouldCollide = true;
            
            // Don't collide if ball is being held
            if (volleyballWasGrabbed)
            {
                shouldCollide = false;
            }
            // Don't collide if ball was recently released
            else if (lastReleaseTime > 0 && Time.time - lastReleaseTime < collisionIgnoreTimeAfterRelease)
            {
                shouldCollide = false;
            }
            
            // Enable/disable collision between arms and volleyball
            foreach (var armCollider in armColliders)
            {
                if (armCollider != null)
                {
                    Physics.IgnoreCollision(armCollider, volleyballCollider, !shouldCollide);
                }
            }
        }
        
        private void UpdateArmVelocities()
        {
            // Update velocity for all arm parts by tracking position changes
            GameObject[] armParts = { leftUpperArm, leftForearm, leftHand, rightUpperArm, rightForearm, rightHand };
            
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0) return;
            
            foreach (var part in armParts)
            {
                if (part != null && previousPositions.ContainsKey(part))
                {
                    Vector3 currentPos = part.transform.position;
                    Vector3 previousPos = previousPositions[part];
                    
                    // Calculate velocity from position change
                    Vector3 velocity = (currentPos - previousPos) / deltaTime;
                    
                    // Use raw velocity for more responsive hits (less smoothing)
                    if (armVelocities.ContainsKey(part))
                    {
                        // Use exponential smoothing with higher weight on new value for responsiveness
                        armVelocities[part] = Vector3.Lerp(armVelocities[part], velocity, 0.7f);
                    }
                    else
                    {
                        armVelocities[part] = velocity;
                    }
                    
                    // Update previous position
                    previousPositions[part] = currentPos;
                }
                else if (part != null)
                {
                    // Initialize if not tracked yet
                    previousPositions[part] = part.transform.position;
                    armVelocities[part] = Vector3.zero;
                }
            }
        }
        
        /// <summary>
        /// Gets the current velocity of an arm part for collision calculations.
        /// </summary>
        public Vector3 GetArmVelocity(GameObject armPart)
        {
            if (armVelocities.ContainsKey(armPart))
            {
                return armVelocities[armPart];
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// Sets the color of all arm parts.
        /// </summary>
        public void SetArmColor(Color color)
        {
            if (leftUpperArm != null) leftUpperArm.GetComponent<Renderer>().material.color = color;
            if (leftForearm != null) leftForearm.GetComponent<Renderer>().material.color = color;
            if (leftHand != null) leftHand.GetComponent<Renderer>().material.color = color;
            if (rightUpperArm != null) rightUpperArm.GetComponent<Renderer>().material.color = color;
            if (rightForearm != null) rightForearm.GetComponent<Renderer>().material.color = color;
            if (rightHand != null) rightHand.GetComponent<Renderer>().material.color = color;
        }
        
        /// <summary>
        /// Resets arm colors to default materials.
        /// </summary>
        public void ResetArmColors()
        {
            if (leftUpperArm != null && defaultArmMaterial != null) leftUpperArm.GetComponent<Renderer>().material = defaultArmMaterial;
            if (leftForearm != null && defaultArmMaterial != null) leftForearm.GetComponent<Renderer>().material = defaultArmMaterial;
            if (leftHand != null && defaultHandMaterial != null) leftHand.GetComponent<Renderer>().material = defaultHandMaterial;
            if (rightUpperArm != null && defaultArmMaterial != null) rightUpperArm.GetComponent<Renderer>().material = defaultArmMaterial;
            if (rightForearm != null && defaultArmMaterial != null) rightForearm.GetComponent<Renderer>().material = defaultArmMaterial;
            if (rightHand != null && defaultHandMaterial != null) rightHand.GetComponent<Renderer>().material = defaultHandMaterial;
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
            
            // Update left arm
            if (leftShoulderAnchor != null && leftController != null)
            {
                UpdateArm(leftShoulderAnchor, leftController, leftHandPositionOffset, leftHandRotationOffset,
                    leftUpperArm, leftForearm, leftHand, leftElbow, true);
            }
            
            // Update right arm
            if (rightShoulderAnchor != null && rightController != null)
            {
                UpdateArm(rightShoulderAnchor, rightController, rightHandPositionOffset, rightHandRotationOffset,
                    rightUpperArm, rightForearm, rightHand, rightElbow, false);
            }
            
            // Update volleyball collision state
            if (enableVolleyballCollision)
            {
                UpdateVolleyballCollision();
            }
        }
        
        private void UpdateShoulderAnchors()
        {
            if (leftShoulderAnchor != null && cameraTransform != null)
            {
                if (leftShoulderAnchor.parent != cameraTransform)
                {
                    leftShoulderAnchor.SetParent(cameraTransform);
                }
                leftShoulderAnchor.localPosition = leftShoulderOffset;
                leftShoulderAnchor.localRotation = Quaternion.identity;
            }
            
            if (rightShoulderAnchor != null && cameraTransform != null)
            {
                if (rightShoulderAnchor.parent != cameraTransform)
                {
                    rightShoulderAnchor.SetParent(cameraTransform);
                }
                rightShoulderAnchor.localPosition = rightShoulderOffset;
                rightShoulderAnchor.localRotation = Quaternion.identity;
            }
        }
        
        private void UpdateArm(Transform shoulderAnchor, Transform controller, Vector3 handPositionOffset, Vector3 handRotationOffset,
            GameObject upperArm, GameObject forearm, GameObject hand, Transform elbow, bool isLeft)
        {
            // 1. Get shoulder position
            Vector3 shoulderPos = shoulderAnchor.position;
            
            // 2. Calculate hand target position (controller + offset)
            Vector3 handTargetPosition = controller.position + controller.rotation * handPositionOffset;
            Quaternion handTargetRotation = controller.rotation * Quaternion.Euler(handRotationOffset);
            
            // 3. Update hand position and rotation
            hand.transform.position = handTargetPosition;
            hand.transform.rotation = handTargetRotation;
            
            // 4. Calculate elbow position using 2-bone IK
            Vector3 shoulderToHand = handTargetPosition - shoulderPos;
            float shoulderToHandDistance = shoulderToHand.magnitude;
            float totalLength = upperArmLength + forearmLength;
            
            Vector3 elbowPos;
            
            if (shoulderToHandDistance > totalLength)
            {
                // Arm is fully extended
                Vector3 direction = shoulderToHand.normalized;
                elbowPos = shoulderPos + direction * upperArmLength;
            }
            else if (shoulderToHandDistance < Mathf.Abs(upperArmLength - forearmLength))
            {
                // Arm is fully contracted
                Vector3 direction = shoulderToHand.normalized;
                elbowPos = shoulderPos + direction * (upperArmLength * 0.5f);
            }
            else
            {
                // Calculate elbow position using law of cosines
                float a = upperArmLength; // shoulder to elbow
                float b = forearmLength;  // elbow to hand
                float c = shoulderToHandDistance; // shoulder to hand
                
                // Angle at shoulder (using law of cosines)
                float angleAtShoulder = Mathf.Acos(Mathf.Clamp((a * a + c * c - b * b) / (2 * a * c), -1f, 1f));
                
                // Direction from shoulder to hand
                Vector3 shoulderToHandDir = shoulderToHand.normalized;
                
                // Calculate bend direction: perpendicular to shoulder-to-hand line, pointing downward
                // We want the elbow to always bend towards the ground (negative Y)
                
                // First, try to get a perpendicular vector using cross product with down
                Vector3 bendDir = Vector3.Cross(shoulderToHandDir, Vector3.down);
                
                // If that's too small (arm is pointing straight down), use forward/back
                if (bendDir.magnitude < 0.01f)
                {
                    bendDir = Vector3.Cross(shoulderToHandDir, Vector3.forward);
                }
                
                // If still too small, use right
                if (bendDir.magnitude < 0.01f)
                {
                    bendDir = Vector3.Cross(shoulderToHandDir, Vector3.right);
                }
                
                bendDir.Normalize();
                
                // Now ensure the elbow position will be lower (more negative Y) than if we used the opposite direction
                // We want the elbow to bend downward, so we check which direction results in a lower elbow
                Vector3 testBendDir1 = bendDir;
                Vector3 testBendDir2 = -bendDir;
                
                // Calculate test elbow positions
                Quaternion rot1 = Quaternion.AngleAxis(angleAtShoulder * Mathf.Rad2Deg, testBendDir1);
                Quaternion rot2 = Quaternion.AngleAxis(angleAtShoulder * Mathf.Rad2Deg, testBendDir2);
                Vector3 testElbow1 = shoulderPos + (rot1 * shoulderToHandDir) * upperArmLength;
                Vector3 testElbow2 = shoulderPos + (rot2 * shoulderToHandDir) * upperArmLength;
                
                // Choose the direction that results in the lower elbow (more negative Y)
                if (testElbow2.y < testElbow1.y)
                {
                    bendDir = testBendDir2;
                }
                else
                {
                    bendDir = testBendDir1;
                }
                
                // Rotate shoulder-to-hand direction around the bend direction by the calculated angle
                Quaternion rotation = Quaternion.AngleAxis(angleAtShoulder * Mathf.Rad2Deg, bendDir);
                Vector3 upperArmDirection = rotation * shoulderToHandDir;
                
                // Elbow position
                elbowPos = shoulderPos + upperArmDirection * upperArmLength;
            }
            
            elbow.position = elbowPos;
            
            // 5. Position and rotate upper arm (shoulder to elbow)
            Vector3 upperArmMid = (shoulderPos + elbowPos) / 2;
            upperArm.transform.position = upperArmMid;
            Vector3 upperArmDir = (elbowPos - shoulderPos).normalized;
            if (upperArmDir != Vector3.zero)
            {
                upperArm.transform.rotation = Quaternion.LookRotation(upperArmDir) * Quaternion.Euler(90, 0, 0);
            }
            
            // 6. Position and rotate forearm (elbow to hand)
            Vector3 forearmMid = (elbowPos + handTargetPosition) / 2;
            forearm.transform.position = forearmMid;
            Vector3 forearmDir = (handTargetPosition - elbowPos).normalized;
            if (forearmDir != Vector3.zero)
            {
                forearm.transform.rotation = Quaternion.LookRotation(forearmDir) * Quaternion.Euler(90, 0, 0);
            }
        }
        
        private Transform FindController(Transform xrOrigin, string side)
        {
            // Look for controller in common locations
            string[] paths = {
                $"Camera Offset/{side} Controller",
                $"Camera Offset/{side} Hand Controller",
                $"{side} Controller",
                $"{side} Hand Controller"
            };
            
            foreach (string path in paths)
            {
                Transform found = xrOrigin.Find(path);
                if (found != null)
                    return found;
            }
            
            // Fallback: search recursively
            return FindControllerRecursive(xrOrigin, side);
        }
        
        private Transform FindControllerRecursive(Transform parent, string side)
        {
            if (parent.name.Contains(side) && parent.name.Contains("Controller"))
                return parent;
            
            foreach (Transform child in parent)
            {
                Transform found = FindControllerRecursive(child, side);
                if (found != null)
                    return found;
            }
            
            return null;
        }
    }
}

