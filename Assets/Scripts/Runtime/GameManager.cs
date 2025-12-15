using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Manages game state including current scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        public enum Scene
        {
            FreePlay,
            FreeBalls,
            ServeReceive,
            SpikeReceive
        }
        
        [Header("Current State")]
        [SerializeField] private Scene currentScene = Scene.FreePlay;
        
        public Scene CurrentScene
        {
            get => currentScene;
            set
            {
                if (currentScene != value)
                {
                    currentScene = value;
                    OnSceneChanged?.Invoke(value);
                    Debug.Log($"[GameManager] Scene changed to: {value}");
                }
            }
        }
        
        // Events
        public System.Action<Scene> OnSceneChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Initialize default
            CurrentScene = Scene.FreePlay;
        }
    }
}
