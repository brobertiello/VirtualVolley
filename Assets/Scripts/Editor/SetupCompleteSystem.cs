using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VirtualVolley.Core.Scripts.Runtime;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Complete setup script for the simplified launcher system.
    /// Creates GameManager, BallLauncherManager, Launchers, and Scene Selection UI.
    /// </summary>
    public static class SetupCompleteSystem
    {
        [MenuItem("VirtualVolley/Setup/Setup Complete System")]
        public static void Setup()
        {
            Debug.Log("[VirtualVolley] ===== Setting Up Complete System =====\n");
            
            // 1. Create GameManager
            SetupGameManager();
            
            // 2. Create BallLauncherManager and Launchers
            SetupLaunchers();
            
            // 3. Create Scene Selection UI
            SetupSceneSelectionUI();
            
            // 4. Create Settings UI
            SetupSettingsUI();
            
            Debug.Log("[VirtualVolley] ✓ Complete system setup finished!");
            Debug.Log("[VirtualVolley] Press X button on left controller to trigger launchers");
            Debug.Log("[VirtualVolley] Use Scene Selection menu to switch between scenes\n");
        }
        
        private static void SetupGameManager()
        {
            GameManager gameManager = Object.FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                GameObject managerObj = new GameObject("Game Manager");
                gameManager = managerObj.AddComponent<GameManager>();
                Debug.Log("[VirtualVolley] ✓ Created GameManager");
            }
            else
            {
                Debug.Log("[VirtualVolley] ✓ GameManager already exists");
            }
        }
        
        private static void SetupLaunchers()
        {
            // Find or create launcher manager
            BallLauncherManager manager = Object.FindObjectOfType<BallLauncherManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("Ball Launcher Manager");
                manager = managerObj.AddComponent<BallLauncherManager>();
                Debug.Log("[VirtualVolley] ✓ Created Ball Launcher Manager");
            }
            
            // Find volleyball prefab
            GameObject volleyballPrefab = GameObject.Find("VolleyballV5");
            if (volleyballPrefab == null)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ VolleyballV5 not found in scene. Launchers will need manual assignment.");
            }
            
            // Court dimensions
            float courtWidth = 9f;
            float courtLength = 18f;
            float netHeight = 2.43f;
            
            // Find court center
            Vector3 courtCenter = Vector3.zero;
            GameObject court = GameObject.Find("Volleyball Court");
            if (court != null)
            {
                courtCenter = court.transform.position;
            }
            
            // Create launcher parent
            GameObject launcherParent = GameObject.Find("Ball Launchers");
            if (launcherParent == null)
            {
                launcherParent = new GameObject("Ball Launchers");
            }
            
            // Clear existing launchers
            BallLauncher[] existingLaunchers = launcherParent.GetComponentsInChildren<BallLauncher>();
            foreach (var launcher in existingLaunchers)
            {
                Object.DestroyImmediate(launcher.gameObject);
            }
            
            // Create launchers with simple angle/power settings
            // Service Line Launchers (Arc type)
            CreateLauncher("Service Line Launcher Left", 
                new Vector3(-courtWidth / 2 + 1f, 1.5f, -10f), 
                launcherParent.transform, volleyballPrefab, 45f, 1f, BallLauncher.LauncherType.Arc);
            
            CreateLauncher("Service Line Launcher Right", 
                new Vector3(courtWidth / 2 - 1f, 1.5f, -10f), 
                launcherParent.transform, volleyballPrefab, 45f, 1f, BallLauncher.LauncherType.Arc);
            
            // Opponent Court Launcher (Arc type)
            CreateLauncher("Opponent Court Launcher", 
                new Vector3(0, 0.5f, -5f), 
                launcherParent.transform, volleyballPrefab, 45f, 1f, BallLauncher.LauncherType.Arc);
            
            // Net Launchers (Direct type)
            CreateLauncher("Net Launcher Left", 
                new Vector3(-courtWidth / 3, netHeight + 0.4f, 0), 
                launcherParent.transform, volleyballPrefab, 0f, 1f, BallLauncher.LauncherType.Direct);
            
            CreateLauncher("Net Launcher Center", 
                new Vector3(0, netHeight + 0.4f, 0), 
                launcherParent.transform, volleyballPrefab, 0f, 1f, BallLauncher.LauncherType.Direct);
            
            CreateLauncher("Net Launcher Right", 
                new Vector3(courtWidth / 3, netHeight + 0.4f, 0), 
                launcherParent.transform, volleyballPrefab, 0f, 1f, BallLauncher.LauncherType.Direct);
            
            // Organize launchers
            BallLauncher[] allLaunchers = launcherParent.GetComponentsInChildren<BallLauncher>();
            SerializedObject so = new SerializedObject(manager);
            
            System.Collections.Generic.List<BallLauncher> serviceLine = new System.Collections.Generic.List<BallLauncher>();
            System.Collections.Generic.List<BallLauncher> net = new System.Collections.Generic.List<BallLauncher>();
            System.Collections.Generic.List<BallLauncher> opponent = new System.Collections.Generic.List<BallLauncher>();
            
            foreach (var launcher in allLaunchers)
            {
                string name = launcher.name.ToLower();
                if (name.Contains("service"))
                {
                    serviceLine.Add(launcher);
                }
                else if (name.Contains("net"))
                {
                    net.Add(launcher);
                }
                else if (name.Contains("opponent"))
                {
                    opponent.Add(launcher);
                }
            }
            
            so.FindProperty("serviceLineLaunchers").arraySize = serviceLine.Count;
            for (int i = 0; i < serviceLine.Count; i++)
            {
                so.FindProperty("serviceLineLaunchers").GetArrayElementAtIndex(i).objectReferenceValue = serviceLine[i];
            }
            
            so.FindProperty("netLaunchers").arraySize = net.Count;
            for (int i = 0; i < net.Count; i++)
            {
                so.FindProperty("netLaunchers").GetArrayElementAtIndex(i).objectReferenceValue = net[i];
            }
            
            so.FindProperty("opponentCourtLaunchers").arraySize = opponent.Count;
            for (int i = 0; i < opponent.Count; i++)
            {
                so.FindProperty("opponentCourtLaunchers").GetArrayElementAtIndex(i).objectReferenceValue = opponent[i];
            }
            
            so.ApplyModifiedProperties();
            
            // Set up input action
            SetupInputAction(manager);
            
            Debug.Log($"[VirtualVolley] ✓ Created {allLaunchers.Length} ball launchers");
        }
        
        private static void CreateLauncher(string name, Vector3 position, Transform parent, GameObject volleyballPrefab, float launchAngle, float power, BallLauncher.LauncherType launcherType)
        {
            GameObject launcherObj = new GameObject(name);
            launcherObj.transform.SetParent(parent);
            launcherObj.transform.position = position;
            
            // Add visual indicator (small sphere)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(launcherObj.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = Vector3.one * 0.1f;
            
            // Remove collider from indicator
            Collider col = indicator.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
            
            // Add BallLauncher component
            BallLauncher launcher = launcherObj.AddComponent<BallLauncher>();
            
            // Set properties via SerializedObject
            SerializedObject so = new SerializedObject(launcher);
            if (volleyballPrefab != null)
            {
                so.FindProperty("volleyballPrefab").objectReferenceValue = volleyballPrefab;
            }
            so.FindProperty("launchAngle").floatValue = launchAngle;
            so.FindProperty("power").floatValue = power;
            so.FindProperty("launcherType").intValue = (int)launcherType;
            so.FindProperty("visualIndicator").objectReferenceValue = indicator;
            so.ApplyModifiedProperties();
        }
        
        private static void SetupInputAction(BallLauncherManager manager)
        {
            // Find the XRI Default Input Actions asset
            InputActionAsset inputAsset = null;
            
            // Try to find it
            inputAsset = Resources.Load<InputActionAsset>("XRI Default Input Actions");
            
            if (inputAsset == null)
            {
                inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/XRI Default Input Actions.inputactions");
            }
            
            if (inputAsset == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                    if (asset != null && asset.name.Contains("XRI"))
                    {
                        inputAsset = asset;
                        break;
                    }
                }
            }
            
            if (inputAsset == null)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ Could not find XRI Default Input Actions asset. Please assign input manually.");
                return;
            }
            
            // Find the "XRI Left Interaction" action map and "XButton" action
            InputActionMap leftInteractionMap = inputAsset.FindActionMap("XRI Left Interaction");
            if (leftInteractionMap == null)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ XRI Left Interaction action map not found. Please assign input manually.");
                return;
            }
            
            InputAction xButtonAction = leftInteractionMap.FindAction("XButton");
            if (xButtonAction == null)
            {
                Debug.LogWarning("[VirtualVolley] ⚠ XButton action not found. Please assign input manually.");
                return;
            }
            
            // Create or find InputActionReference
            string referencePath = "Assets/Settings/BallLauncherLeftActionButton.asset";
            InputActionReference actionRef = AssetDatabase.LoadAssetAtPath<InputActionReference>(referencePath);
            
            if (actionRef == null)
            {
                actionRef = ScriptableObject.CreateInstance<InputActionReference>();
                actionRef.Set(xButtonAction);
                
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }
                
                AssetDatabase.CreateAsset(actionRef, referencePath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                actionRef.Set(xButtonAction);
                EditorUtility.SetDirty(actionRef);
                AssetDatabase.SaveAssets();
            }
            
            // Assign to manager
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("xButtonInput").objectReferenceValue = actionRef;
            so.ApplyModifiedProperties();
            
            Debug.Log("[VirtualVolley] ✓ Set up X button input");
        }
        
        private static void SetupSceneSelectionUI()
        {
            // Find or create UIEventManager
            UIEventManager uiManager = Object.FindObjectOfType<UIEventManager>();
            if (uiManager == null)
            {
                GameObject uiObj = new GameObject("UI Event Manager");
                uiManager = uiObj.AddComponent<UIEventManager>();
                Debug.Log("[VirtualVolley] ✓ Created UI Event Manager");
            }
            
            // Create Scene Selection Menu
            float menuX = -7f;
            float menuY = 1.5f;
            float menuZ = 5f;
            
            GameObject sceneMenu = CreateSceneSelectionMenu(new Vector3(menuX, menuY, menuZ), -90f);
            
            // Wire up UIEventManager
            SerializedObject so = new SerializedObject(uiManager);
            Transform scenePanel = sceneMenu.transform.Find("Panel");
            
            so.FindProperty("freePlayButton").objectReferenceValue = scenePanel?.Find("FreePlayButton")?.GetComponent<Button>();
            so.FindProperty("freeBallsButton").objectReferenceValue = scenePanel?.Find("FreeBallsButton")?.GetComponent<Button>();
            so.FindProperty("serveReceiveButton").objectReferenceValue = scenePanel?.Find("ServeReceiveButton")?.GetComponent<Button>();
            so.FindProperty("spikeReceiveButton").objectReferenceValue = scenePanel?.Find("SpikeReceiveButton")?.GetComponent<Button>();
            
            so.ApplyModifiedProperties();
            
            Debug.Log("[VirtualVolley] ✓ Created Scene Selection UI");
        }
        
        private static void SetupSettingsUI()
        {
            // Find or create UIEventManager (should already exist from SetupSceneSelectionUI)
            UIEventManager uiManager = Object.FindObjectOfType<UIEventManager>();
            if (uiManager == null)
            {
                GameObject uiObj = new GameObject("UI Event Manager");
                uiManager = uiObj.AddComponent<UIEventManager>();
                Debug.Log("[VirtualVolley] ✓ Created UI Event Manager");
            }
            
            // Create Settings Menu
            float menuX = -7f;
            float menuY = 1.5f;
            float menuZ = 5f;
            float menuSpacing = 3f;
            
            GameObject settingsMenu = CreateSettingsMenu(new Vector3(menuX, menuY, menuZ + menuSpacing), -90f);
            
            // Wire up UIEventManager with settings sliders
            SerializedObject so = new SerializedObject(uiManager);
            Transform settingsPanel = settingsMenu.transform.Find("Panel");
            
            // Find and assign all sliders
            FindAndAssignSliders(settingsMenu, so);
            
            // Assign GameManager and POVArmsPrimitives
            so.FindProperty("gameManager").objectReferenceValue = Object.FindObjectOfType<GameManager>();
            so.FindProperty("armsScript").objectReferenceValue = Object.FindObjectOfType<POVArmsPrimitives>();
            
            // Assign preset button
            Transform presetButton = settingsPanel?.Find("TitleContainer/ApplyPresetButton");
            if (presetButton != null)
            {
                so.FindProperty("applyPresetButton").objectReferenceValue = presetButton.GetComponent<Button>();
            }
            
            so.ApplyModifiedProperties();
            
            Debug.Log("[VirtualVolley] ✓ Created Settings UI");
        }
        
        private static void FindAndAssignSliders(GameObject settingsMenu, SerializedObject so)
        {
            Transform panel = settingsMenu.transform.Find("Panel");
            if (panel == null) return;
            
            // Left Shoulder column (first column)
            so.FindProperty("leftShoulderXSlider").objectReferenceValue = panel.Find("Left Shoulder X Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("leftShoulderYSlider").objectReferenceValue = panel.Find("Left Shoulder Y Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("leftShoulderZSlider").objectReferenceValue = panel.Find("Left Shoulder Z Slider/Slider")?.GetComponent<Slider>();
            
            // Right Shoulder column (second column)
            so.FindProperty("rightShoulderXSlider").objectReferenceValue = panel.Find("Right Shoulder X Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("rightShoulderYSlider").objectReferenceValue = panel.Find("Right Shoulder Y Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("rightShoulderZSlider").objectReferenceValue = panel.Find("Right Shoulder Z Slider/Slider")?.GetComponent<Slider>();
            
            // Left Hand column (third column)
            so.FindProperty("leftHandXSlider").objectReferenceValue = panel.Find("Left Hand X Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("leftHandYSlider").objectReferenceValue = panel.Find("Left Hand Y Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("leftHandZSlider").objectReferenceValue = panel.Find("Left Hand Z Slider/Slider")?.GetComponent<Slider>();
            
            // Right Hand column (fourth column)
            so.FindProperty("rightHandXSlider").objectReferenceValue = panel.Find("Right Hand X Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("rightHandYSlider").objectReferenceValue = panel.Find("Right Hand Y Slider/Slider")?.GetComponent<Slider>();
            so.FindProperty("rightHandZSlider").objectReferenceValue = panel.Find("Right Hand Z Slider/Slider")?.GetComponent<Slider>();
            
            // Arm Length slider
            so.FindProperty("armLengthSlider").objectReferenceValue = panel.Find("Arm Length Slider/Slider")?.GetComponent<Slider>();
        }
        
        private static GameObject CreateSceneSelectionMenu(Vector3 position, float rotationY)
        {
            GameObject root = CreateMenuRoot("Scene Selection Menu", position, rotationY);
            GameObject panel = CreatePanel(root, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            
            CreateTitle(panel, "Scene Selection", 48);
            
            // Create buttons
            float buttonY = 0.6f;
            float buttonHeight = 0.1f;
            float buttonSpacing = 0.12f;
            
            CreateSceneButton(panel, "Free Play", GameManager.Scene.FreePlay, new Vector2(0.1f, buttonY - buttonHeight), new Vector2(0.9f, buttonY));
            CreateSceneButton(panel, "Free Balls", GameManager.Scene.FreeBalls, new Vector2(0.1f, buttonY - buttonHeight - buttonSpacing), new Vector2(0.9f, buttonY - buttonSpacing));
            CreateSceneButton(panel, "Serve Receive", GameManager.Scene.ServeReceive, new Vector2(0.1f, buttonY - buttonHeight - buttonSpacing * 2), new Vector2(0.9f, buttonY - buttonSpacing * 2));
            CreateSceneButton(panel, "Spike Receive", GameManager.Scene.SpikeReceive, new Vector2(0.1f, buttonY - buttonHeight - buttonSpacing * 3), new Vector2(0.9f, buttonY - buttonSpacing * 3));
            
            return root;
        }
        
        private static void CreateSceneButton(GameObject parent, string text, GameManager.Scene scene, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject($"{text}Button");
            buttonObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(10, 5);
            rect.offsetMax = new Vector2(-10, -5);
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f); // Blue
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 32;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            
            // Add click handler
            button.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CurrentScene = scene;
                }
            });
        }
        
        private static GameObject CreateMenuRoot(string name, Vector3 position, float rotationY = 0f)
        {
            GameObject root = new GameObject(name);
            
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 800);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0, rotationY, 0);
            root.transform.localScale = Vector3.one * 0.002f;
            
            return root;
        }
        
        private static GameObject CreatePanel(GameObject parent, Color color)
        {
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = color;
            CreateBorderImages(panel);
            
            return panel;
        }
        
        private static void CreateBorderImages(GameObject panel)
        {
            float borderWidth = 3f;
            
            // Top border
            CreateBorder(panel, "TopBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, borderWidth));
            // Bottom border
            CreateBorder(panel, "BottomBorder", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, borderWidth));
            // Left border
            CreateBorder(panel, "LeftBorder", new Vector2(0, 0), new Vector2(0, 1), new Vector2(borderWidth, 0));
            // Right border
            CreateBorder(panel, "RightBorder", new Vector2(1, 0), new Vector2(1, 1), new Vector2(borderWidth, 0));
        }
        
        private static void CreateBorder(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            GameObject border = new GameObject(name);
            border.transform.SetParent(parent.transform, false);
            RectTransform rect = border.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;
            Image image = border.AddComponent<Image>();
            image.color = Color.white;
        }
        
        private static GameObject CreateSettingsMenu(Vector3 position, float rotationY = 0f)
        {
            GameObject root = CreateMenuRoot("Settings Menu", position, rotationY);
            GameObject panel = CreatePanel(root, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            
            // Make canvas wider for 4 columns
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1200, 800); // Wider canvas
            
            // Title container
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(panel.transform, false);
            RectTransform titleContainerRect = titleContainer.AddComponent<RectTransform>();
            titleContainerRect.anchorMin = new Vector2(0.05f, 0.85f);
            titleContainerRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleContainerRect.sizeDelta = Vector2.zero;
            titleContainerRect.anchoredPosition = Vector2.zero;
            
            // Title
            CreateTitle(titleContainer, "Settings", 48);
            
            // Preset button (in line with title)
            GameObject presetButton = new GameObject("ApplyPresetButton");
            presetButton.transform.SetParent(titleContainer.transform, false);
            RectTransform presetRect = presetButton.AddComponent<RectTransform>();
            presetRect.anchorMin = new Vector2(0.75f, 0);
            presetRect.anchorMax = new Vector2(1, 1);
            presetRect.sizeDelta = Vector2.zero;
            presetRect.anchoredPosition = Vector2.zero;
            presetRect.offsetMin = new Vector2(10, 5);
            presetRect.offsetMax = new Vector2(-10, -5);
            
            Image presetImage = presetButton.AddComponent<Image>();
            presetImage.color = new Color(0.2f, 0.6f, 1f); // Blue
            
            Button presetBtn = presetButton.AddComponent<Button>();
            presetBtn.targetGraphic = presetImage;
            
            // Button text
            GameObject presetTextObj = new GameObject("Text");
            presetTextObj.transform.SetParent(presetButton.transform, false);
            RectTransform presetTextRect = presetTextObj.AddComponent<RectTransform>();
            presetTextRect.anchorMin = Vector2.zero;
            presetTextRect.anchorMax = Vector2.one;
            presetTextRect.sizeDelta = Vector2.zero;
            presetTextRect.offsetMin = new Vector2(5, 5);
            presetTextRect.offsetMax = new Vector2(-5, -5);
            
            TextMeshProUGUI presetText = presetTextObj.AddComponent<TextMeshProUGUI>();
            presetText.text = "Apply Preset";
            presetText.fontSize = 28;
            presetText.alignment = TextAlignmentOptions.Center;
            presetText.color = Color.white;
            
            // Column headers
            string[] columnHeaders = { "Left Shoulder", "Right Shoulder", "Left Hand", "Right Hand" };
            float headerY = 0.75f;
            float headerHeight = 0.08f;
            float columnWidth = 0.22f; // 4 columns with spacing
            float startX = 0.05f;
            float columnSpacing = 0.02f;
            
            // Create column headers
            for (int col = 0; col < 4; col++)
            {
                float xMin = startX + (col * (columnWidth + columnSpacing));
                float xMax = xMin + columnWidth;
                
                GameObject headerObj = new GameObject($"{columnHeaders[col]} Header");
                headerObj.transform.SetParent(panel.transform, false);
                RectTransform headerRect = headerObj.AddComponent<RectTransform>();
                headerRect.anchorMin = new Vector2(xMin, headerY - headerHeight);
                headerRect.anchorMax = new Vector2(xMax, headerY);
                headerRect.sizeDelta = Vector2.zero;
                headerRect.anchoredPosition = Vector2.zero;
                headerRect.offsetMin = new Vector2(5, 2);
                headerRect.offsetMax = new Vector2(-5, -2);
                
                TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
                headerText.text = columnHeaders[col];
                headerText.fontSize = 28;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = Color.white;
                headerText.fontStyle = FontStyles.Bold;
            }
            
            // Create sliders in 4 columns (X, Y, Z for each)
            string[] axisLabels = { "X", "Y", "Z" };
            float sliderHeight = 0.08f;
            float sliderStartY = 0.65f;
            float sliderSpacing = sliderHeight + 0.02f;
            
            for (int col = 0; col < 4; col++)
            {
                float xMin = startX + (col * (columnWidth + columnSpacing));
                float xMax = xMin + columnWidth;
                
                for (int row = 0; row < 3; row++)
                {
                    float yPos = sliderStartY - (row * sliderSpacing);
                    string label = axisLabels[row];
                    string fullName = $"{columnHeaders[col]} {label} Slider";
                    CreateSettingsSlider(panel, label, new Vector2(xMin, yPos - sliderHeight), new Vector2(xMax, yPos), true, fullName);
                }
            }
            
            // Arm Length slider at the bottom (full width)
            CreateSettingsSlider(panel, "Arm Length", new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.25f), false, "Arm Length Slider");
            
            return root;
        }
        
        private static void CreateSettingsSlider(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax, bool compact = false, string objectName = null)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                objectName = $"{label} Slider";
            }
            GameObject sliderObj = new GameObject(objectName);
            sliderObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);
            
            // Label area (compact mode)
            if (compact)
            {
                // Compact mode: label above, value and reset on the side
                GameObject labelContainer = new GameObject("LabelContainer");
                labelContainer.transform.SetParent(sliderObj.transform, false);
                RectTransform labelContainerRect = labelContainer.AddComponent<RectTransform>();
                labelContainerRect.anchorMin = new Vector2(0, 0.7f);
                labelContainerRect.anchorMax = new Vector2(1, 1);
                labelContainerRect.sizeDelta = Vector2.zero;
                labelContainerRect.anchoredPosition = Vector2.zero;
                
                // Label text
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(labelContainer.transform, false);
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(0.6f, 1);
                labelRect.sizeDelta = Vector2.zero;
                labelRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = label;
                labelText.fontSize = 18;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                labelText.color = Color.white;
                
                // Value display
                GameObject valueDisplay = new GameObject("ValueDisplay");
                valueDisplay.transform.SetParent(labelContainer.transform, false);
                RectTransform valueRect = valueDisplay.AddComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0.6f, 0);
                valueRect.anchorMax = new Vector2(0.85f, 1);
                valueRect.sizeDelta = Vector2.zero;
                valueRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI valueText = valueDisplay.AddComponent<TextMeshProUGUI>();
                valueText.text = "0.000";
                valueText.fontSize = 16;
                valueText.alignment = TextAlignmentOptions.MidlineLeft;
                valueText.color = new Color(0.8f, 0.8f, 0.8f);
                
                // Reset button
                GameObject resetButton = new GameObject("ResetButton");
                resetButton.transform.SetParent(labelContainer.transform, false);
                RectTransform resetRect = resetButton.AddComponent<RectTransform>();
                resetRect.anchorMin = new Vector2(0.85f, 0.1f);
                resetRect.anchorMax = new Vector2(1, 0.9f);
                resetRect.sizeDelta = Vector2.zero;
                resetRect.anchoredPosition = Vector2.zero;
                resetRect.offsetMin = new Vector2(2, 2);
                resetRect.offsetMax = new Vector2(-2, -2);
                
                Image resetImage = resetButton.AddComponent<Image>();
                resetImage.color = new Color(0.4f, 0.4f, 0.4f);
                
                Button resetBtn = resetButton.AddComponent<Button>();
                resetBtn.targetGraphic = resetImage;
                
                GameObject resetTextObj = new GameObject("Text");
                resetTextObj.transform.SetParent(resetButton.transform, false);
                RectTransform resetTextRect = resetTextObj.AddComponent<RectTransform>();
                resetTextRect.anchorMin = Vector2.zero;
                resetTextRect.anchorMax = Vector2.one;
                resetTextRect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI resetText = resetTextObj.AddComponent<TextMeshProUGUI>();
                resetText.text = "R";
                resetText.fontSize = 14;
                resetText.alignment = TextAlignmentOptions.Center;
                resetText.color = Color.white;
            }
            else
            {
                // Non-compact mode: label on left, slider on right
                GameObject labelContainer = new GameObject("LabelContainer");
                labelContainer.transform.SetParent(sliderObj.transform, false);
                RectTransform labelContainerRect = labelContainer.AddComponent<RectTransform>();
                labelContainerRect.anchorMin = new Vector2(0, 0);
                labelContainerRect.anchorMax = new Vector2(0.35f, 1);
                labelContainerRect.sizeDelta = Vector2.zero;
                labelContainerRect.anchoredPosition = Vector2.zero;
                
                // Label text
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(labelContainer.transform, false);
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(0.7f, 1);
                labelRect.sizeDelta = Vector2.zero;
                labelRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = label;
                labelText.fontSize = 22;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                labelText.color = Color.white;
                
                // Value display
                GameObject valueDisplay = new GameObject("ValueDisplay");
                valueDisplay.transform.SetParent(labelContainer.transform, false);
                RectTransform valueRect = valueDisplay.AddComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0.7f, 0);
                valueRect.anchorMax = new Vector2(0.85f, 1);
                valueRect.sizeDelta = Vector2.zero;
                valueRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI valueText = valueDisplay.AddComponent<TextMeshProUGUI>();
                valueText.text = "0.000";
                valueText.fontSize = 18;
                valueText.alignment = TextAlignmentOptions.MidlineLeft;
                valueText.color = new Color(0.8f, 0.8f, 0.8f);
                
                // Reset button
                GameObject resetButton = new GameObject("ResetButton");
                resetButton.transform.SetParent(labelContainer.transform, false);
                RectTransform resetRect = resetButton.AddComponent<RectTransform>();
                resetRect.anchorMin = new Vector2(0.85f, 0.2f);
                resetRect.anchorMax = new Vector2(1, 0.8f);
                resetRect.sizeDelta = Vector2.zero;
                resetRect.anchoredPosition = Vector2.zero;
                resetRect.offsetMin = new Vector2(2, 2);
                resetRect.offsetMax = new Vector2(-2, -2);
                
                Image resetImage = resetButton.AddComponent<Image>();
                resetImage.color = new Color(0.4f, 0.4f, 0.4f);
                
                Button resetBtn = resetButton.AddComponent<Button>();
                resetBtn.targetGraphic = resetImage;
                
                GameObject resetTextObj = new GameObject("Text");
                resetTextObj.transform.SetParent(resetButton.transform, false);
                RectTransform resetTextRect = resetTextObj.AddComponent<RectTransform>();
                resetTextRect.anchorMin = Vector2.zero;
                resetTextRect.anchorMax = Vector2.one;
                resetTextRect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI resetText = resetTextObj.AddComponent<TextMeshProUGUI>();
                resetText.text = "R";
                resetText.fontSize = 16;
                resetText.alignment = TextAlignmentOptions.Center;
                resetText.color = Color.white;
            }
            
            // Slider
            GameObject sliderControl = new GameObject("Slider");
            sliderControl.transform.SetParent(sliderObj.transform, false);
            RectTransform sliderRect = sliderControl.AddComponent<RectTransform>();
            if (compact)
            {
                sliderRect.anchorMin = new Vector2(0, 0);
                sliderRect.anchorMax = new Vector2(1, 0.7f);
            }
            else
            {
                sliderRect.anchorMin = new Vector2(0.35f, 0);
                sliderRect.anchorMax = new Vector2(1, 1);
            }
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.offsetMin = new Vector2(compact ? 5 : 10, 0);
            sliderRect.offsetMax = new Vector2(compact ? -5 : 0, 0);
            
            // Slider background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderControl.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Slider fill
            GameObject fill = new GameObject("Fill Area");
            fill.transform.SetParent(sliderControl.transform, false);
            RectTransform fillAreaRect = fill.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.anchoredPosition = Vector2.zero;
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fill.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f);
            
            // Slider handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderControl.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0);
            handleRect.anchorMax = new Vector2(0.5f, 1);
            handleRect.sizeDelta = new Vector2(20, 0);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Slider component
            Slider slider = sliderControl.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = -0.5f;
            slider.maxValue = 0.5f;
            slider.value = 0f;
        }
        
        private static void CreateTitle(GameObject parent, string text, float fontSize)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = titleObj.AddComponent<RectTransform>();
            // If parent is TitleContainer, use full width minus button space
            if (parent.name == "TitleContainer")
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0.7f, 1);
            }
            else
            {
                rect.anchorMin = new Vector2(0.05f, 0.85f);
                rect.anchorMax = new Vector2(0.95f, 0.95f);
            }
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = text;
            title.fontSize = fontSize;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
        }
    }
}

