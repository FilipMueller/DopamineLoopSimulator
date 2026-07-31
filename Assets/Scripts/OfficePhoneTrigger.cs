using UnityEngine;
using UnityEngine.InputSystem;

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
    if (Keyboard.current.pKey.wasPressedThisFrame)
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
        if (phoneVibration != null) phoneVibration.TriggerVibration(HandSide.None);}
    if (isRinging && Time.time - ringStartTime >= ringtoneAudio.clip.length)
    {
        ringtoneAudio.Stop(); // coupe le son aussi, au cas où Loop soit coché
        screenCanvas.SetActive(false);
        isRinging = false;
    }
    
}
}