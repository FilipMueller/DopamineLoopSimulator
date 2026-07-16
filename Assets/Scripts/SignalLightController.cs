using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SignalLightController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image signalLight;

    [Header("Settings")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float flashDuration = 0.7f;

    private readonly Color idleColor = Color.gray;

    private readonly Color[] signalColors =
    {
        Color.green,
        Color.red,
        Color.yellow,
        Color.blue
    };

    private void Start()
    {
        signalLight.color = idleColor;
        StartCoroutine(SignalLoop());
    }

    private IEnumerator SignalLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            Color randomColor = signalColors[Random.Range(0, signalColors.Length)];
            signalLight.color = randomColor;

            yield return new WaitForSeconds(flashDuration);

            signalLight.color = idleColor;
        }
    }
}