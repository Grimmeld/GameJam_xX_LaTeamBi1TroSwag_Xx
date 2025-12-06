using Script.Enemy;
using Script.Enemy.Unity.FPS.Game;
using Script.UI;
using System.Collections;
using Unity.FPS.Gameplay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthEnemy : MonoBehaviour
{

    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;

    [SerializeField] private CanvasRenderer healthCanvas;
    [SerializeField] private Renderer enemyRend;
    private Color m_OriginalColor;
    public Color DamageColor = Color.yellow;

    [Header("SFX/Effect")]
    [SerializeField] private string deathSound;
    private AudioManager audioManager;

    private void Start()
    {
        currentHealth = maxHealth;
        healthCanvas = GetComponentInChildren<CanvasRenderer>();
        UpdateHUD();

        enemyRend = GetComponent<Renderer>();
        if(enemyRend) { m_OriginalColor = enemyRend.material.color; }

        audioManager = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<AudioManager>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHUD();

        if (enemyRend) StartCoroutine(FlashColor());

        //Debug.Log($"{name} took: {damage} dmg");

        if (currentHealth <= 0)
        {
            // Death

            // Disable attack while sound and death
            //if (TryGetComponent(out EnemyAI enemyAI))
            //{
            //    enemyAI.enabled = false;
            //}

            //if (TryGetComponent(out FlyingEnemyAi flyingEnemyAI))
            //{
            //    flyingEnemyAI.enabled = false;
            //}
            //if (deathSound != null)
            //{
            //    Sound sound = audioManager.GetSound(deathSound);

            //    Debug.Log(sound.nameMusic);
            //    audioManager.PlayOnActor(deathSound, this.gameObject);

            //    Death(sound.clip.length);

            //}
            //else { Death(0f); }
            //}
            Death(0f);

        }
    }

    public void GetHealth(float bonus)
    {

        currentHealth += bonus;

        if (currentHealth >= maxHealth)
        {
            // Clamp max health
            currentHealth = maxHealth;
        }

        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (healthCanvas)
        {
            Slider healthSlider = healthCanvas.GetComponentInChildren<Slider>();
            if (healthSlider)
            {
                healthSlider.value = currentHealth / maxHealth;
            }
        }
    }


    private void Death(float deathTime)
    {
            //Debug.Log("Enemy dead");
            Destroy(this.gameObject, deathTime);
    }

    private IEnumerator FlashColor()
    {
        if (enemyRend) enemyRend.material.color = DamageColor;
        yield return new WaitForSeconds(0.2f);
        if (enemyRend) enemyRend.material.color = m_OriginalColor;
    }

}
