using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Manages collision between volleyballs and arms, handling grab state for all volleyballs.
    /// </summary>
    public class VolleyballArmCollisionManager : MonoBehaviour
    {
        [Header("Collision Settings")]
        [Tooltip("Enable collision with volleyball")]
        public bool enableVolleyballCollision = true;
        
        [Tooltip("Time after ball release to ignore collisions (prevents immediate re-grab)")]
        public float collisionIgnoreTimeAfterRelease = 0.3f;
        
        [Header("Arm References")]
        [Tooltip("POVArmsPrimitives component (will be found automatically if not assigned)")]
        [SerializeField] private POVArmsPrimitives armsScript;
        
        // Tracking for all volleyballs
        private Dictionary<GameObject, VolleyballGrabState> volleyballStates = new Dictionary<GameObject, VolleyballGrabState>();
        private Collider[] armColliders;
        
        private class VolleyballGrabState
        {
            public bool wasGrabbed = false;
            public float lastReleaseTime = -1f;
            public XRGrabInteractable grabInteractable;
        }
        
        private void Awake()
        {
            // Find arms script if not assigned
            if (armsScript == null)
            {
                armsScript = FindObjectOfType<POVArmsPrimitives>();
            }
            
            if (armsScript == null)
            {
                Debug.LogWarning("[VolleyballArmCollisionManager] POVArmsPrimitives not found!");
                return;
            }
            
            // Get arm colliders from arms script using reflection
            var armCollidersField = typeof(POVArmsPrimitives).GetField("armColliders", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (armCollidersField != null)
            {
                armColliders = armCollidersField.GetValue(armsScript) as Collider[];
            }
            
            if (armColliders == null || armColliders.Length == 0)
            {
                Debug.LogWarning("[VolleyballArmCollisionManager] Could not find arm colliders!");
            }
        }
        
        private void Update()
        {
            // Update collision states for all tracked volleyballs
            UpdateAllVolleyballCollisions();
        }
        
        /// <summary>
        /// Registers a volleyball to be tracked for arm collision management.
        /// </summary>
        public void RegisterVolleyball(GameObject volleyball)
        {
            if (volleyball == null || volleyballStates.ContainsKey(volleyball))
                return;
            
            XRGrabInteractable grabInteractable = volleyball.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                Debug.LogWarning($"[VolleyballArmCollisionManager] Volleyball {volleyball.name} has no XRGrabInteractable component!");
                return;
            }
            
            VolleyballGrabState state = new VolleyballGrabState
            {
                grabInteractable = grabInteractable
            };
            
            volleyballStates[volleyball] = state;
            
            // Subscribe to grab events
            grabInteractable.selectEntered.AddListener((args) => OnVolleyballGrabbed(volleyball));
            grabInteractable.selectExited.AddListener((args) => OnVolleyballReleased(volleyball));
            
            Debug.Log($"[VolleyballArmCollisionManager] Registered volleyball: {volleyball.name}");
        }
        
        /// <summary>
        /// Unregisters a volleyball (when it's destroyed).
        /// </summary>
        public void UnregisterVolleyball(GameObject volleyball)
        {
            if (volleyball != null && volleyballStates.ContainsKey(volleyball))
            {
                volleyballStates.Remove(volleyball);
            }
        }
        
        private void OnVolleyballGrabbed(GameObject volleyball)
        {
            if (volleyballStates.ContainsKey(volleyball))
            {
                volleyballStates[volleyball].wasGrabbed = true;
            }
        }
        
        private void OnVolleyballReleased(GameObject volleyball)
        {
            if (volleyballStates.ContainsKey(volleyball))
            {
                volleyballStates[volleyball].wasGrabbed = false;
                volleyballStates[volleyball].lastReleaseTime = Time.time;
            }
        }
        
        private void UpdateAllVolleyballCollisions()
        {
            if (!enableVolleyballCollision || armColliders == null || armColliders.Length == 0)
                return;
            
            // Create a list of keys to iterate over (to avoid modification during iteration)
            List<GameObject> keysToRemove = new List<GameObject>();
            
            foreach (var kvp in volleyballStates)
            {
                GameObject volleyball = kvp.Key;
                VolleyballGrabState state = kvp.Value;
                
                if (volleyball == null)
                {
                    // Volleyball was destroyed, mark for removal
                    keysToRemove.Add(volleyball);
                    continue;
                }
                
                Collider volleyballCollider = volleyball.GetComponent<Collider>();
                if (volleyballCollider == null)
                    continue;
                
                bool shouldCollide = true;
                
                // Don't collide if ball is being held
                if (state.wasGrabbed)
                {
                    shouldCollide = false;
                }
                // Don't collide if ball was recently released
                else if (state.lastReleaseTime > 0 && Time.time - state.lastReleaseTime < collisionIgnoreTimeAfterRelease)
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
            
            // Remove destroyed volleyballs
            foreach (var key in keysToRemove)
            {
                volleyballStates.Remove(key);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from all grab events
            foreach (var kvp in volleyballStates)
            {
                if (kvp.Key != null && kvp.Value.grabInteractable != null)
                {
                    kvp.Value.grabInteractable.selectEntered.RemoveAllListeners();
                    kvp.Value.grabInteractable.selectExited.RemoveAllListeners();
                }
            }
        }
    }
}

