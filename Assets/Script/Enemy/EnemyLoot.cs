using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [Header("Set up")]
    [SerializeField] private Vector3 offset;

    [Header("Loot")]
    [SerializeField] private List<GameObject> loots;

    //[Header("Parametre apparition")]
    //[SerializeField] private int minAmmo;
    // choisir le loot selon une liste
    // - Si pas beaucoup de munition > Loot munition
    // - Pareil pour la sante

    private void OnDestroy()
    {
        if(loots.Count > 0)
        {
            int rand = Random.Range(0, loots.Count);
            Vector3 position = transform.position + offset;
            if (loots[rand] != null)
            {
                Instantiate(loots[rand], position, transform.rotation);
            }
        }

    }
}
