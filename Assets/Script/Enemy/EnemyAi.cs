using Unity.FPS.Gameplay;

namespace Script.Enemy
{
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.FPS.Game
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection & Movement")]
        [Tooltip("Distance à partir de laquelle l'ennemi voit le joueur")]
        public float DetectionRange = 15f;
        [Tooltip("Distance à laquelle l'ennemi s'arrête pour attaquer")]
        public float AttackRange = 2f;
        [Tooltip("Vitesse de déplacement")]
        public float MoveSpeed = 3.5f;

        [Header("Attack Stats")]
        public float Damage = 10f;
        public float TimeBetweenAttacks = 1.5f;
        public Color AttackColor = Color.red;
        
        [Header("References")]
        public Renderer EnemyRenderer;

        private NavMeshAgent m_Agent;
        private Transform m_PlayerTransform;
        private float m_LastAttackTime;
        private Color m_OriginalColor;

        void Start()
        {
            m_Agent = GetComponent<NavMeshAgent>();
            m_Agent.speed = MoveSpeed;
            m_Agent.stoppingDistance = AttackRange;

            PlayerCharacterController player = FindFirstObjectByType<PlayerCharacterController>();
            if (player)
            {
                m_PlayerTransform = player.transform;
            }

            if (EnemyRenderer) m_OriginalColor = EnemyRenderer.material.color;
        }

        void Update()
        {
            if (!m_PlayerTransform) return;

            float distanceToPlayer = Vector3.Distance(transform.position, m_PlayerTransform.position);

            // 1.a if in range
            if (distanceToPlayer <= DetectionRange)
            {
                // 2. and close enough
                if (distanceToPlayer <= AttackRange)
                {
                    // 3.a attack
                    PerformAttack();
                }
                else
                {
                    // 3.b or chase
                    ChasePlayer();
                }
            }
            else
            {
                // 1.b or give up
                m_Agent.isStopped = true;
                if (EnemyRenderer) EnemyRenderer.material.color = m_OriginalColor;
            }
        }

        void ChasePlayer()
        {
            m_Agent.isStopped = false;
            m_Agent.SetDestination(m_PlayerTransform.position);
            
            if (EnemyRenderer) EnemyRenderer.material.color = m_OriginalColor;
        }

        void PerformAttack()
        {
            LookAtPlayer();
            
            m_Agent.isStopped = true;

            if (Time.time >= m_LastAttackTime + TimeBetweenAttacks)
            {
                m_LastAttackTime = Time.time;
                
                if (EnemyRenderer) StartCoroutine(FlashColor());

                if(m_PlayerTransform.TryGetComponent<Health>(out var playerHealth))
                {
                    playerHealth.TakeDamage(damage: Damage);
                }
            }
        }

        void LookAtPlayer()
        {
            Vector3 direction = (m_PlayerTransform.position - transform.position).normalized;
            direction.y = 0; 
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        IEnumerator FlashColor()
        {
            if (EnemyRenderer) EnemyRenderer.material.color = AttackColor;
            yield return new WaitForSeconds(0.2f);
            if (EnemyRenderer) EnemyRenderer.material.color = m_OriginalColor;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
}