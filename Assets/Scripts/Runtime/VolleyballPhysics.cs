using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Handles all physics interactions for the volleyball, including bounces from ground, objects, and moving objects.
    /// Applies force based on collision direction and relative velocity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VolleyballPhysics : MonoBehaviour
    {
        [Header("Physics Settings")]
        [Tooltip("Base bounciness multiplier for all collisions")]
        [SerializeField] private float baseBounciness = 0.8f;
        
        [Tooltip("Force multiplier for collisions with moving objects (arms, players, etc.)")]
        [SerializeField] private float movingObjectForceMultiplier = 5.0f;
        
        [Tooltip("Force multiplier for collisions with static objects (ground, walls, etc.)")]
        [SerializeField] private float staticObjectForceMultiplier = 1.5f;
        
        [Tooltip("Minimum relative velocity to apply force (prevents tiny forces from slow collisions)")]
        [SerializeField] private float minVelocityThreshold = 0.05f;
        
        [Tooltip("Maximum force that can be applied in a single collision")]
        [SerializeField] private float maxForce = 100f;
        
        [Tooltip("Additional force multiplier based on collision speed (makes fast hits much stronger)")]
        [SerializeField] private float speedBasedForceMultiplier = 2.0f;
        
        [Header("Collision Detection")]
        [Tooltip("Tags that identify moving objects (arms, players, etc.)")]
        [SerializeField] private string[] movingObjectTags = { "Player", "Arm", "Hand" };
        
        [Tooltip("Tags that identify static objects (ground, walls, etc.)")]
        [SerializeField] private string[] staticObjectTags = { "Ground", "Floor", "Wall", "CourtFloor" };
        
        [Header("Audio Settings")]
        [Tooltip("Audio source for collision sounds")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Audio clip for ball hit sound")]
        [SerializeField] private AudioClip hitSoundClip;
        
        [Tooltip("Minimum impact force to play sound (prevents tiny sounds from very light touches)")]
        [SerializeField] private float minImpactForceForSound = 0.01f;
        
        [Tooltip("Maximum impact force for volume scaling (impacts above this will be at max volume)")]
        [SerializeField] private float maxImpactForceForVolume = 15f;
        
        [Tooltip("Minimum volume (for very light impacts)")]
        [Range(0f, 1f)]
        [SerializeField] private float minVolume = 0.1f;
        
        [Tooltip("Maximum volume (for hard impacts)")]
        [Range(0f, 1f)]
        [SerializeField] private float maxVolume = 1.0f;
        
        private Rigidbody rb;
        private Collider ballCollider;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();
            
            // Ensure rigidbody is set up correctly
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            rb.useGravity = true;
            rb.drag = 0.5f; // Air resistance
            rb.angularDrag = 0.5f;
            
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
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length == 0) return;
            
            ContactPoint contact = collision.contacts[0];
            GameObject other = collision.gameObject;
            
            // Get the other object's rigidbody (if it exists)
            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            bool isMovingObject = IsMovingObject(other, otherRb);
            
            // Calculate relative velocity
            Vector3 ballVelocity = rb.velocity;
            Vector3 otherVelocity = otherRb != null && !otherRb.isKinematic ? otherRb.velocity : Vector3.zero;
            Vector3 relativeVelocity = ballVelocity - otherVelocity;
            
            // Calculate collision normal (direction from contact point, pointing away from surface)
            Vector3 normal = contact.normal;
            
            // Project relative velocity onto the normal to get the collision force magnitude
            float relativeSpeed = Vector3.Dot(relativeVelocity, -normal);
            
            // Play sound on EVERY collision (based on impact force)
            // Impact force is the component of velocity towards the collision surface
            float impactForce = relativeSpeed;
            
            // For volleyball-to-volleyball collisions, use impact force for sound
            if (other.name.ToLower().Contains("volleyball") && other != gameObject)
            {
                PlayCollisionSound(impactForce);
                return;
            }
            
            // Play sound for all collisions (even small ones)
            PlayCollisionSound(impactForce);
            
            // Only apply force if there's significant relative velocity
            if (relativeSpeed < minVelocityThreshold)
            {
                return;
            }
            
            // Calculate force based on collision type
            float forceMultiplier = isMovingObject ? movingObjectForceMultiplier : staticObjectForceMultiplier;
            
            // For moving objects, also consider their velocity toward the ball
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
            
            // Calculate force magnitude with speed-based scaling
            // Faster collisions get exponentially stronger
            float speedMultiplier = 1f + (totalEffectiveSpeed * speedBasedForceMultiplier);
            float forceMagnitude = totalEffectiveSpeed * forceMultiplier * speedMultiplier;
            
            // Clamp to maximum force
            forceMagnitude = Mathf.Min(forceMagnitude, maxForce);
            
            // Create force vector in the direction of the normal (away from collision surface)
            Vector3 forceVector = normal * forceMagnitude;
            
            // Apply force as impulse (instant force)
            rb.AddForce(forceVector, ForceMode.Impulse);
            
            // Debug log
            Debug.Log($"[VolleyballPhysics] Collision with {other.name} | " +
                     $"Relative speed: {relativeSpeed:F2} m/s | " +
                     $"Additional speed: {additionalSpeed:F2} m/s | " +
                     $"Total speed: {totalEffectiveSpeed:F2} m/s | " +
                     $"Force: {forceMagnitude:F2} N | " +
                     $"Impact force: {impactForce:F2} m/s | " +
                     $"Type: {(isMovingObject ? "Moving" : "Static")}");
        }
        
        private void PlayCollisionSound(float impactForce)
        {
            if (audioSource == null || hitSoundClip == null)
                return;
            
            // Only play if impact force is above minimum threshold
            if (impactForce < minImpactForceForSound)
                return;
            
            // Calculate volume based on impact force (velocity component towards collision)
            // Map impact force to volume: minImpactForce = minVolume, maxImpactForce = maxVolume
            float normalizedForce = Mathf.Clamp01(impactForce / maxImpactForceForVolume);
            float volume = Mathf.Lerp(minVolume, maxVolume, normalizedForce);
            
            // Randomize pitch slightly for variation
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.volume = volume;
            
            // Play the sound (PlayOneShot allows overlapping sounds)
            audioSource.PlayOneShot(hitSoundClip, volume);
        }
        
        private bool IsMovingObject(GameObject obj, Rigidbody objRb)
        {
            // Check by tag
            foreach (string tag in movingObjectTags)
            {
                if (obj.CompareTag(tag))
                {
                    return true;
                }
            }
            
            // Check by name (for arms, hands, etc.)
            string objName = obj.name.ToLower();
            if (objName.Contains("arm") || objName.Contains("hand") || objName.Contains("player"))
            {
                return true;
            }
            
            // Check if it has a non-kinematic rigidbody (it's a moving physics object)
            if (objRb != null && !objRb.isKinematic && objRb.useGravity == false)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Sets the physics material for the ball's collider.
        /// </summary>
        public void SetPhysicsMaterial(PhysicMaterial material)
        {
            if (ballCollider != null && material != null)
            {
                ballCollider.material = material;
            }
        }
    }
}

