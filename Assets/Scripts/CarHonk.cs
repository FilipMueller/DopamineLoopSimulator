using UnityEngine;
using UnityEngine.InputSystem; // N'oublie pas d'importer le namespace

public class CarHonk : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource honkAudioSource; 
    public AudioClip honkClip;        

    void Update()
    {
        // Vérifie si le clavier est actif et si la touche 'H' vient d'être enfoncée
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            PlayHonk();
        }
    }

    public void PlayHonk()
    {
        if (honkAudioSource != null && honkClip != null)
        {
            honkAudioSource.PlayOneShot(honkClip);
        }
    }
}