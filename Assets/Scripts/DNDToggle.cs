using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DNDToggle : MonoBehaviour
{
    [SerializeField] private string bureauSceneName = "Main VR scene";
    [SerializeField] private string focusSceneName = "FocusScene";

    private InputDevice rightController;
    private bool bWasPressed = false;

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

        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed);
        if (bPressed && !bWasPressed)
        {
            ToggleDNDMode();
        }
        bWasPressed = bPressed;
    }

    void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
            rightController = devices[0];
    }

    public void ToggleDNDMode()
    {
        string current = SceneManager.GetActiveScene().name;
        string target = (current == bureauSceneName) ? focusSceneName : bureauSceneName;
        Debug.Log($"Scene actuelle: '{current}' | Cible: '{target}' | bureauSceneName='{bureauSceneName}' | focusSceneName='{focusSceneName}'");
        SceneManager.LoadScene(target);
    }
}