using UnityEngine;
using UnityEngine.SceneManagement;

public class VRStartMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Main VR Scene";
    [SerializeField] private string focusScene = "FocusScene";

    public void StartWithDistractions()
    {
        PlayerPrefs.SetInt("DistractionsEnabled", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void StartWithoutDistractions()
    {
        PlayerPrefs.SetInt("DistractionsEnabled", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(focusScene);
    }
}