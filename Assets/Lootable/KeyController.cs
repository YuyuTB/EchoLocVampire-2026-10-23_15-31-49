using UnityEngine;

public class KeyController : MonoBehaviour, IInteractable
{
    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    public void Interact(PlayerController player)
    {
        if (player != null)
        {
            player.hasExitKey = true;
        }
        Destroy(gameObject);
    }
}
