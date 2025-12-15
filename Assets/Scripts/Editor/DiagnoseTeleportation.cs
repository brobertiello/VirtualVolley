using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Editor tool to diagnose teleportation setup issues.
    /// </summary>
    public static class DiagnoseTeleportation
    {
        [MenuItem("VirtualVolley/Diagnostics/VR/Diagnose Teleportation Setup")]
        public static void Diagnose()
        {
            Debug.Log("[VirtualVolley] ===== Teleportation Diagnosis =====");

            // Check CourtFloor
            GameObject courtFloor = GameObject.Find("CourtFloor");
            if (courtFloor == null)
            {
                Debug.LogError("[VirtualVolley] ❌ CourtFloor not found!");
                return;
            }
            Debug.Log($"[VirtualVolley] ✓ Found CourtFloor: {courtFloor.name}");

            // Check TeleportationArea
            TeleportationArea teleportArea = courtFloor.GetComponent<TeleportationArea>();
            if (teleportArea == null)
            {
                Debug.LogError("[VirtualVolley] ❌ CourtFloor missing TeleportationArea component!");
                return;
            }
            Debug.Log("[VirtualVolley] ✓ TeleportationArea component found");

            // Check Colliders
            Collider[] colliders = courtFloor.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                Debug.LogError("[VirtualVolley] ❌ CourtFloor has no colliders!");
                return;
            }
            Debug.Log($"[VirtualVolley] ✓ Found {colliders.Length} collider(s) on CourtFloor");

            foreach (var collider in colliders)
            {
                if (collider.isTrigger)
                {
                    Debug.LogWarning($"[VirtualVolley] ⚠ {collider.GetType().Name} is a trigger! Should be false for teleportation.");
                }
                else
                {
                    Debug.Log($"[VirtualVolley] ✓ {collider.GetType().Name} is not a trigger (correct)");
                }
            }

            // Check Interaction Layers
            Debug.Log($"[VirtualVolley] TeleportationArea Interaction Layers: {teleportArea.interactionLayers.value}");

            // Check XR Origin and Ray Interactors
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            }

            if (xrOrigin != null)
            {
                Debug.Log($"[VirtualVolley] ✓ Found XR Origin: {xrOrigin.name}");

                // Find ray interactors
                UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] rayInteractors = xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true);
                Debug.Log($"[VirtualVolley] Found {rayInteractors.Length} XR Ray Interactor(s)");

                foreach (var rayInteractor in rayInteractors)
                {
                    Debug.Log($"[VirtualVolley] Ray Interactor: {rayInteractor.gameObject.name}");
                    Debug.Log($"[VirtualVolley]   - Interaction Layers: {rayInteractor.interactionLayers.value}");
                    Debug.Log($"[VirtualVolley]   - Raycast Mask: {rayInteractor.raycastMask.value}");
                    Debug.Log($"[VirtualVolley]   - Select Action Trigger: {rayInteractor.selectActionTrigger}");

                    // Check if layers overlap
                    int areaLayers = teleportArea.interactionLayers.value;
                    int interactorLayers = rayInteractor.interactionLayers.value;
                    
                    if ((areaLayers & interactorLayers) != 0)
                    {
                        Debug.Log("[VirtualVolley]   ✓ Interaction layers overlap (good!)");
                    }
                    else
                    {
                        Debug.LogWarning("[VirtualVolley]   ⚠ Interaction layers do NOT overlap! This is the problem.");
                        Debug.LogWarning($"[VirtualVolley]   Area layers: {areaLayers}, Interactor layers: {interactorLayers}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[VirtualVolley] ⚠ XR Origin not found!");
            }

            Debug.Log("[VirtualVolley] ===== Diagnosis Complete =====");
        }
    }
}

