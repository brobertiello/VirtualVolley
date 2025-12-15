using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Makes the GameObject always face the camera.
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        private void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}

