using UnityEngine;

public class EndSceneController : MonoBehaviour
{
    [SerializeField] private GameObject endSceneUIPrefab;
    private AudioSource _audioPlayer;
    private bool _isTriggered = false;

    private void Start()
    {
        _audioPlayer = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;

        if (other.CompareTag("Player"))
        {
            _isTriggered = true;

            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.isInCutscene = true;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            PlayEndSceneAudio();
            DisplayGameEndingCanvas();

            float delay = (_audioPlayer != null && _audioPlayer.clip != null) ? _audioPlayer.clip.length + 1f : 2f;
            Destroy(gameObject, delay);
        }
    }

    private void PlayEndSceneAudio()
    {
        if (_audioPlayer != null && _audioPlayer.clip != null)
        {
            _audioPlayer.Play();
        }
    }

    private void DisplayGameEndingCanvas()
    {
        if (endSceneUIPrefab != null)
        {
            Instantiate(endSceneUIPrefab);
        }
        else
        {
            Debug.LogWarning("EndSceneUIPrefab n'est pas assigné dans l'inspecteur !");
        }
    }
}