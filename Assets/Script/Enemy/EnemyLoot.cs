using FPS.Scripts.Game.Shared;
using Script.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class EnemyLoot : MonoBehaviour
{
    [Header("Set up")]
    [SerializeField] private Vector3 offset;

    [Header("Loot")]
    [SerializeField] private List<GameObject> loots;

    [Header("Parametre apparition")]
    [SerializeField] private int minAmmo;
    [SerializeField] private int minHealth;
    // choisir le loot selon une liste
    // - Si pas beaucoup de munition > Loot munition
    // - Pareil pour la sante

    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }


    private void OnDestroy()
    {
        if(loots.Count > 0)
        {
            if (player != null)
            {
                // Choose Ammo 
                if (player.GetComponentInChildren<WeaponController>().ammoStock <= minAmmo)
                {
                    Vector3 position = transform.position + offset;

                    foreach (GameObject loot in loots)
                    {
                        if (loot.TryGetComponent(out AmmoLoot ammoLoot))
                        {
                            Instantiate(ammoLoot.gameObject, position, transform.rotation);
                        }
                    }

                }

                // Choose health
                else if (player.GetComponent<Health>().GetHealth() <= minHealth)
                {
                    Vector3 position = transform.position + offset;

                    foreach (GameObject loot in loots)
                    {
                        if (loot.TryGetComponent(out HealthLoot healthLoot))
                        {
                            Instantiate(healthLoot.gameObject, position, transform.rotation);
                        }
                    }
                }

                //Randomize
                else
                {
                    GetRandomLoot();
                }

            }
            else
            {
                // Sinon randomiser le loot
                GetRandomLoot();
            }

        }

    }

    private void GetRandomLoot()
    {
        int rand = Random.Range(0, loots.Count);
        Vector3 position = transform.position + offset;
        if (loots[rand] != null)
        {
            Instantiate(loots[rand], position, transform.rotation);
        }

    }
}
