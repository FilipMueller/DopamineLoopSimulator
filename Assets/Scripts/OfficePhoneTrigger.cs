using UnityEngine;
// Plus besoin de UnityEngine.InputSystem ici !

public class OfficePhoneTrigger : MonoBehaviour
{
    [Header("Composants à lier")]
    public AudioSource ringtoneAudio;
    public GameObject screenCanvas;
    [SerializeField] private PhoneVibration phoneVibration;
    
    private bool isRinging = false;
    private float ringStartTime;

    void Start()
    {
        if (screenCanvas != null)
        {
            screenCanvas.SetActive(false);
        }
    }

    void Update()
    {
        // L'Update ne sert plus qu'à vérifier si on doit ARRÊTER la sonnerie
        if (isRinging && Time.time - ringStartTime >= ringtoneAudio.clip.length)
        {
            ringtoneAudio.Stop(); 
            if (screenCanvas != null) screenCanvas.SetActive(false);
            isRinging = false;
        }
    }

    /// <summary>
    /// Fonction publique appelée par le DistractionInputManager
    /// </summary>
    public void RingPhone()
    {
        if (screenCanvas != null)
        {
            screenCanvas.SetActive(true);
        }

        if (ringtoneAudio != null && !ringtoneAudio.isPlaying)
        {
            ringtoneAudio.Play();
            ringStartTime = Time.time;
            isRinging = true;
        }
        
        if (phoneVibration != null) 
        {
            phoneVibration.TriggerVibration(HandSide.None);
        }
    }
}