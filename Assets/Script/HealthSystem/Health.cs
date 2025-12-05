using TMPro;
using Unity.FPS.Gameplay;
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


    private void Death()
    {
        if (TryGetComponent(out PlayerCharacterController characterController)) 
        {
            Debug.Log("Player is dead");

            return;
        }

        // Enemy death
    }
}
