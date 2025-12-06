using FPS.Scripts.Game.Shared;
using Unity.FPS.Gameplay;
using UnityEngine;

public class HealthLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private float healthBonus;
    public void Interaction(PlayerCharacterController playerChar)
    {
        Health health = playerChar.GetComponent<Health>();
        if (health != null)
        {
            health.AddHealth(healthBonus);
        }
        Destroy(gameObject);
    }
}
