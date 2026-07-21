using System.Collections;
using UnityEngine;

public enum HandSide { Left, Right, Both, None }

/// <summary>
/// Gère la "vibration" du téléphone :
/// - Shake visuel de l'objet 3D : marche toujours, même en hand tracking pur.
///   Le tremblement est calculé de façon RELATIVE à la position actuelle du
///   téléphone à chaque frame (il "retire" le jitter de la frame précédente
///   avant d'en appliquer un nouveau), donc ça reste stable même si le
///   téléphone est en train d'être tenu/déplacé pendant la vibration.
/// - Haptique controller réelle : ne fonctionne QUE si un Touch physique est
///   connecté et tracké. En hand tracking pur, IsControllerConnected renvoie
///   false et cette partie ne fait simplement rien (aucune erreur).
/// </summary>
public class PhoneVibration : MonoBehaviour
{
    [Header("Visual Shake")]
    [SerializeField] private Transform phoneVisualTransform; 
    [SerializeField] private float shakeDuration = 0.6f;
    [SerializeField] private float shakeMagnitude = 0.004f;  
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Controllers shake (only when using controllers)")]
    [SerializeField] private float hapticAmplitude = 0.6f;
    [SerializeField] private float hapticFrequency = 0.5f;
    [SerializeField] private float hapticDuration = 0.4f;

    private Coroutine shakeRoutine;
    private Vector3 lastAppliedOffset = Vector3.zero;
    private bool isHeld = false;

    private void Awake()
    {
        if (phoneVisualTransform == null) phoneVisualTransform = transform;
    }

    /// <summary>
    /// À appeler depuis ton système de grab existant (event Select/Unselect
    /// ou Grab/Release, déjà présent dans l'Inspector du composant qui rend
    /// le téléphone attrapable) : true quand une main le tient, false sinon.
    /// Tant que isHeld est true, le shake est désactivé (le système de grab
    /// pilote déjà la position, pas la peine/pas prudent d'ajouter du jitter
    /// par-dessus).
    /// </summary>
    public void SetHeld(bool held)
    {
        isHeld = held;
    }

   
    public void TriggerVibration(HandSide heldBy)
    {
        TryControllerHaptics(heldBy);

        if (isHeld) return; 

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            phoneVisualTransform.localPosition -= lastAppliedOffset;
            lastAppliedOffset = Vector3.zero;
        }
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            
            phoneVisualTransform.localPosition -= lastAppliedOffset;

            float damper = 1f - (elapsed / shakeDuration); // s'atténue avec le temps
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
            float offsetZ = (Mathf.PerlinNoise(100f, Time.time * shakeFrequency) - 0.5f) * 2f;
            Vector3 newOffset = new Vector3(offsetX, 0f, offsetZ) * shakeMagnitude * damper;

            phoneVisualTransform.localPosition += newOffset;
            lastAppliedOffset = newOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retire le tout dernier pour revenir pile à la position réelle
        phoneVisualTransform.localPosition -= lastAppliedOffset;
        lastAppliedOffset = Vector3.zero;
    }

    private void TryControllerHaptics(HandSide heldBy)
    {
        if (heldBy == HandSide.Left || heldBy == HandSide.Both)
            PulseController(OVRInput.Controller.LTouch);

        if (heldBy == HandSide.Right || heldBy == HandSide.Both)
            PulseController(OVRInput.Controller.RTouch);
    }

    private void PulseController(OVRInput.Controller controller)
    {
        if (!OVRInput.IsControllerConnected(controller)) return; // hand tracking pur -> sort direct
        StartCoroutine(HapticPulseRoutine(controller));
    }

    private IEnumerator HapticPulseRoutine(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(hapticFrequency, hapticAmplitude, controller);
        yield return new WaitForSeconds(hapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}