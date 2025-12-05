using UnityEngine;

public class LootRotation : MonoBehaviour
{
    [SerializeField] private float speedRotation;

    // Update is called once per frame
    void Update()
    {
        float move = Time.deltaTime * speedRotation;
        transform.Rotate(0, move, 0);
    }
}
