using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Editor
{
    /// <summary>
    /// Fixes the scene selection UI to properly highlight current scene and handle button clicks.
    /// </summary>
    public static class FixSceneSelectionUI
    {
        [MenuItem("VirtualVolley/UI Setup/Fix Scene Selection UI")]
        public static void Fix()
        {
            Debug.Log("[VirtualVolley] ===== Fixing Scene Selection UI =====\n");
            
            // Find UIEventManager
            UIEventManager uiManager = Object.FindObjectOfType<UIEventManager>();
            if (uiManager == null)
            {
                Debug.LogError("[VirtualVolley] UIEventManager not found! Please run Setup Complete System first.");
                return;
            }
            
            // Find Scene Selection Menu
            GameObject sceneMenu = GameObject.Find("Scene Selection Menu");
            if (sceneMenu == null)
            {
                Debug.LogError("[VirtualVolley] Scene Selection Menu not found! Please run Setup Complete System first.");
                return;
            }
            
            Transform panel = sceneMenu.transform.Find("Panel");
            if (panel == null)
            {
                Debug.LogError("[VirtualVolley] Panel not found in Scene Selection Menu!");
                return;
            }
            
            // Find all buttons
            Button freePlayBtn = panel.Find("Free PlayButton")?.GetComponent<Button>();
            Button freeBallsBtn = panel.Find("Free BallsButton")?.GetComponent<Button>();
            Button serveReceiveBtn = panel.Find("Serve ReceiveButton")?.GetComponent<Button>();
            Button spikeReceiveBtn = panel.Find("Spike ReceiveButton")?.GetComponent<Button>();
            
            // Remove all existing listeners
            if (freePlayBtn != null) freePlayBtn.onClick.RemoveAllListeners();
            if (freeBallsBtn != null) freeBallsBtn.onClick.RemoveAllListeners();
            if (serveReceiveBtn != null) serveReceiveBtn.onClick.RemoveAllListeners();
            if (spikeReceiveBtn != null) spikeReceiveBtn.onClick.RemoveAllListeners();
            
            // Add new click handlers that properly set the scene
            if (freePlayBtn != null)
            {
                freePlayBtn.onClick.AddListener(() => {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.FreePlay;
                        Debug.Log("[VirtualVolley] Scene set to Free Play");
                    }
                });
            }
            
            if (freeBallsBtn != null)
            {
                freeBallsBtn.onClick.AddListener(() => {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.FreeBalls;
                        Debug.Log("[VirtualVolley] Scene set to Free Balls");
                    }
                });
            }
            
            if (serveReceiveBtn != null)
            {
                serveReceiveBtn.onClick.AddListener(() => {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.ServeReceive;
                        Debug.Log("[VirtualVolley] Scene set to Serve Receive");
                    }
                });
            }
            
            if (spikeReceiveBtn != null)
            {
                spikeReceiveBtn.onClick.AddListener(() => {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CurrentScene = GameManager.Scene.SpikeReceive;
                        Debug.Log("[VirtualVolley] Scene set to Spike Receive");
                    }
                });
            }
            
            // Update UIEventManager references
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("freePlayButton").objectReferenceValue = freePlayBtn;
            so.FindProperty("freeBallsButton").objectReferenceValue = freeBallsBtn;
            so.FindProperty("serveReceiveButton").objectReferenceValue = serveReceiveBtn;
            so.FindProperty("spikeReceiveButton").objectReferenceValue = spikeReceiveBtn;
            so.ApplyModifiedProperties();
            
            // Force UI to update button colors based on current scene
            if (GameManager.Instance != null)
            {
                // Use the public RefreshSceneButtons method
                uiManager.RefreshSceneButtons();
            }
            
            Debug.Log("[VirtualVolley] ✓ Scene Selection UI fixed!");
            Debug.Log("[VirtualVolley] Buttons should now properly switch scenes and highlight correctly\n");
        }
    }
}

