using UnityEngine;
using System.Collections;
using VirtualVolley.Core.Scripts.Runtime;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// A simple ball launcher that shoots volleyballs using angle and power settings.
    /// </summary>
    public class BallLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        [Tooltip("Prefab of the volleyball to launch")]
        [SerializeField] private GameObject volleyballPrefab;
        
        [Tooltip("Launcher type - determines which settings to use")]
        [SerializeField] private LauncherType launcherType = LauncherType.Arc;
        
        public enum LauncherType
        {
            Arc,        // Service line, opponent court
            Direct      // Net launchers
        }
        
        [Header("Launch Parameters")]
        [Tooltip("Launch angle in degrees (0 = horizontal, positive = up, negative = down)")]
        [SerializeField] private float launchAngle = 45f;
        
        [Tooltip("Power/speed multiplier (1.0 = normal speed)")]
        [SerializeField] private float power = 1f;
        
        [Header("Base Speed Constants")]
        [Tooltip("Base horizontal speed for arc shots (multiplied by power)")]
        [SerializeField] private float baseHorizontalSpeed = 8f;
        
        [Tooltip("Base speed for direct shots (multiplied by power)")]
        [SerializeField] private float baseDirectSpeed = 12f;
        
        [Header("Visual")]
        [Tooltip("Visual indicator for the launcher (optional)")]
        [SerializeField] private GameObject visualIndicator;
        
        private bool isShooting = false;
        private bool isCountingDown = false;
        private Camera mainCamera;
        
        [Header("Countdown")]
        [Tooltip("Show countdown before shooting")]
        [SerializeField] private bool showCountdown = true;
        
        [Tooltip("Countdown duration in seconds")]
        [SerializeField] private float countdownDuration = 3f;
        
        [Tooltip("Text object to display countdown (optional)")]
        [SerializeField] private TMPro.TextMeshPro countdownText;
        
        private void Awake()
        {
            // Find volleyball prefab if not assigned
            if (volleyballPrefab == null)
            {
                GameObject existingBall = GameObject.Find("VolleyballV5");
                if (existingBall != null)
                {
                    volleyballPrefab = existingBall;
                }
            }
            
            // Find camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        private void LateUpdate()
        {
            // Make countdown text face camera
            if (countdownText != null && countdownText.gameObject.activeSelf && mainCamera != null)
            {
                countdownText.transform.LookAt(countdownText.transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
        
        /// <summary>
        /// Shoots a ball at the target position.
        /// </summary>
        public bool ShootAtTarget(Vector3 targetPosition)
        {
            if (isShooting || isCountingDown || volleyballPrefab == null)
                return false;
            
            if (showCountdown)
            {
                StartCoroutine(CountdownAndShoot(targetPosition));
            }
            else
            {
                ExecuteShoot(targetPosition);
            }
            
            return true;
        }
        
        private System.Collections.IEnumerator CountdownAndShoot(Vector3 targetPosition)
        {
            isCountingDown = true;
            
            // Create countdown text if not assigned
            if (countdownText == null)
            {
                CreateCountdownText();
            }
            
            // Countdown: 3, 2, 1
            for (int i = 3; i > 0; i--)
            {
                if (countdownText != null)
                {
                    countdownText.text = i.ToString();
                    countdownText.gameObject.SetActive(true);
                }
                yield return new WaitForSeconds(1f);
            }
            
            // Hide countdown text
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }
            
            isCountingDown = false;
            
            // Execute the shot
            ExecuteShoot(targetPosition);
        }
        
        private void ExecuteShoot(Vector3 targetPosition)
        {
            Vector3 launchVelocity = CalculateVelocity(targetPosition);
            
            if (launchVelocity == Vector3.zero)
            {
                return;
            }
            
            // Spawn ball
            GameObject ball = Instantiate(volleyballPrefab, transform.position, Quaternion.identity);
            
            // Apply velocity
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.velocity = launchVelocity;
            }
            
            // Notify manager to track this ball (if manager exists)
            BallLauncherManager manager = FindObjectOfType<BallLauncherManager>();
            if (manager != null)
            {
                manager.TrackVolleyball(ball);
            }
            
            // Set shooting state briefly to prevent rapid firing
            isShooting = true;
            Invoke(nameof(ResetShooting), 0.5f);
        }
        
        /// <summary>
        /// Calculates launch velocity using angle and power settings.
        /// </summary>
        private Vector3 CalculateVelocity(Vector3 targetPosition)
        {
            Vector3 startPos = transform.position;
            Vector3 toTarget = (targetPosition - startPos).normalized;
            Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z).normalized;
            
            // Apply power multiplier to base speeds
            float horizontalSpeed = baseHorizontalSpeed * power;
            float directSpeed = baseDirectSpeed * power;
            
            // Convert angle to radians
            float angleRad = launchAngle * Mathf.Deg2Rad;
            
            // For direct shots (net launchers)
            if (launcherType == LauncherType.Direct)
            {
                // Use angle to determine direction (positive = up, negative = down, 0 = horizontal)
                Vector3 launchVel = horizontalDir * (directSpeed * Mathf.Cos(angleRad));
                launchVel.y = directSpeed * Mathf.Sin(angleRad);
                return launchVel;
            }
            
            // For arc shots (service line, opponent court)
            Vector3 arcLaunchVel = horizontalDir * (horizontalSpeed * Mathf.Cos(angleRad));
            arcLaunchVel.y = horizontalSpeed * Mathf.Sin(angleRad);
            
            return arcLaunchVel;
        }
        
        private void CreateCountdownText()
        {
            GameObject textObj = new GameObject("CountdownText");
            textObj.transform.SetParent(transform);
            // Position above the visual indicator dot (slightly above launcher)
            textObj.transform.localPosition = Vector3.up * 0.3f; // Above launcher/dot
            textObj.transform.localRotation = Quaternion.identity;
            
            countdownText = textObj.AddComponent<TMPro.TextMeshPro>();
            countdownText.text = "3";
            countdownText.fontSize = 48;
            countdownText.color = Color.white;
            countdownText.alignment = TMPro.TextAlignmentOptions.Center;
            countdownText.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Sets the visibility of the launcher dot.
        /// </summary>
        public void SetDotVisibility(bool visible)
        {
            if (visualIndicator != null)
            {
                visualIndicator.SetActive(visible);
            }
        }
        
        private void ResetShooting()
        {
            isShooting = false;
        }
        
        /// <summary>
        /// Sets the launcher type.
        /// </summary>
        public void SetLauncherType(LauncherType type)
        {
            launcherType = type;
        }
        
        /// <summary>
        /// Sets the launch angle.
        /// </summary>
        public void SetLaunchAngle(float angle)
        {
            launchAngle = angle;
        }
        
        /// <summary>
        /// Sets the power.
        /// </summary>
        public void SetPower(float powerValue)
        {
            power = powerValue;
        }
        
        /// <summary>
        /// Gets whether this launcher is currently shooting.
        /// </summary>
        public bool IsShooting => isShooting;
        
        /// <summary>
        /// Gets whether this launcher is currently counting down.
        /// </summary>
        public bool IsCountingDown => isCountingDown;
        
        /// <summary>
        /// Gets whether this launcher is busy (shooting or counting down).
        /// </summary>
        public bool IsBusy => isShooting || isCountingDown;
    }
}
