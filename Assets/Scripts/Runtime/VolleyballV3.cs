using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Volleyball physics system using Unity's built-in physics with minimal custom code.
    /// Leverages Rigidbody drag, PhysicMaterial, and Unity's collision system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VolleyballV3 : MonoBehaviour
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
        
        [Header("Hit Properties")]
        [Tooltip("Force multiplier when hit by moving objects")]
        [SerializeField] private float hitForceMultiplier = 1.5f;
        
        [Tooltip("Minimum relative velocity for a hit to register")]
        [SerializeField] private float minHitVelocity = 0.5f;
        
        [Tooltip("Maximum force that can be applied in a single hit")]
        [SerializeField] private float maxHitForce = 20f;
        
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
        
        // Contact tracking
        private float lastCollisionTime;
        private const float collisionCooldown = 0.1f;
        
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
            physicsMaterial = new PhysicMaterial("VolleyballV3Material")
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
            HandleCollision(collision);
        }
        
        private void HandleCollision(Collision collision)
        {
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
            
            // Check if this is a ground collision (static object with no rigidbody or kinematic rigidbody)
            bool isGroundCollision = IsGroundCollision(collision);
            
            // Get collision info
            Vector3 relativeVelocity = collision.relativeVelocity;
            float relativeSpeed = relativeVelocity.magnitude;
            
            // For ground collisions, let Unity's PhysicMaterial handle everything - don't interfere
            if (isGroundCollision)
            {
                // Only play sound, don't apply any custom forces
                if (relativeSpeed > minSoundVelocity && audioSource != null && hitSoundClip != null)
                {
                    PlayHitSound(relativeSpeed);
                }
                
                // Let Unity's PhysicMaterial handle the bounce completely
                return;
            }
            
            // For non-ground collisions (moving objects), apply cooldown and handle hits
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;
            
            lastCollisionTime = Time.time;
            
            // Check if this is a "hit" (collision with moving object)
            bool isHit = IsMovingObjectHit(collision, relativeSpeed);
            
            if (isHit)
            {
                HandleHit(collision, relativeVelocity);
            }
            
            // Play sound if velocity is high enough
            if (relativeSpeed > minSoundVelocity && audioSource != null && hitSoundClip != null)
            {
                PlayHitSound(relativeSpeed);
            }
        }
        
        private bool IsGroundCollision(Collision collision)
        {
            // Check if this is a collision with the ground (static object)
            Rigidbody otherRb = collision.rigidbody;
            
            // If no rigidbody, or kinematic rigidbody with no velocity, it's likely ground
            if (otherRb == null || (otherRb.isKinematic && otherRb.velocity.magnitude < 0.01f))
            {
                return true;
            }
            
            // Check by name/tag for ground objects
            GameObject other = collision.gameObject;
            if (other.name.Contains("Floor") || other.name.Contains("Ground") || 
                other.name.Contains("Court") || other.CompareTag("Ground") || 
                other.CompareTag("Floor"))
            {
                return true;
            }
            
            return false;
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
        
        private bool IsMovingObjectHit(Collision collision, float relativeSpeed)
        {
            // Only apply extra hit force for actual moving objects, not static ground
            Rigidbody otherRb = collision.rigidbody;
            
            if (otherRb != null && !otherRb.isKinematic)
            {
                // If other object has significant velocity, it's a hit (arm, another ball, etc.)
                if (otherRb.velocity.magnitude > minHitVelocity)
                {
                    return true;
                }
            }
            
            // Don't treat static ground collisions as hits - let Unity's PhysicMaterial handle bounces naturally
            return false;
        }
        
        private void HandleHit(Collision collision, Vector3 relativeVelocity)
        {
            // Get collision normal
            Vector3 normal = collision.contacts[0].normal;
            
            // Calculate hit force based on relative velocity
            float hitSpeed = relativeVelocity.magnitude;
            Vector3 hitDirection = relativeVelocity.normalized;
            
            // Reflect the hit direction off the surface normal
            Vector3 reflectedDirection = Vector3.Reflect(hitDirection, normal);
            
            // Apply force (clamped to prevent excessive speed)
            float forceMagnitude = hitSpeed * hitForceMultiplier;
            forceMagnitude = Mathf.Min(forceMagnitude, maxHitForce);
            Vector3 force = reflectedDirection * forceMagnitude;
            
            // Use Unity's built-in AddForce - Unity handles the physics
            rb.AddForce(force, ForceMode.Impulse);
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
            Debug.Log("[VolleyballV3] Ball grabbed - arm collisions disabled");
        }
        
        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            lastReleaseTime = Time.time;
            Debug.Log("[VolleyballV3] Ball released - arm collisions will re-enable after cooldown");
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

