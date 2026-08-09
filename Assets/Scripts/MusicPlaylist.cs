using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlaylist : MonoBehaviour
{
    public static MusicPlaylist Instance { get; private set; }
    [SerializeField] private AudioClip[] songs;
    [SerializeField] private bool shuffle = false;

    private AudioSource audioSource;
    private int currentSongIndex = 0;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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

            // Wait until the song actually finishes.
            // If we manually pause it, stay here.
            while (audioSource.isPlaying || isPaused)
            {
                yield return null;
            }

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

    public void PauseMusic()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
        {
            isPaused = true;
            audioSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (audioSource == null)
            return;

        if (isPaused)
        {
            audioSource.UnPause();
            isPaused = false;
        }
    }
}