using UnityEngine;
using System.Collections;

namespace VirtualVolley.Core.Scripts.Interactables
{
    /// <summary>
    /// Plays a sound effect when the volleyball hits the floor, with predictive timing.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VolleyballHitSound : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("The sound clip to play when hitting the floor")]
        [SerializeField] private AudioClip hitSoundClip;
        
        [Tooltip("Minimum time between sound plays (prevents rapid-fire sounds). Set to 0 to allow full overlap.")]
        [SerializeField] private float minTimeBetweenSounds = 0f;
        
        [Tooltip("Minimum collision velocity to trigger sound")]
        [SerializeField] private float minVelocityThreshold = 0.5f;
        
        [Tooltip("Volume of the hit sound")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.7f;
        
        [Tooltip("Time offset in seconds - skips the beginning of the audio clip")]
        [SerializeField] private float audioStartOffset = 0.2f;
        
        [Header("Predictive Timing")]
        [Tooltip("How many seconds before impact to play the sound")]
        [SerializeField] private float soundLeadTime = 0.2f;
        
        [Tooltip("How often to check for predicted ground impact (in seconds)")]
        [SerializeField] private float predictionCheckInterval = 0.05f;
        
        [Tooltip("Maximum distance to check for ground impact")]
        [SerializeField] private float maxPredictionDistance = 10f;
        
        [Header("Floor Detection")]
        [Tooltip("Name of the floor GameObject (leave empty to detect any collision)")]
        [SerializeField] private string floorName = "CourtFloor";
        
        [Tooltip("Layer mask for ground detection (leave as Everything to detect any layer)")]
        [SerializeField] private LayerMask groundLayerMask = -1;
        
        [Tooltip("If true, only play sound when hitting objects with 'CourtFloor' in the name")]
        [SerializeField] private bool onlyPlayOnFloor = true;

        private AudioSource audioSource;
        private float lastSoundTime;
        private Rigidbody rb;
        private bool soundScheduled = false;
        private Coroutine predictionCoroutine;
        private float scheduledSoundTime = 0f; // When the scheduled sound will play
        
        [Header("Overlapping Audio")]
        [Tooltip("Maximum number of overlapping sounds (for reverb tails)")]
        [SerializeField] private int maxOverlappingSounds = 5;
        
        private AudioSource[] audioSourcePool;
        private int currentAudioSourceIndex = 0;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Configure AudioSource
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 200f; // Increased from 50f to reduce distance falloff
            audioSource.volume = volume;
            
            // Create pool of AudioSources for overlapping sounds
            audioSourcePool = new AudioSource[maxOverlappingSounds];
            audioSourcePool[0] = audioSource; // Use the main one as first in pool
            
            for (int i = 1; i < maxOverlappingSounds; i++)
            {
                GameObject audioObj = new GameObject($"AudioSource_{i}");
                audioObj.transform.SetParent(transform);
                audioObj.transform.localPosition = Vector3.zero;
                
                AudioSource pooledSource = audioObj.AddComponent<AudioSource>();
                pooledSource.playOnAwake = false;
                pooledSource.spatialBlend = 1f; // 3D sound
                pooledSource.rolloffMode = AudioRolloffMode.Logarithmic;
                pooledSource.minDistance = 1f;
                pooledSource.maxDistance = 200f; // Increased from 50f to reduce distance falloff
                
                audioSourcePool[i] = pooledSource;
            }
            
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            // Try to find the hit sound clip if not assigned
            if (hitSoundClip == null)
            {
                hitSoundClip = Resources.Load<AudioClip>("ball-sound-effect");
                
                if (hitSoundClip != null)
                {
                    audioSource.clip = hitSoundClip;
                    Debug.Log("[VolleyballHitSound] Loaded hit sound clip from Resources");
                }
                else
                {
                    Debug.LogWarning("[VolleyballHitSound] Hit sound clip not found. Please assign 'Assets/Imports/ball-sound-effect.wav' in the Inspector.");
                }
            }
            else
            {
                audioSource.clip = hitSoundClip;
            }
        }

        private void OnEnable()
        {
            // Start predictive collision detection
            if (predictionCoroutine == null)
            {
                predictionCoroutine = StartCoroutine(PredictGroundImpact());
            }
        }

