using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Manages all ball launchers and handles input to trigger random launcher shots.
    /// </summary>
    public class BallLauncherManager : MonoBehaviour
    {
        [Header("Launchers")]
        [Tooltip("Service line launchers (for ServeReceive scene)")]
        [SerializeField] private BallLauncher[] serviceLineLaunchers;
        
        [Tooltip("Net launchers (for SpikeReceive scene)")]
        [SerializeField] private BallLauncher[] netLaunchers;
        
        [Tooltip("Opponent court launcher (for FreeBalls scene)")]
        [SerializeField] private BallLauncher[] opponentCourtLaunchers;
        
        [Header("Input Settings")]
        [Tooltip("Input action reference for left controller X button")]
        [SerializeField] private InputActionReference xButtonInput;
        
        [Tooltip("Target transform (usually the camera/head)")]
        [SerializeField] private Transform targetTransform;
        
        [Header("Ball Spawning (Free Play)")]
        [Tooltip("Volleyball prefab to spawn in Free Play")]
        [SerializeField] private GameObject volleyballPrefab;
        
        [Tooltip("Spawn offset from controller")]
        [SerializeField] private Vector3 controllerSpawnOffset = new Vector3(0, 0, 0.1f);
        
        [Tooltip("Maximum number of volleyballs")]
        [SerializeField] private int maxVolleyballs = 10;
        
        [Header("Court Settings")]
        [Tooltip("Court width (X axis)")]
        [SerializeField] private float courtWidth = 9f;
        
        [Tooltip("Court length (Z axis)")]
        [SerializeField] private float courtLength = 18f;
        
        [Tooltip("Court floor height")]
        [SerializeField] private float courtFloorHeight = 0f;
        
        private XROrigin xrOrigin;
        private Transform leftController;
        private bool isPressed = false;
        private System.Collections.Generic.List<GameObject> spawnedVolleyballs = new System.Collections.Generic.List<GameObject>();
        
        private void Awake()
        {
            // Find XR Origin
            xrOrigin = FindObjectOfType<XROrigin>();
            
            // Find target (camera/head)
            if (targetTransform == null && xrOrigin != null)
            {
                targetTransform = xrOrigin.Camera?.transform;
            }
            
            // Find left controller
            if (xrOrigin != null)
            {
                leftController = FindController(xrOrigin.transform, "Left");
            }
            
            // Find volleyball prefab if not assigned
            if (volleyballPrefab == null)
            {
                volleyballPrefab = GameObject.Find("VolleyballV5");
            }
            
            // Find all launchers if not assigned
            OrganizeLaunchers();
            
            // Set up input action
            if (xButtonInput != null)
            {
                xButtonInput.action.Enable();
            }
            
            // Subscribe to scene changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSceneChanged += OnSceneChanged;
                // Set initial visibility
                UpdateLauncherVisibility(GameManager.Instance.CurrentScene);
            }
        }
        
        private void Start()
        {
            // Ensure initial visibility is set
            if (GameManager.Instance != null)
            {
                UpdateLauncherVisibility(GameManager.Instance.CurrentScene);
            }
            
            // Re-enable input action in case it was disabled
            if (xButtonInput != null && xButtonInput.action != null)
            {
                if (!xButtonInput.action.enabled)
                {
                    xButtonInput.action.Enable();
                }
            }
        }
        
        private void OnEnable()
        {
            // Re-subscribe to input events when component is enabled
            if (xButtonInput != null && xButtonInput.action != null)
            {
                xButtonInput.action.performed += OnXButtonPressed;
                xButtonInput.action.canceled += OnXButtonReleased;
                if (!xButtonInput.action.enabled)
                {
                    xButtonInput.action.Enable();
                }
            }
            
            // Re-subscribe to scene changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSceneChanged += OnSceneChanged;
            }
        }
        
        private void OnDisable()
        {
            if (xButtonInput != null && xButtonInput.action != null)
            {
                xButtonInput.action.performed -= OnXButtonPressed;
                xButtonInput.action.canceled -= OnXButtonReleased;
            }
            
            // Unsubscribe from scene changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSceneChanged -= OnSceneChanged;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSceneChanged -= OnSceneChanged;
            }
        }
        
        private void OnSceneChanged(GameManager.Scene newScene)
        {
            // Update launcher dot visibility based on scene
            UpdateLauncherVisibility(newScene);
        }
        
        private void UpdateLauncherVisibility(GameManager.Scene scene)
        {
            // Show dots only for the active scene's launchers
            bool showServiceLine = (scene == GameManager.Scene.ServeReceive);
            bool showNet = (scene == GameManager.Scene.SpikeReceive);
            bool showOpponent = (scene == GameManager.Scene.FreeBalls);
            
            if (serviceLineLaunchers != null)
            {
                foreach (var launcher in serviceLineLaunchers)
                {
                    if (launcher != null)
                    {
                        launcher.SetDotVisibility(showServiceLine);
                    }
                }
            }
            
            if (netLaunchers != null)
            {
                foreach (var launcher in netLaunchers)
                {
                    if (launcher != null)
                    {
                        launcher.SetDotVisibility(showNet);
                    }
                }
            }
            
            if (opponentCourtLaunchers != null)
            {
                foreach (var launcher in opponentCourtLaunchers)
                {
                    if (launcher != null)
                    {
                        launcher.SetDotVisibility(showOpponent);
                    }
                }
            }
        }
        
        private void Update()
        {
            // Find left controller if not found
            if (leftController == null && xrOrigin != null)
            {
                leftController = FindController(xrOrigin.transform, "Left");
            }
            
            // Clean up fallen volleyballs
            CleanupFallenVolleyballs();
        }
        
        private Transform FindController(Transform parent, string side)
        {
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
        
        private void OrganizeLaunchers()
        {
            // Find all launchers in scene
            BallLauncher[] allLaunchers = FindObjectsOfType<BallLauncher>();
            
            if (allLaunchers == null || allLaunchers.Length == 0) return;
            
            System.Collections.Generic.List<BallLauncher> serviceLine = new System.Collections.Generic.List<BallLauncher>();
            System.Collections.Generic.List<BallLauncher> net = new System.Collections.Generic.List<BallLauncher>();
            System.Collections.Generic.List<BallLauncher> opponent = new System.Collections.Generic.List<BallLauncher>();
            
            foreach (var launcher in allLaunchers)
            {
                if (launcher == null) continue;
                
                string name = launcher.name.ToLower();
                if (name.Contains("service"))
                {
                    serviceLine.Add(launcher);
                }
                else if (name.Contains("net"))
                {
                    net.Add(launcher);
                }
                else if (name.Contains("opponent"))
                {
                    opponent.Add(launcher);
                }
            }
            
            if (serviceLineLaunchers == null || serviceLineLaunchers.Length == 0)
                serviceLineLaunchers = serviceLine.ToArray();
            if (netLaunchers == null || netLaunchers.Length == 0)
                netLaunchers = net.ToArray();
            if (opponentCourtLaunchers == null || opponentCourtLaunchers.Length == 0)
                opponentCourtLaunchers = opponent.ToArray();
        }
        
        private void OnXButtonPressed(InputAction.CallbackContext context)
        {
            if (!isPressed && targetTransform != null)
            {
                isPressed = true;
                HandleXButtonPress();
            }
        }
        
        private void OnXButtonReleased(InputAction.CallbackContext context)
        {
            isPressed = false;
        }
        
        private void HandleXButtonPress()
        {
            // Check current scene from GameManager
            if (GameManager.Instance == null)
            {
                return;
            }
            
            GameManager.Scene currentScene = GameManager.Instance.CurrentScene;
            
            if (currentScene == GameManager.Scene.FreePlay)
            {
                // Spawn ball at hand position
                SpawnVolleyball();
            }
            else if (currentScene == GameManager.Scene.FreeBalls)
            {
                // Free Balls: toss to random spot on proper side of court
                BallLauncher[] launcherGroup = GetLauncherGroupForScene(currentScene);
                if (launcherGroup != null && launcherGroup.Length > 0)
                {
                    ShootRandomLauncherToRandomCourtPosition(launcherGroup);
                }
            }
            else
            {
                // Select launcher group based on scene and shoot
                BallLauncher[] launcherGroup = GetLauncherGroupForScene(currentScene);
                if (launcherGroup != null && launcherGroup.Length > 0)
                {
                    ShootRandomLauncherFromGroup(launcherGroup);
                }
            }
        }
        
        /// <summary>
        /// Shoots a random launcher to a random position on the proper side of the court (Free Balls).
        /// </summary>
        private void ShootRandomLauncherToRandomCourtPosition(BallLauncher[] launcherGroup)
        {
            if (launcherGroup == null || launcherGroup.Length == 0 || targetTransform == null)
                return;
            
            // Get available launchers
            System.Collections.Generic.List<BallLauncher> availableLaunchers = new System.Collections.Generic.List<BallLauncher>();
            foreach (var launcher in launcherGroup)
            {
                if (launcher != null && !launcher.IsBusy)
                {
                    availableLaunchers.Add(launcher);
                }
            }
            
            if (availableLaunchers.Count == 0)
                return;
            
            // Pick random launcher
            int randomIndex = Random.Range(0, availableLaunchers.Count);
            BallLauncher selectedLauncher = availableLaunchers[randomIndex];
            
            // Calculate random target position on proper side of court (negative Z = user's side)
            Vector3 userPosition = targetTransform.position;
            
            // Random position on user's side of court (negative Z)
            float randomX = Random.Range(-courtWidth / 2 + 1f, courtWidth / 2 - 1f);
            float randomZ = Random.Range(-courtLength / 2, -2f); // User's side, not too close to net
            float randomY = courtFloorHeight + 1.5f; // Eye level
            
            Vector3 targetPosition = new Vector3(randomX, randomY, randomZ);
            
            // Adjust to be within reasonable distance from user (3-6 meters)
            float distanceToUser = Vector3.Distance(targetPosition, userPosition);
            if (distanceToUser < 3f || distanceToUser > 6f)
            {
                Vector3 directionFromUser = (targetPosition - userPosition).normalized;
                float adjustedDistance = Random.Range(3f, 6f);
                targetPosition = userPosition + directionFromUser * adjustedDistance;
                targetPosition.y = courtFloorHeight + 1.5f; // Keep at eye level
            }
            
            // Shoot at target
            selectedLauncher.ShootAtTarget(targetPosition);
        }
        
        /// <summary>
        /// Shoots a random launcher from the group at the target (player).
        /// </summary>
        private void ShootRandomLauncherFromGroup(BallLauncher[] launcherGroup)
        {
            if (launcherGroup == null || launcherGroup.Length == 0 || targetTransform == null)
                return;
            
            // Get available launchers
            System.Collections.Generic.List<BallLauncher> availableLaunchers = new System.Collections.Generic.List<BallLauncher>();
            foreach (var launcher in launcherGroup)
            {
                if (launcher != null && !launcher.IsBusy)
                {
                    availableLaunchers.Add(launcher);
                }
            }
            
            if (availableLaunchers.Count == 0)
                return;
            
            // Pick random launcher
            int randomIndex = Random.Range(0, availableLaunchers.Count);
            BallLauncher selectedLauncher = availableLaunchers[randomIndex];
            
            // Shoot at target
            Vector3 targetPosition = targetTransform.position;
            selectedLauncher.ShootAtTarget(targetPosition);
        }
        
        private BallLauncher[] GetLauncherGroupForScene(GameManager.Scene scene)
        {
            switch (scene)
            {
                case GameManager.Scene.ServeReceive:
                    return serviceLineLaunchers;
                case GameManager.Scene.SpikeReceive:
                    return netLaunchers;
                case GameManager.Scene.FreeBalls:
                    return opponentCourtLaunchers;
                default:
                    return null;
            }
        }
        
        private void SpawnVolleyball()
        {
            if (volleyballPrefab == null || leftController == null)
                return;
            
            // Check max count
            if (CountAllVolleyballs() >= maxVolleyballs)
            {
                DeleteOldestVolleyball();
            }
            
            // Spawn at controller position
            Vector3 spawnPosition = leftController.position + leftController.TransformDirection(controllerSpawnOffset);
            GameObject newBall = Instantiate(volleyballPrefab, spawnPosition, Quaternion.identity);
            
            // Track it
            spawnedVolleyballs.Add(newBall);
        }
        
        private int CountAllVolleyballs()
        {
            VolleyballV5[] allVolleyballs = FindObjectsOfType<VolleyballV5>();
            return allVolleyballs != null ? allVolleyballs.Length : 0;
        }
        
        private void DeleteOldestVolleyball()
        {
            // Delete from tracked list first (FIFO)
            if (spawnedVolleyballs.Count > 0)
            {
                GameObject oldest = spawnedVolleyballs[0];
                spawnedVolleyballs.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
                return;
            }
            
            // Fallback: find oldest volleyball in scene
            VolleyballV5[] allVolleyballs = FindObjectsOfType<VolleyballV5>();
            if (allVolleyballs != null && allVolleyballs.Length > 0)
            {
                // Find the one with the oldest spawn time (or just delete first one)
                Destroy(allVolleyballs[0].gameObject);
            }
        }
        
        private void CleanupFallenVolleyballs()
        {
            VolleyballV5[] allVolleyballs = FindObjectsOfType<VolleyballV5>();
            if (allVolleyballs == null) return;
            
            foreach (var volleyball in allVolleyballs)
            {
                if (volleyball != null && volleyball.transform.position.y < -10f)
                {
                    // Remove from tracked list if present
                    spawnedVolleyballs.Remove(volleyball.gameObject);
                    Destroy(volleyball.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Tracks a volleyball spawned by a launcher.
        /// </summary>
        public void TrackVolleyball(GameObject volleyball)
        {
            if (volleyball != null && !spawnedVolleyballs.Contains(volleyball))
            {
                spawnedVolleyballs.Add(volleyball);
                
                // Check max count
                if (spawnedVolleyballs.Count > maxVolleyballs)
                {
                    DeleteOldestVolleyball();
                }
            }
        }
    }
}
