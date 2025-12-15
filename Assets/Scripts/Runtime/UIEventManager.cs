using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Manages UI interactions for scene selection menu and settings.
    /// </summary>
    public class UIEventManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private POVArmsPrimitives armsScript;
        
        [Header("Scene Selection Buttons")]
        [SerializeField] private Button freePlayButton;
        [SerializeField] private Button freeBallsButton;
        [SerializeField] private Button serveReceiveButton;
        [SerializeField] private Button spikeReceiveButton;
        
        [Header("Settings Sliders")]
        [SerializeField] private Slider leftShoulderXSlider;
        [SerializeField] private Slider leftShoulderYSlider;
        [SerializeField] private Slider leftShoulderZSlider;
        [SerializeField] private Slider rightShoulderXSlider;
        [SerializeField] private Slider rightShoulderYSlider;
        [SerializeField] private Slider rightShoulderZSlider;
        [SerializeField] private Slider leftHandXSlider;
        [SerializeField] private Slider leftHandYSlider;
        [SerializeField] private Slider leftHandZSlider;
        [SerializeField] private Slider rightHandXSlider;
        [SerializeField] private Slider rightHandYSlider;
        [SerializeField] private Slider rightHandZSlider;
        [SerializeField] private Slider armLengthSlider;
        [SerializeField] private Button applyPresetButton;
        
        private Color selectedColor = new Color(0.2f, 1f, 0.2f); // Green
        private Color normalColor = new Color(0.2f, 0.6f, 1f); // Blue
        
        // Base values for offsets (stored to allow reset)
        private Vector3 baseLeftShoulderOffset;
        private Vector3 baseRightShoulderOffset;
        private Vector3 baseLeftHandOffset;
        private Vector3 baseRightHandOffset;
        private float baseArmLength;
        
        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
            
            if (armsScript == null)
            {
                armsScript = FindObjectOfType<POVArmsPrimitives>();
            }
        }
        
        private void Start()
        {
            SetupSceneSelectionButtons();
            SetupPresetButton();
            
            // Store base values before setting up sliders
            StoreBaseValues();
            SetupSettingsSliders();
            
            // Subscribe to game state changes
            if (gameManager != null)
            {
                gameManager.OnSceneChanged += UpdateSceneButtons;
            }
            else if (GameManager.Instance != null)
            {
                gameManager = GameManager.Instance;
                gameManager.OnSceneChanged += UpdateSceneButtons;
            }
            
            // Initialize button states - delay slightly to ensure GameManager is ready
            StartCoroutine(DelayedButtonUpdate());
        }
        
        private System.Collections.IEnumerator DelayedButtonUpdate()
        {
            yield return null; // Wait one frame
            GameManager.Scene currentScene = GameManager.Scene.FreePlay;
            if (gameManager != null)
            {
                currentScene = gameManager.CurrentScene;
            }
            else if (GameManager.Instance != null)
            {
                currentScene = GameManager.Instance.CurrentScene;
            }
            UpdateSceneButtons(currentScene);
        }
        
        private void OnEnable()
        {
            // Re-subscribe when enabled
            if (gameManager != null)
            {
                gameManager.OnSceneChanged += UpdateSceneButtons;
            }
            else if (GameManager.Instance != null)
            {
                gameManager = GameManager.Instance;
                gameManager.OnSceneChanged += UpdateSceneButtons;
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe when disabled
            if (gameManager != null)
            {
                gameManager.OnSceneChanged -= UpdateSceneButtons;
            }
        }
        
        private void SetupPresetButton()
        {
            if (applyPresetButton != null)
            {
                applyPresetButton.onClick.AddListener(ApplyPresetValues);
            }
        }
        
        public void ApplyPresetValues()
        {
            // Left Shoulder
            if (leftShoulderXSlider != null) leftShoulderXSlider.value = -0.2f;
            if (leftShoulderYSlider != null) leftShoulderYSlider.value = -0.15f;
            if (leftShoulderZSlider != null) leftShoulderZSlider.value = 0.05f;
            
            // Right Shoulder
            if (rightShoulderXSlider != null) rightShoulderXSlider.value = 0.2f;
            if (rightShoulderYSlider != null) rightShoulderYSlider.value = -0.15f;
            if (rightShoulderZSlider != null) rightShoulderZSlider.value = 0.05f;
            
            // Left Hand
            if (leftHandXSlider != null) leftHandXSlider.value = -0.03f;
            if (leftHandYSlider != null) leftHandYSlider.value = -0.03f;
            if (leftHandZSlider != null) leftHandZSlider.value = -0.1f;
            
            // Right Hand
            if (rightHandXSlider != null) rightHandXSlider.value = 0.03f;
            if (rightHandYSlider != null) rightHandYSlider.value = -0.03f;
            if (rightHandZSlider != null) rightHandZSlider.value = -0.1f;
            
            // Arm Length
            if (armLengthSlider != null) armLengthSlider.value = 0.32f;
        }
        
        private void StoreBaseValues()
        {
            if (armsScript != null)
            {
                baseLeftShoulderOffset = armsScript.leftShoulderOffset;
                baseRightShoulderOffset = armsScript.rightShoulderOffset;
                baseLeftHandOffset = armsScript.leftHandPositionOffset;
                baseRightHandOffset = armsScript.rightHandPositionOffset;
                baseArmLength = armsScript.upperArmLength;
            }
        }
        
        private void SetupSceneSelectionButtons()
        {
            // Remove all existing listeners first
            if (freePlayButton != null) freePlayButton.onClick.RemoveAllListeners();
            if (freeBallsButton != null) freeBallsButton.onClick.RemoveAllListeners();
            if (serveReceiveButton != null) serveReceiveButton.onClick.RemoveAllListeners();
            if (spikeReceiveButton != null) spikeReceiveButton.onClick.RemoveAllListeners();
            
            // Add new listeners
            if (freePlayButton != null)
            {
                freePlayButton.onClick.AddListener(() => {
                    if (gameManager != null)
                    {
                        gameManager.CurrentScene = GameManager.Scene.FreePlay;
                        Debug.Log("[UIEventManager] Scene set to Free Play");
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.FreePlay;
                        Debug.Log("[UIEventManager] Scene set to Free Play (via Instance)");
                    }
                });
            }
            
            if (freeBallsButton != null)
            {
                freeBallsButton.onClick.AddListener(() => {
                    if (gameManager != null)
                    {
                        gameManager.CurrentScene = GameManager.Scene.FreeBalls;
                        Debug.Log("[UIEventManager] Scene set to Free Balls");
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.FreeBalls;
                        Debug.Log("[UIEventManager] Scene set to Free Balls (via Instance)");
                    }
                });
            }
            
            if (serveReceiveButton != null)
            {
                serveReceiveButton.onClick.AddListener(() => {
                    if (gameManager != null)
                    {
                        gameManager.CurrentScene = GameManager.Scene.ServeReceive;
                        Debug.Log("[UIEventManager] Scene set to Serve Receive");
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.ServeReceive;
                        Debug.Log("[UIEventManager] Scene set to Serve Receive (via Instance)");
                    }
                });
            }
            
            if (spikeReceiveButton != null)
            {
                spikeReceiveButton.onClick.AddListener(() => {
                    if (gameManager != null)
                    {
                        gameManager.CurrentScene = GameManager.Scene.SpikeReceive;
                        Debug.Log("[UIEventManager] Scene set to Spike Receive");
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.SpikeReceive;
                        Debug.Log("[UIEventManager] Scene set to Spike Receive (via Instance)");
                    }
                });
            }
        }
        
        private void SetupSettingsSliders()
        {
            if (armsScript == null) return;
            
            // Shoulder sliders - initialize with current values (as offsets from base)
            SetupSliderWithReset(leftShoulderXSlider, baseLeftShoulderOffset.x, -0.5f, 0.5f, 
                value => UpdateLeftShoulderOffset(new Vector3(value, leftShoulderYSlider != null ? leftShoulderYSlider.value : 0f, leftShoulderZSlider != null ? leftShoulderZSlider.value : 0f)),
                () => ResetLeftShoulderX());
            SetupSliderWithReset(leftShoulderYSlider, baseLeftShoulderOffset.y, -0.5f, 0.5f,
                value => UpdateLeftShoulderOffset(new Vector3(leftShoulderXSlider != null ? leftShoulderXSlider.value : 0f, value, leftShoulderZSlider != null ? leftShoulderZSlider.value : 0f)),
                () => ResetLeftShoulderY());
            SetupSliderWithReset(leftShoulderZSlider, baseLeftShoulderOffset.z, -0.5f, 0.5f,
                value => UpdateLeftShoulderOffset(new Vector3(leftShoulderXSlider != null ? leftShoulderXSlider.value : 0f, leftShoulderYSlider != null ? leftShoulderYSlider.value : 0f, value)),
                () => ResetLeftShoulderZ());
            
            SetupSliderWithReset(rightShoulderXSlider, baseRightShoulderOffset.x, -0.5f, 0.5f,
                value => UpdateRightShoulderOffset(new Vector3(value, rightShoulderYSlider != null ? rightShoulderYSlider.value : 0f, rightShoulderZSlider != null ? rightShoulderZSlider.value : 0f)),
                () => ResetRightShoulderX());
            SetupSliderWithReset(rightShoulderYSlider, baseRightShoulderOffset.y, -0.5f, 0.5f,
                value => UpdateRightShoulderOffset(new Vector3(rightShoulderXSlider != null ? rightShoulderXSlider.value : 0f, value, rightShoulderZSlider != null ? rightShoulderZSlider.value : 0f)),
                () => ResetRightShoulderY());
            SetupSliderWithReset(rightShoulderZSlider, baseRightShoulderOffset.z, -0.5f, 0.5f,
                value => UpdateRightShoulderOffset(new Vector3(rightShoulderXSlider != null ? rightShoulderXSlider.value : 0f, rightShoulderYSlider != null ? rightShoulderYSlider.value : 0f, value)),
                () => ResetRightShoulderZ());
            
            // Hand sliders - initialize with current values (as offsets from base)
            SetupSliderWithReset(leftHandXSlider, baseLeftHandOffset.x, -0.5f, 0.5f,
                value => UpdateLeftHandOffset(new Vector3(value, leftHandYSlider != null ? leftHandYSlider.value : 0f, leftHandZSlider != null ? leftHandZSlider.value : 0f)),
                () => ResetLeftHandX());
            SetupSliderWithReset(leftHandYSlider, baseLeftHandOffset.y, -0.5f, 0.5f,
                value => UpdateLeftHandOffset(new Vector3(leftHandXSlider != null ? leftHandXSlider.value : 0f, value, leftHandZSlider != null ? leftHandZSlider.value : 0f)),
                () => ResetLeftHandY());
            SetupSliderWithReset(leftHandZSlider, baseLeftHandOffset.z, -0.5f, 0.5f,
                value => UpdateLeftHandOffset(new Vector3(leftHandXSlider != null ? leftHandXSlider.value : 0f, leftHandYSlider != null ? leftHandYSlider.value : 0f, value)),
                () => ResetLeftHandZ());
            
            SetupSliderWithReset(rightHandXSlider, baseRightHandOffset.x, -0.5f, 0.5f,
                value => UpdateRightHandOffset(new Vector3(value, rightHandYSlider != null ? rightHandYSlider.value : 0f, rightHandZSlider != null ? rightHandZSlider.value : 0f)),
                () => ResetRightHandX());
            SetupSliderWithReset(rightHandYSlider, baseRightHandOffset.y, -0.5f, 0.5f,
                value => UpdateRightHandOffset(new Vector3(rightHandXSlider != null ? rightHandXSlider.value : 0f, value, rightHandZSlider != null ? rightHandZSlider.value : 0f)),
                () => ResetRightHandY());
            SetupSliderWithReset(rightHandZSlider, baseRightHandOffset.z, -0.5f, 0.5f,
                value => UpdateRightHandOffset(new Vector3(rightHandXSlider != null ? rightHandXSlider.value : 0f, rightHandYSlider != null ? rightHandYSlider.value : 0f, value)),
                () => ResetRightHandZ());
            
            // Arm length slider
            SetupSliderWithReset(armLengthSlider, baseArmLength, 0.1f, 0.5f,
                UpdateArmLength,
                () => ResetArmLength());
        }
        
        private void SetupSliderWithReset(Slider slider, float initialValue, float minValue, float maxValue, 
            System.Action<float> onValueChanged, System.Action onReset)
        {
            if (slider == null) return;
            
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = initialValue;
            slider.onValueChanged.AddListener(value => 
            {
                onValueChanged?.Invoke(value);
                UpdateSliderValueDisplay(slider, value);
            });
            
            // Store reset callback in slider's parent GameObject for button access
            var resetComponent = slider.transform.parent.GetComponent<SliderResetHelper>();
            if (resetComponent == null)
            {
                resetComponent = slider.transform.parent.gameObject.AddComponent<SliderResetHelper>();
            }
            resetComponent.Initialize(slider, initialValue, onReset);
            
            // Wire up reset button
            Transform resetButton = slider.transform.parent.Find("LabelContainer/ResetButton");
            if (resetButton != null)
            {
                Button btn = resetButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => resetComponent.ResetToDefault());
                }
            }
            
            // Update initial value display
            UpdateSliderValueDisplay(slider, initialValue);
        }
        
        private void UpdateSliderValueDisplay(Slider slider, float value)
        {
            if (slider == null) return;
            
            // Find value display text in slider's parent
            Transform sliderParent = slider.transform.parent;
            if (sliderParent != null)
            {
                Transform valueDisplay = sliderParent.Find("LabelContainer/ValueDisplay");
                if (valueDisplay != null)
                {
                    TextMeshProUGUI text = valueDisplay.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = value.ToString("F3");
                    }
                }
            }
        }
        
        // Reset methods
        private void ResetLeftShoulderX() { if (leftShoulderXSlider != null) leftShoulderXSlider.value = baseLeftShoulderOffset.x; }
        private void ResetLeftShoulderY() { if (leftShoulderYSlider != null) leftShoulderYSlider.value = baseLeftShoulderOffset.y; }
        private void ResetLeftShoulderZ() { if (leftShoulderZSlider != null) leftShoulderZSlider.value = baseLeftShoulderOffset.z; }
        private void ResetRightShoulderX() { if (rightShoulderXSlider != null) rightShoulderXSlider.value = baseRightShoulderOffset.x; }
        private void ResetRightShoulderY() { if (rightShoulderYSlider != null) rightShoulderYSlider.value = baseRightShoulderOffset.y; }
        private void ResetRightShoulderZ() { if (rightShoulderZSlider != null) rightShoulderZSlider.value = baseRightShoulderOffset.z; }
        private void ResetLeftHandX() { if (leftHandXSlider != null) leftHandXSlider.value = baseLeftHandOffset.x; }
        private void ResetLeftHandY() { if (leftHandYSlider != null) leftHandYSlider.value = baseLeftHandOffset.y; }
        private void ResetLeftHandZ() { if (leftHandZSlider != null) leftHandZSlider.value = baseLeftHandOffset.z; }
        private void ResetRightHandX() { if (rightHandXSlider != null) rightHandXSlider.value = baseRightHandOffset.x; }
        private void ResetRightHandY() { if (rightHandYSlider != null) rightHandYSlider.value = baseRightHandOffset.y; }
        private void ResetRightHandZ() { if (rightHandZSlider != null) rightHandZSlider.value = baseRightHandOffset.z; }
        private void ResetArmLength() { if (armLengthSlider != null) armLengthSlider.value = baseArmLength; }
        
        
        private void UpdateSceneButtons(GameManager.Scene scene)
        {
            Debug.Log($"[UIEventManager] Updating scene buttons for scene: {scene}");
            UpdateButtonColor(freePlayButton, scene == GameManager.Scene.FreePlay);
            UpdateButtonColor(freeBallsButton, scene == GameManager.Scene.FreeBalls);
            UpdateButtonColor(serveReceiveButton, scene == GameManager.Scene.ServeReceive);
            UpdateButtonColor(spikeReceiveButton, scene == GameManager.Scene.SpikeReceive);
        }
        
        // Public method to allow external updates
        public void RefreshSceneButtons()
        {
            if (gameManager != null)
            {
                UpdateSceneButtons(gameManager.CurrentScene);
            }
            else if (GameManager.Instance != null)
            {
                UpdateSceneButtons(GameManager.Instance.CurrentScene);
            }
        }
        
        private void UpdateButtonColor(Button button, bool isSelected)
        {
            if (button != null)
            {
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? selectedColor : normalColor;
                }
            }
        }
        
        private void UpdateLeftShoulderOffset(Vector3 offset)
        {
            if (armsScript != null)
            {
                // Offset is already the full value (base + slider adjustment)
                armsScript.leftShoulderOffset = offset;
            }
        }
        
        private void UpdateRightShoulderOffset(Vector3 offset)
        {
            if (armsScript != null)
            {
                // Offset is already the full value (base + slider adjustment)
                armsScript.rightShoulderOffset = offset;
            }
        }
        
        private void UpdateLeftHandOffset(Vector3 offset)
        {
            if (armsScript != null)
            {
                // Offset is already the full value (base + slider adjustment)
                armsScript.leftHandPositionOffset = offset;
            }
        }
        
        private void UpdateRightHandOffset(Vector3 offset)
        {
            if (armsScript != null)
            {
                // Offset is already the full value (base + slider adjustment)
                armsScript.rightHandPositionOffset = offset;
            }
        }
        
        private void UpdateArmLength(float length)
        {
            if (armsScript != null)
            {
                armsScript.upperArmLength = length;
                armsScript.forearmLength = length;
            }
        }
    }
}
