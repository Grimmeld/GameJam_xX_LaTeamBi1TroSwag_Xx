using System;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class Teleporter : MonoBehaviour
    {
        [SerializeField] public Transform destination;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                
                other.gameObject.transform.position = destination.position;
            }
        }
    }
}