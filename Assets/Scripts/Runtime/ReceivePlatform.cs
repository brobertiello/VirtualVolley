using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Creates a transparent receive platform between the user's wrists and elbows when in receive position.
    /// Only active when hands are close together below the shoulders.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class ReceivePlatform : MonoBehaviour
    {
        [Header("POV Arms Reference")]
        [Tooltip("Reference to POVArmsPrimitives component")]
        [SerializeField] private POVArmsPrimitives povArms;
        
        [Header("Receive Position Detection")]
        [Tooltip("Maximum distance between hands to activate receive platform")]
        [SerializeField] private float maxHandDistance = 0.25f;
        
        [Tooltip("Maximum height relative to shoulders to activate (negative = below shoulders)")]
        [SerializeField] private float maxHeightBelowShoulders = 0f;
        
        [Tooltip("Minimum height relative to shoulders to activate (negative = below shoulders)")]
        [SerializeField] private float minHeightBelowShoulders = -2f;
        
        [Header("Visual Settings")]
        [Tooltip("Material for the receive platform (should be transparent)")]
        [SerializeField] private Material platformMaterial;
        
        [Tooltip("Color of the platform")]
        [SerializeField] private Color platformColor = new Color(0.2f, 0.6f, 1.0f, 0.3f); // Light blue, 30% transparent
        
        [Tooltip("Color for arms when in receive position")]
        [SerializeField] private Color receivePositionColor = Color.green;
        
        [Tooltip("Original arm material colors (stored to restore when not in receive position)")]
        private Color originalLeftArmColor;
        private Color originalRightArmColor;
        private Color originalLeftForearmColor;
        private Color originalRightForearmColor;
        private bool colorsStored = false;
        
        [Header("Collision Settings")]
        [Tooltip("Physics material for the platform")]
        [SerializeField] private PhysicMaterial physicsMaterial;
        
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh platformMesh;
        private bool isActive = false;
        
        // References to arm parts (will be found from POVArmsPrimitives)
        private Transform leftElbow;
        private Transform rightElbow;
        private GameObject leftHand;
        private GameObject rightHand;
        private Transform leftShoulderAnchor;
        private Transform rightShoulderAnchor;
        
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            
            // Create mesh
            platformMesh = new Mesh();
            platformMesh.name = "ReceivePlatform";
            meshFilter.mesh = platformMesh;
            
            // Initially disable
            SetActive(false);
        }
        
        private void Start()
        {
            // Find POVArmsPrimitives if not assigned
            if (povArms == null)
            {
                povArms = FindObjectOfType<POVArmsPrimitives>();
            }
            
            if (povArms == null)
            {
                enabled = false;
                return;
            }
            
            // Get references to arm parts using reflection (since they're private)
            GetArmReferences();
            
            // Create material if not assigned
            if (platformMaterial == null)
            {
                CreateDefaultMaterial();
            }
            
            meshRenderer.material = platformMaterial;
            
            // Create physics material if not assigned
            if (physicsMaterial == null)
            {
                physicsMaterial = new PhysicMaterial("ReceivePlatformMaterial")
                {
                    bounciness = 0.8f,
                    staticFriction = 0.1f,
                    dynamicFriction = 0.1f,
                    bounceCombine = PhysicMaterialCombine.Maximum,
                    frictionCombine = PhysicMaterialCombine.Minimum
                };
            }
            
            meshCollider.material = physicsMaterial;
            meshCollider.convex = true; // Convex required for dynamic collisions
        }
        
        private void GetArmReferences()
        {
            if (povArms == null) return;
            
            // Use public properties from POVArmsPrimitives
            leftElbow = povArms.LeftElbow;
            rightElbow = povArms.RightElbow;
            leftHand = povArms.LeftHand;
            rightHand = povArms.RightHand;
            leftShoulderAnchor = povArms.LeftShoulderAnchor;
            rightShoulderAnchor = povArms.RightShoulderAnchor;
        }
        
        private void CreateDefaultMaterial()
        {
            // Try to use URP/Lit shader first, fallback to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            
            if (shader == null)
            {
                return;
            }
            
            platformMaterial = new Material(shader);
            
            // Set up transparency
            if (shader.name.Contains("Standard"))
            {
                platformMaterial.SetFloat("_Mode", 3); // Transparent mode
                platformMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                platformMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                platformMaterial.SetInt("_ZWrite", 0);
                platformMaterial.DisableKeyword("_ALPHATEST_ON");
                platformMaterial.EnableKeyword("_ALPHABLEND_ON");
                platformMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                platformMaterial.renderQueue = 3000;
            }
            else
            {
                // URP Lit shader
                platformMaterial.SetFloat("_Surface", 1); // Transparent
                platformMaterial.SetFloat("_Blend", 0); // Alpha
            }
            
            platformMaterial.color = platformColor;
        }
        
        private void Update()
        {
            // Check if we have all required references
            if (leftElbow == null || rightElbow == null || leftHand == null || rightHand == null ||
                leftShoulderAnchor == null || rightShoulderAnchor == null)
            {
                // Try to get references again (in case arms weren't initialized yet)
                GetArmReferences();
                
                if (leftElbow == null || rightElbow == null || leftHand == null || rightHand == null)
                {
                    SetActive(false);
                    return;
                }
            }
            
            // Check if in receive position
            bool inReceivePosition = CheckReceivePosition();
            
            if (inReceivePosition != isActive)
            {
                SetActive(inReceivePosition);
                UpdateArmColors(inReceivePosition);
            }
            
            // Update platform mesh if active
            if (isActive)
            {
                UpdatePlatformMesh();
            }
        }
        
        private void UpdateArmColors(bool inReceivePosition)
        {
            if (povArms == null) return;
            
            if (inReceivePosition)
            {
                povArms.SetArmColor(receivePositionColor);
            }
            else
            {
                povArms.ResetArmColors();
            }
        }
        
        private bool CheckReceivePosition()
        {
            // Get hand positions
            Vector3 leftHandPos = leftHand.transform.position;
            Vector3 rightHandPos = rightHand.transform.position;
            
            // Get shoulder positions
            Vector3 leftShoulderPos = leftShoulderAnchor.position;
            Vector3 rightShoulderPos = rightShoulderAnchor.position;
            Vector3 averageShoulderPos = (leftShoulderPos + rightShoulderPos) / 2f;
            
            // Check distance between hands
            float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
            if (handDistance > maxHandDistance)
            {
                return false;
            }
            
            // Check if hands are below shoulders
            float averageHandHeight = (leftHandPos.y + rightHandPos.y) / 2f;
            float heightBelowShoulders = averageHandHeight - averageShoulderPos.y;
            
            if (heightBelowShoulders > maxHeightBelowShoulders || heightBelowShoulders < minHeightBelowShoulders)
            {
                return false;
            }
            
            return true;
        }
        
        // Public method for debugging
        public bool IsInReceivePosition()
        {
            if (leftElbow == null || rightElbow == null || leftHand == null || rightHand == null ||
                leftShoulderAnchor == null || rightShoulderAnchor == null)
            {
                return false;
            }
            return CheckReceivePosition();
        }
        
        private void SetActive(bool active)
        {
            isActive = active;
            
            if (meshRenderer != null)
            {
                meshRenderer.enabled = active;
                
                // Ensure material is set
                if (active && meshRenderer.material == null)
                {
                    if (platformMaterial == null)
                    {
                        CreateDefaultMaterial();
                    }
                    if (platformMaterial != null)
                    {
                        meshRenderer.material = platformMaterial;
                    }
                }
            }
            
            if (meshCollider != null)
            {
                meshCollider.enabled = active;
            }
            
            // If activating, ensure mesh is updated immediately
            if (active && leftElbow != null && rightElbow != null && leftHand != null && rightHand != null)
            {
                UpdatePlatformMesh();
            }
        }
        
        private void UpdatePlatformMesh()
        {
            // Get the 4 corner positions
            Vector3 leftElbowPos = leftElbow.position;
            Vector3 leftHandPos = leftHand.transform.position;
            Vector3 rightHandPos = rightHand.transform.position;
            Vector3 rightElbowPos = rightElbow.position;
            
            // Create vertices (4 corners)
            Vector3[] vertices = new Vector3[4]
            {
                leftElbowPos,    // 0: Left elbow
                leftHandPos,      // 1: Left hand
                rightHandPos,     // 2: Right hand
                rightElbowPos     // 3: Right elbow
            };
            
            // Create triangles (2 triangles to form a quad)
            int[] triangles = new int[6]
            {
                0, 1, 2,  // First triangle: left elbow -> left hand -> right hand
                0, 2, 3   // Second triangle: left elbow -> right hand -> right elbow
            };
            
            // Create UVs (for texture mapping)
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),  // Left elbow
                new Vector2(0, 1),  // Left hand
                new Vector2(1, 1),  // Right hand
                new Vector2(1, 0)   // Right elbow
            };
            
            // Update mesh
            platformMesh.Clear();
            platformMesh.vertices = vertices;
            platformMesh.triangles = triangles;
            platformMesh.uv = uvs;
            platformMesh.RecalculateNormals();
            platformMesh.RecalculateBounds();
            
            // Update collider - need to recreate it for dynamic meshes
            if (meshCollider.sharedMesh != null)
            {
                meshCollider.sharedMesh = null;
            }
            meshCollider.sharedMesh = platformMesh;
        }
    }
}

