using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// UI Menu controller for triggering volleyball spawner stations.
    /// </summary>
    public class SpawnerStationUIMenu : MonoBehaviour
    {
        [Header("Spawner Stations")]
        [Tooltip("Reference to the spawner station manager")]
        [SerializeField] private VolleyballSpawnerStationManager stationManager;
        
        [Tooltip("List of spawner stations (if manager not assigned)")]
        [SerializeField] private VolleyballSpawnerStation[] stations = new VolleyballSpawnerStation[3];

        [Header("UI References")]
        [Tooltip("Button for Shooter 1")]
        [SerializeField] private Button shooter1Button;
        
        [Tooltip("Button for Shooter 2")]
        [SerializeField] private Button shooter2Button;
        
        [Tooltip("Button for Shooter 3")]
        [SerializeField] private Button shooter3Button;

        [Header("Button Labels")]
        [Tooltip("Text for Shooter 1 button")]
        [SerializeField] private TextMeshProUGUI shooter1ButtonText;
        
        [Tooltip("Text for Shooter 2 button")]
        [SerializeField] private TextMeshProUGUI shooter2ButtonText;
        
        [Tooltip("Text for Shooter 3 button")]
        [SerializeField] private TextMeshProUGUI shooter3ButtonText;

        private void Awake()
        {
            // Find manager if not assigned
            if (stationManager == null)
            {
                stationManager = FindObjectOfType<VolleyballSpawnerStationManager>();
            }

            // Find stations if not assigned
            if (stations[0] == null || stations[1] == null || stations[2] == null)
            {
                FindStations();
            }

            SetupButtons();
        }

        private void FindStations()
        {
            if (stationManager != null)
            {
                // Get stations from manager via reflection
                var stationsField = typeof(VolleyballSpawnerStationManager).GetField("spawnerStations", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stationsField != null)
                {
                    var managerStations = stationsField.GetValue(stationManager) as System.Collections.Generic.List<VolleyballSpawnerStation>;
                    if (managerStations != null && managerStations.Count >= 3)
                    {
                        stations[0] = managerStations[0];
                        stations[1] = managerStations[1];
                        stations[2] = managerStations[2];
                    }
                }
            }

            // Fallback: find all stations in scene
            if (stations[0] == null || stations[1] == null || stations[2] == null)
            {
                VolleyballSpawnerStation[] allStations = FindObjectsOfType<VolleyballSpawnerStation>();
                for (int i = 0; i < Mathf.Min(3, allStations.Length); i++)
                {
                    stations[i] = allStations[i];
                }
            }
        }

        private void SetupButtons()
        {
            // Setup Shooter 1 button
            if (shooter1Button != null)
            {
                shooter1Button.onClick.RemoveAllListeners();
                shooter1Button.onClick.AddListener(() => TriggerShooter(0));
                
                if (shooter1ButtonText != null)
                {
                    shooter1ButtonText.text = stations[0] != null ? $"Shooter 1\n{stations[0].name}" : "Shooter 1";
                }
            }

            // Setup Shooter 2 button
            if (shooter2Button != null)
            {
                shooter2Button.onClick.RemoveAllListeners();
                shooter2Button.onClick.AddListener(() => TriggerShooter(1));
                
                if (shooter2ButtonText != null)
                {
                    shooter2ButtonText.text = stations[1] != null ? $"Shooter 2\n{stations[1].name}" : "Shooter 2";
                }
            }

            // Setup Shooter 3 button
            if (shooter3Button != null)
            {
                shooter3Button.onClick.RemoveAllListeners();
                shooter3Button.onClick.AddListener(() => TriggerShooter(2));
                
                if (shooter3ButtonText != null)
                {
                    shooter3ButtonText.text = stations[2] != null ? $"Shooter 3\n{stations[2].name}" : "Shooter 3";
                }
            }
        }

        private void TriggerShooter(int index)
        {
            if (index < 0 || index >= stations.Length)
            {
                Debug.LogWarning($"[SpawnerStationUIMenu] Invalid shooter index: {index}");
                return;
            }

            VolleyballSpawnerStation station = stations[index];
            if (station == null)
            {
                Debug.LogWarning($"[SpawnerStationUIMenu] Shooter {index + 1} is not assigned!");
                return;
            }

            if (station.IsAvailable())
            {
                station.StartCountdownAndShoot();
                Debug.Log($"[SpawnerStationUIMenu] Triggered Shooter {index + 1}: {station.name}");
            }
            else
            {
                Debug.Log($"[SpawnerStationUIMenu] Shooter {index + 1} is busy (counting down or shooting)");
            }
        }

        /// <summary>
        /// Updates button states based on station availability.
        /// </summary>
        public void UpdateButtonStates()
        {
            if (shooter1Button != null)
            {
                shooter1Button.interactable = stations[0] != null && stations[0].IsAvailable();
            }

            if (shooter2Button != null)
            {
                shooter2Button.interactable = stations[1] != null && stations[1].IsAvailable();
            }

            if (shooter3Button != null)
            {
                shooter3Button.interactable = stations[2] != null && stations[2].IsAvailable();
            }
        }

        private void Update()
        {
            // Update button states periodically
            UpdateButtonStates();
        }
    }
}

