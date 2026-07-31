using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NBackTaskController : MonoBehaviour
{
    public static NBackTaskController Instance { get; private set; }
    
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

    // Variables d'état pour les boutons et la pause
    private bool hasTaskStarted = false; 
    public bool isPaused = false; 

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
    
    private float sessionStartRealtime;
    private float totalPausedDuration = 0f;
    private float teleportPauseStart;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        sessionStartRealtime = Time.realtimeSinceStartup;
    }

    private void OnEnable()  => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var uiRefs = FindObjectOfType<NBackUIRefs>();
        if (uiRefs != null)
        {
            // 1. On connecte les textes du nouveau Canvas
            stimulusText = uiRefs.stimulusText;
            feedbackText = uiRefs.feedbackText;
            trialCounterText = uiRefs.trialCounterText;

            // 2. On affiche le bon bouton
            if (uiRefs.startButton != null)
                uiRefs.startButton.SetActive(!hasTaskStarted && !taskFinished);
            
            if (uiRefs.resumeButton != null)
                uiRefs.resumeButton.SetActive(hasTaskStarted && isPaused && !taskFinished);

            // --- 3. LA CORRECTION EST ICI : On rafraîchit immédiatement le visuel ---
            if (hasTaskStarted)
            {
                UpdateTrialCounterText(); // Met le bon numéro d'essai (ex: Trial 15/60)
                
                if (feedbackText != null) 
                    feedbackText.text = ""; // Efface les vieux messages de feedback

                if (stimulusText != null)
                {
                    // Si on a déjà des lettres en mémoire, on remet la dernière à l'écran
                    if (stimulusHistory.Count > 0)
                    {
                        stimulusText.text = stimulusHistory[stimulusHistory.Count - 1].ToString();
                    }
                    else
                    {
                        stimulusText.text = "";
                    }
                }
            }
        }
    }

    // --- GESTION DES BOUTONS DE L'UI ---
    public void OnClickStart()
    {
        if (hasTaskStarted) return;
        hasTaskStarted = true;
        
        var uiRefs = FindObjectOfType<NBackUIRefs>();
        if (uiRefs != null && uiRefs.startButton != null) uiRefs.startButton.SetActive(false);

        StartTask();
    }

    public void OnClickResume()
    {
        if (!isPaused) return;
        
        var uiRefs = FindObjectOfType<NBackUIRefs>();
        if (uiRefs != null && uiRefs.resumeButton != null) uiRefs.resumeButton.SetActive(false);

        ResumeTask();
    }

    public void StartTask()
    {
        if (taskRunning) return;

        taskRunning = true;
        taskFinished = false;
        isPaused = false;

        if (feedbackText != null)
            feedbackText.text = "Appuyez quand la lettre correspond au " + nBackLevel + "-back";

        taskCoroutine = StartCoroutine(TaskLoop());
    }

    // --- SYSTÈME DE PAUSE ---
    public void PauseTask()
    {
        if (!hasTaskStarted || taskFinished || isPaused) return;
        
        isPaused = true;
        teleportPauseStart = Time.realtimeSinceStartup;
        Debug.Log("Jeu mis en pause.");
    }
    public void BeginTeleportPause()
    {
        PauseTask();
    }

    private void ResumeTask()
    {
        isPaused = false;
        float pauseDuration = Time.realtimeSinceStartup - teleportPauseStart;
        totalPausedDuration += pauseDuration;
        
        // Ajuste le timer de la lettre en cours pour ne pas fausser le temps de réaction
        currentStimulusStartTime += pauseDuration; 
        
        Debug.Log("Reprise du jeu.");
    }

    public float GetTotalElapsedExcludingTeleport()
        => (Time.realtimeSinceStartup - sessionStartRealtime) - totalPausedDuration;

    // Coroutine personnalisée pour mettre les timers en pause
    private IEnumerator WaitWithPause(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!isPaused)
            {
                elapsed += Time.deltaTime;
            }
            yield return null; 
        }
    }

    // --- LOGIQUE DU N-BACK ---
    private IEnumerator TaskLoop()
    {
        for (currentTrialNumber = 1; currentTrialNumber <= totalTrialCount; currentTrialNumber++)
        {
            yield return new WaitWhile(() => isPaused);

            StartNewTrial();

            yield return StartCoroutine(WaitWithPause(stimulusVisibleDuration));

            if (stimulusText != null)
            {
                stimulusText.text = "";
            }

            float remainingTime = trialDuration - stimulusVisibleDuration;

            if (remainingTime > 0f)
            {
                yield return StartCoroutine(WaitWithPause(remainingTime));
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
        // Empêche de valider une réponse si le jeu est en pause
        if (!taskRunning || taskFinished || isPaused)
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

        PlayerPrefs.SetFloat("NBackSessionDurationExcludingTeleport", GetTotalElapsedExcludingTeleport());

        PlayerPrefs.Save();

        Debug.Log("N-back results saved.");
        Debug.Log("Hits: " + hits);
        Debug.Log("Misses: " + misses);
        Debug.Log("False Alarms: " + falseAlarms);
        Debug.Log("Correct Rejections: " + correctRejections);
        Debug.Log("Accuracy: " + accuracy.ToString("0.000"));
        Debug.Log("Avg RT: " + averageReactionTime.ToString("0.000"));
    }
    // Nouvelle fonction qui décide quoi faire quand on appuie sur "A"
    public void HandleMainInput()
    {
        // 1. Si le jeu n'a pas commencé, "A" clique sur le bouton Start
        if (!hasTaskStarted)
        {
            OnClickStart();
        }
        // 2. Si le jeu est en pause, "A" clique sur le bouton Resume
        else if (isPaused)
        {
            OnClickResume();
        }
        // 3. Sinon (le jeu tourne normalement), "A" sert à jouer au N-Back
        else
        {
            RegisterPlayerResponse();
        }
    }
}