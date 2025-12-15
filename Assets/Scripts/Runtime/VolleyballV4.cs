using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Volleyball physics system combining V1's responsive bounce feel with V3's grab detection and Unity physics.
    /// Applies force to all collisions with speed-based exponential scaling and moving object velocity components.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VolleyballV4 : MonoBehaviour
    {
        [Header("Physics Properties")]
        [Tooltip("Mass of the volleyball (kg)")]
        [SerializeField] private float mass = 0.27f;
        
        [Tooltip("Air resistance (Rigidbody drag - Unity handles this automatically)")]
        [SerializeField] private float airDrag = 0.5f;
        
        [Tooltip("Angular drag (how fast rotation slows down)")]
        [SerializeField] private float angularDrag = 0.5f;
        
        [Header("Physics Material Properties")]
        [Tooltip("Bounciness (0 = no bounce, 1 = perfect bounce)")]
        [SerializeField] private float bounciness = 0.5f;
        
        [Tooltip("Static friction (friction when not moving) - low for smooth rolling")]
        [SerializeField] private float staticFriction = 0.05f;
        
        [Tooltip("Dynamic friction (friction when moving) - low for smooth rolling")]
        [SerializeField] private float dynamicFriction = 0.05f;
        
        [Header("Force Multipliers (V1 Style)")]
        [Tooltip("Force multiplier for collisions with moving objects (arms, players, etc.)")]
        [SerializeField] private float movingObjectForceMultiplier = 5.0f;
        
        [Tooltip("Force multiplier for collisions with static objects (ground, walls, etc.)")]
        [SerializeField] private float staticObjectForceMultiplier = 1.5f;
        
        [Tooltip("Additional force multiplier based on collision speed (makes fast hits much stronger)")]
        [SerializeField] private float speedBasedForceMultiplier = 2.0f;
        
        [Tooltip("Minimum relative velocity to apply force (prevents tiny forces from slow collisions)")]
        [SerializeField] private float minVelocityThreshold = 0.05f;
        
        [Tooltip("Maximum force that can be applied in a single collision")]
        [SerializeField] private float maxForce = 100f;
        
        [Header("Collision Detection")]
        [Tooltip("Tags that identify moving objects (arms, players, etc.)")]
        [SerializeField] private string[] movingObjectTags = { "Player", "Arm", "Hand" };
        
        [Tooltip("Tags that identify static objects (ground, walls, etc.)")]
        [SerializeField] private string[] staticObjectTags = { "Ground", "Floor", "Wall", "CourtFloor" };
        
        [Header("Grab Detection")]
        [Tooltip("Time after release before collisions with arms are re-enabled")]
        [SerializeField] private float armCollisionCooldown = 0.1f;
        
        [Header("Audio")]
        [Tooltip("Audio source for collision sounds")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Audio clip for ball hit sound")]
        [SerializeField] private AudioClip hitSoundClip;
        
        [Tooltip("Minimum velocity for sound to play")]
        [SerializeField] private float minSoundVelocity = 0.5f;
        
        // Private fields
        private Rigidbody rb;
        private SphereCollider sphereCollider;
        private XRGrabInteractable grabInteractable;
        private PhysicMaterial physicsMaterial;
        
        // Grab state tracking
        private bool isGrabbed = false;
        private float lastReleaseTime = 0f;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            
            // Configure Rigidbody - Unity handles air resistance via drag
            rb.mass = mass;
            rb.drag = airDrag; // Unity's built-in air resistance
            rb.angularDrag = angularDrag;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Create and configure PhysicMaterial - Unity handles bounciness and friction
            physicsMaterial = new PhysicMaterial("VolleyballV4Material")
            {
                bounciness = bounciness,
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                bounceCombine = PhysicMaterialCombine.Maximum, // Use Maximum to ensure full bounce regardless of ground material
                frictionCombine = PhysicMaterialCombine.Minimum // Use Minimum so friction doesn't interfere with bounces
            };
            sphereCollider.material = physicsMaterial;
            
            // Get or add XRGrabInteractable for grab detection
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            
            // Subscribe to grab events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            
            // Setup audio if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 200f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length == 0) return;
            
            ContactPoint contact = collision.contacts[0];
            GameObject other = collision.gameObject;
            
            // Check if this is a collision with an arm
            bool isArmCollision = IsArmCollision(collision);
            
            // Ignore arm collisions if grabbed or recently released
            if (isArmCollision)
            {
                if (isGrabbed)
                {
                    return; // Ignore collisions while grabbed
                }
                
                // Check if we're still in cooldown after release
                if (Time.time - lastReleaseTime < armCollisionCooldown)
                {
                    return; // Ignore collisions during cooldown
                }
            }
            
            // Get the other object's rigidbody (if it exists)
            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            
            // Don't process collisions with other volleyballs (they handle their own)
            // But still trigger sound for volleyball-to-volleyball collisions
            if (other.name.ToLower().Contains("volleyball") && other != gameObject)
            {
                float speedForSound = collision.relativeVelocity.magnitude;
                
                if (speedForSound > minSoundVelocity && audioSource != null && hitSoundClip != null)
                {
                    PlayHitSound(speedForSound);
                }
                return;
            }
            
            bool isMovingObject = IsMovingObject(other, otherRb);
            
            // Calculate relative velocity
            Vector3 ballVelocity = rb.velocity;
            Vector3 otherVelocity = otherRb != null && !otherRb.isKinematic ? otherRb.velocity : Vector3.zero;
            Vector3 relativeVelocity = ballVelocity - otherVelocity;
            
            // Calculate collision normal (direction from contact point, pointing away from surface)
            Vector3 normal = contact.normal;
            
            // Project relative velocity onto the normal to get the collision force magnitude
            float relativeSpeed = Vector3.Dot(relativeVelocity, -normal);
            
            // Only apply force if there's significant relative velocity
            if (relativeSpeed < minVelocityThreshold)
            {
                // Still play sound for small collisions
                if (relativeSpeed > minSoundVelocity && audioSource != null && hitSoundClip != null)
                {
                    PlayHitSound(relativeSpeed);
                }
                return;
            }
            
            // Calculate force based on collision type (V1 style)
            float forceMultiplier = isMovingObject ? movingObjectForceMultiplier : staticObjectForceMultiplier;
            
            // For moving objects, also consider their velocity toward the ball (V1 style)
            float additionalSpeed = 0f;
            if (isMovingObject && otherRb != null && !otherRb.isKinematic)
            {
                // Add the other object's velocity component toward the ball
                float otherPushForce = Vector3.Dot(otherVelocity, -normal);
                if (otherPushForce > 0)
                {
                    additionalSpeed = otherPushForce;
                }
            }
            
            // Total effective speed includes both relative speed and additional push from moving object
            float totalEffectiveSpeed = relativeSpeed + additionalSpeed;
            
            // Calculate force magnitude with speed-based scaling (V1 style exponential scaling)
            // Faster collisions get exponentially stronger
            float speedMultiplier = 1f + (totalEffectiveSpeed * speedBasedForceMultiplier);
            float forceMagnitude = totalEffectiveSpeed * forceMultiplier * speedMultiplier;
            
            // Clamp to maximum force
            forceMagnitude = Mathf.Min(forceMagnitude, maxForce);
            
            // Create force vector in the direction of the normal (away from collision surface)
            Vector3 forceVector = normal * forceMagnitude;
            
            // Apply force as impulse (instant force) - V1 style
            rb.AddForce(forceVector, ForceMode.Impulse);
            
            // Play sound if velocity is high enough
            if (totalEffectiveSpeed > minSoundVelocity && audioSource != null && hitSoundClip != null)
            {
                PlayHitSound(totalEffectiveSpeed);
            }
        }
        
        private bool IsArmCollision(Collision collision)
        {
            // Check if the collision is with an arm (check by name or tag)
            GameObject other = collision.gameObject;
            
            // Check by name
            if (other.name.Contains("Arm") || other.name.Contains("Hand") || 
                other.name.Contains("Cylinder") || other.name.Contains("Sphere"))
            {
                // Check if it's part of the POV arms system
                if (other.transform.root.name.Contains("POVArms") || 
                    (other.transform.parent != null && other.transform.parent.name.Contains("POVArms")))
                {
                    return true;
                }
            }
            
            // Check by tag (if arms are tagged)
            if (other.CompareTag("Arm") || other.CompareTag("Hand"))
            {
                return true;
            }
            
            return false;
        }
        
        private bool IsMovingObject(GameObject obj, Rigidbody objRb)
        {
            // Check by tag (V1 style)
            foreach (string tag in movingObjectTags)
            {
                if (obj.CompareTag(tag))
                {
                    return true;
                }
            }
            
            // Check by name (for arms, hands, etc.) - V1 style
            string objName = obj.name.ToLower();
            if (objName.Contains("arm") || objName.Contains("hand") || objName.Contains("player"))
            {
                return true;
            }
            
            // Check if it has a non-kinematic rigidbody (it's a moving physics object) - V1 style
            if (objRb != null && !objRb.isKinematic && objRb.useGravity == false)
            {
                return true;
            }
            
            return false;
        }
        
        private void PlayHitSound(float velocity)
        {
            if (audioSource == null || hitSoundClip == null)
                return;
            
            // Scale volume based on velocity
            float volume = Mathf.Clamp01(0.2f + (velocity / 10f) * 0.8f);
            audioSource.volume = volume;
            
            // Randomize pitch slightly
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            
            audioSource.PlayOneShot(hitSoundClip);
        }
        
        // Grab event handlers
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            Debug.Log("[VolleyballV4] Ball grabbed - arm collisions disabled");
        }
        
        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            lastReleaseTime = Time.time;
            Debug.Log("[VolleyballV4] Ball released - arm collisions will re-enable after cooldown");
        }
        
        // Public methods for external control
        public bool IsGrabbed()
        {
            return isGrabbed;
        }
        
        // Public method to update physics material properties at runtime
        public void UpdatePhysicsMaterial(float newBounciness, float newStaticFriction, float newDynamicFriction)
        {
            if (physicsMaterial != null)
            {
                physicsMaterial.bounciness = newBounciness;
                physicsMaterial.staticFriction = newStaticFriction;
                physicsMaterial.dynamicFriction = newDynamicFriction;
            }
        }
        
        // Public method to update air drag at runtime
        public void UpdateAirDrag(float newDrag)
        {
            if (rb != null)
            {
                rb.drag = newDrag;
            }
        }
    }
}

