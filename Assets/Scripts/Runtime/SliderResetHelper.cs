using UnityEngine;
using UnityEngine.UI;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Helper component to store reset functionality for sliders.
    /// </summary>
    public class SliderResetHelper : MonoBehaviour
    {
        private Slider slider;
        private float defaultValue;
        private System.Action resetAction;
        
        public void Initialize(Slider slider, float defaultValue, System.Action resetAction)
        {
            this.slider = slider;
            this.defaultValue = defaultValue;
            this.resetAction = resetAction;
        }
        
        public void ResetToDefault()
        {
            resetAction?.Invoke();
        }
    }
}

