using Unity.FPS.Gameplay;
using UnityEngine;

namespace Script.Enemy
{
    public class EnemyProjectile : MonoBehaviour
    {
        public float Speed = 20f;
        public float Damage = 10f;
        public float MaxLifeTime = 5f;

        private void Start()
        {
            Destroy(gameObject, MaxLifeTime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * (Speed * Time.deltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            // if player hi
            PlayerCharacterController player = other.GetComponent<PlayerCharacterController>();
            if (player != null)
            {
                player.SendMessage("TakeDamage", Damage, SendMessageOptions.DontRequireReceiver);
                Destroy(gameObject);
            }
            
            else if (!other.isTrigger && other.gameObject.layer != gameObject.layer)
            {
                Destroy(gameObject);
            }
        }
    }
}