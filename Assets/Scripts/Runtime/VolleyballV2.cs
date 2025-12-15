using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Advanced volleyball physics system with air resistance, ground friction, and hit reactions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VolleyballV2 : MonoBehaviour
    {
        [Header("Physics Properties")]
        [Tooltip("Mass of the volleyball (kg)")]
        [SerializeField] private float mass = 0.27f;
        
        [Tooltip("Air resistance coefficient (higher = more drag)")]
        [SerializeField] private float airResistance = 0.02f;
        
        [Tooltip("Ground friction coefficient (0 = no friction, 1 = maximum friction)")]
        [SerializeField] private float groundFriction = 0.3f;
        
        [Tooltip("Bounciness coefficient (0 = no bounce, 1 = perfect bounce)")]
        [SerializeField] private float bounciness = 0.8f;
        
        [Tooltip("Minimum velocity for bounce to occur")]
        [SerializeField] private float minBounceVelocity = 0.1f;
        
        [Header("Hit Properties")]
        [Tooltip("Force multiplier when hit by moving objects")]
        [SerializeField] private float hitForceMultiplier = 1.5f;
        
        [Tooltip("Minimum relative velocity for a hit to register")]
        [SerializeField] private float minHitVelocity = 0.5f;
        
        [Header("Grab Detection")]
        [Tooltip("Time after release before collisions with arms are re-enabled")]
        [SerializeField] private float armCollisionCooldown = 0.1f;
        
        [Header("Ground Detection")]
        [Tooltip("Distance to check for ground")]
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        [Tooltip("Layer mask for ground detection")]
        [SerializeField] private LayerMask groundLayerMask = -1;
        
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
        private bool isGrounded;
        private Vector3 lastVelocity;
        private float lastGroundCheckTime;
        private const float groundCheckInterval = 0.1f;
        
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
            
            // Configure Rigidbody
            rb.mass = mass;
            rb.drag = 0f; // We'll handle air resistance manually
            rb.angularDrag = 0.5f;
            
            // Get or add XRGrabInteractable for grab detection
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            
            // Subscribe to grab events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            
            // Configure Collider
            if (sphereCollider != null)
            {
                PhysicMaterial material = new PhysicMaterial("VolleyballV2Material")
                {
                    bounciness = bounciness,
                    staticFriction = groundFriction,
                    dynamicFriction = groundFriction,
                    bounceCombine = PhysicMaterialCombine.Maximum,
                    frictionCombine = PhysicMaterialCombine.Average
                };
                sphereCollider.material = material;
            }
            
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
        
        private void Start()
        {
            lastVelocity = rb.velocity;
        }
        
        private void FixedUpdate()
        {
            // Check if grounded
            CheckGrounded();
            
            // Apply air resistance (only when in air)
            if (!isGrounded)
            {
                ApplyAirResistance();
            }
            else
            {
                ApplyGroundFriction();
            }
            
            // Update last velocity
            lastVelocity = rb.velocity;
        }
        
        private void CheckGrounded()
        {
            // Only check periodically to save performance
            if (Time.time - lastGroundCheckTime < groundCheckInterval)
            {
                return;
            }
            
            lastGroundCheckTime = Time.time;
            
            // Raycast downward to check for ground
            float radius = sphereCollider != null ? sphereCollider.radius : 0.1f;
            Vector3 origin = transform.position;
            origin.y -= radius * 0.9f; // Check slightly below the ball
            
            isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayerMask);
        }
        
        private void ApplyAirResistance()
        {
            // Air resistance is proportional to velocity squared
            Vector3 velocity = rb.velocity;
            float speed = velocity.magnitude;
            
            if (speed > 0.01f)
            {
                Vector3 dragForce = -velocity.normalized * (airResistance * speed * speed);
                rb.AddForce(dragForce, ForceMode.Force);
            }
        }
        
        private void ApplyGroundFriction()
        {
            // Apply friction when on ground
            Vector3 velocity = rb.velocity;
            velocity.y = 0; // Don't affect vertical velocity
            
            if (velocity.magnitude > 0.01f)
            {
                Vector3 frictionForce = -velocity.normalized * (groundFriction * rb.mass * Physics.gravity.magnitude);
                rb.AddForce(frictionForce, ForceMode.Force);
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }
        
        private void OnCollisionStay(Collision collision)
        {
            // Handle continuous collisions (like rolling on ground)
            if (isGrounded && collision.gameObject.layer == LayerMask.NameToLayer("Default"))
            {
                HandleGroundCollision(collision);
            }
        }
        
        private void HandleCollision(Collision collision)
        {
            // Prevent multiple collision events in quick succession
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;
            
            lastCollisionTime = Time.time;
            
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
            
            // Get collision info
            Vector3 contactPoint = collision.contacts[0].point;
            Vector3 normal = collision.contacts[0].normal;
            Vector3 relativeVelocity = collision.relativeVelocity;
            float relativeSpeed = relativeVelocity.magnitude;
            
            // Check if this is a "hit" (collision with moving object)
            bool isHit = IsMovingObjectHit(collision, relativeSpeed);
            
            if (isHit)
            {
                HandleHit(collision, relativeVelocity, normal);
            }
            else
            {
                HandleBounce(collision, relativeVelocity, normal);
            }
            
            // Play sound if velocity is high enough
            if (relativeSpeed > minSoundVelocity && audioSource != null && hitSoundClip != null)
            {
                PlayHitSound(relativeSpeed);
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
                    other.transform.parent != null && other.transform.parent.name.Contains("POVArms"))
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
            // Check if the other object is moving (like an arm or another ball)
            Rigidbody otherRb = collision.rigidbody;
            
            if (otherRb != null && !otherRb.isKinematic)
            {
                // If other object has significant velocity, it's a hit
                if (otherRb.velocity.magnitude > minHitVelocity)
                {
                    return true;
                }
            }
            
            // Also check if relative speed is high (ball moving fast into static object)
            if (relativeSpeed > minHitVelocity * 2f)
            {
                return true;
            }
            
            return false;
        }
        
        private void HandleHit(Collision collision, Vector3 relativeVelocity, Vector3 normal)
        {
            // Calculate hit force based on relative velocity
            float hitSpeed = relativeVelocity.magnitude;
            Vector3 hitDirection = relativeVelocity.normalized;
            
            // Reflect the hit direction off the surface normal
            Vector3 reflectedDirection = Vector3.Reflect(hitDirection, normal);
            
            // Apply force (clamped to prevent excessive speed)
            float forceMagnitude = hitSpeed * hitForceMultiplier;
            // Clamp force to prevent extreme speeds (max 20 units of force)
            forceMagnitude = Mathf.Min(forceMagnitude, 20f);
            Vector3 force = reflectedDirection * forceMagnitude;
            rb.AddForce(force, ForceMode.Impulse);
        }
        
        private void HandleBounce(Collision collision, Vector3 relativeVelocity, Vector3 normal)
        {
            // Standard bounce physics
            float bounceSpeed = Vector3.Dot(relativeVelocity, -normal);
            
            if (bounceSpeed > minBounceVelocity)
            {
                // Calculate bounce velocity
                Vector3 bounceVelocity = normal * (bounceSpeed * bounciness);
                
                // Apply bounce
                Vector3 currentVelocity = rb.velocity;
                currentVelocity += bounceVelocity;
                rb.velocity = currentVelocity;
            }
        }
        
        private void HandleGroundCollision(Collision collision)
        {
            // Additional ground-specific handling (rolling, etc.)
            // The PhysicMaterial handles most of this, but we can add custom behavior here
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
            Debug.Log("[VolleyballV2] Ball grabbed - arm collisions disabled");
        }
        
        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            lastReleaseTime = Time.time;
            Debug.Log("[VolleyballV2] Ball released - arm collisions will re-enable after cooldown");
        }
        
        // Public methods for external control
        public bool IsGrounded()
        {
            return isGrounded;
        }
        
        public bool IsGrabbed()
        {
            return isGrabbed;
        }
    }
}

