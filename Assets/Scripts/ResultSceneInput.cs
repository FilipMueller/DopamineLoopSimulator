using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// Même schéma que DNDToggle : lecture via l'API XR historique (InputDevice / CommonUsages).
// À poser sur un GameObject de la scène du N-back (pas besoin de DontDestroyOnLoad,
// NBackTaskController.HandleResultSceneTransitionInput() est déjà protégée par taskFinished).
public class ResultSceneInput : MonoBehaviour
{
    private InputDevice rightController;
    private bool triggerWasPressed = false;

    void Start()
    {
        TryInitializeController();
    }

    void Update()
    {
        if (!rightController.isValid)
        {
            TryInitializeController();
            return;
        }

        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed);
        if (triggerPressed && !triggerWasPressed)
        {
            if (NBackTaskController.Instance != null)
                NBackTaskController.Instance.HandleResultSceneTransitionInput();
        }
        triggerWasPressed = triggerPressed;
    }

    void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
            rightController = devices[0];
    }
}