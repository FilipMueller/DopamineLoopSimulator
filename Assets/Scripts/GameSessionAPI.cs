using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameSessionAPI : MonoBehaviour
{
    public static GameSessionAPI Instance { get; private set; }

    [SerializeField]
    private string baseUrl = "http://192.168.6.106:3000";

    private string sessionId;

    public string SessionId => sessionId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(CreateGameSession());
    }

    // =========================================================
    // CREATE SESSION
    // POST /game-sessions
    // =========================================================

    private IEnumerator CreateGameSession()
    {
        string url = baseUrl + "/game-sessions";

        Debug.Log("[API] POST " + url);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes("{}");

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[API] Could not create session.\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "Error: " + request.error + "\n" +
                    "Response: " + request.downloadHandler.text
                );

                yield break;
            }

            Debug.Log(
                "[API] Create session response:\n" +
                request.downloadHandler.text
            );

            GameSessionResponse response =
                JsonUtility.FromJson<GameSessionResponse>(
                    request.downloadHandler.text
                );

            if (response == null ||
                string.IsNullOrEmpty(response._id))
            {
                Debug.LogError(
                    "[API] Server returned no _id."
                );
                yield break;
            }

            sessionId = response._id;

            Debug.Log(
                "[API] Session UUID = " + sessionId
            );
        }
    }


    // =========================================================
    // SEND SCORE JSON
    // =========================================================

    public void SendScore(
        string json,
        bool distraction)
    {
        if (!CheckSessionId())
            return;

        string type =
            distraction
                ? "distraction"
                : "distractionless";

        StartCoroutine(
            PatchScoreCoroutine(type, json)
        );
    }

    private IEnumerator PatchScoreCoroutine(
        string type,
        string json)
    {
        string url =
            baseUrl +
            "/game-sessions/" +
            sessionId +
            "/score/" +
            type;

        Debug.Log(
            "[API] PATCH " + url +
            "\nBody:\n" + json
        );

        using (UnityWebRequest request =
               new UnityWebRequest(url, "PATCH"))
        {
            byte[] body =
                Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[API] Score PATCH failed.\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "Error: " + request.error + "\n" +
                    "Response: " + request.downloadHandler.text
                );

                yield break;
            }

            Debug.Log(
                "[API] Score PATCH successful:\n" +
                request.downloadHandler.text
            );
        }
    }


    // =========================================================
    // UPLOAD DISTRACTION CSV
    // POST /distractions
    // multipart/form-data
    // field name = "file"
    // =========================================================

    public void UploadDistractionFile(string filePath)
    {
        if (!CheckSessionId())
            return;

        if (!File.Exists(filePath))
        {
            Debug.LogError(
                "[API] Distraction CSV not found:\n" +
                filePath
            );

            return;
        }

        StartCoroutine(
            UploadDistractionFileCoroutine(filePath)
        );
    }

    private IEnumerator UploadDistractionFileCoroutine(
        string filePath)
    {
        string url =
            baseUrl +
            "/game-sessions/" +
            sessionId +
            "/distractions";

        Debug.Log(
            "[API] POST CSV to: " + url
        );

        Debug.Log(
            "[API] CSV file: " + filePath
        );

        byte[] fileData;

        try
        {
            fileData = File.ReadAllBytes(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[API] Could not read CSV:\n" +
                e.Message
            );

            yield break;
        }

        WWWForm form = new WWWForm();

        // "file" MUST match your Postman form-data key.
        form.AddBinaryData(
            "file",
            fileData,
            Path.GetFileName(filePath),
            "text/csv"
        );

        using (UnityWebRequest request =
               UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[API] CSV upload failed.\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "Error: " + request.error + "\n" +
                    "Response: " + request.downloadHandler.text
                );

                yield break;
            }

            Debug.Log(
                "[API] CSV upload successful:\n" +
                request.downloadHandler.text
            );
        }
    }


    // =========================================================

    private bool CheckSessionId()
    {
        if (!string.IsNullOrEmpty(sessionId))
            return true;

        Debug.LogError(
            "[API] Cannot send request because sessionId is empty."
        );

        return false;
    }
}


[Serializable]
public class GameSessionResponse
{
    public string _id;
    public string timestamp;
}