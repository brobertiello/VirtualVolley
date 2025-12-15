using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Controller for a custom VR UI Menu with title, slider, button, dropdown, and toggle.
    /// Based on UI Menu Example but with custom functionality.
    /// </summary>
    public class UIMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Title text component")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Tooltip("Slider component")]
        [SerializeField] private Slider slider;
        
        [Tooltip("Slider value display text")]
        [SerializeField] private TextMeshProUGUI sliderValueText;
        
        [Tooltip("Button component")]
        [SerializeField] private Button button;
        
        [Tooltip("Button text component")]
        [SerializeField] private TextMeshProUGUI buttonText;
        
        [Tooltip("Dropdown component")]
        [SerializeField] private TMP_Dropdown dropdown;
        
        [Tooltip("Toggle component")]
        [SerializeField] private Toggle toggle;
        
        [Tooltip("Toggle label text")]
        [SerializeField] private TextMeshProUGUI toggleLabelText;

        [Header("Settings")]
        [Tooltip("Default title text")]
        [SerializeField] private string defaultTitle = "VR Menu";
        
        [Tooltip("Default button text")]
        [SerializeField] private string defaultButtonText = "Click Me";
        
        [Tooltip("Default toggle label")]
        [SerializeField] private string defaultToggleLabel = "Enable Feature";

        [Header("Events")]
        [Tooltip("Called when slider value changes")]
        public UnityEngine.Events.UnityEvent<float> OnSliderValueChanged;
        
        [Tooltip("Called when button is clicked")]
        public UnityEngine.Events.UnityEvent OnButtonClicked;
        
        [Tooltip("Called when dropdown value changes")]
        public UnityEngine.Events.UnityEvent<int> OnDropdownValueChanged;
        
        [Tooltip("Called when toggle value changes")]
        public UnityEngine.Events.UnityEvent<bool> OnToggleValueChanged;

        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
        }

        private void InitializeComponents()
        {
            // Initialize title
            if (titleText != null)
            {
                titleText.text = defaultTitle;
            }

            // Initialize slider
            if (slider != null)
            {
                UpdateSliderValueText();
            }

            // Initialize button
            if (buttonText != null)
            {
                buttonText.text = defaultButtonText;
            }

            // Initialize toggle label
            if (toggleLabelText != null)
            {
                toggleLabelText.text = defaultToggleLabel;
            }
        }

        private void SetupEventListeners()
        {
            // Slider
            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderChanged);
            }

            // Button
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            // Dropdown
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDropdownChanged);
            }

            // Toggle
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        private void OnSliderChanged(float value)
        {
            UpdateSliderValueText();
            OnSliderValueChanged?.Invoke(value);
        }

        private void UpdateSliderValueText()
        {
            if (sliderValueText != null && slider != null)
            {
                sliderValueText.text = slider.value.ToString("F2");
            }
        }

        private void OnButtonClick()
        {
            OnButtonClicked?.Invoke();
        }

        private void OnDropdownChanged(int value)
        {
            OnDropdownValueChanged?.Invoke(value);
        }

        private void OnToggleChanged(bool value)
        {
            OnToggleValueChanged?.Invoke(value);
        }

        // Public methods for external control
        public void SetTitle(string text)
        {
            if (titleText != null)
            {
                titleText.text = text;
            }
        }

        public void SetSliderValue(float value)
        {
            if (slider != null)
            {
                slider.value = value;
            }
        }

        public float GetSliderValue()
        {
            return slider != null ? slider.value : 0f;
        }

        public void SetButtonText(string text)
        {
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }

        public void SetToggleLabel(string text)
        {
            if (toggleLabelText != null)
            {
                toggleLabelText.text = text;
            }
        }

        public void SetToggleValue(bool value)
        {
            if (toggle != null)
            {
                toggle.isOn = value;
            }
        }

        public bool GetToggleValue()
        {
            return toggle != null && toggle.isOn;
        }

        public void SetDropdownOptions(string[] options)
        {
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                List<TMP_Dropdown.OptionData> optionDataList = new List<TMP_Dropdown.OptionData>();
                foreach (string option in options)
                {
                    optionDataList.Add(new TMP_Dropdown.OptionData(option));
                }
                dropdown.AddOptions(optionDataList);
            }
        }

        public int GetDropdownValue()
        {
            return dropdown != null ? dropdown.value : 0;
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }

            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }

            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }
    }
}

