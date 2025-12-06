using Unity.FPS.Gameplay;

namespace Script.Interactables
{
using UnityEngine;

namespace Unity.FPS.Game
{
    public class Bumper : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Landing poinmt")]
        public Transform TargetDestination;

        [Tooltip("Max height relative to landing point")]
        public float ArcHeight = 2f;

        [Header("Audio & Visuals")]
        public AudioClip BounceSfx;
        
        // Throttling to avoid multiple triggers and spam player
        private float m_LastBounceTime;
        private float m_BounceCooldown = 0.5f;

        private void OnTriggerEnter(Collider other)
        {
            // Check if player, maybe check collision matrix
            if (other.TryGetComponent<PlayerCharacterController>(out var player) && TargetDestination)
            {
                if (Time.time < m_LastBounceTime + m_BounceCooldown) return;
                
                LaunchPlayer(player);
                m_LastBounceTime = Time.time;
            }
        }

        private void LaunchPlayer(PlayerCharacterController player)
        {
            float gravity = player.GravityDownForce;
            Vector3 startPos = transform.position;
            Vector3 targetPos = TargetDestination.position;
            
            Vector3 launchVelocity = CalculateVelocity(startPos, targetPos, gravity);

            // Reset velocity to ensure no deviation
            player.AddForce(launchVelocity, true);

            // Audio
            if (BounceSfx && player.GetComponent<AudioSource>())
            {
                player.GetComponent<AudioSource>().PlayOneShot(BounceSfx);
            }
        }

        /// <summary>
        /// Gets initial velocity (X, Y, Z) to reach point
        /// </summary>
        private Vector3 CalculateVelocity(Vector3 start, Vector3 end, float gravity)
        {
            // separate Y (height) and XZ (horizontal plan)
            float displacementY = end.y - start.y;
            Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);
            float distXZ = displacementXZ.magnitude;

            // Here we get check that we are specified height above landing point
            float maxHeight = Mathf.Max(start.y, end.y) + ArcHeight;
            float heightFromStart = maxHeight - start.y;
            float heightFromEnd = maxHeight - end.y;

            // Physique : v = sqrt(2 * g * h)
            // get vertical speed to reach apex point
            float velocityY = Mathf.Sqrt(2 * gravity * heightFromStart);

            // height reaching calculus
            float timeToApex = Mathf.Sqrt(2 * heightFromStart / gravity);

            // tim to fallback
            float timeToDescend = Mathf.Sqrt(2 * heightFromEnd / gravity);

            float totalTime = timeToApex + timeToDescend;

            // get horizontal speed
            Vector3 velocityXZ = displacementXZ / totalTime;

            // get final vector
            return velocityXZ + Vector3.up * velocityY;
        }

        private void OnDrawGizmos()
        {
            if (TargetDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, TargetDestination.position);

                Gizmos.DrawWireSphere(TargetDestination.position, 0.5f);
                
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position;
                Vector3 end = TargetDestination.position;
                float h = Mathf.Max(start.y, end.y) + ArcHeight;
                Vector3 apex = Vector3.Lerp(start, end, 0.5f);
                apex.y = h;
                Gizmos.DrawLine(start, apex);
                Gizmos.DrawLine(apex, end);
            }
        }
    }
}
}