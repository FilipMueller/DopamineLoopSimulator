using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleporter : MonoBehaviour
{
    public void TeleportToFocusRoom()
    {
        SceneManager.LoadScene("Focus Scene");
    }
}