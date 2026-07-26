using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NBackTaskController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text stimulusText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text trialCounterText;

    [Header("N-Back Settings")]
    [SerializeField] private int nBackLevel = 2;
    [SerializeField] private string possibleLetters = "ABCDEFGHJKLMNPQRST";

    [Header("Trial Settings")]
    [SerializeField] private int totalTrialCount = 60;
    [SerializeField] private float trialDuration = 2.5f;
    [SerializeField] private float stimulusVisibleDuration = 1.0f;

    [Header("Target Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float targetProbability = 0.3f;

    [Header("Scene Transition")]
    [SerializeField] private SceneFadeLoader sceneFadeLoader;
    [SerializeField] private float endDelay = 1.0f;

    private readonly List<char> stimulusHistory = new List<char>();

    private bool taskRunning = false;
    private bool currentIsTarget = false;
    private bool responseGivenThisTrial = false;
    private bool taskFinished = false;

    private float currentStimulusStartTime = 0f;

    private int currentTrialNumber = 0;
    private int targetTrials = 0;

    private int hits = 0;
    private int misses = 0;
    private int falseAlarms = 0;
    private int correctRejections = 0;

    private float totalReactionTime = 0f;
    private int reactionTimeCount = 0;

    private Coroutine taskCoroutine;

    private void Start()
    {
        StartTask();
    }

    public void StartTask()
    {
        if (taskRunning)
            return;

        taskRunning = true;
        taskFinished = false;

        if (feedbackText != null)
        {
            feedbackText.text = "Press when current letter matches " + nBackLevel + "-back";
        }

        taskCoroutine = StartCoroutine(TaskLoop());
    }

    private IEnumerator TaskLoop()
    {
        for (currentTrialNumber = 1; currentTrialNumber <= totalTrialCount; currentTrialNumber++)
        {
            StartNewTrial();

            yield return new WaitForSeconds(stimulusVisibleDuration);

            if (stimulusText != null)
            {
                stimulusText.text = "";
            }

            float remainingTime = trialDuration - stimulusVisibleDuration;

            if (remainingTime > 0f)
            {
                yield return new WaitForSeconds(remainingTime);
            }

            FinishTrial();
        }

        FinishTask();
    }

    private void StartNewTrial()
    {
        responseGivenThisTrial = false;
        currentStimulusStartTime = Time.time;

        UpdateTrialCounterText();

        char nextLetter = GenerateNextLetter();

        stimulusHistory.Add(nextLetter);

        if (currentIsTarget)
        {
            targetTrials++;
        }

        if (stimulusText != null)
        {
            stimulusText.text = nextLetter.ToString();
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        Debug.Log("Trial " + currentTrialNumber + "/" + totalTrialCount +
                  ": " + nextLetter + " Target: " + currentIsTarget);
    }

    private char GenerateNextLetter()
    {
        bool canCreateTarget = stimulusHistory.Count >= nBackLevel;
        bool shouldCreateTarget = canCreateTarget && Random.value < targetProbability;

        if (shouldCreateTarget)
        {
            currentIsTarget = true;
            return stimulusHistory[stimulusHistory.Count - nBackLevel];
        }

        currentIsTarget = false;

        char randomLetter = GetRandomLetter();

        if (canCreateTarget)
        {
            char nBackLetter = stimulusHistory[stimulusHistory.Count - nBackLevel];

            int safetyCounter = 0;

            while (randomLetter == nBackLetter && safetyCounter < 100)
            {
                randomLetter = GetRandomLetter();
                safetyCounter++;
            }
        }

        return randomLetter;
    }

    private char GetRandomLetter()
    {
        int randomIndex = Random.Range(0, possibleLetters.Length);
        return possibleLetters[randomIndex];
    }

    public void RegisterPlayerResponse()
    {
        if (!taskRunning || taskFinished)
            return;

        if (responseGivenThisTrial)
            return;

        responseGivenThisTrial = true;

        float reactionTime = Time.time - currentStimulusStartTime;

        if (currentIsTarget)
        {
            hits++;
            totalReactionTime += reactionTime;
            reactionTimeCount++;

            if (feedbackText != null)
            {
                feedbackText.text = "Correct";
            }

            Debug.Log("HIT. RT: " + reactionTime.ToString("0.000") + " s");
        }
        else
        {
            falseAlarms++;

            if (feedbackText != null)
            {
                feedbackText.text = "False alarm";
            }

            Debug.Log("FALSE ALARM");
        }
    }

    private void FinishTrial()
    {
        if (currentIsTarget && !responseGivenThisTrial)
        {
            misses++;
            Debug.Log("MISS");
        }
        else if (!currentIsTarget && !responseGivenThisTrial)
        {
            correctRejections++;
        }
    }

    private void FinishTask()
    {
        taskRunning = false;
        taskFinished = true;

        SaveResults();

        if (stimulusText != null)
        {
            stimulusText.text = "Finished";
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Task complete";
        }

        Debug.Log("N-back task finished.");

        StartCoroutine(TransitionToResultScene());
    }

    private IEnumerator TransitionToResultScene()
    {
        yield return new WaitForSeconds(endDelay);

        if (sceneFadeLoader != null)
        {
            sceneFadeLoader.FadeToResultScene();
        }
        else
        {
            Debug.LogWarning("SceneFadeLoader is not assigned.");
        }
    }

    private void UpdateTrialCounterText()
    {
        if (trialCounterText != null)
        {
            trialCounterText.text = "Trial " + currentTrialNumber + " / " + totalTrialCount;
        }
    }

    public void SaveResults()
    {
        int totalTrials = totalTrialCount;

        float accuracy = 0f;

        if (totalTrials > 0)
        {
            accuracy = (float)(hits + correctRejections) / totalTrials;
        }

        float averageReactionTime = 0f;

        if (reactionTimeCount > 0)
        {
            averageReactionTime = totalReactionTime / reactionTimeCount;
        }

        PlayerPrefs.SetInt("NBackLevel", nBackLevel);
        PlayerPrefs.SetInt("NBackTotalTrials", totalTrials);
        PlayerPrefs.SetInt("NBackTargetTrials", targetTrials);

        PlayerPrefs.SetInt("NBackHits", hits);
        PlayerPrefs.SetInt("NBackMisses", misses);
        PlayerPrefs.SetInt("NBackFalseAlarms", falseAlarms);
        PlayerPrefs.SetInt("NBackCorrectRejections", correctRejections);

        PlayerPrefs.SetFloat("NBackAccuracy", accuracy);
        PlayerPrefs.SetFloat("NBackAverageReactionTime", averageReactionTime);

        PlayerPrefs.Save();

        Debug.Log("N-back results saved.");
        Debug.Log("Hits: " + hits);
        Debug.Log("Misses: " + misses);
        Debug.Log("False Alarms: " + falseAlarms);
        Debug.Log("Correct Rejections: " + correctRejections);
        Debug.Log("Accuracy: " + accuracy.ToString("0.000"));
        Debug.Log("Avg RT: " + averageReactionTime.ToString("0.000"));
    }
}