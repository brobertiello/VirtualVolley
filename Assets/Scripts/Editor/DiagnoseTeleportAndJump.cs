using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Diagnoses teleportation and jump setup issues.
    /// </summary>
    public static class DiagnoseTeleportAndJump
    {
        [MenuItem("VirtualVolley/Diagnostics/VR/Teleportation & Jump Setup")]
        public static void Diagnose()
        {
            Debug.Log("========================================");
            Debug.Log("TELEPORTATION & JUMP DIAGNOSIS");
            Debug.Log("========================================");

            // Check CourtFloor
            GameObject courtFloor = GameObject.Find("CourtFloor");
            if (courtFloor == null)
            {
                GameObject volleyballCourt = GameObject.Find("Volleyball Court");
                if (volleyballCourt != null)
                {
                    courtFloor = FindChild(volleyballCourt.transform, "CourtFloor");
                }
            }

            int floorLayer = 0;
            if (courtFloor != null)
            {
                floorLayer = courtFloor.layer;
                string layerName = LayerMask.LayerToName(floorLayer);
                Debug.Log($"✓ CourtFloor found on layer {floorLayer} ({layerName})");

                TeleportationArea teleportArea = courtFloor.GetComponent<TeleportationArea>();
                if (teleportArea != null)
                {
                    Debug.Log($"✓ TeleportationArea found - Interaction Layers: {teleportArea.interactionLayers.value}");
                }
                else
                {
                    Debug.LogError("✗ CourtFloor missing TeleportationArea component!");
                }
            }
            else
            {
                Debug.LogError("✗ CourtFloor not found!");
            }

            // Check all XR Ray Interactors
            Debug.Log("\n--- XR Ray Interactors ---");
            XRRayInteractor[] allRayInteractors = Object.FindObjectsOfType<XRRayInteractor>(true);
            Debug.Log($"Found {allRayInteractors.Length} XR Ray Interactor(s):");
            LayerMask floorLayerMask = 1 << floorLayer;

            foreach (var rayInteractor in allRayInteractors)
            {
                string goName = rayInteractor.gameObject.name;
                string fullPath = GetFullPath(rayInteractor.transform);
                LayerMask currentMask = rayInteractor.raycastMask;
                bool includesFloor = (currentMask & floorLayerMask) != 0;

                bool isController = goName.Contains("LeftHand") || goName.Contains("RightHand") || goName.Contains("Controller");
                bool isTeleport = goName.Contains("Teleport") || fullPath.Contains("Teleport");

                Debug.Log($"  - {fullPath}");
                Debug.Log($"    Type: {(isController ? "GRAB" : isTeleport ? "TELEPORT" : "UNKNOWN")}");
                Debug.Log($"    Raycast Mask includes floor layer {floorLayer}: {includesFloor}");
                Debug.Log($"    Current Raycast Mask value: {currentMask.value}");
            }

            // Check Jump Providers
            Debug.Log("\n--- Jump Providers ---");
            JumpProvider[] jumpProviders = Object.FindObjectsOfType<JumpProvider>(true);
            Debug.Log($"Found {jumpProviders.Length} JumpProvider(s):");

            if (jumpProviders.Length == 0)
            {
                Debug.LogWarning("✗ No JumpProvider found! Jump functionality may not be set up.");
            }
            else
            {
                foreach (var jumpProvider in jumpProviders)
                {
                    string fullPath = GetFullPath(jumpProvider.transform);
                    Debug.Log($"  - {fullPath}");

                    SerializedObject so = new SerializedObject(jumpProvider);
                    SerializedProperty unlimitedProp = so.FindProperty("m_UnlimitedInAirJumps");
                    SerializedProperty countProp = so.FindProperty("m_InAirJumpCount");

                    if (unlimitedProp != null)
                    {
                        Debug.Log($"    Unlimited In-Air Jumps: {unlimitedProp.boolValue}");
                    }
                    if (countProp != null)
                    {
                        Debug.Log($"    In-Air Jump Count: {countProp.intValue}");
                    }
                }
            }

            Debug.Log("\n========================================");
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child.gameObject;
                GameObject found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static string GetFullPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}

