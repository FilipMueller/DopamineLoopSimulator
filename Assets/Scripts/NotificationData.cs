using UnityEngine;


[System.Serializable]
public class NotificationData
{
    public string appName;         // "Messenger", "Instagram", "Slack"...
    public Sprite appIcon;         

    public string senderName;      // optional
    [TextArea(2, 4)]
    public string message;         

    public string timestamp;       // optional
}