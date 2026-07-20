using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlaylist : MonoBehaviour
{
    [SerializeField] private AudioClip[] songs;
    [SerializeField] private bool shuffle = false;

    private AudioSource audioSource;
    private int currentSongIndex = 0;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (songs == null || songs.Length == 0)
        {
            Debug.LogWarning("No songs assigned to MusicPlaylist.");
            return;
        }

        audioSource.loop = false;
        StartCoroutine(PlaylistRoutine());
    }

    private IEnumerator PlaylistRoutine()
    {
        while (true)
        {
            PlayCurrentSong();

            yield return new WaitWhile(() => audioSource.isPlaying);

            GoToNextSong();
        }
    }

    private void PlayCurrentSong()
    {
        audioSource.clip = songs[currentSongIndex];
        audioSource.Play();

        Debug.Log("Now playing: " + songs[currentSongIndex].name);
    }

    private void GoToNextSong()
    {
        if (shuffle)
        {
            currentSongIndex = Random.Range(0, songs.Length);
        }
        else
        {
            currentSongIndex++;

            if (currentSongIndex >= songs.Length)
            {
                currentSongIndex = 0;
            }
        }
    }
}