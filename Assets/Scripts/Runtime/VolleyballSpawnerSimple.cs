using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using System.Collections.Generic;
using System.Linq;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Simple volleyball spawner that spawns balls on left action button press.
    /// </summary>
    public class VolleyballSpawnerSimple : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Reference to the volleyball prefab or scene object to clone")]
        [SerializeField] private GameObject volleyballPrefab;
        
        [Tooltip("Maximum number of volleyballs allowed in the scene")]
        [SerializeField] private int maxVolleyballs = 10;
        
        [Tooltip("Y position threshold - volleyballs below this will be deleted")]
        [SerializeField] private float deleteBelowY = -10f;
        
        [Tooltip("Spawn position offset from hand (local space)")]
        [SerializeField] private Vector3 spawnOffsetFromHand = new Vector3(0f, 0.1f, 0f);
        
        [Tooltip("Spawn velocity (how fast the ball moves when spawned)")]
        [SerializeField] private Vector3 spawnVelocity = Vector3.zero;
        
        [Header("Hand Tracking")]
        [Tooltip("Offset from controller position to spawn ball")]
        [SerializeField] private Vector3 controllerSpawnOffset = new Vector3(0f, 0f, 0.1f);
        
        [Header("Input Settings")]
        [Tooltip("Input Action Reference for left controller action button (to spawn ball)")]
        [SerializeField] private InputActionReference leftActionButtonInput;
        
        // Tracking
        private List<GameObject> spawnedVolleyballs = new List<GameObject>();
        private bool wasButtonPressed = false;
        private XROrigin xrOrigin;
        private Transform leftController;
        
        private void Awake()
        {
            // Find XR Origin
            xrOrigin = FindObjectOfType<XROrigin>();
            
            // Find volleyball prefab if not assigned
            if (volleyballPrefab == null)
            {
                GameObject existing = GameObject.Find("volleyball");
                if (existing == null)
                {
                    existing = GameObject.Find("Volleyball");
                }
                if (existing != null)
                {
                    volleyballPrefab = existing;
                }
            }
            
            // Enable input action if assigned
            if (leftActionButtonInput != null)
            {
                leftActionButtonInput.action.Enable();
            }
        }
        
        private void Update()
        {
            // Find left controller if not found
            if (leftController == null && xrOrigin != null)
            {
                leftController = FindController(xrOrigin.transform, "Left");
            }
            
            // Check for button press
            CheckLeftActionButton();
            
            // Clean up volleyballs that fell below threshold
            CleanupFallenVolleyballs();
        }
        
        private Transform FindController(Transform parent, string side)
        {
            // Look for controller in XR Origin hierarchy
            Transform[] children = parent.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                string name = child.name.ToLower();
                if (name.Contains(side.ToLower()) && name.Contains("controller"))
                {
                    return child;
                }
            }
            return null;
        }
        
        private void CheckLeftActionButton()
        {
            if (leftActionButtonInput == null || leftActionButtonInput.action == null)
                return;
            
            bool isPressed = leftActionButtonInput.action.IsPressed();
            
            // Detect button press (was not pressed, now is pressed)
            if (isPressed && !wasButtonPressed)
            {
                SpawnVolleyball();
            }
            
            wasButtonPressed = isPressed;
        }
        
        private void SpawnVolleyball()
        {
            if (volleyballPrefab == null)
            {
                Debug.LogWarning("[VolleyballSpawnerSimple] Volleyball prefab not found! Cannot spawn.");
                return;
            }
            
            // Delete oldest if we're at max
            if (spawnedVolleyballs.Count >= maxVolleyballs)
            {
                GameObject oldest = spawnedVolleyballs[0];
                if (oldest != null)
                {
                    spawnedVolleyballs.RemoveAt(0);
                    Object.Destroy(oldest);
                    Debug.Log($"[VolleyballSpawnerSimple] Deleted oldest volleyball (over limit of {maxVolleyballs})");
                }
            }
            
            // Calculate spawn position (on left hand/controller)
            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;
            
            if (leftController != null)
            {
                // Spawn at controller position with offset
                spawnPos = leftController.position + leftController.TransformDirection(controllerSpawnOffset);
                spawnRot = leftController.rotation;
            }
            else
            {
                // Fallback: spawn in front of camera
                if (xrOrigin != null && xrOrigin.Camera != null)
                {
                    Transform cam = xrOrigin.Camera.transform;
                    spawnPos = cam.position + cam.forward * 0.5f + cam.up * 0.1f;
                    spawnRot = cam.rotation;
                }
                else
                {
                    // Last resort: use default position
                    spawnPos = new Vector3(0f, 1.5f, 0f);
                }
            }
            
            // Spawn new volleyball
            GameObject newBall = Instantiate(volleyballPrefab, spawnPos, spawnRot);
            newBall.name = $"Volleyball_{spawnedVolleyballs.Count + 1}";
            
            // Set up physics if needed
            Rigidbody rb = newBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = spawnVelocity;
            }
            
            // Register with arm collision manager
            RegisterVolleyballWithArms(newBall);
            
            // Add to tracking list
            spawnedVolleyballs.Add(newBall);
            
            Debug.Log($"[VolleyballSpawnerSimple] Spawned volleyball at {spawnPos}. Total: {spawnedVolleyballs.Count}");
        }
        
        private void RegisterVolleyballWithArms(GameObject volleyball)
        {
            // Find or create the collision manager using reflection to avoid compilation order issues
            GameObject managerObj = GameObject.Find("Volleyball Arm Collision Manager");
            if (managerObj == null)
            {
                managerObj = new GameObject("Volleyball Arm Collision Manager");
                // Add component using reflection
                System.Type managerType = System.Type.GetType("VirtualVolley.Core.Scripts.Runtime.VolleyballArmCollisionManager, Assembly-CSharp");
                if (managerType != null)
                {
                    managerObj.AddComponent(managerType);
                    Debug.Log("[VolleyballSpawnerSimple] Created VolleyballArmCollisionManager");
                }
                else
                {
                    Debug.LogWarning("[VolleyballSpawnerSimple] Could not find VolleyballArmCollisionManager type. Please run Setup Volleyball Arm Collision Manager first.");
                    return;
                }
            }
            
            // Get the manager component and call RegisterVolleyball using reflection
            MonoBehaviour manager = managerObj.GetComponent("VolleyballArmCollisionManager") as MonoBehaviour;
            if (manager != null)
            {
                var registerMethod = manager.GetType().GetMethod("RegisterVolleyball");
                if (registerMethod != null)
                {
                    registerMethod.Invoke(manager, new object[] { volleyball });
                }
            }
        }
        
        private void CleanupFallenVolleyballs()
        {
            GameObject managerObj = GameObject.Find("Volleyball Arm Collision Manager");
            MonoBehaviour manager = managerObj != null ? managerObj.GetComponent("VolleyballArmCollisionManager") as MonoBehaviour : null;
            
            for (int i = spawnedVolleyballs.Count - 1; i >= 0; i--)
            {
                GameObject vb = spawnedVolleyballs[i];
                
                if (vb == null)
                {
                    // Already destroyed, remove from list
                    spawnedVolleyballs.RemoveAt(i);
                    continue;
                }
                
                if (vb.transform.position.y < deleteBelowY)
                {
                    // Unregister from collision manager before destroying
                    if (manager != null)
                    {
                        var unregisterMethod = manager.GetType().GetMethod("UnregisterVolleyball");
                        if (unregisterMethod != null)
                        {
                            unregisterMethod.Invoke(manager, new object[] { vb });
                        }
                    }
                    
                    spawnedVolleyballs.RemoveAt(i);
                    Object.Destroy(vb);
                    Debug.Log($"[VolleyballSpawnerSimple] Deleted volleyball (fell below {deleteBelowY})");
                }
            }
        }
        
        private void OnDestroy()
        {
            // Disable input action
            if (leftActionButtonInput != null && leftActionButtonInput.action != null)
            {
                leftActionButtonInput.action.Disable();
            }
        }
    }
}

