using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [SerializeField] private TMP_Text nBackTitleText;
    [SerializeField] private TMP_Text tutorialText;

    [Header("N-Back Settings")]
    [SerializeField] private int nBackLevel = 2;
    [SerializeField] private string possibleLetters = "ABCDEFGHJKLMNPQRST";

    [Header("Trial Settings")]
    [SerializeField] private int totalTrialCount = 60;
    [SerializeField] private bool enableTutorial = true;
    [SerializeField] private int tutorialTrialCount = 10;
    [SerializeField] private float trialDuration = 2.5f;
    [SerializeField] private float stimulusVisibleDuration = 1.0f;

    [Header("Target Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float targetProbability = 0.3f;

    [Header("Scene Transition")]
    [SerializeField] private string resultSceneName = "ResultScene";

    [Header("Sounds")]
    [SerializeField] private AudioSource roadAmbience;

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

    private enum TaskPhase
    {
        Tutorial,
        TutorialFinished,
        Experiment,
        Finished
    }

    private TaskPhase currentPhase;

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
        FindAndAssignUI();

        var uiRefs = FindObjectOfType<NBackUIRefs>();

        if (uiRefs == null)
            return;

        if (uiRefs.startButton != null)
        {
            uiRefs.startButton.SetActive(
                !hasTaskStarted && !taskFinished
            );
        }

        if (uiRefs.resumeButton != null)
        {
            uiRefs.resumeButton.SetActive(
                hasTaskStarted &&
                isPaused &&
                !taskFinished
            );
        }

        RestoreUIForCurrentPhase();
    }

    private void RestoreUIForCurrentPhase()
    {
        UpdateNBackTitleText();

        if (!hasTaskStarted)
        {
            if (tutorialText != null)
            {
                tutorialText.text = "";
                tutorialText.gameObject.SetActive(false);
            }

            return;
        }

        if (currentPhase == TaskPhase.Tutorial && !taskRunning)
        {
            ShowTutorialInstructions();
            return;
        }

        if (currentPhase == TaskPhase.TutorialFinished)
        {
            if (stimulusText != null)
                stimulusText.text = "";

            if (trialCounterText != null)
                trialCounterText.text = "";

            if (feedbackText != null)
                feedbackText.text = "";

            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(true);

                tutorialText.text =
                    "TUTORIAL COMPLETE\n\n" +
                    "You are now ready for the real experiment.\n\n" +
                    "No feedback will be shown during the experiment.\n\n" +
                    "Press A to begin.";
            }

            return;
        }

        // Running tutorial/experiment
        if (tutorialText != null)
        {
            tutorialText.text = "";
            tutorialText.gameObject.SetActive(false);
        }

        if (taskRunning)
        {
            UpdateTrialCounterText();
        }
    }

    private void FindAndAssignUI()
    {
        var uiRefs = FindObjectOfType<NBackUIRefs>();

        if (uiRefs == null)
        {
            Debug.LogWarning("[NBack] No NBackUIRefs found in current scene.");
            return;
        }

        stimulusText = uiRefs.stimulusText;
        feedbackText = uiRefs.feedbackText;
        trialCounterText = uiRefs.trialCounterText;
        nBackTitleText = uiRefs.nBackTitleText;
        tutorialText = uiRefs.tutorialText;

        Debug.Log(
            "[NBack] UI connected. " +
            "Title=" + (nBackTitleText != null) +
            ", Tutorial=" + (tutorialText != null)
        );

        UpdateNBackTitleText();
    }

    // --- GESTION DES BOUTONS DE L'UI ---
    public void OnClickStart()
    {
        if (hasTaskStarted) return;

        hasTaskStarted = true;

        var uiRefs = FindObjectOfType<NBackUIRefs>();
        if (uiRefs != null && uiRefs.startButton != null)
            uiRefs.startButton.SetActive(false);

        if (enableTutorial)
        {
            currentPhase = TaskPhase.Tutorial;

            ShowTutorialInstructions();
        }
        else
        {
            currentPhase = TaskPhase.Experiment;
            StartExperiment();
        }
    }

    private void ShowTutorialInstructions()
    {
        taskRunning = false;
        taskFinished = false;
        isPaused = false;

        UpdateNBackTitleText();

        if (stimulusText != null)
            stimulusText.text = "";

        if (trialCounterText != null)
            trialCounterText.text = "";

        if (feedbackText != null)
            feedbackText.text = "";

        if (tutorialText == null)
        {
            Debug.LogError(
                "[NBack] Cannot show tutorial: tutorialText is NULL. " +
                "Check NBackUIRefs in the Inspector."
            );

            return;
        }

        tutorialText.gameObject.SetActive(true);

        tutorialText.text =
            "TUTORIAL\n\n" +
            "Press A when the current letter matches\n" +
            "the letter shown " + nBackLevel + " positions ago.\n\n" +
            "You will receive feedback during the tutorial.\n\n" +
            "Press A to begin.";

        Debug.Log("[NBack] Tutorial instructions displayed.");
    }

    public void HandleTutorialRestartInput()
    {
        // B should only restart after the tutorial has finished
        if (currentPhase != TaskPhase.TutorialFinished)
            return;

        Debug.Log("[NBack] Restarting tutorial.");

        ResetStatistics();

        currentTrialNumber = 0;

        taskRunning = false;
        taskFinished = false;
        isPaused = false;

        currentPhase = TaskPhase.Tutorial;

        ShowTutorialInstructions();
    }

    public void ResetForNewExperiment()
    {
        Debug.Log("[NBack] Resetting controller for new experiment.");

        // Stop old task coroutine if it still exists
        if (taskCoroutine != null)
        {
            StopCoroutine(taskCoroutine);
            taskCoroutine = null;
        }

        hasTaskStarted = false;
        taskRunning = false;
        taskFinished = false;
        isPaused = false;

        currentTrialNumber = 0;

        ResetStatistics();

        // Reset timing
        sessionStartRealtime = Time.realtimeSinceStartup;
        totalPausedDuration = 0f;

        // Start from tutorial again if enabled
        if (enableTutorial)
            currentPhase = TaskPhase.Tutorial;
        else
            currentPhase = TaskPhase.Experiment;

        Debug.Log("[NBack] Controller reset complete.");
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

        UpdateNBackTitleText();

        if (feedbackText != null)
            feedbackText.text = "Appuyez quand la lettre correspond au " + nBackLevel + "-back";

        taskCoroutine = StartCoroutine(TaskLoop());
    }

    // --- SYSTÈME DE PAUSE ---
    public void PauseTask()
    {
        // Pausing is ONLY allowed during the real experiment
        if (currentPhase != TaskPhase.Experiment)
        {
            Debug.Log(
                "[NBack] Pause ignored because current phase is: " +
                currentPhase
            );
            return;
        }

        if (!hasTaskStarted ||
            taskFinished ||
            isPaused ||
            !taskRunning)
        {
            return;
        }

        if (roadAmbience != null)
            roadAmbience.Pause();

        isPaused = true;
        teleportPauseStart = Time.realtimeSinceStartup;

        // Show resume window
        var uiRefs = FindObjectOfType<NBackUIRefs>();

        if (uiRefs != null && uiRefs.resumeButton != null)
        {
            uiRefs.resumeButton.SetActive(true);
        }

        // Pause music
        MusicPlaylist.Instance?.PauseMusic();

        Debug.Log("[NBack] Experiment paused.");
    }
    public void BeginTeleportPause()
    {
        PauseTask();
    }

    private void ResumeTask()
    {
        float pauseDuration =
            Time.realtimeSinceStartup - teleportPauseStart;

        totalPausedDuration += pauseDuration;

        // Adjust reaction-time reference
        currentStimulusStartTime += pauseDuration;

        isPaused = false;

        if (roadAmbience != null)
            roadAmbience.UnPause();

        // Continue music from exact same position
        MusicPlaylist.Instance?.ResumeMusic();

        Debug.Log("Reprise du jeu.");
    }

    private void UpdateNBackTitleText()
    {
        if (nBackTitleText == null)
        {
            Debug.LogError(
                "[NBack] Cannot update title: nBackTitleText is NULL."
            );

            return;
        }

        nBackTitleText.text = nBackLevel + "-Back Test";

        Debug.Log(
            "[NBack] Title updated to: " +
            nBackTitleText.text
        );
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
        int numberOfTrials;

        if (currentPhase == TaskPhase.Tutorial)
        {
            numberOfTrials = tutorialTrialCount;
        }
        else
        {
            numberOfTrials = totalTrialCount;
        }

        for (currentTrialNumber = 1; currentTrialNumber <= numberOfTrials; currentTrialNumber++)
        {
            yield return new WaitWhile(() => isPaused);

            StartNewTrial();

            yield return StartCoroutine(
                WaitWithPause(stimulusVisibleDuration)
            );

            if (stimulusText != null)
            {
                stimulusText.text = "";
            }

            float remainingTime = trialDuration - stimulusVisibleDuration;

            if (remainingTime > 0f)
            {
                yield return StartCoroutine(
                    WaitWithPause(remainingTime)
                );
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
        bool shouldCreateTarget = canCreateTarget && UnityEngine.Random.value < targetProbability;

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
        int randomIndex = UnityEngine.Random.Range(0, possibleLetters.Length);
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
                if (currentPhase == TaskPhase.Tutorial)
                {
                    feedbackText.text = "Correct";
                }
            }

            Debug.Log("HIT. RT: " + reactionTime.ToString("0.000") + " s");
        }
        else
        {
            falseAlarms++;

            if (feedbackText != null)
            {
                if (currentPhase == TaskPhase.Tutorial)
                {
                    feedbackText.text = "False alarm";
                }
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
        // =====================================================
        // TUTORIAL FINISHED
        // =====================================================
        if (currentPhase == TaskPhase.Tutorial)
        {
            taskRunning = false;
            taskFinished = false;

            currentPhase = TaskPhase.TutorialFinished;

            if (stimulusText != null)
                stimulusText.text = "";

            if (trialCounterText != null)
                trialCounterText.text = "";

            if (feedbackText != null)
                feedbackText.text = "";

            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(true);

                tutorialText.text =
                    "TUTORIAL COMPLETE\n\n" +
                    "A - Begin experiment\n\n" +
                    "B - Repeat tutorial";
            }

            UpdateNBackTitleText();

            Debug.Log("[NBack] Tutorial finished.");

            return;
        }


        // =====================================================
        // REAL EXPERIMENT FINISHED
        // =====================================================
        if (currentPhase == TaskPhase.Experiment)
        {
            taskRunning = false;
            taskFinished = true;

            currentPhase = TaskPhase.Finished;

            SaveResults();

            if (stimulusText != null)
                stimulusText.text = "";

            if (trialCounterText != null)
                trialCounterText.text = "";

            if (feedbackText != null)
                feedbackText.text = "";

            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(true);

                tutorialText.text =
                    "EXPERIMENT FINISHED\n\n" +
                    "Press the Right Trigger\n" +
                    "to view your results.";
            }

            UpdateNBackTitleText();

            Debug.Log("[NBack] Experiment finished.");

            return;
        }
    }

    public void HandleCancelInput()
    {
        // =====================================================
        // TUTORIAL
        // B = restart tutorial
        // =====================================================
        if (currentPhase == TaskPhase.Tutorial ||
            currentPhase == TaskPhase.TutorialFinished)
        {
            RestartTutorial();
            return;
        }

        // =====================================================
        // REAL EXPERIMENT
        // B = pause
        // =====================================================
        if (currentPhase == TaskPhase.Experiment)
        {
            if (!isPaused && taskRunning)
            {
                PauseTask();
            }

            return;
        }

        // =====================================================
        // EXPERIMENT FINISHED
        // B does nothing.
        // Right Trigger handles the Result Scene.
        // =====================================================
        if (currentPhase == TaskPhase.Finished || taskFinished)
        {
            Debug.Log(
                "[NBack] Experiment already finished. " +
                "Use Right Trigger to continue to results."
            );

            return;
        }
    }

    private void RestartTutorial()
    {
        Debug.Log("[NBack] Restarting tutorial.");

        // Stop current tutorial if it is still running
        if (taskCoroutine != null)
        {
            StopCoroutine(taskCoroutine);
            taskCoroutine = null;
        }

        taskRunning = false;
        taskFinished = false;
        isPaused = false;

        currentTrialNumber = 0;

        ResetStatistics();

        currentPhase = TaskPhase.Tutorial;

        ShowTutorialInstructions();
    }

    // À appeler depuis là où la gâchette est déjà lue. C'est la SEULE façon de
    // rejoindre la Result Scene : ne fait rien tant que la tâche n'est pas
    // terminée, pour éviter de sauter vers des résultats qui n'existent pas encore.
    public void HandleResultSceneTransitionInput()
    {
        if (!taskFinished)
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        Debug.Log("[NBack] Leaving experiment scene: " + currentScene);

        // Create the N-back result JSON directly from the current values.
        string scoreJson = CreateResultsJson();


        // =====================================================
        // FOCUS SCENE = NO DISTRACTIONS
        // =====================================================

        if (currentScene == "FocusScene")
        {
            Debug.Log("[NBack] Sending distractionless score.");

            if (GameSessionAPI.Instance != null)
            {
                GameSessionAPI.Instance.SendScore(
                    scoreJson,
                    false
                );
            }
            else
            {
                Debug.LogError("[NBack] GameSessionAPI.Instance is null!");
            }
        }


        // =====================================================
        // MAIN VR SCENE = DISTRACTIONS
        // =====================================================

        else if (currentScene == "Main VR Scene")
        {
            Debug.Log("[NBack] Sending distraction score.");

            if (GameSessionAPI.Instance != null)
            {
                // Send N-back JSON
                GameSessionAPI.Instance.SendScore(
                    scoreJson,
                    true
                );
            }

            // Stop the distraction/session logger first.
            if (DistractionInputManager.Instance != null)
            {
                DistractionInputManager.Instance
                    .LogResultsTransitionAndStopLogging();
            }

            // Upload the CSV file.
            if (GameSessionAPI.Instance != null &&
                SessionLogger.Instance != null)
            {
                string csvPath =
                    SessionLogger.Instance.FilePath;

                Debug.Log(
                    "[NBack] Uploading distraction CSV: " +
                    csvPath
                );

                GameSessionAPI.Instance
                    .UploadDistractionFile(csvPath);
            }
        }


        // =====================================================
        // NOW GO TO RESULTS
        // =====================================================

        SceneManager.LoadScene(resultSceneName);
    }

    private void UpdateTrialCounterText()
    {
        if (trialCounterText != null)
        {
            int totalTrials =
                currentPhase == TaskPhase.Tutorial
                    ? tutorialTrialCount
                    : totalTrialCount;

            trialCounterText.text =
                "Trial " + currentTrialNumber + " / " + totalTrials;
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

        // nonTargetTrials = essais où la lettre n'était PAS une cible
        int nonTargetTrials = totalTrials - targetTrials;

        float hitRate = targetTrials > 0 ? (float)hits / targetTrials : 0f;
        float falseAlarmRate = nonTargetTrials > 0 ? (float)falseAlarms / nonTargetTrials : 0f;
        float dPrime = ComputeDPrime(targetTrials, nonTargetTrials, hits, falseAlarms);

        PlayerPrefs.SetInt("NBackLevel", nBackLevel);
        PlayerPrefs.SetInt("NBackTotalTrials", totalTrials);
        PlayerPrefs.SetInt("NBackTargetTrials", targetTrials);

        PlayerPrefs.SetInt("NBackHits", hits);
        PlayerPrefs.SetInt("NBackMisses", misses);
        PlayerPrefs.SetInt("NBackFalseAlarms", falseAlarms);
        PlayerPrefs.SetInt("NBackCorrectRejections", correctRejections);

        PlayerPrefs.SetFloat("NBackAccuracy", accuracy);
        PlayerPrefs.SetFloat("NBackAverageReactionTime", averageReactionTime);

        PlayerPrefs.SetFloat("NBackHitRate", hitRate);
        PlayerPrefs.SetFloat("NBackFalseAlarmRate", falseAlarmRate);
        PlayerPrefs.SetFloat("NBackDPrime", dPrime);

        PlayerPrefs.SetFloat("NBackSessionDurationExcludingTeleport", GetTotalElapsedExcludingTeleport());

        PlayerPrefs.Save();

        Debug.Log("N-back results saved.");
        Debug.Log("Hits: " + hits);
        Debug.Log("Misses: " + misses);
        Debug.Log("False Alarms: " + falseAlarms);
        Debug.Log("Correct Rejections: " + correctRejections);
        Debug.Log("Accuracy: " + accuracy.ToString("0.000"));
        Debug.Log("Avg RT: " + averageReactionTime.ToString("0.000"));
        Debug.Log("d': " + dPrime.ToString("0.000"));

        SaveResultsToJson();
        Debug.Log("N-back results saved.");
    }

    // --- STATISTIQUES DE DÉTECTION DU SIGNAL (d') ---
    // d' mesure la capacité à distinguer les cibles du bruit, indépendamment du biais de réponse
    // (contrairement à la précision brute, qui peut être gonflée par un participant qui répond souvent "au cas où").
    // Correction log-linéaire (Hautus, 1995) pour éviter les valeurs infinies quand hitRate = 1 ou falseAlarmRate = 0.
    private float ComputeDPrime(int targetTrialCount, int nonTargetTrialCount, int hitCount, int falseAlarmCount)
    {
        float adjustedHitRate = (hitCount + 0.5f) / (targetTrialCount + 1f);
        float adjustedFalseAlarmRate = (falseAlarmCount + 0.5f) / (nonTargetTrialCount + 1f);

        return NormSInv(adjustedHitRate) - NormSInv(adjustedFalseAlarmRate);
    }

    // Approximation de l'inverse de la fonction de répartition normale (probit),
    // algorithme d'Acklam. Précision largement suffisante pour un usage comportemental.
    private float NormSInv(float p)
    {
        if (p <= 0f) p = 0.0001f;
        if (p >= 1f) p = 0.9999f;

        double[] a = { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };
        double[] b = { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01 };
        double[] c = { -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00 };
        double[] d = { 7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00 };

        double pLow = 0.02425;
        double pHigh = 1 - pLow;
        double q, r, result;
        double pd = p;

        if (pd < pLow)
        {
            q = System.Math.Sqrt(-2 * System.Math.Log(pd));
            result = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                     ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        else if (pd <= pHigh)
        {
            q = pd - 0.5;
            r = q * q;
            result = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                     (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }
        else
        {
            q = System.Math.Sqrt(-2 * System.Math.Log(1 - pd));
            result = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                      ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }

        return (float)result;
    }

    // Nouvelle fonction qui décide quoi faire quand on appuie sur "A"
    public void HandleMainInput()
    {
        // 1. Start screen -> show tutorial or start experiment
        if (!hasTaskStarted)
        {
            OnClickStart();
            return;
        }

        // 2. Tutorial instructions -> start tutorial
        if (currentPhase == TaskPhase.Tutorial && !taskRunning)
        {
            StartTutorial();
            return;
        }

        // 3. Tutorial finished -> start experiment
        if (currentPhase == TaskPhase.TutorialFinished)
        {
            StartExperiment();
            return;
        }

        // 4. Game paused -> resume
        if (isPaused)
        {
            OnClickResume();
            return;
        }

        // 5. Task is running -> register N-back response
        if (taskRunning)
        {
            RegisterPlayerResponse();
        }
    }

    private void ResetStatistics()
    {
        hits = 0;
        misses = 0;
        falseAlarms = 0;
        correctRejections = 0;

        targetTrials = 0;

        totalReactionTime = 0;
        reactionTimeCount = 0;

        stimulusHistory.Clear();
    }

    private void StartExperiment()
    {
        currentPhase = TaskPhase.Experiment;

        ResetStatistics();

        taskRunning = true;
        taskFinished = false;
        isPaused = false;

        UpdateNBackTitleText();

        if (tutorialText != null)
        {
            tutorialText.text = "";
            tutorialText.gameObject.SetActive(false);
        }

        if (feedbackText != null) {
            feedbackText.text = "";
        }

        taskCoroutine = StartCoroutine(TaskLoop());
    }

    private void StartTutorial()
    {
        currentPhase = TaskPhase.Tutorial;

        ResetStatistics();

        taskRunning = true;
        taskFinished = false;
        isPaused = false;

        UpdateNBackTitleText();

        if (tutorialText != null)
        {
            tutorialText.text = "";
            tutorialText.gameObject.SetActive(false);
        }

        if (feedbackText != null)
            feedbackText.text = "";

        taskCoroutine = StartCoroutine(TaskLoop());
    }

    private void SaveResultsToJson()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string sessionCondition = "";

        if (SessionLogger.Instance != null)
        {
            sessionCondition = SessionLogger.Instance.sessionCondition;
        }

        NBackResults results = new NBackResults
        {
            nBackLevel = nBackLevel,
            totalTrials = totalTrialCount,
            targetTrials = targetTrials,

            hits = hits,
            misses = misses,
            falseAlarms = falseAlarms,
            correctRejections = correctRejections,

            accuracy = (float)(hits + correctRejections) / totalTrialCount,

            averageReactionTime =
                reactionTimeCount > 0
                    ? totalReactionTime / reactionTimeCount
                    : 0f,

            hitRate =
                targetTrials > 0
                    ? (float)hits / targetTrials
                    : 0f,

            falseAlarmRate =
                (totalTrialCount - targetTrials) > 0
                    ? (float)falseAlarms / (totalTrialCount - targetTrials)
                    : 0f,

            dPrime = ComputeDPrime(
                targetTrials,
                totalTrialCount - targetTrials,
                hits,
                falseAlarms
            ),

            sessionDuration =
                GetTotalElapsedExcludingTeleport()
        };

        string json = JsonUtility.ToJson(results, true);

        string fileName =
            $"NBackResults_{timestamp}_{sessionCondition}.json";

        string filePath =
            Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(filePath, json);

        Debug.Log("[NBack] Results JSON saved to: " + filePath);
    }

    private string CreateResultsJson()
    {
        NBackResults results = new NBackResults
        {
            nBackLevel = nBackLevel,
            totalTrials = totalTrialCount,
            targetTrials = targetTrials,

            hits = hits,
            misses = misses,
            falseAlarms = falseAlarms,
            correctRejections = correctRejections,

            accuracy =
                totalTrialCount > 0
                    ? (float)(hits + correctRejections)
                      / totalTrialCount
                    : 0f,

            averageReactionTime =
                reactionTimeCount > 0
                    ? totalReactionTime / reactionTimeCount
                    : 0f,

            hitRate =
                targetTrials > 0
                    ? (float)hits / targetTrials
                    : 0f,

            falseAlarmRate =
                (totalTrialCount - targetTrials) > 0
                    ? (float)falseAlarms /
                      (totalTrialCount - targetTrials)
                    : 0f,

            dPrime =
                ComputeDPrime(
                    targetTrials,
                    totalTrialCount - targetTrials,
                    hits,
                    falseAlarms
                ),

            sessionDuration =
                GetTotalElapsedExcludingTeleport()
        };

        return JsonUtility.ToJson(results, true);
    }

    private void Start()
    {
        FindAndAssignUI();
    }
}

[Serializable]
public class NBackResults
{
    public int nBackLevel;
    public int totalTrials;
    public int targetTrials;

    public int hits;
    public int misses;
    public int falseAlarms;
    public int correctRejections;

    public float accuracy;
    public float averageReactionTime;
    public float hitRate;
    public float falseAlarmRate;
    public float dPrime;

    public float sessionDuration;
}
