using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleporter : MonoBehaviour
{
     public void TeleportToFocusRoom()
    {
        if (NBackTaskController.Instance != null)
            NBackTaskController.Instance.BeginTeleportPause();

        SceneManager.LoadScene("Focus Scene");
    }
}