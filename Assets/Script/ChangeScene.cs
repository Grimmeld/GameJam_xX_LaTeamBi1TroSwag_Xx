using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour, IInteractable
{
    [SerializeField] private string levelName;


    private void Start()
    {
    }

    public void Interaction(PlayerCharacterController player)
    {
        SceneManager.LoadScene(levelName);
    }
}
