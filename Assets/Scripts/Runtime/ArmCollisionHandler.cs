using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Handles collisions between arm parts and the volleyball, applying velocity-based bounce.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArmVelocityCollisionHandler : MonoBehaviour
    {
        private POVArmsPrimitives armsScript;
        private GameObject armPart;
        private Rigidbody armRigidbody;
        
        public void Initialize(POVArmsPrimitives script, GameObject part)
        {
            armsScript = script;
            armPart = part;
            armRigidbody = GetComponent<Rigidbody>();
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit the volleyball
            GameObject other = collision.gameObject;
            if (other.name.ToLower().Contains("volleyball") && armsScript != null)
            {
                // Get the volleyball's rigidbody
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
                if (ballRb != null && !ballRb.isKinematic)
                {
                    // Get the arm's velocity from the parent script
                    Vector3 armVelocity = armsScript.GetArmVelocity(armPart);
                    
                    // Calculate bounce direction from contact normal
                    if (collision.contacts.Length > 0)
                    {
                        ContactPoint contact = collision.contacts[0];
                        Vector3 normal = contact.normal;
                        
                        // Reflect the ball's velocity off the surface
                        Vector3 ballVelocity = ballRb.velocity;
                        Vector3 reflectedVelocity = Vector3.Reflect(ballVelocity, normal);
                        
                        // Add the arm's velocity component to the bounce
                        // Project arm velocity onto the normal direction (how much the arm is pushing)
                        float armPushForce = Vector3.Dot(armVelocity, -normal);
                        
                        // Add the arm's velocity to the bounce, weighted by how much it's pushing
                        Vector3 enhancedVelocity = reflectedVelocity + (normal * armPushForce * 1.5f);
                        
                        // Apply the enhanced velocity to the ball
                        ballRb.velocity = enhancedVelocity;
                        
                        Debug.Log($"[ArmCollisionHandler] Arm velocity: {armVelocity.magnitude:F2} m/s, Enhanced bounce applied");
                    }
                }
            }
        }
    }
}

