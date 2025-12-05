using TMPro;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;


    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHUD();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHUD();

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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthHUD(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage(10f);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GetHealth(20f);
        }
    }

    private void Death()
    {
        Debug.Log("Player is dead");
    }
}
