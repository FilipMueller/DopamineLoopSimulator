using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NotificationCard : MonoBehaviour
{
    [Header("Références UI (les enfants de CE panel)")]
    [SerializeField] private Image appIconImage;
    [SerializeField] private TMP_Text appNameText;
    [SerializeField] private TMP_Text notifContentText; // expéditeur + message
    [SerializeField] private TMP_Text timestampText;

    public void Populate(NotificationData data)
    {
        if (appIconImage != null) appIconImage.sprite = data.appIcon;
        if (appNameText != null) appNameText.text = data.appName;

        if (notifContentText != null)
        {
            notifContentText.text = string.IsNullOrEmpty(data.senderName)
                ? data.message
                : $"{data.senderName} : {data.message}";
        }

        if (timestampText != null) timestampText.text = data.timestamp;
    }
}