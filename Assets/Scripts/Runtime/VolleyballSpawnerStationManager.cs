using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Manages all volleyball spawner stations and coordinates their shooting.
    /// </summary>
    public class VolleyballSpawnerStationManager : MonoBehaviour
    {
        [Header("Spawner Stations")]
        [Tooltip("List of spawner stations to manage")]
        [SerializeField] private List<VolleyballSpawnerStation> spawnerStations = new List<VolleyballSpawnerStation>();
        
        [Header("Input Settings")]
        [Tooltip("Input Action Reference for action button (to trigger all spawners)")]
        [SerializeField] private InputActionReference actionButtonInput;
        
        [Header("Auto Spawn Settings")]
        [Tooltip("Automatically spawn balls at stations on start")]
        [SerializeField] private bool autoSpawnOnStart = true;
        
        private bool wasButtonPressed = false;
        
        private void Awake()
        {
            // Only find stations if none are explicitly assigned in Inspector
            // This prevents picking up unwanted stations
            if (spawnerStations.Count == 0)
            {
                // Only find stations that are above a certain height (in the air, not on ground)
                VolleyballSpawnerStation[] allStations = FindObjectsOfType<VolleyballSpawnerStation>();
                foreach (var station in allStations)
                {
                    // Only add stations that are above 2 units (in the air)
                    if (station != null && station.transform.position.y > 2f)
                    {
                        spawnerStations.Add(station);
                    }
                }
                Debug.Log($"[VolleyballSpawnerStationManager] Found {spawnerStations.Count} spawner stations (above ground)");
            }
            
            // Enable input action if assigned
            if (actionButtonInput != null)
            {
                actionButtonInput.action.Enable();
            }
        }
        
        private void Start()
        {
            // Auto spawn balls if enabled
            if (autoSpawnOnStart)
            {
                SpawnBallsAtAllStations();
            }
        }
        
        private void Update()
        {
            CheckActionButton();
        }
        
        private void CheckActionButton()
        {
            if (actionButtonInput == null || actionButtonInput.action == null)
            {
                Debug.LogWarning("[VolleyballSpawnerStationManager] Action button input not assigned!");
                return;
            }
            
            // Check for button press using multiple methods
            bool isPressed = actionButtonInput.action.IsPressed() || 
                            actionButtonInput.action.WasPressedThisFrame() ||
                            actionButtonInput.action.ReadValue<float>() > 0.5f;
            
            // Detect button press (was not pressed, now is pressed)
            if (isPressed && !wasButtonPressed)
            {
                Debug.Log("[VolleyballSpawnerStationManager] Action button pressed - triggering random spawner!");
                TriggerRandomSpawner();
            }
            
            wasButtonPressed = isPressed;
        }
        
        /// <summary>
        /// Triggers a random spawner station to start countdown and shoot.
        /// Only picks from stations that are not currently shooting or counting down.
        /// </summary>
        public void TriggerRandomSpawner()
        {
            // Filter to only available stations (not counting down or shooting)
            List<VolleyballSpawnerStation> availableStations = new List<VolleyballSpawnerStation>();
            foreach (var station in spawnerStations)
            {
                if (station != null && station.IsAvailable())
                {
                    availableStations.Add(station);
                }
            }
            
            if (availableStations.Count == 0)
            {
                Debug.Log("[VolleyballSpawnerStationManager] All stations are busy (counting down or shooting). Skipping trigger.");
                return;
            }
            
            // Pick a random station
            int randomIndex = Random.Range(0, availableStations.Count);
            VolleyballSpawnerStation selectedStation = availableStations[randomIndex];
            
            selectedStation.StartCountdownAndShoot();
            Debug.Log($"[VolleyballSpawnerStationManager] Triggered random station: {selectedStation.name} ({availableStations.Count} available)");
        }
        
        /// <summary>
        /// Spawns balls at all stations.
        /// </summary>
        public void SpawnBallsAtAllStations()
        {
            foreach (var station in spawnerStations)
            {
                if (station != null)
                {
                    station.SpawnAndHoldBall();
                }
            }
            
            Debug.Log($"[VolleyballSpawnerStationManager] Spawned balls at {spawnerStations.Count} stations");
        }
        
        /// <summary>
        /// Adds a spawner station to the manager.
        /// </summary>
        public void AddSpawnerStation(VolleyballSpawnerStation station)
        {
            if (station != null && !spawnerStations.Contains(station))
            {
                spawnerStations.Add(station);
            }
        }
        
        private void OnDestroy()
        {
            // Disable input action
            if (actionButtonInput != null && actionButtonInput.action != null)
            {
                actionButtonInput.action.Disable();
            }
        }
    }
}

