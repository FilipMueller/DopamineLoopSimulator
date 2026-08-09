using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Minimal session event logger.
/// Records:
/// - Session start/end
/// - Custom events
/// - Head movements during distractions
/// </summary>
public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; }

    [Header("Session")]
    [Tooltip("Ex: WithDistraction or NoDistraction")]
    public string sessionCondition = "";

    [Header("Head Tracking")]
    public Transform headTransform;
    public float headLogFrequency = 10f; // Hz

    private StreamWriter writer;
    private DateTime sessionStartTime;
    private string filePath;
    public string FilePath => filePath;

    private bool trackHead = false;
    private float nextHeadLogTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartSession();
    }

    void Update()
    {
        if (!trackHead || headTransform == null)
            return;

        if (Time.time >= nextHeadLogTime)
        {
            nextHeadLogTime = Time.time + (1f / headLogFrequency);

            Vector3 pos = headTransform.position;
            Vector3 rot = headTransform.eulerAngles;

            LogEvent(
                "HeadPose",
                $"pos=({pos.x:F3};{pos.y:F3};{pos.z:F3}) " +
                $"rot=({rot.x:F1};{rot.y:F1};{rot.z:F1})"
            );
        }
    }

    void StartSession()
    {
        sessionStartTime = DateTime.Now;

        string timestamp = sessionStartTime.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Session_{timestamp}_{sessionCondition}.csv";

        filePath = Path.Combine(Application.persistentDataPath, fileName);

        writer = new StreamWriter(filePath, false);

        writer.WriteLine("ElapsedSeconds,WallClockTime,EventType,Details");
        writer.Flush();

        LogEvent("SessionStart", sessionCondition);

        Debug.Log("[SessionLogger] Logging to: " + filePath);
        StartHeadTracking();
    }

    /// <summary>
    /// Logs an event.
    /// </summary>
    public void LogEvent(string eventType, string details = "")
    {
        if (writer == null)
            return;

        float elapsed = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
        string wallClock = DateTime.Now.ToString("HH:mm:ss.fff");

        string safeDetails = details.Replace(",", ";");

        writer.WriteLine($"{elapsed:F3},{wallClock},{eventType},{safeDetails}");
        writer.Flush();
    }

    /// <summary>
    /// Starts recording head movements.
    /// </summary>
    public void StartHeadTracking()
    {
        if (headTransform == null)
        {
            Debug.LogWarning("SessionLogger: Head Transform not assigned.");
            return;
        }

        trackHead = true;
        nextHeadLogTime = Time.time;

        LogEvent("HeadTrackingStart");
    }

    /// <summary>
    /// Stops recording head movements.
    /// </summary>
    public void StopHeadTracking()
    {
        if (!trackHead)
            return;

        trackHead = false;
        LogEvent("HeadTrackingStop");
    }

    public void EndSession()
    {
        if (writer == null)
            return;

        LogEvent("SessionEnd");

        writer.Close();
        writer = null;
    }

    void OnApplicationQuit()
    {
        EndSession();
    }
}