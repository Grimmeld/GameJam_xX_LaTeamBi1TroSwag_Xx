using Unity.FPS.Gameplay;
using UnityEngine;

public class EndGame : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject panelEnd;
    [SerializeField] private Animator animator;
    [SerializeField] private bool isPlayed;

    private void Start()
    {
        
        animator = panelEnd.GetComponent<Animator>();
    }

    public void Interaction(PlayerCharacterController player)
    {
        if (!isPlayed)
        {
            animator.SetTrigger("End");
            isPlayed = true;
        }
    }
}
