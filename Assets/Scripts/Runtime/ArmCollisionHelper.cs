using UnityEngine;

namespace VirtualVolley.Core.Scripts.Runtime
{
    /// <summary>
    /// Helper component to handle collisions between arm parts and the volleyball.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArmCollisionHelper : MonoBehaviour
    {
        private POVArmsPrimitives parentScript;
        private GameObject armPart;
        
        public void SetParentScript(POVArmsPrimitives script, GameObject part)
        {
            parentScript = script;
            armPart = part;
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit the volleyball
            GameObject other = collision.gameObject;
            if (other != null && other.name.ToLower().Contains("volleyball") && parentScript != null)
            {
                // Get the volleyball's rigidbody
                Rigidbody ballRb = other.GetComponent<Rigidbody>();
                if (ballRb != null && !ballRb.isKinematic)
                {
                    // Get the arm's velocity from the parent script
                    Vector3 armVelocity = parentScript.GetArmVelocity(armPart);
                    
                    // Calculate bounce direction from contact normal
                    if (collision.contacts.Length > 0)
                    {
                        ContactPoint contact = collision.contacts[0];
                        Vector3 normal = contact.normal;
                        
                        // Get current ball velocity
                        Vector3 ballVelocity = ballRb.velocity;
                        
                        // Calculate relative velocity (arm velocity relative to ball)
                        Vector3 relativeVelocity = armVelocity - ballVelocity;
                        
                        // Project relative velocity onto the contact normal (how much the arm is pushing toward the ball)
                        float pushForce = Vector3.Dot(relativeVelocity, -normal);
                        
                        // Only apply force if arm is moving toward the ball faster than the ball is moving away
                        if (pushForce > 0.1f) // Minimum threshold to avoid tiny forces
                        {
                            // Calculate the impulse force to apply
                            // Use the relative velocity magnitude as the base force
                            float forceMagnitude = pushForce * 3.0f; // Increased multiplier for stronger hits
                            
                            // Create force vector in the direction of the normal (away from the arm surface)
                            Vector3 forceVector = normal * forceMagnitude;
                            
                            // Apply the force as an impulse (instant force)
                            ballRb.AddForce(forceVector, ForceMode.Impulse);
                            
                            // Also add some of the arm's velocity directly to make it more responsive
                            Vector3 velocityBoost = armVelocity * 0.5f;
                            ballRb.velocity += velocityBoost;
                            
                            Debug.Log($"[ArmCollisionHelper] Arm velocity: {armVelocity.magnitude:F2} m/s, Ball velocity: {ballVelocity.magnitude:F2} m/s, Push force: {pushForce:F2}, Force applied: {forceMagnitude:F2}");
                        }
                        else
                        {
                            Debug.Log($"[ArmCollisionHelper] Collision detected but push force too low: {pushForce:F2}");
                        }
                    }
                }
            }
        }
    }
}

