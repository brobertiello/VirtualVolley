using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// UI panel for spawning volleyballs with automatic cleanup.
    /// </summary>
    public class VolleyballSpawner : MonoBehaviour
    {
        [Header("Volleyball Settings")]
        [Tooltip("Reference to the volleyball prefab or scene object to clone")]
        [SerializeField] private GameObject volleyballPrefab;
        
        [Tooltip("Maximum number of volleyballs allowed in the scene")]
        [SerializeField] private int maxVolleyballs = 10;
        
        [Tooltip("Y position threshold - volleyballs below this will be deleted")]
        [SerializeField] private float deleteBelowY = -10f;
        
        [Header("Spawn Settings")]
        [Tooltip("Spawn position in world space")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1.5f, 0f);
        
        [Tooltip("Spawn velocity (how fast the ball moves when spawned)")]
        [SerializeField] private Vector3 spawnVelocity = Vector3.zero;
        
        [Tooltip("Spread radius when spawning multiple balls")]
        [SerializeField] private float spawnSpreadRadius = 0.5f;
        
        [Header("UI Settings")]
        [Tooltip("UI Panel GameObject (will be created if not assigned)")]
        [SerializeField] private GameObject uiPanel;
        
        [Tooltip("Show UI panel by default")]
        [SerializeField] private bool showUIByDefault = true;
        
        [Header("Input Settings")]
        [Tooltip("Input Action Reference for left controller action button (to toggle menu)")]
        [SerializeField] private InputActionReference leftActionButtonInput;
        
        // Tracking
        private List<GameObject> spawnedVolleyballs = new List<GameObject>();
        private Camera mainCamera;
        private Transform cameraTransform;
        private Unity.XR.CoreUtils.XROrigin xrOrigin;
        private bool menuOpen = true;
        private bool wasActionButtonPressed = false;
        
        private void Awake()
        {
            // Find camera and XR Origin
            xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                mainCamera = xrOrigin.Camera;
                cameraTransform = mainCamera.transform;
            }
            else
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                }
            }
            
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
            
            // Create UI
            CreateUI();
        }
        
        private void Start()
        {
            // Set initial menu state
            if (uiPanel != null)
            {
                uiPanel.SetActive(showUIByDefault);
                menuOpen = showUIByDefault;
            }
            
            // Find left action button input if not assigned
            if (leftActionButtonInput == null)
            {
                FindLeftActionButtonInput();
            }
            
            // Subscribe to input
            if (leftActionButtonInput != null)
            {
                leftActionButtonInput.action.Enable();
            }
        }
        
        private void Update()
        {
            // Clean up volleyballs that fall too low
            CleanupFallenVolleyballs();
            
            // Enforce max count
            EnforceMaxCount();
            
            // Check for left action button press
            CheckLeftActionButton();
        }
        
        private void FindLeftActionButtonInput()
        {
            // This will be set up manually in the editor or via a setup script
            // The user can assign the InputActionReference in the Inspector
            // Common path: XRI LeftHand/ActivateValue or XRI LeftHand/Activate
            Debug.Log("[VolleyballSpawner] Please assign Left Action Button Input in Inspector (XRI LeftHand/ActivateValue)");
        }
        
        private void CheckLeftActionButton()
        {
            if (leftActionButtonInput == null || leftActionButtonInput.action == null)
                return;
            
            bool isPressed = leftActionButtonInput.action.ReadValue<float>() > 0.5f || 
                            leftActionButtonInput.action.WasPressedThisFrame();
            
            if (isPressed && !wasActionButtonPressed)
            {
                ToggleMenu();
            }
            
            wasActionButtonPressed = isPressed;
        }
        
        public void ToggleMenu()
        {
            menuOpen = !menuOpen;
            if (uiPanel != null)
            {
                uiPanel.SetActive(menuOpen);
            }
        }
        
        public void CloseMenu()
        {
            menuOpen = false;
            if (uiPanel != null)
            {
                uiPanel.SetActive(false);
            }
        }
        
        private void CreateUI()
        {
            // Find or create Canvas (look for one specifically for spawner)
            Canvas canvas = null;
            GameObject canvasObj = GameObject.Find("Volleyball Spawner Canvas");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
            
            if (canvas == null)
            {
                canvasObj = new GameObject("Volleyball Spawner Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = mainCamera;
                
                // Add CanvasScaler (like Spatial Panel)
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Add TrackedDeviceGraphicRaycaster for VR interaction (like Spatial Panel)
                canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                
                // Parent canvas to camera so it follows the player
                if (cameraTransform != null)
                {
                    canvasObj.transform.SetParent(cameraTransform);
                    // Position in front of camera (local space)
                    canvasObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                    canvasObj.transform.localRotation = Quaternion.identity;
                    canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                }
                else
                {
                    // Fallback: position in world space
                    canvasObj.transform.position = new Vector3(0, 1.5f, 0);
                    canvasObj.transform.rotation = Quaternion.identity;
                    canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                }
                
                // CRITICAL: Remove any physics components that could cause falling
                Rigidbody rb = canvasObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Object.DestroyImmediate(rb);
                }
                
                // Also check for any colliders and make sure they're triggers
                Collider[] colliders = canvasObj.GetComponents<Collider>();
                foreach (var col in colliders)
                {
                    if (col != null)
                    {
                        col.isTrigger = true;
                    }
                }
            }
            
            // Ensure EventSystem has XRUIInputModule (like Spatial Panel setup)
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            }
            else
            {
                // Check if XRUIInputModule exists, add if not
                if (eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                }
            }
            
            // Create or find panel
            if (uiPanel == null)
            {
                Transform panelTransform = canvas.transform.Find("Volleyball Spawner Panel");
                if (panelTransform != null)
                {
                    uiPanel = panelTransform.gameObject;
                }
                else
                {
                    uiPanel = new GameObject("Volleyball Spawner Panel");
                    uiPanel.transform.SetParent(canvas.transform, false);
                    
                    // Add Image component for background
                    Image panelImage = uiPanel.AddComponent<Image>();
                    panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                    
                    // Set RectTransform
                    RectTransform panelRect = uiPanel.GetComponent<RectTransform>();
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.sizeDelta = new Vector2(300, 220);
                    panelRect.anchoredPosition = Vector2.zero;
                }
            }
            
            // Add draggable functionality (like Spatial Panel Manipulator)
            AddDraggableFunctionality(uiPanel);
            
            // Create close button (X in top right)
            CreateCloseButton(uiPanel);
            
            // Create buttons
            CreateSpawnButton("Spawn 1 Volleyball", new Vector2(0, 30), () => SpawnVolleyball(1));
            CreateSpawnButton("Spawn 5 Volleyballs", new Vector2(0, -20), () => SpawnVolleyball(5));
            CreateSpawnButton("Spawn 10 Volleyballs", new Vector2(0, -70), () => SpawnVolleyball(10));
            
            // Set panel visibility
            uiPanel.SetActive(showUIByDefault);
        }
        
        private void AddDraggableFunctionality(GameObject panel)
        {
            // Add draggable functionality to the canvas root (so the whole canvas moves)
            GameObject canvasObj = panel.transform.root.gameObject;
            
            // CRITICAL: Remove any Rigidbody that might cause physics
            Rigidbody rb = canvasObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Object.DestroyImmediate(rb);
            }
            
            // Add XR Grab Interactable to make it grabbable/draggable
            var interactable = canvasObj.GetComponent<XRGrabInteractable>();
            if (interactable == null)
            {
                interactable = canvasObj.AddComponent<XRGrabInteractable>();
                // Configure for UI dragging (not physics-based)
                interactable.movementType = XRBaseInteractable.MovementType.Instantaneous; // No physics
                interactable.trackPosition = true;
                interactable.trackRotation = false;
                
                // Subscribe to grab events for custom dragging
                interactable.selectEntered.AddListener(OnPanelGrabbed);
                interactable.selectExited.AddListener(OnPanelReleased);
            }
            else
            {
                // Ensure existing interactable doesn't use physics
                interactable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            }
            
            // After adding XRGrabInteractable, check if it added a Rigidbody and remove/disable it
            rb = canvasObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // If XRGrabInteractable added a Rigidbody, remove it completely
                Object.DestroyImmediate(rb);
            }
            
            // Add a collider for interaction (but make it a trigger so it doesn't interact with physics)
            BoxCollider collider = canvasObj.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = canvasObj.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true; // Make it a trigger so it doesn't interact with physics
            
            // Set collider size to match panel (accounting for canvas scale)
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                Vector3 size = new Vector3(panelRect.sizeDelta.x * 0.001f, panelRect.sizeDelta.y * 0.001f, 0.01f);
                collider.size = size;
                collider.center = new Vector3(0, 0, -size.z / 2);
            }
        }
        
        private Transform panelGrabTransform;
        private Vector3 panelGrabOffset;
        private bool isPanelGrabbed = false;
        
        private void OnPanelGrabbed(SelectEnterEventArgs args)
        {
            isPanelGrabbed = true;
            panelGrabTransform = args.interactorObject.transform;
            GameObject canvasObj = uiPanel != null ? uiPanel.transform.root.gameObject : null;
            if (canvasObj != null)
            {
                panelGrabOffset = panelGrabTransform.position - canvasObj.transform.position;
            }
        }
        
        private void OnPanelReleased(SelectExitEventArgs args)
        {
            isPanelGrabbed = false;
            panelGrabTransform = null;
        }
        
        private void LateUpdate()
        {
            if (uiPanel == null) return;
            
            GameObject canvasObj = uiPanel.transform.root.gameObject;
            if (canvasObj == null) return;
            
            // CRITICAL: Always ensure no Rigidbody exists (XRGrabInteractable might add one)
            Rigidbody rb = canvasObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Object.Destroy(rb);
            }
            
            // Ensure collider is a trigger
            Collider col = canvasObj.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
            
            // Handle panel dragging
            if (isPanelGrabbed && panelGrabTransform != null)
            {
                // When dragging, temporarily unparent from camera and position in world space
                if (canvasObj.transform.parent == cameraTransform)
                {
                    canvasObj.transform.SetParent(null);
                }
                canvasObj.transform.position = panelGrabTransform.position - panelGrabOffset;
            }
            else
            {
                // When not dragging, ensure it's parented to camera
                if (cameraTransform != null && canvasObj.transform.parent != cameraTransform)
                {
                    canvasObj.transform.SetParent(cameraTransform);
                    canvasObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);
                    canvasObj.transform.localRotation = Quaternion.identity;
                }
            }
        }
        
        private void CreateCloseButton(GameObject parent)
        {
            GameObject closeButtonObj = new GameObject("CloseButton");
            closeButtonObj.transform.SetParent(parent.transform, false);
            
            // Add Image component
            Image buttonImage = closeButtonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            // Set RectTransform (top right corner)
            RectTransform buttonRect = closeButtonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(30, 30);
            buttonRect.anchoredPosition = new Vector2(-15, -15);
            
            // Add Button component
            Button button = closeButtonObj.AddComponent<Button>();
            button.onClick.AddListener(CloseMenu);
            
            // Create X text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeButtonObj.transform, false);
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = "X";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 20;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        private void CreateSpawnButton(string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(text.Replace(" ", ""));
            buttonObj.transform.SetParent(uiPanel.transform, false);
            
            // Add Image component
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
            
            // Set RectTransform
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(250, 40);
            buttonRect.anchoredPosition = position;
            
            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            
            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        public void SpawnVolleyball(int count = 1)
        {
            if (volleyballPrefab == null)
            {
                Debug.LogError("[VolleyballSpawner] Volleyball prefab not found! Cannot spawn.");
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                // Calculate spawn position with spread
                Vector3 spawnPos = spawnPosition;
                
                if (count > 1)
                {
                    // Spread balls in a circle pattern
                    float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
                    float radius = spawnSpreadRadius * (1f + i * 0.1f); // Slightly increasing radius
                    spawnPos.x += Mathf.Cos(angle) * radius;
                    spawnPos.z += Mathf.Sin(angle) * radius;
                    spawnPos.y += Random.Range(-0.1f, 0.1f); // Small vertical variation
                }
                
                // Instantiate volleyball
                GameObject newVolleyball = Instantiate(volleyballPrefab, spawnPos, Quaternion.identity);
                newVolleyball.name = $"Volleyball_{spawnedVolleyballs.Count + 1}";
                
                // Apply velocity if Rigidbody exists
                Rigidbody rb = newVolleyball.GetComponent<Rigidbody>();
                if (rb != null && spawnVelocity != Vector3.zero)
                {
                    rb.velocity = spawnVelocity;
                }
                
                // Add to tracking list
                spawnedVolleyballs.Add(newVolleyball);
            }
            
            Debug.Log($"[VolleyballSpawner] Spawned {count} volleyball(s). Total: {spawnedVolleyballs.Count}");
            
            // Clean up if over limit
            EnforceMaxCount();
        }
        
        private void EnforceMaxCount()
        {
            if (spawnedVolleyballs.Count <= maxVolleyballs)
                return;
            
            // Delete oldest volleyballs first
            int toDelete = spawnedVolleyballs.Count - maxVolleyballs;
            for (int i = 0; i < toDelete; i++)
            {
                if (spawnedVolleyballs[i] != null)
                {
                    Debug.Log($"[VolleyballSpawner] Deleting volleyball (over limit): {spawnedVolleyballs[i].name}");
                    Destroy(spawnedVolleyballs[i]);
                }
            }
            
            // Remove from list
            spawnedVolleyballs.RemoveRange(0, toDelete);
        }
        
        private void CleanupFallenVolleyballs()
        {
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
                    Debug.Log($"[VolleyballSpawner] Deleting volleyball (fell below {deleteBelowY}): {vb.name}");
                    Destroy(vb);
                    spawnedVolleyballs.RemoveAt(i);
                }
            }
        }
        
        public void ToggleUI()
        {
            if (uiPanel != null)
            {
                uiPanel.SetActive(!uiPanel.activeSelf);
            }
        }
    }
}

