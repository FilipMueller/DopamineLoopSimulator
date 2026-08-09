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
            (Keyboard.current != null &&
             Keyboard.current.spaceKey.wasPressedThisFrame);

        bool cancelPressed =
            OVRInput.GetDown(cancelButton) ||
            (Keyboard.current != null &&
             Keyboard.current.escapeKey.wasPressedThisFrame);


        // A
        if (confirmPressed)
        {
            Debug.Log("[Input] A pressed");
            onConfirmPressed?.Invoke();
        }


        // B
        if (cancelPressed)
        {
            Debug.Log("[Input] B pressed");
            onCancelPressed?.Invoke();
        }
    }
}