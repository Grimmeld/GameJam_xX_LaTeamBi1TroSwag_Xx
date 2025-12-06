using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Gameplay;

namespace Unity.FPS.Game
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Primary Fire (Single Shot)")]
        [Tooltip("Max distance for damage")]
        public float WeaponRange = 15f;
        
        [Tooltip("Cone angle in degrees")]
        [Range(0, 90)] public float ConeAngle = 30f;
        
        [Tooltip("Base damage per shell")]
        public float DamagePerShell = 50f; 
        
        [Tooltip("Minimum damage at range per shell")]
        public float MinDamagePerShell = 5f;

        [Header("Secondary Fire (Rocket Jump)")]
        [Tooltip("Force applied to the player on Right Click")]
        public float RocketJumpForce = 30f;
        [Tooltip("Cooldown in seconds for the rocket jump")]
        public float RocketJumpCooldown = 1.5f;

        [Header("Integration Stats")]
        public float ReloadDuration = 2f;
        public float RecoilForce = 4f; 
        [Tooltip("Delay between the two shots (Fire Rate)")]
        public float DelayBetweenShots = 0.2f; // Nouveau paramètre exposé pour régler la vitesse entre les 2 tirs

        [Header("References")]
        public GameObject WeaponRoot;
        public Transform WeaponMuzzle;
        public Animator WeaponAnimator;
        public LayerMask HitMask;
        public GameObject MuzzleFlashPrefab;

        [Header("Audio")]
        public AudioClip ShootSfx;
        public AudioClip RocketJumpSfx; 
        public AudioClip ChangeWeaponSfx;
        public AudioClip ReloadSfx;
        public AudioClip DryFireSfx;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public bool IsCharging { get; private set; } = false;
        public bool IsReloading { get; private set; } = false;
        public bool AutomaticReload { get; private set; } = false;
        public float CurrentAmmoRatio => (float)m_CurrentAmmo / m_MaxAmmo;

        // --- Internal State ---
        private int m_CurrentAmmo;
        private int m_MaxAmmo = 2;
        private float m_LastTimeShot;
        private float m_LastRocketJumpTime;
        private AudioSource m_AudioSource;

        public Action OnShoot;

        void Awake()
        {
            m_CurrentAmmo = m_MaxAmmo;
            m_AudioSource = GetComponent<AudioSource>();
            if (!m_AudioSource) m_AudioSource = gameObject.AddComponent<AudioSource>();
        }

        void Update()
        {
            // Listen for Secondary Fire (Right Click)
            if (IsWeaponActive && !IsReloading)
            {
                if (Input.GetButtonDown("FireSecondary")) 
                {
                    TryRocketJump();
                }
            }
        }

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);
            if (show && ChangeWeaponSfx) m_AudioSource.PlayOneShot(ChangeWeaponSfx);
        }

        public bool IsWeaponActive => WeaponRoot.activeInHierarchy;

        // --- PRIMARY FIRE (Left Click) ---
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            if (inputDown) return TryShoot();
            return false;
        }

        private bool TryShoot()
        {
            if (IsReloading || Time.time < m_LastTimeShot + DelayBetweenShots) return false;

            if (m_CurrentAmmo > 0)
            {
                HandleShot();
                return true; 
            }
            else
            {
                if(DryFireSfx) m_AudioSource.PlayOneShot(DryFireSfx);
                return false;
            }
        }

        private void HandleShot()
        {
            m_LastTimeShot = Time.time;


            m_CurrentAmmo--; 

            // Visuals
            if (ShootSfx) m_AudioSource.PlayOneShot(ShootSfx);
            if (WeaponAnimator) WeaponAnimator.SetTrigger("Fire"); // Assure-toi que ton Animator a bien une animation simple pour "Fire"
            if (MuzzleFlashPrefab)
            {
                GameObject flash = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation, WeaponMuzzle);
                Destroy(flash, 0.5f);
            }

            // Damage Logic
            DetectAndDamage();

            OnShoot?.Invoke();
        }

        private void DetectAndDamage()
        {
            // TODO use NonAllocVersion
            Collider[] hits = Physics.OverlapSphere(WeaponMuzzle.position, WeaponRange, HitMask);

            foreach (var hit in hits)
            {
                Transform target = hit.transform;
                Vector3 dirToTarget = (target.position - WeaponMuzzle.position).normalized;

                if (Vector3.Angle(WeaponMuzzle.forward, dirToTarget) < ConeAngle / 2)
                {
                    float dist = Vector3.Distance(WeaponMuzzle.position, target.position);
                    if (!Physics.Raycast(WeaponMuzzle.position, dirToTarget, dist, ~HitMask))
                    {
                        // Calculate Damage
                        // Simple calcul basé sur la distance, sans multiplicateur
                        float totalDmg = Mathf.Lerp(DamagePerShell, MinDamagePerShell, dist / WeaponRange);

                        // Use the health
                        // target.SendMessage("TakeDamage", totalDmg, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        // --- SECONDARY FIRE (Right Click - Rocket Jump) ---
        private void TryRocketJump()
        {
            if (Time.time >= m_LastRocketJumpTime + RocketJumpCooldown)
            {
                m_LastRocketJumpTime = Time.time;
                
                AudioClip clipToPlay = RocketJumpSfx ? RocketJumpSfx : ShootSfx;
                if(clipToPlay) m_AudioSource.PlayOneShot(clipToPlay);
                
                if (MuzzleFlashPrefab)
                {
                    GameObject flash = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation, WeaponMuzzle);
                    Destroy(flash, 0.5f);
                }

                ApplyRocketJumpForce();
                
                OnShoot?.Invoke(); 
            }
        }

        private void ApplyRocketJumpForce()
        {
            if (Owner)
            {
                PlayerCharacterController playerController = Owner.GetComponent<PlayerCharacterController>();
                if (playerController)
                {
                    Vector3 knockbackDirection = -WeaponMuzzle.forward;
                    playerController.AddForce(knockbackDirection * RocketJumpForce);
                }
            }
        }

        // --- RELOAD LOGIC ---
        public void StartReloadAnimation()
        {
            // Reload only if full
            if (!IsReloading && m_CurrentAmmo < m_MaxAmmo)
            {
                StartCoroutine(ReloadSequence());
            }
        }

        private IEnumerator ReloadSequence()
        {
            IsReloading = true;
            if (ReloadSfx) m_AudioSource.PlayOneShot(ReloadSfx);
            if (WeaponAnimator) WeaponAnimator.SetTrigger("Reload");
            yield return new WaitForSeconds(ReloadDuration);
            m_CurrentAmmo = m_MaxAmmo;
            IsReloading = false;
        }

        // --- DEBUG ---
        private void OnDrawGizmosSelected()
        {
            if (!WeaponMuzzle) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(WeaponMuzzle.position, WeaponRange);
            Gizmos.DrawRay(WeaponMuzzle.position, Quaternion.Euler(0, -ConeAngle/2, 0) * WeaponMuzzle.forward * WeaponRange);
            Gizmos.DrawRay(WeaponMuzzle.position, Quaternion.Euler(0, ConeAngle/2, 0) * WeaponMuzzle.forward * WeaponRange);
        }
    }
}