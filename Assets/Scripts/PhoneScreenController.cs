using UnityEngine;
using System.Collections;


public class PhoneScreenController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup screenCanvasGroup;
    [SerializeField] private Light screenGlowLight;          

    [Header("movement detection")]
    [SerializeField] private float movementThreshold = 0.15f; // m/s
    [SerializeField] private float rotationThreshold = 40f;   // deg/s

    [Header("Timing")]
    [SerializeField] private float sleepDelay = 6f;   // secondes d'inactivité avant extinction
    [SerializeField] private float fadeDuration = 0.15f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float sleepTimer;
    private bool isAwake;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        SetScreenInstant(false); // écran éteint au démarrage
    }

    private void Update()
    {
        DetectMovement();

        if (isAwake)
        {
            sleepTimer -= Time.deltaTime;
            if (sleepTimer <= 0f) SetScreen(false);
        }
    }

    private void DetectMovement()
    {
        if (Time.deltaTime <= 0f) return;

        float linearSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        float angularSpeed = Quaternion.Angle(transform.rotation, lastRotation) / Time.deltaTime;

        if (linearSpeed > movementThreshold || angularSpeed > rotationThreshold)
            WakeUp();

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

   
    public void WakeUp()
    {
        sleepTimer = sleepDelay;
        if (!isAwake) SetScreen(true);
    }

    private void SetScreen(bool on)
    {
        isAwake = on;
        if (screenGlowLight != null) screenGlowLight.enabled = on;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(on ? 1f : 0f));
    }

    private void SetScreenInstant(bool on)
    {
        isAwake = on;
        if (screenGlowLight != null) screenGlowLight.enabled = on;
        if (screenCanvasGroup != null) screenCanvasGroup.alpha = on ? 1f : 0f;
    }

    private IEnumerator FadeTo(float target)
    {
        if (screenCanvasGroup == null) yield break;

        float start = screenCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            screenCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        screenCanvasGroup.alpha = target;
    }
}
