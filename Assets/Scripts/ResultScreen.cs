using TMPro;
using UnityEngine;

// To be placed on a GameObject in the Result Scene.
// Reads PlayerPrefs written by NBackTaskController.SaveResults() and displays them.
// No reference to NBackTaskController is needed here: the result scene
// can be completely decoupled from the task scene.
public class NBackResultDisplay : MonoBehaviour
{
    [Header("Summary (Simple option: a single block of text)")]
    [SerializeField] private TMP_Text summaryText;

    [Header("Detail (Option: one field per statistic, leave empty if unused)")]
    [SerializeField] private TMP_Text hitsText;
    [SerializeField] private TMP_Text missesText;
    [SerializeField] private TMP_Text falseAlarmsText;
    [SerializeField] private TMP_Text correctRejectionsText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text avgReactionTimeText;
    [SerializeField] private TMP_Text dPrimeText;
    [SerializeField] private TMP_Text sessionDurationText;

    private void Start()
    {
        DisplayResults();
    }

    private void DisplayResults()
    {
        int nBackLevel = PlayerPrefs.GetInt("NBackLevel", 0);
        int totalTrials = PlayerPrefs.GetInt("NBackTotalTrials", 0);
        int targetTrials = PlayerPrefs.GetInt("NBackTargetTrials", 0);

        int hits = PlayerPrefs.GetInt("NBackHits", 0);
        int misses = PlayerPrefs.GetInt("NBackMisses", 0);
        int falseAlarms = PlayerPrefs.GetInt("NBackFalseAlarms", 0);
        int correctRejections = PlayerPrefs.GetInt("NBackCorrectRejections", 0);

        float accuracy = PlayerPrefs.GetFloat("NBackAccuracy", 0f);
        float avgReactionTime = PlayerPrefs.GetFloat("NBackAverageReactionTime", 0f);
        float dPrime = PlayerPrefs.GetFloat("NBackDPrime", 0f);
        float sessionDuration = PlayerPrefs.GetFloat("NBackSessionDurationExcludingTeleport", 0f);

        // Separate fields
        if (hitsText != null) hitsText.text = "Hits: " + hits;
        if (missesText != null) missesText.text = "Misses: " + misses;
        if (falseAlarmsText != null) falseAlarmsText.text = "False Alarms: " + falseAlarms;
        if (correctRejectionsText != null) correctRejectionsText.text = "Correct Rejections: " + correctRejections;
        if (accuracyText != null) accuracyText.text = "Accuracy: " + (accuracy * 100f).ToString("0.0") + " %";
        if (avgReactionTimeText != null) avgReactionTimeText.text = "Average Reaction Time: " + (avgReactionTime * 1000f).ToString("0") + " ms";
        if (dPrimeText != null) dPrimeText.text = "Sensitivity (d'): " + dPrime.ToString("0.00");
        if (sessionDurationText != null) sessionDurationText.text = "Session Duration: " + FormatDuration(sessionDuration);

        // Single summary block
        if (summaryText != null)
        {
            summaryText.text =
                nBackLevel + "-back — " + totalTrials + " trials (" + targetTrials + " targets)\n\n" +
                "Hits: " + hits + "\n" +
                "Misses: " + misses + "\n" +
                "False Alarms: " + falseAlarms + "\n" +
                "Correct Rejections: " + correctRejections + "\n\n" +
                "Accuracy: " + (accuracy * 100f).ToString("0.0") + " %\n" +
                "Average Reaction Time: " + (avgReactionTime * 1000f).ToString("0") + " ms\n" +
                "Sensitivity (d'): " + dPrime.ToString("0.00") + "\n\n" +
                "Session Duration: " + FormatDuration(sessionDuration);
        }
    }

    private string FormatDuration(float seconds)
    {
        int totalSeconds = Mathf.RoundToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return minutes + "min " + secs.ToString("00") + "s";
    }
}