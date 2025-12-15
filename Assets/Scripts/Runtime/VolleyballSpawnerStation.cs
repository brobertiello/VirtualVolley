using UnityEngine;
using UnityEngine.UI;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// A spawner station that spawns, holds, and shoots volleyballs toward the player.
    /// </summary>
    public class VolleyballSpawnerStation : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Reference to the volleyball prefab to spawn")]
        [SerializeField] private GameObject volleyballPrefab;
        
        [Tooltip("Position offset where ball is held (relative to spawner)")]
        [SerializeField] private Vector3 holdPositionOffset = new Vector3(0f, -0.2f, 0f);
        
        [Tooltip("Force to apply when shooting the ball")]
        [SerializeField] private float shootForce = 15f;
        
        [Header("Countdown Settings")]
        [Tooltip("Countdown duration in seconds")]
        [SerializeField] private float countdownDuration = 3f;
        
        [Tooltip("Show countdown before shooting")]
        [SerializeField] private bool showCountdown = true;
        
        [Header("Visual Settings")]
        [Tooltip("Material for the spawner dot")]
        [SerializeField] private Material dotMaterial;
        
        [Tooltip("Size of the spawner dot")]
        [SerializeField] private float dotSize = 0.1f;
        
        // State
        private GameObject currentBall;
        private GameObject dotVisual;
        private GameObject countdownUI;
        private TextMesh countdownText;
        private bool isCountingDown = false;
        private float countdownTimer = 0f;
        
        /// <summary>
        /// Checks if this station is available (not counting down or shooting).
        /// </summary>
        public bool IsAvailable()
        {
            return !isCountingDown && hasBall;
        }
        private bool hasBall = false;
        
        // References
        private Transform playerCamera;
        
        private void Awake()
        {
            CreateDotVisual();
            CreateCountdownUI();
            FindPlayerCamera();
        }
        
        private void Update()
        {
            // Update countdown
            if (isCountingDown)
            {
                UpdateCountdown();
            }
            
            // Face countdown UI toward player
            if (countdownUI != null && playerCamera != null)
            {
                countdownUI.transform.LookAt(playerCamera);
                countdownUI.transform.Rotate(0, 180, 0); // Flip to face player
            }
        }
        
        private void CreateDotVisual()
        {
            // Create a sphere for the dot
            dotVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotVisual.name = "Spawner Dot";
            dotVisual.transform.SetParent(transform);
            dotVisual.transform.localPosition = Vector3.zero;
            dotVisual.transform.localScale = Vector3.one * dotSize;
            
            // Remove collider (we don't need physics for the dot)
            Collider col = dotVisual.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }
            
            // Apply material if provided
            Renderer renderer = dotVisual.GetComponent<Renderer>();
            if (dotMaterial != null)
            {
                renderer.sharedMaterial = dotMaterial;
            }
            else
            {
                // Try to load "Flat Blue" from Resources (must be in Resources folder)
                Material flatBlue = Resources.Load<Material>("Flat Blue");
                
                if (flatBlue != null)
                {
                    renderer.sharedMaterial = flatBlue;
                }
                else
                {
                    Debug.LogWarning("[VolleyballSpawnerStation] Flat Blue material not found in Resources! Please assign dotMaterial in Inspector. The material should be at: Assets/Samples/XR Interaction Toolkit/3.1.2/Starter Assets/Materials/Flat Blue.mat");
                }
            }
            
            // Ensure proper rendering for VR (both eyes see the same)
            dotVisual.layer = 0; // Default layer
            
            // Make sure it's not a UI element (which can cause stereo issues)
            Canvas canvas = dotVisual.GetComponent<Canvas>();
            if (canvas != null)
            {
                Object.Destroy(canvas);
            }
        }
        
        private void CreateCountdownUI()
        {
            // Create countdown text using TextMeshPro
            GameObject countdownObj = new GameObject("Countdown Text");
            countdownObj.transform.SetParent(transform);
            countdownObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            
            // Add TextMesh component
            countdownText = countdownObj.AddComponent<TextMesh>();
            countdownText.text = "";
            countdownText.fontSize = 12; // 1/4 of 50 = 12.5, rounded to 12
            countdownText.anchor = TextAnchor.MiddleCenter;
            countdownText.color = Color.white;
            countdownText.fontStyle = FontStyle.Bold;
            
            // Set font if available
            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont != null)
            {
                countdownText.font = defaultFont;
            }
            
            // Make it face camera initially
            countdownObj.transform.rotation = Quaternion.identity;
            
            countdownUI = countdownObj;
            countdownUI.SetActive(false);
        }
        
        private void FindPlayerCamera()
        {
            // Find XR Origin camera
            Unity.XR.CoreUtils.XROrigin xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                playerCamera = xrOrigin.Camera.transform;
            }
            else
            {
                // Fallback to main camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerCamera = mainCam.transform;
                }
            }
        }
        
        /// <summary>
        /// Spawns a ball and holds it at the spawner.
        /// </summary>
        public void SpawnAndHoldBall()
        {
            if (volleyballPrefab == null)
            {
                Debug.LogWarning($"[VolleyballSpawnerStation] {gameObject.name}: Volleyball prefab not assigned!");
                return;
            }
            
            // Destroy existing ball if any
            if (currentBall != null)
            {
                Object.Destroy(currentBall);
            }
            
            // Calculate hold position
            Vector3 holdPosition = transform.position + transform.TransformDirection(holdPositionOffset);
            
            // Spawn new ball
            currentBall = Instantiate(volleyballPrefab, holdPosition, Quaternion.identity);
            currentBall.name = $"Ball_{gameObject.name}";
            
            // Make ball kinematic so it stays in place
            Rigidbody rb = currentBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Register with arm collision manager
            RegisterBallWithArmManager(currentBall);
            
            hasBall = true;
            Debug.Log($"[VolleyballSpawnerStation] {gameObject.name}: Spawned and holding ball");
        }
        
        private void RegisterBallWithArmManager(GameObject ball)
        {
            // Find or create the collision manager
            GameObject managerObj = GameObject.Find("Volleyball Arm Collision Manager");
            if (managerObj == null)
            {
                managerObj = new GameObject("Volleyball Arm Collision Manager");
                System.Type managerType = System.Type.GetType("VirtualVolley.Core.Scripts.Runtime.VolleyballArmCollisionManager, Assembly-CSharp");
                if (managerType != null)
                {
                    managerObj.AddComponent(managerType);
                }
            }
            
            if (managerObj != null)
            {
                MonoBehaviour manager = managerObj.GetComponent("VolleyballArmCollisionManager") as MonoBehaviour;
                if (manager != null)
                {
                    var registerMethod = manager.GetType().GetMethod("RegisterVolleyball");
                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(manager, new object[] { ball });
                    }
                }
            }
        }
        
        /// <summary>
        /// Starts the countdown and shoots the ball when countdown reaches 0.
        /// </summary>
        public void StartCountdownAndShoot()
        {
            if (!hasBall || currentBall == null)
            {
                // Spawn a ball first if we don't have one
                SpawnAndHoldBall();
            }
            
            if (currentBall == null)
            {
                Debug.LogWarning($"[VolleyballSpawnerStation] {gameObject.name}: Cannot shoot - no ball!");
                return;
            }
            
            if (showCountdown)
            {
                isCountingDown = true;
                countdownTimer = countdownDuration;
                countdownUI.SetActive(true);
            }
            else
            {
                // Shoot immediately
                ShootBall();
            }
        }
        
        private void UpdateCountdown()
        {
            if (!isCountingDown) return;
            
            countdownTimer -= Time.deltaTime;
            
            if (countdownTimer > 0)
            {
                // Show countdown number
                int count = Mathf.CeilToInt(countdownTimer);
                countdownText.text = count.ToString();
                
                // Change color based on urgency
                if (count == 1)
                {
                    countdownText.color = Color.red;
                }
                else if (count == 2)
                {
                    countdownText.color = Color.yellow;
                }
                else
                {
                    countdownText.color = Color.white;
                }
            }
            else
            {
                // Countdown finished - shoot!
                isCountingDown = false;
                countdownUI.SetActive(false);
                ShootBall();
            }
        }
        
        private void ShootBall()
        {
            if (currentBall == null)
            {
                Debug.LogWarning($"[VolleyballSpawnerStation] {gameObject.name}: Cannot shoot - missing ball!");
                return;
            }
            
            // Re-find player camera if needed
            if (playerCamera == null)
            {
                FindPlayerCamera();
            }
            
            if (playerCamera == null)
            {
                Debug.LogWarning($"[VolleyballSpawnerStation] {gameObject.name}: Cannot shoot - player camera not found!");
                return;
            }
            
            // Calculate direction to player
            Vector3 directionToPlayer = (playerCamera.position - currentBall.transform.position).normalized;
            
            // Make ball non-kinematic
            Rigidbody rb = currentBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                
                // Apply force toward player
                rb.AddForce(directionToPlayer * shootForce, ForceMode.Impulse);
                
                Debug.Log($"[VolleyballSpawnerStation] {gameObject.name}: Shot ball toward player at {playerCamera.position} with force {shootForce}");
            }
            else
            {
                Debug.LogWarning($"[VolleyballSpawnerStation] {gameObject.name}: Ball has no Rigidbody!");
            }
            
            hasBall = false;
            currentBall = null;
        }
        
        /// <summary>
        /// Sets the volleyball prefab reference.
        /// </summary>
        public void SetVolleyballPrefab(GameObject prefab)
        {
            volleyballPrefab = prefab;
        }
    }
}

