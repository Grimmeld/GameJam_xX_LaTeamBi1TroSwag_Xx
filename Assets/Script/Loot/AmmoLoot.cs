using FPS.Scripts.Game.Shared;
using Unity.FPS.Gameplay;
using UnityEngine;

public class AmmoLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private int bonusAmmo;

    public void Interaction(PlayerCharacterController playerChar)
    {
        WeaponController weaponController = playerChar.GetComponentInChildren<WeaponController>();
        weaponController.ammoStock += bonusAmmo;
        weaponController.UpdateHUD();

        Destroy(gameObject);
    }

}
