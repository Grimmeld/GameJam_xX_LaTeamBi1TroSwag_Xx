using Script.Enemy;
using Script.Enemy.Unity.FPS.Game;
using Script.UI;
using System.Collections;
using Unity.FPS.Gameplay;
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

    private void Start()
    {
        currentHealth = maxHealth;
        healthCanvas = GetComponentInChildren<CanvasRenderer>();
        UpdateHUD();

        enemyRend = GetComponent<Renderer>();
        if(enemyRend) { m_OriginalColor = enemyRend.material.color; }

    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHUD();

        if (enemyRend) StartCoroutine(FlashColor());

        Debug.Log($"{name} took: {damage} dmg");

        if (currentHealth <= 0)
        {
            // Death
            Death();
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
        if (healthCanvas != null)
        {
            Slider healthSlider = healthCanvas.GetComponentInChildren<Slider>();
            if (healthSlider != null)
            {
                healthSlider.value = currentHealth / maxHealth;
            }
        }
    }


    private void Death()
    {
            Debug.Log("Enemy dead");
            Destroy(this.gameObject);
    }

    private IEnumerator FlashColor()
    {
        if (enemyRend) enemyRend.material.color = DamageColor;
        yield return new WaitForSeconds(0.2f);
        if (enemyRend) enemyRend.material.color = m_OriginalColor;
    }
}
