using System.Collections;
using UnityEngine;

namespace FPS.Scripts.Cinematic
{ 
    public class CameraRotation : MonoBehaviour
    {
        [Header("Paramètres des Caméras")]
        public Camera cinematicCamera;
        public Camera playerCamera;

        [Header("Réglages de l'Animation")]
        public float rotationDuration = 2.0f;

        private bool isCinematicPlaying = false;

        [SerializeField] public Collider colliderToEnable;

        private void Start()
        {
            if(cinematicCamera) cinematicCamera.gameObject.SetActive(false);
            if(playerCamera) playerCamera.gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isCinematicPlaying)
            {
                StartCoroutine(PlayCinematicSequence());
            }
            
            GetComponentInChildren<LaternSwitcher>().SwitchToSecondColor();
        }

        private IEnumerator PlayCinematicSequence()
        {
            isCinematicPlaying = true;

            playerCamera.gameObject.SetActive(false);
            cinematicCamera.gameObject.SetActive(true);

            Quaternion startRotation = cinematicCamera.transform.rotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, 180);

            float timeElapsed = 0;

            while (timeElapsed < rotationDuration)
            {
                cinematicCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, timeElapsed / rotationDuration);
            
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            cinematicCamera.transform.rotation = endRotation;

            yield return new WaitForSeconds(0.5f);

            cinematicCamera.gameObject.SetActive(false);
            playerCamera.gameObject.SetActive(true);

            isCinematicPlaying = false;
            GetComponent<Collider>().enabled = false;

            colliderToEnable.enabled = true;
        }
    }
}