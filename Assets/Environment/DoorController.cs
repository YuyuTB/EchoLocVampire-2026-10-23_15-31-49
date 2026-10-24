using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    public bool isLocked = false;
    public bool canBeUnlocked = false;

    private bool _isOpen = false;

    private void Start()
    {

    }

    private void Update()
    {

    }

    // Required method from the IInteractable interface
    public void Interact()
    {
        if (isLocked && canBeUnlocked)
        {
            UnlockDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    public void UnlockDoor()
    {
        if (canBeUnlocked)
        {
            isLocked = false;
            Debug.Log("La porte a été déverrouillée.");
        }
        else
        {
            Debug.Log("La porte ne peut pas être déverrouillée.");
        }
    }

    public void StartMoveUp()
    {
        StartCoroutine(MoveUpRoutine());
    }

    private IEnumerator MoveUpRoutine()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * 4.5f;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            // Smoothly progresses the position over time
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / 1f);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensures the object reaches the exact final position
        transform.position = targetPosition;
    }

    public void OpenDoor()
    {
        // Makes the door slide upwards if it's not locked and not already open
        if (!isLocked && !_isOpen)
        {
            // Slide the door upwards progressively over 1 second
            StartMoveUp();
            _isOpen = true;
        }
        else if (isLocked)
        {
            Debug.Log("La porte est verrouillée.");
        }
        else if (_isOpen)
        {
            Debug.Log("La porte est déjà ouverte.");
        }
    }
}