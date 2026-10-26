using UnityEngine;
using System.Collections;

public class FeedingSceneController : MonoBehaviour
{
    [SerializeField] private GameObject feedingScenePlayer;
    [SerializeField] private AudioClip[] feedingSceneAudioClips;
    private AudioSource _audioPlayer;

    private void Start()
    {
        _audioPlayer = feedingScenePlayer.GetComponent<AudioSource>();
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(FeedingSceneSequence(other.GetComponent<PlayerController>()));
        }
    }

    private IEnumerator FeedingSceneSequence(PlayerController player)
    {
        player.isInCutscene = true;
        PlayFeedingSceneAudio();
        yield return new WaitForSeconds(_audioPlayer.clip.length + 1.5f);
        player.isInCutscene = false;
        // After the feeding scene, destroy the player and this controller
        Destroy(feedingScenePlayer);
        Destroy(gameObject);
    }

    private void PlayFeedingSceneAudio()
    {
        if (feedingSceneAudioClips.Length > 0)
        {
            // Play the first audio clip THEN the second one after the first finishes
            _audioPlayer.clip = feedingSceneAudioClips[0];
            _audioPlayer.Play();
            StartCoroutine(PlaySecondAudioAfterFirst());
        }
    }

    private IEnumerator PlaySecondAudioAfterFirst()
    {
        yield return new WaitForSeconds(_audioPlayer.clip.length + 1f);
        if (feedingSceneAudioClips.Length > 1)
        {
            _audioPlayer.clip = feedingSceneAudioClips[1];
            _audioPlayer.Play();
        }
    }
}
