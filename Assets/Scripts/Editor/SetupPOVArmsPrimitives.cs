using UnityEditor;
using UnityEngine;
using Unity.XR.CoreUtils;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Sets up POV Arms using simple primitives (spheres and cylinders).
    /// </summary>
    public static class SetupPOVArmsPrimitives
    {
        [MenuItem("VirtualVolley/Arms Setup/Setup POV Arms (Primitives)")]
        public static void Setup()
        {
            Debug.Log("[VirtualVolley] ===== Setting Up POV Arms Primitives =====\n");
            
            // Remove old POVArms if it exists
            GameObject oldArms = GameObject.Find("POVArms");
            if (oldArms != null)
            {
                Debug.Log("  Removing old POVArms...");
                Object.DestroyImmediate(oldArms);
            }
            
            // Create new POVArms GameObject
            GameObject povArms = new GameObject("POVArms");
            povArms.transform.position = Vector3.zero;
            povArms.transform.rotation = Quaternion.identity;
            povArms.transform.localScale = Vector3.one;
            
            // Add POVArmsPrimitives component
            POVArmsPrimitives armsScript = povArms.AddComponent<POVArmsPrimitives>();
            
            // Set default values
            SerializedObject so = new SerializedObject(armsScript);
            so.FindProperty("upperArmLength").floatValue = 0.3f;
            so.FindProperty("forearmLength").floatValue = 0.3f;
            so.FindProperty("armThickness").floatValue = 0.02f;
            so.FindProperty("handSize").floatValue = 0.03f;
            so.FindProperty("leftShoulderOffset").vector3Value = new Vector3(-0.2f, -0.1f, 0.1f);
            so.FindProperty("rightShoulderOffset").vector3Value = new Vector3(0.2f, -0.1f, 0.1f);
            so.ApplyModifiedProperties();
            
            Debug.Log("✓ Created POVArms with POVArmsPrimitives component");
            Debug.Log("✓ Default values set");
            
            // Force creation of primitives in edit mode
            CreatePrimitivesInEditMode(armsScript);
            
            Debug.Log("\n[VirtualVolley] ===== Setup Complete =====\n");
            Debug.Log("Arms primitives have been created!");
            Debug.Log("You can adjust the dimensions and materials in the Inspector.\n");
            
            // Select the new object
            Selection.activeGameObject = povArms;
        }
        
        private static void CreatePrimitivesInEditMode(POVArmsPrimitives script)
        {
            // Use reflection to call the private CreateArmPrimitives method
            var method = typeof(POVArmsPrimitives).GetMethod("CreateArmPrimitives", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(script, null);
                Debug.Log("✓ Created arm primitives in edit mode");
            }
            else
            {
                // Fallback: create primitives manually
                CreatePrimitivesManually(script);
            }
        }
        
        private static void CreatePrimitivesManually(POVArmsPrimitives script)
        {
            GameObject povArms = script.gameObject;
            
            // Get values using reflection
            var upperArmLengthProp = typeof(POVArmsPrimitives).GetField("upperArmLength", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var forearmLengthProp = typeof(POVArmsPrimitives).GetField("forearmLength", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var armThicknessProp = typeof(POVArmsPrimitives).GetField("armThickness", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var handSizeProp = typeof(POVArmsPrimitives).GetField("handSize", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            float upperArmLength = (float)(upperArmLengthProp?.GetValue(script) ?? 0.3f);
            float forearmLength = (float)(forearmLengthProp?.GetValue(script) ?? 0.3f);
            float armThickness = (float)(armThicknessProp?.GetValue(script) ?? 0.02f);
            float handSize = (float)(handSizeProp?.GetValue(script) ?? 0.03f);
            
            // Create left arm
            GameObject leftUpperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftUpperArm.name = "Left Upper Arm";
            leftUpperArm.transform.SetParent(povArms.transform);
            leftUpperArm.transform.localScale = new Vector3(armThickness * 2, upperArmLength / 2, armThickness * 2);
            
            GameObject leftForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftForearm.name = "Left Forearm";
            leftForearm.transform.SetParent(povArms.transform);
            leftForearm.transform.localScale = new Vector3(armThickness * 2, forearmLength / 2, armThickness * 2);
            
            GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftHand.name = "Left Hand";
            leftHand.transform.SetParent(povArms.transform);
            leftHand.transform.localScale = Vector3.one * handSize * 2;
            
            GameObject leftElbowObj = new GameObject("Left Elbow");
            leftElbowObj.transform.SetParent(povArms.transform);
            
            // Create right arm
            GameObject rightUpperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightUpperArm.name = "Right Upper Arm";
            rightUpperArm.transform.SetParent(povArms.transform);
            rightUpperArm.transform.localScale = new Vector3(armThickness * 2, upperArmLength / 2, armThickness * 2);
            
            GameObject rightForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightForearm.name = "Right Forearm";
            rightForearm.transform.SetParent(povArms.transform);
            rightForearm.transform.localScale = new Vector3(armThickness * 2, forearmLength / 2, armThickness * 2);
            
            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHand.name = "Right Hand";
            rightHand.transform.SetParent(povArms.transform);
            rightHand.transform.localScale = Vector3.one * handSize * 2;
            
            GameObject rightElbowObj = new GameObject("Right Elbow");
            rightElbowObj.transform.SetParent(povArms.transform);
            
            // Remove colliders
            Object.DestroyImmediate(leftUpperArm.GetComponent<Collider>());
            Object.DestroyImmediate(leftForearm.GetComponent<Collider>());
            Object.DestroyImmediate(leftHand.GetComponent<Collider>());
            Object.DestroyImmediate(rightUpperArm.GetComponent<Collider>());
            Object.DestroyImmediate(rightForearm.GetComponent<Collider>());
            Object.DestroyImmediate(rightHand.GetComponent<Collider>());
            
            Debug.Log("✓ Created arm primitives manually");
        }
    }
}

