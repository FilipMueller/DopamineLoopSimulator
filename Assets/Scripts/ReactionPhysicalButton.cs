using UnityEngine;

public class ReactionPhysicalButton : MonoBehaviour
{
    [SerializeField] private ReactionLightController reactionLightController;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonPressSound;

    [Header("Press Settings")]
    [SerializeField] private float pressCooldown = 0.5f;

    private float lastPressTime = -999f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastPressTime < pressCooldown)
            return;

        lastPressTime = Time.time;

        PlayButtonSound();

        Debug.Log("Reaction button pressed by: " + other.name);

        if (reactionLightController != null)
        {
            reactionLightController.PressReactionButton();
        }
        else
        {
            Debug.LogWarning("ReactionLightController is not assigned.");
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource == null || buttonPressSound == null)
            return;

        audioSource.PlayOneShot(buttonPressSound);
    }
}