using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadFocusSceneButton : MonoBehaviour
{
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            LoadFocusScene();
        }
    }

    public void LoadFocusScene()
    {
        Debug.Log("[Scene] Loading FocusScene...");

        if (NBackTaskController.Instance != null)
        {
            NBackTaskController.Instance.ResetForNewExperiment();
        }

        SceneManager.LoadScene("FocusScene");
    }
}