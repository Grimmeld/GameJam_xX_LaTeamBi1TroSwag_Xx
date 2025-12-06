using Unity.FPS.Gameplay;
using UnityEngine;

public class TriggerMusic : MonoBehaviour, IInteractable
{
    [SerializeField] private string musicName;
    [SerializeField] private bool isPlayed;

    private AudioManager audioManager;

    private void Start()
    {
        if(AudioManager.instance != null)
            audioManager = AudioManager.instance;
    }

    public void Interaction(PlayerCharacterController player)
    {
        if (audioManager != null && musicName != null
            && !isPlayed)
        {
            audioManager.Play(musicName);
            isPlayed = true;
        }
    }
}
