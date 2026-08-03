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
    [SerializeField] private string resultSceneName = "ResultScene";

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
        // Pas de transition automatique : on attend l'appui sur la gâchette
        // (cf. HandleResultSceneTransitionInput), même principe que le bouton B
        // pour la transition scène 1 / scène 2.
    }

    // À appeler depuis là où la gâchette est déjà lue. C'est la SEULE façon de
    // rejoindre la Result Scene : ne fait rien tant que la tâche n'est pas
    // terminée, pour éviter de sauter vers des résultats qui n'existent pas encore.
    public void HandleResultSceneTransitionInput()
    {
        if (!taskFinished) return;

        SceneManager.LoadScene(resultSceneName);
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