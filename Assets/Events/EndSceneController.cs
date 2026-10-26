using UnityEngine;

public class EndSceneController : MonoBehaviour
{
    private AudioSource _audioPlayer;
    
    private void Start()
    {
        _audioPlayer = GetComponent<AudioSource>();
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Trigger the end scene logic here
            PlayEndSceneAudio();
            // Optionally, you can disable the trigger to prevent multiple triggers
            Destroy(gameObject, _audioPlayer.clip.length + 1f); // Destroy after audio finishes
        }
    }

    private void PlayEndSceneAudio()
    {
        if (_audioPlayer != null)
        {
            _audioPlayer.Play();
        }
    }
}
