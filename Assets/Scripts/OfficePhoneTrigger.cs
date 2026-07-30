using UnityEngine;

public class OfficePhoneTrigger : MonoBehaviour
{
    [Header("Composants à lier")]
    public AudioSource ringtoneAudio;
    public GameObject screenCanvas;

    void Update()
    {
        // On écoute si la touche P est pressée
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Allumer l'écran s'il est assigné
            if (screenCanvas != null)
            {
                screenCanvas.SetActive(true);
            }

            // Lancer la sonnerie si elle n'est pas déjà en cours
            if (ringtoneAudio != null && !ringtoneAudio.isPlaying)
            {
                ringtoneAudio.Play();
            }
        }
    }
}