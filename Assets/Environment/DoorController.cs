using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("UI de Message (Prefab)")]
    [SerializeField] private GameObject unlockUIPrefab; // Glisse ton Prefab de Canvas de message ici dans l'inspecteur

    public bool isLocked = false;
    public bool canBeUnlocked = false;

    private bool _isOpen = false;
    private GameObject _currentUnlockCanvasInstance;
    private Coroutine _currentUnlockCoroutine;

    private void Start()
    {

    }

    private void Update()
    {

    }

    // Required method from the IInteractable interface
    public void Interact(PlayerController player)
    {
        if (isLocked && canBeUnlocked && player.hasExitKey)
        {
            UnlockDoor();
        }
        else if (isLocked && !canBeUnlocked)
        {
            ShowUnlockMessage("That door is locked. You cannot open it at the moment.");
        }
        else if (isLocked && !player.hasExitKey)
        {
            ShowUnlockMessage("That door is locked. You need a key to open it.");
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
            OpenDoor();
        }
    }

    private void ShowUnlockMessage(string message)
    {
        // Si un message est déjà affiché, on le nettoie proprement avant d'afficher le nouveau
        if (_currentUnlockCoroutine != null)
        {
            StopCoroutine(_currentUnlockCoroutine);
            if (_currentUnlockCanvasInstance != null)
            {
                Destroy(_currentUnlockCanvasInstance);
            }
        }

        _currentUnlockCoroutine = StartCoroutine(ShowUnlockMessageCoroutine(message));
    }

    private IEnumerator ShowUnlockMessageCoroutine(string message)
    {
        // 1. Instancier le Prefab du Canvas à l'écran
        if (unlockUIPrefab != null)
        {
            _currentUnlockCanvasInstance = Instantiate(unlockUIPrefab);

            // 2. Mettre à jour le texte (compatible Text classique ou TextMeshPro)
            var textComponent = _currentUnlockCanvasInstance.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
            else
            {
                var tmpComponent = _currentUnlockCanvasInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpComponent != null)
                {
                    tmpComponent.text = message;
                }
            }
        }

        // 3. Attendre 2 secondes en temps réel
        yield return new WaitForSecondsRealtime(2f);

        // 4. Détruire (dé-pop) l'instance du Canvas
        if (_currentUnlockCanvasInstance != null)
        {
            Destroy(_currentUnlockCanvasInstance);
        }
        _currentUnlockCoroutine = null;
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
    }
}