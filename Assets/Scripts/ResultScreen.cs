using UnityEngine;
using TMPro;

public class ResultScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    private void Start()
    {
        int hits = PlayerPrefs.GetInt("Hits", 0);
        int lateFails = PlayerPrefs.GetInt("LateFails", 0);
        int falseAlarms = PlayerPrefs.GetInt("FalseAlarms", 0);
        int missedGreens = PlayerPrefs.GetInt("MissedGreens", 0);

        resultText.text =
            "Hits: " + hits + "\n" +
            "Late Fails: " + lateFails + "\n" +
            "False Alarms: " + falseAlarms + "\n" +
            "Missed Greens: " + missedGreens;
    }
}