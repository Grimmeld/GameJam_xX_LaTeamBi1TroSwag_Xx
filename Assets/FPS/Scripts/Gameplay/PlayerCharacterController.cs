using FPS.Scripts.Gameplay.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : MonoBehaviour
    {
        // ... (Le début du code reste inchangé jusqu'aux headers) ...
        private const float k_JumpGroundingPreventionTime = 0.2f;
        private const float k_GroundCheckDistanceInAir = 0.07f;

        [Header("Wall Run Game Feel")] 
        public float WallRunFOV = 75f;
        public float FOVTransitionDuration = 0.25f;
        public float WallRunCoyoteTime = 0.2f;

        [Header("References")] 
        public Camera PlayerCamera;
        public AudioSource AudioSource;

        [Header("General")] 
        public float GravityDownForce = 20f;
        public LayerMask GroundCheckLayers = -1;
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")] 
        public float MaxSpeedOnGround = 10f;
        public float MovementSharpnessOnGround = 15;
        [Range(0, 1)] public float MaxSpeedCrouchedRatio = 0.5f;
        public float MaxSpeedInAir = 10f;
        public float AccelerationSpeedInAir = 25f;

        // --- OLD SPRINT (Commented out) ---
        // public float SprintSpeedModifier = 2f;
        // ----------------------------------

        // --- NEW: DASH PARAMETERS ---
        [Header("Dash Ability")]
        [Tooltip("Force de l'impulsion du dash")]
        public float DashForce = 15f;
        
        [Tooltip("Temps d'attente entre deux dashs (en secondes)")]
        public float DashCooldown = 1f;

        [Tooltip("Son joué lors du dash")]
        public AudioClip DashSfx;
        // ----------------------------

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")] 
        public float RotationSpeed = 200f;
        [Range(0.1f, 1f)] public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")] 
        public float JumpForce = 9f;

        [Header("Stance")] 
        public float CameraHeightRatio = 0.9f;
        public float CapsuleHeightStanding = 1.8f;
        public float CapsuleHeightCrouching = 0.9f;
        public float CrouchingSharpness = 10f;

        [Header("Audio")] 
        public float FootstepSfxFrequency = 1f;
        // public float FootstepSfxFrequencyWhileSprinting = 1f; // Removed
        public AudioClip FootstepSfx;
        public AudioClip JumpSfx;
        public AudioClip LandSfx;
        public AudioClip FallDamageSfx;

        [Header("Fall Damage")]
        public bool RecievesFallDamage;
        public float MinSpeedForFallDamage = 10f;
        public float MaxSpeedForFallDamage = 30f;
        public float FallDamageAtMinSpeed = 10f;
        public float FallDamageAtMaxSpeed = 50f;
        
        [Header("Camera Landing Bob")]
        public float LandBobMultiplier = 1.5f; 
        public float LandPitchMultiplier = 10f; 
        public float LandBobSmoothTime = 0.15f;

        // ... (Variables Wall Run inchangées) ...
        [Header("Wall Running")] 
        public LayerMask WallRunLayers;
        public float WallMaxDistance = 1f;
        public float MinWallRunHeight = 1.5f;
        public float WallGravityDownForce = 5f;
        public float WallRunSpeedMultiplier = 1.2f;
        public Vector2 WallJumpForce = new(10f, 8f);
        public float MaxAngleRoll = 15f;
        public float CamTransitionDuration = 10f;
        public float NormalizedAngleThreshold = 0.1f;
        public float WallRunUpwardDashForce = 8f;
        public float WallRunLookUpThreshold = 15f;

        private float m_CameraVerticalAngle;
        private Vector3 m_CharacterVelocity;
        private CharacterController m_Controller;

        private float m_CurrentCameraTilt;
        private float m_DefaultFOV;
        private float m_FootstepDistanceCounter;
        private Vector3 m_GroundNormal;
        
        private float m_CurrentLandBobY = 0f;
        private float m_LandBobVelocityRef = 0f; 
        private float m_CurrentLandPitch = 0f;
        private float m_LandPitchVelocityRef = 0f;

        // --- NEW: Dash Logic Variables ---
        private float m_LastTimeDashed = -10f; // Initialisé bas pour permettre de dash tout de suite
        private bool m_WasSprintInputHeldLastFrame = false; // Pour détecter l'appui "Down"
        // ---------------------------------

        private PlayerInputHandler m_InputHandler;
        private float m_LastTimeJumped;
        private Vector3 m_LastWallNormal;
        private Vector3 m_LatestImpactSpeed;
        private float m_TargetCharacterHeight;
        private float m_TimeWallRunEnded;
        private RaycastHit m_WallHitLeft;
        private RaycastHit m_WallHitRight;

        private PlayerWeaponsManager m_WeaponsManager;

        public UnityAction<bool> OnStanceChanged;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        public bool IsWallRunning { get; private set; }
        public bool IsWallLeft { get; private set; }
        public bool IsWallRight { get; private set; }

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming) return AimingRotationMultiplier;
                return 1f;
            }
        }

        private void Awake()
        {
            //ActorsManager actorsManager = FindFirstObjectByType<ActorsManager>();
            //if (actorsManager != null)
            //    actorsManager.SetPlayer(gameObject);
        }

        private void Start()
        {
            m_Controller = GetComponent<CharacterController>();
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();

            m_Controller.enableOverlapRecovery = true;

            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);

            m_DefaultFOV = PlayerCamera.fieldOfView;
        }

        private void Update()
        {
            // check for Y kill
            if (!IsDead && transform.position.y < KillHeight)
            {
                //m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            var wasGrounded = IsGrounded;
            GroundCheck();

            CheckForWall();
            ManageWallRunState();

            // landing
            if (IsGrounded && !wasGrounded)
            {
                IsWallRunning = false;

                var fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                
                // Impulse Landing
                if (fallSpeed > 2f) 
                {
                    m_LandBobVelocityRef = -fallSpeed * LandBobMultiplier;
                    m_LandPitchVelocityRef = fallSpeed * LandPitchMultiplier;
                }

                var fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                     (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    var dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    //m_Health.TakeDamage(dmgFromFall, null);
                    AudioSource.PlayOneShot(FallDamageSfx);
                }
                else
                {
                    AudioSource.PlayOneShot(LandSfx);
                }
            }

            if (m_InputHandler.GetCrouchInputDown()) SetCrouchingState(!IsCrouching, false);
            
            // Smooth Damping
            m_CurrentLandBobY = Mathf.SmoothDamp(m_CurrentLandBobY, 0f, ref m_LandBobVelocityRef, LandBobSmoothTime);
            m_CurrentLandPitch = Mathf.SmoothDamp(m_CurrentLandPitch, 0f, ref m_LandPitchVelocityRef, LandBobSmoothTime);

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        private void OnDie()
        {
            IsDead = true;
            m_WeaponsManager.SwitchToWeaponIndex(-1, true);
        }

        private void CheckForWall()
        {
            IsWallLeft = false;
            IsWallRight = false;
            if (Physics.Raycast(transform.position, transform.right, out m_WallHitRight, WallMaxDistance, WallRunLayers))
                if (Mathf.Abs(Vector3.Dot(m_WallHitRight.normal, Vector3.up)) < NormalizedAngleThreshold) IsWallRight = true;
            if (Physics.Raycast(transform.position, -transform.right, out m_WallHitLeft, WallMaxDistance, WallRunLayers))
                if (Mathf.Abs(Vector3.Dot(m_WallHitLeft.normal, Vector3.up)) < NormalizedAngleThreshold) IsWallLeft = true;
        }

        private void ManageWallRunState()
        {
            var isMovingForward = m_InputHandler.GetMoveInput().z > 0.1f;
            var isHighEnough = !Physics.Raycast(transform.position, Vector3.down, MinWallRunHeight, GroundCheckLayers);
            if ((IsWallLeft || IsWallRight) && isMovingForward && isHighEnough && !IsGrounded) { if (!IsWallRunning) StartWallRun(); }
            else { if (IsWallRunning) StopWallRun(); }
        }

        private void StartWallRun() { IsWallRunning = true; if (CharacterVelocity.y < 0) CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z); }
        private void StopWallRun()
        {
            if (IsWallRunning)
            {
                m_TimeWallRunEnded = Time.time;
                if (IsWallLeft) m_LastWallNormal = m_WallHitLeft.normal;
                else if (IsWallRight) m_LastWallNormal = m_WallHitRight.normal;
                else m_LastWallNormal = Vector3.zero;
            }
            IsWallRunning = false;
        }
        private void GroundCheck()
        {
            var chosenGroundCheckDistance = IsGrounded ? m_Controller.skinWidth + GroundCheckDistance : k_GroundCheckDistanceInAir;
            IsGrounded = false;
            m_GroundNormal = Vector3.up;
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out var hit, chosenGroundCheckDistance, GroundCheckLayers, QueryTriggerInteraction.Ignore))
                {
                    m_GroundNormal = hit.normal;
                    if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;
                        if (hit.distance > m_Controller.skinWidth) m_Controller.Move(Vector3.down * hit.distance);
                    }
                }
        }

        private void HandleCharacterMovement()
        {
            // Horizontal rotation
            transform.Rotate(new Vector3(0f, m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier, 0f), Space.Self);

            //  rotation & Tilt
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            var targetTilt = 0f;
            var targetFOV = m_DefaultFOV;
            if (IsWallRunning)
            {
                if (IsWallLeft) targetTilt = -MaxAngleRoll;
                else if (IsWallRight) targetTilt = MaxAngleRoll;
                targetFOV = WallRunFOV;
            }
            m_CurrentCameraTilt = Mathf.Lerp(m_CurrentCameraTilt, targetTilt, CamTransitionDuration * Time.deltaTime);
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, targetFOV, Time.deltaTime / FOVTransitionDuration);
            
            PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle + m_CurrentLandPitch, 0, m_CurrentCameraTilt);

            bool isSprintHeld = m_InputHandler.GetSprintInputHeld();
            

            var speedModifier = 1f;

            var worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

            bool isDashDown = isSprintHeld && !m_WasSprintInputHeldLastFrame;
            
            if (isDashDown && Time.time >= m_LastTimeDashed + DashCooldown)
            {
                Vector3 dashDirection = worldspaceMoveInput.normalized;
                
                if (dashDirection == Vector3.zero)
                {
                    dashDirection = transform.forward;
                }
                
                CharacterVelocity += dashDirection * DashForce;
                
                m_LandPitchVelocityRef = -DashForce * 0.5f;

                // Sound
                if(DashSfx) AudioSource.PlayOneShot(DashSfx);
                
                m_LastTimeDashed = Time.time;
            }
            m_WasSprintInputHeldLastFrame = isSprintHeld;

            if (IsGrounded)
            {
                var targetVelocity = worldspaceMoveInput * (MaxSpeedOnGround * speedModifier);
                if (IsCrouching) targetVelocity *= MaxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity, MovementSharpnessOnGround * Time.deltaTime);

                if (IsGrounded && m_InputHandler.GetJumpInputDown())
                    if (SetCrouchingState(false, false))
                    {
                        CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                        CharacterVelocity += Vector3.up * JumpForce;
                        AudioSource.PlayOneShot(JumpSfx);
                        m_LastTimeJumped = Time.time;
                        HasJumpedThisFrame = true;
                        IsGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }

                if (m_FootstepDistanceCounter >= 1f / FootstepSfxFrequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    AudioSource.PlayOneShot(FootstepSfx);
                }
                m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
            }
            else if (IsWallRunning)
            {
                 // (Wall run logic inchangée)
                var wallNormal = IsWallRight ? m_WallHitRight.normal : m_WallHitLeft.normal;
                var wallForward = Vector3.Cross(wallNormal, Vector3.up);
                if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude) wallForward = -wallForward;
                var wallRunVelocity = wallForward * (MaxSpeedInAir * WallRunSpeedMultiplier);
                CharacterVelocity = Vector3.Lerp(CharacterVelocity, wallRunVelocity, MovementSharpnessOnGround * Time.deltaTime);
                CharacterVelocity += Vector3.down * (WallGravityDownForce * Time.deltaTime);
                CharacterVelocity += -wallNormal * (1f * Time.deltaTime);

                if (m_InputHandler.GetJumpInputDown()) PerformWallJump(IsWallRight ? m_WallHitRight.normal : m_WallHitLeft.normal);
            }
            else if (Time.time < m_TimeWallRunEnded + WallRunCoyoteTime && m_InputHandler.GetJumpInputDown())
            {
                PerformWallJump(m_LastWallNormal);
            }
            else
            {
                // Air Movement
                CharacterVelocity += worldspaceMoveInput * (AccelerationSpeedInAir * Time.deltaTime);
                var verticalVelocity = CharacterVelocity.y;
                var horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                var maxAirSpeed = MaxSpeedInAir * speedModifier;


                if (horizontalVelocity.magnitude > maxAirSpeed)
                {
                    horizontalVelocity = Vector3.Lerp(horizontalVelocity, horizontalVelocity.normalized * maxAirSpeed, Time.deltaTime * 2f);
                }
                else
                {
                     horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed);
                }

                CharacterVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
                CharacterVelocity += Vector3.down * (GravityDownForce * Time.deltaTime);
            }

            var capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            var capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, CharacterVelocity.normalized, out var hit, CharacterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore))
            {
                m_LatestImpactSpeed = CharacterVelocity;
                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }

            return;

            void PerformWallJump(Vector3 wallNormal)
            {
                var jumpDir = wallNormal * WallJumpForce.x + Vector3.up * WallJumpForce.y;
                if (m_CameraVerticalAngle < -WallRunLookUpThreshold)
                {
                    jumpDir += Vector3.up * WallRunUpwardDashForce;
                    jumpDir += transform.forward * (WallRunUpwardDashForce * 0.3f);
                }
                CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                CharacterVelocity += jumpDir;
                AudioSource.PlayOneShot(JumpSfx);
                m_LastTimeJumped = Time.time;
                HasJumpedThisFrame = true;
                IsWallRunning = false; 
                m_TimeWallRunEnded = 0f;
            }
        }

        private bool IsNormalUnderSlopeLimit(Vector3 normal) { return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit; }
        private Vector3 GetCapsuleBottomHemisphere() { return transform.position + transform.up * m_Controller.radius; }
        private Vector3 GetCapsuleTopHemisphere(float atHeight) { return transform.position + transform.up * (atHeight - m_Controller.radius); }
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal) { var directionRight = Vector3.Cross(direction, transform.up); return Vector3.Cross(slopeNormal, directionRight).normalized; }
        private void UpdateCharacterHeight(bool force)
        {
            Vector3 finalCameraPosition;
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * (m_Controller.height * 0.5f);
                finalCameraPosition = Vector3.up * (m_TargetCharacterHeight * CameraHeightRatio);
            }
            else 
            {
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * (m_Controller.height * 0.5f);
                finalCameraPosition = Vector3.Lerp(PlayerCamera.transform.localPosition, Vector3.up * (m_TargetCharacterHeight * CameraHeightRatio), CrouchingSharpness * Time.deltaTime);
            }
            PlayerCamera.transform.localPosition = finalCameraPosition + Vector3.up * m_CurrentLandBobY;
        }

        private bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            if (crouched) { m_TargetCharacterHeight = CapsuleHeightCrouching; }
            else
            {
                if (!ignoreObstructions)
                {
                    var standingOverlaps = Physics.OverlapCapsule(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(CapsuleHeightStanding), m_Controller.radius, -1, QueryTriggerInteraction.Ignore);
                    foreach (var c in standingOverlaps) if (c != m_Controller) return false;
                }
                m_TargetCharacterHeight = CapsuleHeightStanding;
            }
            if (OnStanceChanged != null) OnStanceChanged.Invoke(crouched);
            IsCrouching = crouched;
            return true;
        }

        public void AddForce(Vector3 force, bool resetVelocity = false)
        {
            if (resetVelocity) CharacterVelocity = Vector3.zero;
            CharacterVelocity += force;
            if (force.magnitude > 1f) { IsGrounded = false; m_GroundNormal = Vector3.up; m_LastTimeJumped = Time.time; }
        }
    }
}