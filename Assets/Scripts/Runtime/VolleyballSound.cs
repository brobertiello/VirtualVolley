using UnityEngine;
using System.Collections.Generic;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Plays varied sound effects for all collisions with the volleyball.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VolleyballSound : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("Sound clips to play on collision (will be randomly selected)")]
        [SerializeField] private AudioClip[] hitSoundClips;
        
        [Tooltip("Base volume for hit sounds")]
        [Range(0f, 1f)]
        [SerializeField] private float baseVolume = 0.7f;
        
        [Tooltip("Volume variation range (randomly adjusts volume by this amount)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float volumeVariation = 0.2f;
        
        [Tooltip("Pitch variation range (randomly adjusts pitch by this amount)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pitchVariation = 0.2f;
        
        [Tooltip("Minimum collision velocity to trigger sound")]
        [SerializeField] private float minVelocityForSound = 0.3f;
        
        [Tooltip("Volume scales with collision velocity up to this multiplier")]
        [SerializeField] private float maxVelocityVolumeMultiplier = 2.0f;
        
        [Tooltip("Minimum time between sounds (prevents too many overlapping sounds)")]
        [SerializeField] private float minTimeBetweenSounds = 0.05f;
        
        [Header("3D Audio Settings")]
        [Tooltip("Enable 3D spatial audio")]
        [SerializeField] private bool use3DAudio = true;
        
        [Tooltip("Minimum distance for 3D audio")]
        [SerializeField] private float minDistance = 1f;
        
        [Tooltip("Maximum distance for 3D audio")]
        [SerializeField] private float maxDistance = 50f;
        
        private AudioSource audioSource;
        private VolleyballPhysics volleyballPhysics;
        private float lastSoundTime = 0f;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            volleyballPhysics = GetComponent<VolleyballPhysics>();
            
            // Configure audio source
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = use3DAudio ? 1f : 0f; // 1 = 3D, 0 = 2D
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        
        /// <summary>
        /// Called by VolleyballPhysics when a collision occurs.
        /// </summary>
        public void OnBallCollision(Collision collision, float relativeSpeed)
        {
            // Check minimum velocity threshold
            if (relativeSpeed < minVelocityForSound)
            {
                return;
            }
            
            // Check minimum time between sounds
            if (Time.time - lastSoundTime < minTimeBetweenSounds)
            {
                return;
            }
            
            // Select random clip
            if (hitSoundClips == null || hitSoundClips.Length == 0)
            {
                Debug.LogWarning("[VolleyballSound] No sound clips assigned!");
                return;
            }
            
            AudioClip clipToPlay = hitSoundClips[Random.Range(0, hitSoundClips.Length)];
            if (clipToPlay == null)
            {
                return;
            }
            
            // Calculate volume based on collision speed
            // Map speed to volume: 0 m/s = 0.2x volume, 10 m/s = maxVolumeMultiplier
            float normalizedSpeed = Mathf.Clamp01(relativeSpeed / 10f); // Normalize to 0-1 based on 10 m/s max
            float speedVolumeMultiplier = Mathf.Lerp(0.2f, maxVelocityVolumeMultiplier, normalizedSpeed);
            float volume = baseVolume * speedVolumeMultiplier;
            
            // Add random volume variation
            volume += Random.Range(-volumeVariation, volumeVariation);
            volume = Mathf.Clamp01(volume);
            
            // Calculate pitch variation
            float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            
            // Play the sound
            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.PlayOneShot(clipToPlay);
            
            lastSoundTime = Time.time;
            
            Debug.Log($"[VolleyballSound] Playing sound | Speed: {relativeSpeed:F2} m/s | Volume: {volume:F2} | Pitch: {pitch:F2}");
        }
        
        /// <summary>
        /// Sets the sound clips to use.
        /// </summary>
        public void SetSoundClips(AudioClip[] clips)
        {
            hitSoundClips = clips;
        }
        
        /// <summary>
        /// Adds a sound clip to the array.
        /// </summary>
        public void AddSoundClip(AudioClip clip)
        {
            if (clip == null) return;
            
            List<AudioClip> clipsList = new List<AudioClip>();
            if (hitSoundClips != null)
            {
                clipsList.AddRange(hitSoundClips);
            }
            clipsList.Add(clip);
            hitSoundClips = clipsList.ToArray();
        }
    }
}

