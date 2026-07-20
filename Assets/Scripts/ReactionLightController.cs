using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReactionLightController : MonoBehaviour
{
    [Header("Light UI")]
    [SerializeField] private Image signalLight;

    [Header("Optional Text UI")]
    [SerializeField] private TMP_Text resultText;

    [Header("Timing")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float lightDuration = 0.8f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.gray;

    private Color[] possibleColors =
    {
        Color.green,
        Color.red,
        Color.yellow,
        Color.blue
    };

    private bool waitingForReaction = false;
    private bool greenIsCurrentlyVisible = false;

    private float greenStartTime;

    private int hits = 0;
    private int lateFails = 0;
    private int falseAlarms = 0;
    private int missedGreens = 0;

    private void Start()
    {
        signalLight.color = idleColor;
        StartCoroutine(LightLoop());
    }

    private IEnumerator LightLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            Color nextColor = possibleColors[Random.Range(0, possibleColors.Length)];

            // If a new green starts while the previous green was never answered,
            // count the previous one as missed.
            if (nextColor == Color.green && waitingForReaction)
            {
                missedGreens++;
                waitingForReaction = false;
                greenIsCurrentlyVisible = false;
                Debug.Log("Missed green: no button press before next green.");
            }

            signalLight.color = nextColor;

            if (nextColor == Color.green)
            {
                StartGreenTimer();
            }

            yield return new WaitForSeconds(lightDuration);

            if (nextColor == Color.green)
            {
                // Green is gone, but the timer is still open.
                // If the user presses now, it will count as late fail.
                greenIsCurrentlyVisible = false;
            }

            signalLight.color = idleColor;
        }
    }

    private void StartGreenTimer()
    {
        greenStartTime = Time.time;
        waitingForReaction = true;
        greenIsCurrentlyVisible = true;

        Debug.Log("Green appeared. Timer started.");
    }

    public void PressReactionButton()
    {
        if (!waitingForReaction)
        {
            falseAlarms++;
            Debug.Log("False alarm: button pressed but no green signal is active/pending.");

            if (resultText != null)
            {
                resultText.text = "False alarm";
            }

            return;
        }

        float reactionTime = Time.time - greenStartTime;

        if (greenIsCurrentlyVisible)
        {
            hits++;
            Debug.Log("HIT. Reaction time: " + reactionTime.ToString("0.000") + " seconds");

            if (resultText != null)
            {
                resultText.text = "Hit: " + reactionTime.ToString("0.000") + " s";
            }
        }
        else
        {
            lateFails++;
            Debug.Log("LATE FAIL. Reaction time: " + reactionTime.ToString("0.000") + " seconds");

            if (resultText != null)
            {
                resultText.text = "Late: " + reactionTime.ToString("0.000") + " s";
            }
        }

        waitingForReaction = false;
        greenIsCurrentlyVisible = false;
    }
}