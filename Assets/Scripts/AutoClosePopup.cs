using System.Collections;
using UnityEngine;

// Cette ligne force Unity à ajouter un AudioSource si tu l'as oublié
[RequireComponent(typeof(AudioSource))]
public class AutoClosePopup : MonoBehaviour
{
    [Header("Réglages du Pop-up")]
    [Tooltip("Temps en secondes avant que le pop-up ne disparaisse.")]
    [SerializeField] private float displayDuration = 3.0f;

    [Header("Son (Optionnel)")]
    [Tooltip("Le son qui se joue à l'apparition du pop-up.")]
    [SerializeField] private AudioClip popupSound;

    private AudioSource audioSource;

    private void Awake()
    {
        // Récupère le composant AudioSource caché sur l'objet
        audioSource = GetComponent<AudioSource>();
        
        // Empêche le son de se jouer tout seul au démarrage du jeu
        audioSource.playOnAwake = false; 
    }

    private void OnEnable()
    {
        // Dès que le pop-up s'allume, on joue le son (s'il y en a un)
        if (popupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popupSound);
        }

        // On lance le compte à rebours
        StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        gameObject.SetActive(false);
    }
}