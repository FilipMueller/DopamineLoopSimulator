using UnityEngine;
using TMPro;

public class RoundTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float roundDuration = 180f; // 3 minutes

    private TMP_Text timerText;
    private float remainingTime;
    private bool timerRunning = true;

    private void Awake()
    {
        timerText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        remainingTime = roundDuration;
        UpdateTimerText();
    }

    private void Update()
    {
        if (!timerRunning)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timerRunning = false;
            Debug.Log("Round timer finished.");
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}