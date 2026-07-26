using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeLoader : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;

    [Header("Scene")]
    [SerializeField] private string resultSceneName = "ResultScene";

    private bool isTransitioning = false;

    public void FadeToResultScene()
    {
        if (isTransitioning)
            return;

        StartCoroutine(FadeAndLoadRoutine());
    }

    private IEnumerator FadeAndLoadRoutine()
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;

            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = timer / fadeDuration;
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(resultSceneName);
    }
}