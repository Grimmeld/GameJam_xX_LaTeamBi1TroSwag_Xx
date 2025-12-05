using Unity.VisualScripting;
using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [SerializeField] private GameObject loot; // faire une liste
    // choisir le loot selon une liste
    // - Si pas beaucoup de munition > Loot munition
    // - Pareil pour la santé

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    Destroy(gameObject);
        //}
    }

    private void OnDestroy()
    {
        if(loot != null)
        Instantiate(loot, transform.position, transform.rotation);

    }
}
