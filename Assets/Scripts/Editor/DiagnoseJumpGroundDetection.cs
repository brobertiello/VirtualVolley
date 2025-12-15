using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Diagnoses jump ground detection issues.
    /// </summary>
    public static class DiagnoseJumpGroundDetection
    {
        [MenuItem("VirtualVolley/Diagnostics/VR/Jump Ground Detection")]
        public static void Diagnose()
        {
            Debug.Log("========================================");
            Debug.Log("JUMP GROUND DETECTION DIAGNOSIS");
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
            }
            else
            {
                Debug.LogError("✗ CourtFloor not found!");
            }

            // Check GravityProvider
            Debug.Log("\n--- Gravity Provider ---");
            GravityProvider[] gravityProviders = Object.FindObjectsOfType<GravityProvider>(true);
            Debug.Log($"Found {gravityProviders.Length} GravityProvider(s):");

            if (gravityProviders.Length == 0)
            {
                Debug.LogWarning("✗ No GravityProvider found! Jump will not work.");
            }
            else
            {
                foreach (var gravityProvider in gravityProviders)
                {
                    string fullPath = GetFullPath(gravityProvider.transform);
                    Debug.Log($"  - {fullPath}");

                    SerializedObject so = new SerializedObject(gravityProvider);
                    SerializedProperty layerMaskProp = so.FindProperty("m_SphereCastLayerMask");
                    SerializedProperty distanceProp = so.FindProperty("m_SphereCastDistanceBuffer");
                    SerializedProperty radiusProp = so.FindProperty("m_SphereCastRadius");

                    if (layerMaskProp != null)
                    {
                        LayerMask currentMask = layerMaskProp.intValue;
                        LayerMask floorLayerMask = 1 << floorLayer;
                        bool includesFloor = (currentMask & floorLayerMask) != 0;
                        
                        Debug.Log($"    Sphere Cast Layer Mask: {currentMask.value}");
                        Debug.Log($"    Includes floor layer {floorLayer}: {includesFloor}");
                        
                        if (!includesFloor && floorLayer > 0)
                        {
                            Debug.LogWarning($"    ⚠ Floor layer {floorLayer} is NOT included in ground detection!");
                        }
                    }

                    if (distanceProp != null)
                    {
                        Debug.Log($"    Sphere Cast Distance Buffer: {distanceProp.floatValue}");
                    }

                    if (radiusProp != null)
                    {
                        Debug.Log($"    Sphere Cast Radius: {radiusProp.floatValue}");
                    }
                }
            }

            // Check JumpProvider
            Debug.Log("\n--- Jump Provider ---");
            JumpProvider[] jumpProviders = Object.FindObjectsOfType<JumpProvider>(true);
            Debug.Log($"Found {jumpProviders.Length} JumpProvider(s):");

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

