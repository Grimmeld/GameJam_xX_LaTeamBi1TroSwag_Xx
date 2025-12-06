using System;
using Script.Enemy;
using Script.Enemy.Unity.FPS.Game;
using Script.UI;
using TMPro;
using Unity.FPS.Gameplay;
using UnityEngine;

public class Health : MonoBehaviour
{

    [SerializeField] private float maxHealth;
    private float currentHealth;


    public Action<float, float> OnPlayerHealthChanged;
    

    public Action OnPlayerDied;
    
    
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHUD();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

        UpdateHUD();
        
        Debug.Log($"{name} took: {damage} dmg");

        if (currentHealth <= 0)
        {
            // Death
            Death();
        }

    }

    public void AddHealth(float bonus)
    {
        currentHealth += bonus;
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth >= maxHealth)
        {
            // Clamp max health
            currentHealth = maxHealth;
        }

        UpdateHUD();
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    private void UpdateHUD()
    {
        if (UIManager.Instance) 
        {
            UIManager.Instance.UpdateHealthHUD(currentHealth, maxHealth);
        }
    }


    private void Death()
    {
        if (TryGetComponent(out PlayerCharacterController characterController)) 
        {
            Debug.Log("Player is dead");
            
            OnPlayerDied?.Invoke();

            return;
        }
    }
}