        private void OnDisable()
        {
            // Stop predictive collision detection
            if (predictionCoroutine != null)
            {
                StopCoroutine(predictionCoroutine);
                predictionCoroutine = null;
            }
            soundScheduled = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // If we have a scheduled sound that's about to play (within 0.1s), don't play again
            // This prevents the collision from overriding the predictive timing
            if (soundScheduled && Time.time >= scheduledSoundTime - 0.1f)
            {
                // Sound was already scheduled and is about to play, just cancel the flag
                soundScheduled = false;
                return;
            }
            
            // Only play on collision if predictive system didn't already schedule it
            // This is a fallback in case prediction failed
            if (ShouldPlaySound(collision) && !soundScheduled)
            {
                PlayHitSound(collision.relativeVelocity.magnitude);
            }
        }

        private bool ShouldPlaySound(Collision collision)
        {
            // Check minimum time between sounds
            if (Time.time - lastSoundTime < minTimeBetweenSounds)
            {
                return false;
            }

            // Check minimum velocity
            if (rb != null && collision.relativeVelocity.magnitude < minVelocityThreshold)
            {
                return false;
            }

            // Check if we should only play on floor
            if (onlyPlayOnFloor)
            {
                // Check if the collided object is the floor
                GameObject hitObject = collision.gameObject;
                if (!hitObject.name.Contains(floorName))
                {
                    // Check parent objects too
                    Transform parent = hitObject.transform.parent;
                    bool isFloor = false;
                    while (parent != null)
                    {
                        if (parent.name.Contains(floorName))
                        {
                            isFloor = true;
                            break;
                        }
                        parent = parent.parent;
                    }
                    
                    if (!isFloor)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Continuously predicts when the ball will hit the ground and schedules the sound.
        /// </summary>
        private IEnumerator PredictGroundImpact()
        {
            while (true)
            {
                yield return new WaitForSeconds(predictionCheckInterval);
                
                // Don't predict if we just played a sound
                if (Time.time - lastSoundTime < minTimeBetweenSounds)
                {
                    continue;
                }
                
                // Don't predict if sound is already scheduled
                if (soundScheduled)
                {
                    continue;
                }
                
                // Need Rigidbody to calculate trajectory
                if (rb == null || rb.isKinematic)
                {
                    continue;
                }
                
                // Only predict if moving downward
                if (rb.velocity.y >= 0)
                {
                    continue;
                }
                
                // Calculate when ball will hit ground using physics
                float timeToImpact = CalculateTimeToGroundImpact();
                
                if (timeToImpact > 0 && timeToImpact <= soundLeadTime + predictionCheckInterval)
                {
                    // Schedule sound to play at the right time
                    float delay = Mathf.Max(0f, timeToImpact - soundLeadTime);
                    scheduledSoundTime = Time.time + delay;
                    StartCoroutine(PlaySoundAfterDelay(delay, rb.velocity.magnitude));
                    soundScheduled = true;
                    
                    Debug.Log($"[VolleyballHitSound] Scheduled sound in {delay:F3}s (impact in {timeToImpact:F3}s, lead time: {soundLeadTime:F3}s)");
                }
            }
        }

        /// <summary>
        /// Calculates the time until the ball hits the ground using physics trajectory.
        /// </summary>
        private float CalculateTimeToGroundImpact()
        {
            if (rb == null) return -1f;
            
            Vector3 position = transform.position;
            Vector3 velocity = rb.velocity;
            float gravity = Physics.gravity.y;
            
            // Raycast downward to find the ground
            RaycastHit hit;
            Vector3 rayDirection = Vector3.down;
            float maxDistance = maxPredictionDistance;
            
            // Check if we should filter by layer
            int layerMask = groundLayerMask.value;
            if (onlyPlayOnFloor && !string.IsNullOrEmpty(floorName))
            {
                // Try to find CourtFloor layer
                int floorLayer = LayerMask.NameToLayer(floorName);
                if (floorLayer == -1)
                {
                    // If layer doesn't exist, use the layer mask as-is
                    layerMask = groundLayerMask.value;
                }
                else
                {
                    // Only check the floor layer
                    layerMask = 1 << floorLayer;
                }
            }
            
            if (Physics.Raycast(position, rayDirection, out hit, maxDistance, layerMask))
            {
                // Check if it's the floor (if filtering enabled)
                if (onlyPlayOnFloor && !string.IsNullOrEmpty(floorName))
                {
                    if (!hit.collider.gameObject.name.Contains(floorName))
                    {
                        // Check parent
                        Transform parent = hit.collider.transform.parent;
                        bool isFloor = false;
                        while (parent != null)
                        {
                            if (parent.name.Contains(floorName))
                            {
                                isFloor = true;
                                break;
                            }
                            parent = parent.parent;
                        }
                        if (!isFloor)
                        {
                            return -1f;
                        }
                    }
                }
                
                // Calculate time to impact using physics
                // Position equation: y = y0 + v0*t + 0.5*a*t^2
                // We want: hit.point.y = position.y + velocity.y*t + 0.5*gravity*t^2
                float distanceToGround = position.y - hit.point.y;
                
                if (distanceToGround <= 0)
                {
                    return 0f; // Already on or below ground
                }
                
                // Solve quadratic equation: 0.5*gravity*t^2 + velocity.y*t - distanceToGround = 0
                float a = 0.5f * gravity;
                float b = velocity.y;
                float c = -distanceToGround;
                
                float discriminant = b * b - 4 * a * c;
                
                if (discriminant < 0)
                {
                    return -1f; // No real solution (ball won't hit)
                }
                
                float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                
                // Return the positive, smaller time
                float timeToImpact = Mathf.Max(t1, t2);
                if (t1 > 0 && t2 > 0)
                {
                    timeToImpact = Mathf.Min(t1, t2);
                }
                else if (t1 > 0)
                {
                    timeToImpact = t1;
                }
                else if (t2 > 0)
                {
                    timeToImpact = t2;
                }
                else
                {
                    return -1f; // No positive solution
                }
                
                return timeToImpact;
            }
            
            return -1f; // No ground found
        }

        /// <summary>
        /// Plays the sound after a delay.
        /// </summary>
        private IEnumerator PlaySoundAfterDelay(float delay, float predictedVelocity)
        {
            yield return new WaitForSeconds(delay);
            
            // Check if we should still play (might have been cancelled by collision)
            if (soundScheduled && rb != null)
            {
                // Use current velocity at impact time (more accurate)
                float actualVelocity = rb.velocity.magnitude;
                PlayHitSound(actualVelocity);
                soundScheduled = false;
                scheduledSoundTime = 0f;
            }
        }

        private void PlayHitSound(float impactVelocity)
        {
            if (hitSoundClip == null || audioSourcePool == null || audioSourcePool.Length == 0)
                return;
            
            // Adjust volume based on impact velocity (louder hits = louder sound)
            // Use a wider range - typical ball speeds are 1-15 m/s
            float velocityFactor = Mathf.Clamp01(impactVelocity / 15f); // Normalize to 0-1 range based on max expected speed
            
            // Increased range: quiet bounces (0.15-0.2 volume) to very loud bounces (1.5x base volume, clamped to 1.0)
            // Use exponential curve for more dramatic difference
            float volumeMultiplier = Mathf.Pow(velocityFactor, 0.6f); // Slightly steeper curve for more dramatic difference
            float adjustedVolume = volume * (0.15f + 1.35f * volumeMultiplier); // Range from 15% to 150% of base volume
            
            // Ensure volume is within valid range (allow up to 1.0 for very fast balls)
            adjustedVolume = Mathf.Clamp(adjustedVolume, 0.1f, 1f); // Minimum 10% to avoid silence, max 100%
            
            // Get next available AudioSource from pool (round-robin for overlapping sounds)
            AudioSource sourceToUse = audioSourcePool[currentAudioSourceIndex];
            currentAudioSourceIndex = (currentAudioSourceIndex + 1) % audioSourcePool.Length;
            
            // Configure and play
            sourceToUse.clip = hitSoundClip;
            sourceToUse.volume = adjustedVolume;
            
            // Play the clip
            sourceToUse.Play();
            
            // Set the time offset after Play() has started
            StartCoroutine(SetAudioTimeAfterPlay(sourceToUse, audioStartOffset));
            
            lastSoundTime = Time.time;
            
            Debug.Log($"[VolleyballHitSound] Played hit sound (velocity: {impactVelocity:F2} m/s, volume: {adjustedVolume:F3}, offset: {audioStartOffset:F2}s)");
        }

        /// <summary>
        /// Sets the audio time after Play() has been called (ensures offset works correctly).
        /// </summary>
        private IEnumerator SetAudioTimeAfterPlay(AudioSource source, float offset)
        {
            // Wait a frame to ensure Play() has started
            yield return null;
            
            if (source != null && source.isPlaying && hitSoundClip != null)
            {
                float startTime = Mathf.Clamp(offset, 0f, hitSoundClip.length - 0.1f);
                source.time = startTime;
            }
        }

        /// <summary>
        /// Manually set the hit sound clip (useful for runtime assignment).
        /// </summary>
        public void SetHitSoundClip(AudioClip clip)
        {
            hitSoundClip = clip;
            if (audioSource != null)
            {
                audioSource.clip = clip;
            }
        }
    }
}

