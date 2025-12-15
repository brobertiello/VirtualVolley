using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Handles dragging of UI panels in VR using XR Interaction Toolkit.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PanelDragHandler : MonoBehaviour
    {
        private XRGrabInteractable interactable;
        private Transform attachTransform;
        private bool isGrabbed = false;
        private Vector3 grabOffset;
        
        private void Awake()
        {
            interactable = GetComponent<XRGrabInteractable>();
            
            // Subscribe to grab events
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
        }
        
        private void Update()
        {
            if (isGrabbed && attachTransform != null)
            {
                // Move the canvas to follow the controller
                Transform canvas = transform;
                if (canvas != null)
                {
                    // Calculate new position based on controller position and offset
                    canvas.position = attachTransform.position - grabOffset;
                    
                    // Keep the canvas facing the same direction (don't rotate)
                    // Or optionally rotate to face the controller
                    // Vector3 directionToController = (attachTransform.position - canvas.position).normalized;
                    // if (directionToController != Vector3.zero)
                    // {
                    //     canvas.rotation = Quaternion.LookRotation(-directionToController, Vector3.up);
                    // }
                }
            }
        }
        
        private void OnGrab(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            attachTransform = args.interactorObject.transform;
            
            // Calculate offset from canvas center to grab point
            Transform canvas = transform.root;
            if (canvas != null)
            {
                grabOffset = attachTransform.position - canvas.position;
            }
        }
        
        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
            attachTransform = null;
        }
        
        private void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnGrab);
                interactable.selectExited.RemoveListener(OnRelease);
            }
        }
    }
}

