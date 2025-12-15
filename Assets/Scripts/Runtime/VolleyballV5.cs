using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Simple volleyball physics system - bounce physics with ground, grabbable, and ball-to-ball collisions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VolleyballV5 : MonoBehaviour
    {
        [Header("Physics Properties")]
        [Tooltip("Mass of the volleyball (kg)")]
        [SerializeField] private float mass = 0.27f;
        
        [Tooltip("Air resistance (Rigidbody drag)")]
        [SerializeField] private float airDrag = 0.5f;
        
        [Tooltip("Angular drag (how fast rotation slows down)")]
        [SerializeField] private float angularDrag = 0.5f;
        
        [Header("Physics Material Properties")]
        [Tooltip("Bounciness (0 = no bounce, 1 = perfect bounce)")]
        [SerializeField] private float bounciness = 0.9f;
        
        [Tooltip("Static friction (friction when not moving)")]
        [SerializeField] private float staticFriction = 0.05f;
        
        [Tooltip("Dynamic friction (friction when moving)")]
        [SerializeField] private float dynamicFriction = 0.05f;
        
        [Header("Audio Settings")]
        [Tooltip("Audio source for collision sounds")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Audio clip for ball hit sound")]
        [SerializeField] private AudioClip hitSoundClip;
        
        [Tooltip("Maximum impact force for volume scaling (impacts above this will be at max volume)")]
        [SerializeField] private float maxImpactForceForVolume = 15f;
        
        [Tooltip("Minimum volume (for very light impacts)")]
        [Range(0f, 1f)]
        [SerializeField] private float minVolume = 0.25f;
        
        [Tooltip("Maximum volume (for hard impacts)")]
        [Range(0f, 1f)]
        [SerializeField] private float maxVolume = 1.0f;
        
        private Rigidbody rb;
        private SphereCollider sphereCollider;
        private PhysicMaterial physicsMaterial;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            
            // Configure Rigidbody
            rb.mass = mass;
            rb.drag = airDrag;
            rb.angularDrag = angularDrag;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Ensure collider radius matches visual size
            if (sphereCollider != null)
            {
                // Calculate proper collider radius to match visual mesh
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null && renderer.bounds.size.magnitude > 0)
                {
                    // Use the largest dimension of the visual bounds as diameter
                    float visualDiameter = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                    sphereCollider.radius = visualDiameter / 2f;
                }
                else
                {
                    // Fallback: check transform scale
                    // Standard volleyball model should be ~0.2m diameter when scaled properly
                    float baseRadius = 0.1f; // 10cm radius = 20cm diameter
                    float scaleFactor = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                    sphereCollider.radius = baseRadius * scaleFactor;
                }
                
                // Ensure collider is not too small (minimum 0.08m radius)
                sphereCollider.radius = Mathf.Max(0.08f, sphereCollider.radius);
            }
            
            // Create and configure PhysicMaterial for bouncing
            physicsMaterial = new PhysicMaterial("VolleyballV5Material")
            {
                bounciness = bounciness,
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
            if (sphereCollider != null)
            {
                sphereCollider.material = physicsMaterial;
            }
            
            // Ensure XRGrabInteractable exists for VR grabbing
            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            
            // Configure grab settings for both distance and direct grabbing
            grabInteractable.throwVelocityScale = 1.0f;
            grabInteractable.throwAngularVelocityScale = 1.0f;
            grabInteractable.throwSmoothingDuration = 0.1f;
            
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
                audioSource.spatialBlend = 0f; // 2D sound - no distance dampening
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 10000f; // Very large distance so no falloff
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length == 0) return;
            
            ContactPoint contact = collision.contacts[0];
            GameObject other = collision.gameObject;
            
            // Calculate impact force (velocity component towards the collision surface)
            Vector3 relativeVelocity = collision.relativeVelocity;
            Vector3 normal = contact.normal;
            float impactForce = Vector3.Dot(relativeVelocity, -normal);
            
            // Play sound on every collision (based on impact force)
            PlayCollisionSound(impactForce);
            
            // Handle collisions with other volleyballs
            if (other.name.ToLower().Contains("volleyball") && other != gameObject)
            {
                // Let Unity's PhysicMaterial handle the bounce between balls
                // No special handling needed - physics will work naturally
                return;
            }
            
            // For all other collisions (ground, walls, etc.), Unity's PhysicMaterial handles bouncing
            // No custom code needed - just let Unity's physics work
        }
        
        private void PlayCollisionSound(float impactForce)
        {
            if (audioSource == null || hitSoundClip == null)
                return;
            
            // Play sound on every collision, regardless of impact force
            // Calculate volume based on impact force (velocity component towards collision)
            // Use logarithmic scaling for more natural volume curve
            // Clamp impact force to positive values (negative means moving away, use minimum volume)
            float clampedForce = Mathf.Max(0f, impactForce);
            float normalizedForce = Mathf.Clamp01(clampedForce / maxImpactForceForVolume);
            // Use logarithmic curve for more natural sound scaling
            float logNormalized = Mathf.Log10(1f + normalizedForce * 9f) / Mathf.Log10(10f); // Maps 0-1 to 0-1 with log curve
            float volume = Mathf.Lerp(minVolume, maxVolume, logNormalized);
            
            // Randomize pitch slightly for variation
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.volume = volume;
            
            // Play the sound (PlayOneShot allows overlapping sounds)
            audioSource.PlayOneShot(hitSoundClip, volume);
        }
    }
}

