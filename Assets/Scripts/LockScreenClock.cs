using UnityEngine;
using TMPro;
using System;
using System.Globalization;


public class LockScreenClock : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dateText;

    private const float RefreshInterval = 1f;
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < RefreshInterval) return;
        timer = 0f;

        DateTime now = DateTime.Now;

        if (timeText != null)
            timeText.text = now.ToString("HH:mm");

        if (dateText != null)
            dateText.text = now.ToString("dddd d MMMM", new CultureInfo("en-US"));
    }
}
