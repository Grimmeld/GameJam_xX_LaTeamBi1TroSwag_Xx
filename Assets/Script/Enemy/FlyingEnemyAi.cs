using System.Collections;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Script.Enemy
{
    [RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
    public class FlyingEnemyAi : MonoBehaviour
    {
        [Header("Stun Settings")]
        public float StunDuration = 0.5f;
        private float m_StunEndTime;
        
        [Header("Random Patrol Settings")]
        public float PatrolRange = 10f;
        public float ObstacleCheckRadius = 1.0f;
        public float ReachThreshold = 1.0f;

        [Header("Natural Movement Stats")]
        public float MoveSpeed = 3.5f;
        public float ChaseSpeedMultiplier = 1.5f;
        public float PositionSmoothing = 0.6f; 
        public float RotationSpeed = 5f; 
        
        [Header("Hover Effect")]
        public float BobFrequency = 1f; 
        public float BobAmplitude = 0.5f; 
        
        [Header("Altitude Settings")]
        public float MinHeightFromGround = 2.0f;

        [Header("Idle Behavior")]
        public float MinIdleTime = 1f;
        public float MaxIdleTime = 3f;
        
        [Header("Obstacle Avoidance")]
        public float AvoidanceDistance = 3f;
        public float AvoidanceForce = 2f; 
        public LayerMask ObstacleMask;

        [Header("Combat & Detection")]
        public float DetectionRange = 20f;
        public float StopChaseDistance = 30f;
        public float PreferredCombatDistance = 8f;
        
        [Header("Salvo Weapon Stats")]
        public GameObject ProjectilePrefab; 
        public Transform MuzzlePoint; 
        
        public int ProjectilesPerSalvo = 5;
        public float TimeBetweenShots = 0.15f;
        public float AttackChargeTime = 0.5f;
        public float AttackCooldown = 3f;
        
        [Header("Prediction")]
        [Range(0f, 1f)] 
        public float PredictionAccuracy = 0.8f; 

        [Header("Visuals & Audio")]
        public LineRenderer LaserBeamRenderer;
        public AudioClip ChargeSfx;
        public AudioClip ShootSfx;

        private Vector3 m_SpawnPosition;
        private Vector3 m_CurrentTargetPosition;
        private Vector3 m_SmoothDampVelocity; 
        
        private Transform m_PlayerTransform;
        private PlayerCharacterController m_PlayerController;
        
        private bool m_IsAttacking = false;
        private bool m_IsIdle = false; 
        private bool m_IsChasing = false;
        private bool m_HasLineOfSight = false;
        
        private float m_LastAttackTime;
        private Rigidbody m_Rb;

        
        
        void Start()
        {
            m_Rb = GetComponent<Rigidbody>();
            m_Rb.useGravity = false; 
            m_Rb.isKinematic = true; 

            m_SpawnPosition = transform.position;
            SetNewRandomTarget();

            m_PlayerController = FindFirstObjectByType<PlayerCharacterController>();
            if (m_PlayerController) m_PlayerTransform = m_PlayerController.transform;

            if (LaserBeamRenderer) LaserBeamRenderer.enabled = false;
            
            if (StopChaseDistance < DetectionRange) StopChaseDistance = DetectionRange + 5f;
            
            if (MuzzlePoint == null) MuzzlePoint = transform;
        }

        Vector3 AdjustForGroundHeight(Vector3 targetPos)
        {

            if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, ObstacleMask))
            {
                float groundY = hit.point.y;
                float targetHeight = groundY + MinHeightFromGround;

                if (targetPos.y < targetHeight)
                {
                    return new Vector3(targetPos.x, targetHeight, targetPos.z);
                }
            }
            return targetPos;
        }
        
        void Update()
        {
            if (!m_PlayerTransform) return;

            float distToPlayer = Vector3.Distance(transform.position, m_PlayerTransform.position);
            
            // Update LoS, used for future tracking maybe
            m_HasLineOfSight = CheckLineOfSight();

            if (m_IsChasing)
            {
                // de aggro if player reaaly far
                if (distToPlayer > StopChaseDistance) 
                {
                    StopChasing();
                }
                else 
                {
                    HandleChaseAndCombat(distToPlayer);
                }
            }
            else
            {
                // To shoot you need LoS and range
                if (distToPlayer <= DetectionRange && m_HasLineOfSight) 
                {
                    StartChasing();
                }
                else 
                {
                    HandlePatrol();
                }
            }
        }

        public void ApplyKnockback(Vector3 force)
        {

            m_SmoothDampVelocity += force;
    
            m_StunEndTime = Time.time + StunDuration;
        }
        
        bool CheckLineOfSight()
        {
            if (!m_PlayerTransform) return false;

            Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * 1.0f;
            Vector3 target = m_PlayerTransform.position + Vector3.up * 1.0f;
    
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

                if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, ObstacleMask))
            {

                if (hit.transform != m_PlayerTransform)
                {
                    return false;
                }
            }

            return true;
        }

        void StartChasing()
        {
            m_IsChasing = true;
            m_IsIdle = false;
            StopCoroutine("WaitAndPickNewTarget");
        }

        void StopChasing()
        {
            m_IsChasing = false;
            m_IsAttacking = false;
            SetNewRandomTarget(); 
        }

        void HandleChaseAndCombat(float distToPlayer)
        {
            ChasePlayerLogic();

            if (m_HasLineOfSight && !m_IsAttacking && Time.time >= m_LastAttackTime + AttackCooldown)
            {
                StartCoroutine(AttackSalvoSequence());
            }
        }

        void HandlePatrol()
        {
            if (!m_IsIdle) RandomPatrolLogic();
            else
            {
                ApplyHoverEffect(transform.position);
                transform.Rotate(Vector3.up * (10f * Time.deltaTime));
            }
        }

        void ChasePlayerLogic()
        {
            Vector3 dirToPlayer = (m_PlayerTransform.position - transform.position).normalized;
            Vector3 targetCombatPosition = m_PlayerTransform.position - (dirToPlayer * PreferredCombatDistance);
            Vector3 avoidance = ComputeAvoidance();

            Vector3 rawDestination = targetCombatPosition + avoidance;
            Vector3 finalDestination = AdjustForGroundHeight(rawDestination);

            if (Time.time < m_StunEndTime)
            {

                finalDestination = transform.position;
            }

            float speed = m_IsAttacking ? MoveSpeed * 0.5f : MoveSpeed * ChaseSpeedMultiplier;


            Vector3 nextPos = Vector3.SmoothDamp(
                transform.position, 
                finalDestination, 
                ref m_SmoothDampVelocity, 
                PositionSmoothing * 0.5f, 
                speed
            );

            if(Physics.Raycast(nextPos + Vector3.up, Vector3.down, out RaycastHit hit, 2f, ObstacleMask))
            {
                if(nextPos.y < hit.point.y + MinHeightFromGround)
                {
                    nextPos.y = hit.point.y + MinHeightFromGround;
                }
            }

            ApplyHoverEffect(nextPos);
    
            if (Time.time >= m_StunEndTime)
            {
                RotateTowards(m_PlayerTransform.position);
            }
        }
        
        void RandomPatrolLogic()
        {
            Vector3 avoidanceVector = ComputeAvoidance();
            Vector3 desiredDestination = m_CurrentTargetPosition + avoidanceVector;

            Vector3 nextPos = Vector3.SmoothDamp(
                transform.position, 
                desiredDestination, 
                ref m_SmoothDampVelocity, 
                PositionSmoothing, 
                MoveSpeed
            );

            ApplyHoverEffect(nextPos);

            if ((desiredDestination - transform.position).sqrMagnitude > 0.1f)
            {
                RotateTowards(desiredDestination);
            }

            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTarget = new Vector3(m_CurrentTargetPosition.x, 0, m_CurrentTargetPosition.z);

            if (Vector3.Distance(flatPos, flatTarget) < ReachThreshold)
            {
                StartCoroutine(WaitAndPickNewTarget());
            }
        }

        void ApplyHoverEffect(Vector3 basePosition)
        {
            float bobOffset = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
            transform.position = new Vector3(basePosition.x, basePosition.y + bobOffset * Time.deltaTime, basePosition.z);
        }

        IEnumerator WaitAndPickNewTarget()
        {
            m_IsIdle = true;
            float waitDuration = Random.Range(MinIdleTime, MaxIdleTime);
            yield return new WaitForSeconds(waitDuration);
            SetNewRandomTarget();
            m_IsIdle = false;
        }

        void SetNewRandomTarget()
        {
            for (int i = 0; i < 20; i++) 
            {
                Vector3 randomPoint = m_SpawnPosition + Random.insideUnitSphere * PatrolRange;
                if (!Physics.CheckSphere(randomPoint, ObstacleCheckRadius, ObstacleMask))
                {
                    m_CurrentTargetPosition = randomPoint;
                    return; 
                }
            }
            m_CurrentTargetPosition = m_SpawnPosition;
        }

        Vector3 ComputeAvoidance()
        {
            Vector3 avoidance = Vector3.zero;
            Vector3[] rayDirections = { transform.forward, transform.forward + transform.right * 0.5f, transform.forward - transform.right * 0.5f };

            foreach (var dir in rayDirections)
            {
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, AvoidanceDistance, ObstacleMask))
                {
                    float urgency = 1f - (hit.distance / AvoidanceDistance); 
                    avoidance += hit.normal * (AvoidanceForce * urgency);
                }
            }
            return avoidance;
        }

        void RotateTowards(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; 
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * RotationSpeed);
            }
        }

        IEnumerator AttackSalvoSequence()
        {
            m_IsAttacking = true;

            if (GetComponent<AudioSource>() && ChargeSfx) GetComponent<AudioSource>().PlayOneShot(ChargeSfx);
            
            if (LaserBeamRenderer)
            {
                LaserBeamRenderer.enabled = true;
                LaserBeamRenderer.startWidth = 0.05f;
                float timer = 0f;
                while(timer < AttackChargeTime)
                {
                    timer += Time.deltaTime;
                    if(m_PlayerTransform && CheckLineOfSight()) 
                    {
                        LaserBeamRenderer.SetPosition(0, MuzzlePoint.position);
                        LaserBeamRenderer.SetPosition(1, m_PlayerTransform.position);
                    }
                    yield return null;
                }
                LaserBeamRenderer.enabled = false;
            }
            else
            {
                yield return new WaitForSeconds(AttackChargeTime);
            }

            for (int i = 0; i < ProjectilesPerSalvo; i++)
            {
                if (m_PlayerTransform == null) break;

                Vector3 targetPoint = m_PlayerTransform.position;
                
                float projSpeed = 20f; 
                if(ProjectilePrefab && ProjectilePrefab.GetComponent<EnemyProjectile>())
                    projSpeed = ProjectilePrefab.GetComponent<EnemyProjectile>().Speed;

                float distance = Vector3.Distance(transform.position, m_PlayerTransform.position);
                float timeToImpact = distance / projSpeed;

                if (m_PlayerController)
                {
                    Vector3 predictedPos = m_PlayerTransform.position + (m_PlayerController.CharacterVelocity * timeToImpact * PredictionAccuracy);
                    targetPoint = predictedPos;
                }
                
                targetPoint += Random.insideUnitSphere * 0.5f; 

                if (ProjectilePrefab)
                {
                    Vector3 aimDirection = (targetPoint - MuzzlePoint.position).normalized;
                    Quaternion aimRotation = Quaternion.LookRotation(aimDirection);
                    Instantiate(ProjectilePrefab, MuzzlePoint.position, aimRotation);
                }

                if (GetComponent<AudioSource>() && ShootSfx) GetComponent<AudioSource>().PlayOneShot(ShootSfx);

                yield return new WaitForSeconds(TimeBetweenShots);
            }

            m_LastAttackTime = Time.time;
            m_IsAttacking = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Vector3 center = Application.isPlaying ? m_SpawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, PatrolRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, StopChaseDistance);

            if (m_PlayerTransform != null)
            {
                Gizmos.color = m_HasLineOfSight ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, m_PlayerTransform.position + Vector3.up);
            }
        }
    }
}