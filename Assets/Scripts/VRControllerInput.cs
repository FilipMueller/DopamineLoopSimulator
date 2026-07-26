using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class VRControllerInput : MonoBehaviour
{
    [Header("Quest Controller Input")]
    [SerializeField] private OVRInput.RawButton confirmButton = OVRInput.RawButton.A;
    [SerializeField] private OVRInput.RawButton cancelButton = OVRInput.RawButton.B;

    [Header("Events")]
    [SerializeField] private UnityEvent onConfirmPressed;
    [SerializeField] private UnityEvent onCancelPressed;

    private void Update()
    {
        bool confirmPressed =
            OVRInput.GetDown(confirmButton) ||
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        bool cancelPressed =
            OVRInput.GetDown(cancelButton) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);

        if (confirmPressed)
        {
            Debug.Log("Confirm pressed");
            onConfirmPressed.Invoke();
        }

        if (cancelPressed)
        {
            Debug.Log("Cancel pressed");
            onCancelPressed.Invoke();
        }
    }
}