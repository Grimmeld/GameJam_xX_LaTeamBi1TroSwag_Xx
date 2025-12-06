using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("Wall Run Game Feel")]
        [Tooltip("Field of View cible pendant le wall run")]
        public float WallRunFOV = 75f; 
        [Tooltip("Durée de transition du FOV")]
        public float FOVTransitionDuration = 0.25f;
        [Tooltip("Temps de grâce pour sauter après avoir quitté le mur (Coyote Time)")]
        public float WallRunCoyoteTime = 0.2f;
        
        [Header("References")] [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;

        [Tooltip("Audio source for footsteps, jump, etc...")]
        public AudioSource AudioSource;

        [Header("General")] [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")] [Tooltip("Max movement speed when grounded (when not sprinting)")]
        public float MaxSpeedOnGround = 10f;

        [Tooltip(
            "Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Max movement speed when crouching")] [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        [Tooltip("Max movement speed when not grounded")]
        public float MaxSpeedInAir = 10f;

        [Tooltip("Acceleration speed when in the air")]
        public float AccelerationSpeedInAir = 25f;

        [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
        public float SprintSpeedModifier = 2f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")] [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Range(0.1f, 1f)] [Tooltip("Rotation speed multiplier when aiming")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")] [Tooltip("Force applied upward when jumping")]
        public float JumpForce = 9f;

        [Header("Stance")] [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.9f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 1.8f;

        [Tooltip("Height of character when crouching")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("Speed of crouching transitions")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")] [Tooltip("Amount of footstep sounds played when moving one meter")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("Sound played for footsteps")]
        public AudioClip FootstepSfx;

        [Tooltip("Sound played when jumping")] public AudioClip JumpSfx;
        [Tooltip("Sound played when landing")] public AudioClip LandSfx;

        [Tooltip("Sound played when taking damage froma fall")]
        public AudioClip FallDamageSfx;

        [Header("Fall Damage")]
        [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
        public bool RecievesFallDamage;

        [Tooltip("Minimun fall speed for recieving fall damage")]
        public float MinSpeedForFallDamage = 10f;

        [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
        public float MaxSpeedForFallDamage = 30f;

        [Tooltip("Damage recieved when falling at the mimimum speed")]
        public float FallDamageAtMinSpeed = 10f;

        [Tooltip("Damage recieved when falling at the maximum speed")]
        public float FallDamageAtMaxSpeed = 50f;


        [Header("Wall Running")]
        [Tooltip("Layers that count as runnable walls")]
        public LayerMask WallRunLayers;

        [Tooltip("Max distance to detect a wall on the side")]
        public float WallMaxDistance = 1f;

        [Tooltip("Minimum height from ground to allow wall running")]
        public float MinWallRunHeight = 1.5f;

        [Tooltip("Gravity applied while wall running (usually lower than normal gravity)")]
        public float WallGravityDownForce = 5f;

        [Tooltip("Speed multiplier while wall running")]
        public float WallRunSpeedMultiplier = 1.2f;

        [Tooltip("Force of the jump off the wall (Side, Up)")]
        public Vector2 WallJumpForce = new Vector2(10f, 8f); // X = Side force, Y = Up force

        [Tooltip("Camera tilt angle when wall running")]
        public float MaxAngleRoll = 15f;

        [Tooltip("Speed of camera transition to/from tilt")]
        public float CamTransitionDuration = 10f;

        [Tooltip("Threshold to determine if the wall angle is runnable (0 to 1)")]
        public float NormalizedAngleThreshold = 0.1f;

        [Tooltip("Force extra verticale ajoutée si le joueur regarde vers le haut lors du saut")]
        public float WallRunUpwardDashForce = 8f;

        [Tooltip("Angle minimum (en degrés) vers le haut pour activer le boost vertical")]
        public float WallRunLookUpThreshold = 15f;
        // ---------------------------------------------------------

        public UnityAction<bool> OnStanceChanged;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }
        
        // Wall Run Public Properties
        public bool IsWallRunning { get; private set; }
        public bool IsWallLeft { get; private set; }
        public bool IsWallRight { get; private set; }

        float m_DefaultFOV;
        float m_TimeWallRunEnded;
        Vector3 m_LastWallNormal;
        
        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        //Health m_Health;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        PlayerWeaponsManager m_WeaponsManager;
        //Actor m_Actor;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;
        
        // Camera Tilt internal variable
        float m_CurrentCameraTilt = 0f;
        RaycastHit m_WallHitLeft;
        RaycastHit m_WallHitRight;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake()
        {
            //ActorsManager actorsManager = FindFirstObjectByType<ActorsManager>();
            //if (actorsManager != null)
            //    actorsManager.SetPlayer(gameObject);
        }

        void Start()
        {
            // fetch components on the same gameObject
            m_Controller = GetComponent<CharacterController>();
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            
            m_Controller.enableOverlapRecovery = true;

            // force the crouch state to false when starting
            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);
            
            m_DefaultFOV = PlayerCamera.fieldOfView;
        }

        void Update()
        {
            // check for Y kill
            if (!IsDead && transform.position.y < KillHeight)
            {
                //m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            GroundCheck();
            
            // Wall Run Detection
            CheckForWall();
            ManageWallRunState();

            // landing
            if (IsGrounded && !wasGrounded)
            {
                // Reset Wall Run state on land
                IsWallRunning = false;

                // Fall damage
                float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                       (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    //m_Health.TakeDamage(dmgFromFall, null);

                    // fall damage SFX
                    AudioSource.PlayOneShot(FallDamageSfx);
                }
                else
                {
                    // land SFX
                    AudioSource.PlayOneShot(LandSfx);
                }
            }

            // crouching
            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie()
        {
            IsDead = true;

            // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
            m_WeaponsManager.SwitchToWeaponIndex(-1, true);

            //EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void CheckForWall()
        {
            IsWallLeft = false;
            IsWallRight = false;

            // Raycast Right
            if (Physics.Raycast(transform.position, transform.right, out m_WallHitRight, WallMaxDistance, WallRunLayers))
            {
                if (Mathf.Abs(Vector3.Dot(m_WallHitRight.normal, Vector3.up)) < NormalizedAngleThreshold)
                {
                    IsWallRight = true;
                }
            }

            // Raycast Left
            if (Physics.Raycast(transform.position, -transform.right, out m_WallHitLeft, WallMaxDistance, WallRunLayers))
            {
                if (Mathf.Abs(Vector3.Dot(m_WallHitLeft.normal, Vector3.up)) < NormalizedAngleThreshold)
                {
                    IsWallLeft = true;
                }
            }
        }

        void ManageWallRunState()
        {
            // Can only wall run if NOT grounded and Moving Forward
            bool isMovingForward = m_InputHandler.GetMoveInput().z > 0.1f;
            
            // Height Check: Cast a ray down to make sure we are high enough off the ground
            bool isHighEnough = !Physics.Raycast(transform.position, Vector3.down, MinWallRunHeight, GroundCheckLayers);

            if ((IsWallLeft || IsWallRight) && isMovingForward && isHighEnough && !IsGrounded)
            {
                if (!IsWallRunning)
                    StartWallRun();
            }
            else
            {
                if (IsWallRunning)
                    StopWallRun();
            }
        }

        void StartWallRun()
        {
            IsWallRunning = true;
            
            // Optional: Reset vertical velocity slightly so player doesn't slide down immediately
            if (CharacterVelocity.y < 0)
            {
                CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
            }
        }

        void StopWallRun()
        {
            if (IsWallRunning)
            {
                // save time and normal for coyotte jump
                m_TimeWallRunEnded = Time.time;
                
                // Save wall normal for later use
                if (IsWallLeft) m_LastWallNormal = m_WallHitLeft.normal;
                else if (IsWallRight) m_LastWallNormal = m_WallHitRight.normal;
                else m_LastWallNormal = Vector3.zero; // Sécurité
            }

            IsWallRunning = false;
        }

        void GroundCheck()
        {
            // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
            float chosenGroundCheckDistance =
                IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // reset values before the ground check
            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    // storing the upward direction for the surface found
                    m_GroundNormal = hit.normal;

                    // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                    // and if the slope angle is lower than the character controller's limit
                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;

                        // handle snapping to the ground
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }

        void HandleCharacterMovement()
        {
            // horizontal character rotation
            {
                // rotate the transform with the input speed around its local Y axis
                transform.Rotate(
                    new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier),
                        0f), Space.Self);
            }

            // vertical camera rotation AND Wall Run Tilt
            {
                // add vertical inputs to the camera's vertical angle
                m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;

                // limit the camera's vertical angle to min/max
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

                // --- CAMERA TILT CALCULATION ---
                float targetTilt = 0f;
                float targetFOV = m_DefaultFOV;

                if (IsWallRunning)
                {
                    if (IsWallLeft) targetTilt = -MaxAngleRoll;
                    else if (IsWallRight) targetTilt = MaxAngleRoll;
                    
                    targetFOV = WallRunFOV; // speed fov
                }

                // Smooth Tilt
                m_CurrentCameraTilt = Mathf.Lerp(m_CurrentCameraTilt, targetTilt, CamTransitionDuration * Time.deltaTime);
                
                // Smooth FOV (Game Feel)
                PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, targetFOV, Time.deltaTime / FOVTransitionDuration);

                PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, m_CurrentCameraTilt);
            }

            // character movement handling
            bool isSprinting = m_InputHandler.GetSprintInputHeld();
            {
                if (isSprinting)
                {
                    isSprinting = SetCrouchingState(false, false);
                }

                float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

                // converts move input to a worldspace vector based on our character's transform orientation
                Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

                // handle grounded movement
                if (IsGrounded)
                {
                    // calculate the desired velocity from inputs, max speed, and current slope
                    Vector3 targetVelocity = worldspaceMoveInput * (MaxSpeedOnGround * speedModifier);
                    // reduce speed if crouching by crouch speed ratio
                    if (IsCrouching)
                        targetVelocity *= MaxSpeedCrouchedRatio;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                     targetVelocity.magnitude;

                    // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                        MovementSharpnessOnGround * Time.deltaTime);

                    // jumping
                    if (IsGrounded && m_InputHandler.GetJumpInputDown())
                    {
                        // force the crouch state to false
                        if (SetCrouchingState(false, false))
                        {
                            // start by canceling out the vertical component of our velocity
                            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);

                            // then, add the jumpSpeed value upwards
                            CharacterVelocity += Vector3.up * JumpForce;

                            // play sound
                            AudioSource.PlayOneShot(JumpSfx);

                            // remember last time we jumped because we need to prevent snapping to ground for a short time
                            m_LastTimeJumped = Time.time;
                            HasJumpedThisFrame = true;

                            // Force grounding to false
                            IsGrounded = false;
                            m_GroundNormal = Vector3.up;
                        }
                    }

                    // footsteps sound
                    float chosenFootstepSfxFrequency =
                        (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                    if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                    {
                        m_FootstepDistanceCounter = 0f;
                        AudioSource.PlayOneShot(FootstepSfx);
                    }

                    // keep track of distance traveled for footsteps sound
                    m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
                }
                else if (IsWallRunning)
                {
                    // Calculate wall forward direction
                    Vector3 wallNormal = IsWallRight ? m_WallHitRight.normal : m_WallHitLeft.normal;
                    Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

                    // Determine direction based on player facing
                    if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
                    {
                        wallForward = -wallForward;
                    }

                    // Apply Wall Velocity
                    Vector3 wallRunVelocity = wallForward * (MaxSpeedInAir * WallRunSpeedMultiplier);
                    
                    // Smoothly blend horizontal velocity
                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, wallRunVelocity, MovementSharpnessOnGround * Time.deltaTime);

                    // Handle Wall Gravity (Much lighter)
                    CharacterVelocity += Vector3.down * (WallGravityDownForce * Time.deltaTime);
                    
                    // Add a small force pushing player INTO the wall to stick
                    CharacterVelocity += -wallNormal * (1f * Time.deltaTime);

                    // Wall Jump
                    if (m_InputHandler.GetJumpInputDown())
                    {
                        PerformWallJump(IsWallRight ? m_WallHitRight.normal : m_WallHitLeft.normal);
                    }
                }
                else if (Time.time < m_TimeWallRunEnded + WallRunCoyoteTime && m_InputHandler.GetJumpInputDown())
                {
                    PerformWallJump(m_LastWallNormal);
                }
                else
                {
                    // 1. Add Input Acceleration (WASD)
                    CharacterVelocity += worldspaceMoveInput * (AccelerationSpeedInAir * Time.deltaTime);

                    // 2. Momentum Logic
                    float verticalVelocity = CharacterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                    float maxAirSpeed = MaxSpeedInAir * speedModifier;

                    if (horizontalVelocity.magnitude > maxAirSpeed)
                    {
                        
                        horizontalVelocity = Vector3.Lerp(horizontalVelocity, horizontalVelocity.normalized * maxAirSpeed, Time.deltaTime * 0.5f);
                    }
                    else
                    {
                        
                        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed);
                    }

                    CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    // 3. Apply Gravity
                    CharacterVelocity += Vector3.down * (GravityDownForce * Time.deltaTime);
                    
                }
            }

            void PerformWallJump(Vector3 wallNormal)
            {
                // Calculate jump direction: Up + Away from wall (Base Jump)
                Vector3 jumpDir = (wallNormal * WallJumpForce.x) + (Vector3.up * WallJumpForce.y);

                // UPWARD DASH check
                if (m_CameraVerticalAngle < -WallRunLookUpThreshold)
                {
                    jumpDir += Vector3.up * WallRunUpwardDashForce;
                    jumpDir += transform.forward * (WallRunUpwardDashForce * 0.3f);
                }

                // Apply force
                CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                CharacterVelocity += jumpDir;

                // Play Sound
                AudioSource.PlayOneShot(JumpSfx);

                m_LastTimeJumped = Time.time;
                HasJumpedThisFrame = true;
                IsWallRunning = false; // Detach
            
                m_TimeWallRunEnded = 0f; 
            }
            
            // apply the final calculated velocity value as a character movement
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            // detect obstructions to adjust velocity accordingly
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        // Gets the center point of the bottom hemisphere of the character controller capsule    
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }

        // Gets the center point of the top hemisphere of the character controller capsule    
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        // Gets a reoriented direction that is tangent to a given slope
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // Update height instantly
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * (m_Controller.height * 0.5f);
                PlayerCamera.transform.localPosition = Vector3.up * (m_TargetCharacterHeight * CameraHeightRatio);
                //m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
            // Update smooth height
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                // resize the capsule and adjust camera position
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight,
                    CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * (m_Controller.height * 0.5f);
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                    Vector3.up * (m_TargetCharacterHeight * CameraHeightRatio), CrouchingSharpness * Time.deltaTime);
                //m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
        }

        // returns false if there was an obstruction
        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // set appropriate heights
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // Detect obstructions
                if (!ignoreObstructions)
                {
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        if (c != m_Controller)
                        {
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            if (OnStanceChanged != null)
            {
                OnStanceChanged.Invoke(crouched);
            }

            IsCrouching = crouched;
            return true;
        }
        
        public void AddForce(Vector3 force, bool resetVelocity = false)
        {
            if (resetVelocity)
            {
                CharacterVelocity = Vector3.zero;
            }
            
            // Apply the force
            CharacterVelocity += force;

            if (force.magnitude > 1f)
            {
                IsGrounded = false;
                m_GroundNormal = Vector3.up;
                
                m_LastTimeJumped = Time.time; 
            }
        }
    }
}