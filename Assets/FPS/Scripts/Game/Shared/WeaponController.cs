using System;
using System.Collections;
using Script.Enemy;
using Script.UI;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace FPS.Scripts.Game.Shared
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Primary Fire (Single Shot)")] [Tooltip("Max distance for damage")]
        public float WeaponRange = 15f;

        [Tooltip("Knockback ennemy force")] public float ImpactForce = 15f;

        [Tooltip("Cone angle in degrees")] [Range(0, 90)]
        public float ConeAngle = 30f;

        [Tooltip("Base damage per shell")] public float DamagePerShell = 50f;

        [Tooltip("Minimum damage at range per shell")]
        public float MinDamagePerShell = 5f;

        [Header("Secondary Fire (Rocket Jump)")] [Tooltip("Force applied to the player on Right Click")]
        public float RocketJumpForce = 30f;

        [Tooltip("Cooldown in seconds for the rocket jump")]
        public float RocketJumpCooldown = 1.5f;

        [Header("Integration Stats")] public float ReloadDuration = 2f;

        public float RecoilForce = 4f;

        [Tooltip("Delay between the two shots (Fire Rate)")]
        public float DelayBetweenShots = 0.2f;

        [Header("References")] public GameObject WeaponRoot;

        public Transform WeaponMuzzle;
        public Animator WeaponAnimator;
        public LayerMask HitMask;
        public GameObject MuzzleFlashPrefab;
        public GameObject MuzzleFlashRocketJumpPrefab;

        [SerializeField] public GameObject EnemyImpactVfx;

        [Header("Audio")] public AudioClip ShootSfx;

        public AudioClip RocketJumpSfx;
        public AudioClip ChangeWeaponSfx;
        public AudioClip ReloadSfx;
        public AudioClip DryFireSfx;

        [Header("Reload")] [SerializeField] public int ammoStock;

        private readonly int m_MaxAmmo = 2;
        private AudioSource m_AudioSource;
        private int m_CurrentAmmo;
        private float m_LastRocketJumpTime;
        private float m_LastTimeShot;


        public Action OnShoot;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public bool IsCharging { get; private set; } = false;
        public bool IsReloading { get; private set; }
        public bool AutomaticReload { get; private set; } = false;
        public float CurrentAmmoRatio => (float)m_CurrentAmmo / m_MaxAmmo;

        public bool IsWeaponActive => WeaponRoot.activeInHierarchy;

        private void Awake()
        {
            m_CurrentAmmo = m_MaxAmmo;
            m_AudioSource = GetComponent<AudioSource>();
            if (!m_AudioSource) m_AudioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            UpdateHUD();
        }

        private void Update()
        {
            if (IsWeaponActive && !IsReloading)
                if (Input.GetButtonDown("FireSecondary"))
                    TryRocketJump();
        }

        private void OnDrawGizmosSelected()
        {
            if (!WeaponMuzzle) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(WeaponMuzzle.position, WeaponRange);
            Gizmos.DrawRay(WeaponMuzzle.position,
                Quaternion.Euler(0, -ConeAngle / 2, 0) * WeaponMuzzle.forward * WeaponRange);
            Gizmos.DrawRay(WeaponMuzzle.position,
                Quaternion.Euler(0, ConeAngle / 2, 0) * WeaponMuzzle.forward * WeaponRange);
        }

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);
            if (show && ChangeWeaponSfx) m_AudioSource.PlayOneShot(ChangeWeaponSfx);
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            if (inputDown) return TryShoot();
            return false;
        }


        private bool TryShoot()
        {
            Debug.Log("Try to shoot");


            if (IsReloading || Time.time < m_LastTimeShot + DelayBetweenShots) return false;

            if (m_CurrentAmmo > 0)
            {
                HandleShot();
                return true;
            }

            if (DryFireSfx) m_AudioSource.PlayOneShot(DryFireSfx);
            return false;
        }

        private void DetectAndDamage()
        {
            var hits = Physics.OverlapSphere(WeaponMuzzle.position, WeaponRange, HitMask);

            foreach (var hit in hits)
            {
                var target = hit.transform;
                var dirToTarget = (target.position - WeaponMuzzle.position).normalized;

                if (Vector3.Angle(WeaponMuzzle.forward, dirToTarget) < ConeAngle / 2)
                {
                    var dist = Vector3.Distance(WeaponMuzzle.position, target.position);

                    if (!Physics.Raycast(WeaponMuzzle.position, dirToTarget, dist, ~HitMask))
                    {
                        var totalDmg = Mathf.Lerp(DamagePerShell, MinDamagePerShell, dist / WeaponRange);

                        if (hit.TryGetComponent<HealthEnemy>(out var playerHealth))
                        {
                            playerHealth.TakeDamage(totalDmg);

                            if (Physics.Raycast(WeaponMuzzle.position, dirToTarget, out var impactHit, dist + 1f,
                                    HitMask))
                            {
                                Instantiate(EnemyImpactVfx, impactHit.point, Quaternion.LookRotation(impactHit.normal));
                            }
                            else
                            {

                                var closestPoint = hit.ClosestPoint(WeaponMuzzle.position);
                                var blood = Instantiate(EnemyImpactVfx, closestPoint, Quaternion.LookRotation(-dirToTarget));
                                
                                // TODO this is bad
                                Destroy(blood, 3f);
                            }

                        }

                        if (hit.TryGetComponent<FlyingEnemyAi>(out var flyingEnemy))
                            flyingEnemy.ApplyKnockback(dirToTarget * ImpactForce);
                        else if (hit.TryGetComponent<Rigidbody>(out var rb))
                            rb.AddForce(dirToTarget * ImpactForce, ForceMode.Impulse);
                    }
                }
            }
        }

        private void HandleShot()
        {
            m_LastTimeShot = Time.time;

            Debug.Log("SHOT");


            m_CurrentAmmo--;
            UpdateHUD();

            // Visuals
            if (ShootSfx) m_AudioSource.PlayOneShot(ShootSfx);
            if (WeaponAnimator)
                WeaponAnimator
                    .SetTrigger("Fire");
            if (MuzzleFlashPrefab)
            {
                var flash = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation, WeaponMuzzle);
                Destroy(flash, 0.5f);
            }

            DetectAndDamage();

            OnShoot?.Invoke();
        }


        private void TryRocketJump()
        {
            if (Time.time >= m_LastRocketJumpTime + RocketJumpCooldown)
            {
                m_LastRocketJumpTime = Time.time;

                var clipToPlay = RocketJumpSfx ? RocketJumpSfx : ShootSfx;
                if (clipToPlay) m_AudioSource.PlayOneShot(clipToPlay);

                if (MuzzleFlashRocketJumpPrefab)
                {
                    var flash = Instantiate(MuzzleFlashRocketJumpPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation,
                        WeaponMuzzle);
                    Destroy(flash, 0.5f);
                }

                ApplyRocketJumpForce();

                OnShoot?.Invoke();
            }
        }

        private void ApplyRocketJumpForce()
        {
            if (Owner)
                if (Owner.TryGetComponent<PlayerCharacterController>(out var playerController))
                {
                    var knockbackDirection = -WeaponMuzzle.forward;
                    playerController.AddForce(knockbackDirection * RocketJumpForce);
                }
        }

        public void StartReloadAnimation()
        {
            // Reload only if full
            if (!IsReloading && m_CurrentAmmo < m_MaxAmmo) StartCoroutine(ReloadSequence());
        }

        private IEnumerator ReloadSequence()
        {
            IsReloading = true;
            //if (ReloadSfx) m_AudioSource.PlayOneShot(ReloadSfx);
            if (AudioManager.instance != null)
            {
                AudioManager.instance.Play("Reload");
                var sound = AudioManager.instance.GetSound("Reload");
                if (sound != null) ReloadDuration = sound.clip.length; // Set the length
            }

            if (WeaponAnimator) WeaponAnimator.SetTrigger("Reload");
            yield return new WaitForSeconds(ReloadDuration);

            if (ammoStock >= 0)
            {
                if (m_CurrentAmmo <= 0) ammoStock = ammoStock - (m_MaxAmmo - m_CurrentAmmo); // Change ammo stock
            }
            else
            {
                Debug.Log("Can't reload");
            }

            m_CurrentAmmo = m_MaxAmmo;

            UpdateHUD();

            IsReloading = false;
        }

        public void UpdateHUD()
        {
            if (UIManager.Instance) UIManager.Instance.UpdateAmmoHUD(m_CurrentAmmo, ammoStock);
        }
    }
}