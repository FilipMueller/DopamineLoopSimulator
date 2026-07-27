using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class NotificationManager : MonoBehaviour
{
    [Header("Model NotificationPanel (in the scene)")]
    [SerializeField] private RectTransform notificationTemplate;
    [SerializeField] private Transform notificationParent; 

    [Header("Notification Cards Empilement")]
    [SerializeField] private float cardSpacing = 5f;
    [SerializeField] private int maxVisibleNotifications = 5;

    [Header("Vibration")]
    [SerializeField] private PhoneVibration phoneVibration;

    [Header("Screen On/Off")]
    [SerializeField] private PhoneScreenController screenController;

    [Header("Audio Notification")]
    public AudioSource audioSource;
    public AudioClip notifSound;

    private readonly List<RectTransform> activeCards = new List<RectTransform>();
    private Vector2 restingPosition;

    private void Awake()
    {
        restingPosition = notificationTemplate.anchoredPosition;
        notificationTemplate.gameObject.SetActive(false); // le modèle ne s'affiche jamais lui-même
    }

    /// <summary>
    /// Ajoute une nouvelle notification en haut de la pile. Les notifs déjà
    /// affichées descendent d'un cran, elles ne disparaissent pas.
    /// </summary>
    public void Enqueue(NotificationData data)
    {
        if (screenController != null) screenController.WakeUp();

        if (phoneVibration != null)
            phoneVibration.TriggerVibration(HandSide.Both);

        if (audioSource != null && notifSound != null)
        {
            audioSource.PlayOneShot(notifSound);
        }

        // On ne bloque plus ici : on crée toujours la nouvelle notification
        SpawnCard(data);
    }

    private void SpawnCard(NotificationData data)
    {
        // 1. SI on a déjà atteint la limite (5), on supprime IMMÉDIATEMENT la plus ancienne (celle tout en bas)
        // avant même de bouger les autres. Ça empêche de dépasser le bas de l'écran.
        while (activeCards.Count >= maxVisibleNotifications)
        {
            int lastIndex = activeCards.Count - 1;
            Destroy(activeCards[lastIndex].gameObject);
            activeCards.RemoveAt(lastIndex);
        }

        // 2. Maintenant qu'il y a de la place, on décale les cartes restantes vers le bas
        Vector2 shiftDirection = Quaternion.Euler(0f, 0f, notificationTemplate.localEulerAngles.z) * Vector2.down;
        foreach (var existingCard in activeCards)
            existingCard.anchoredPosition += shiftDirection * cardSpacing;

        // 3. On clone le modèle et on place la NOUVELLE carte tout en haut
        GameObject newCardObj = Instantiate(notificationTemplate.gameObject, notificationParent);
        newCardObj.SetActive(true);

        RectTransform newCardRect = newCardObj.GetComponent<RectTransform>();
        newCardRect.anchoredPosition = restingPosition;

        var card = newCardObj.GetComponent<NotificationCard>();
        if (card != null) card.Populate(data);

        // On l'ajoute en première position dans notre liste
        activeCards.Insert(0, newCardRect);
    }

    // --- TEST : touche N au clavier, passe à la notif de démo suivante à
    //     chaque appui (boucle). Remplis la liste ci-dessous dans l'Inspector
    //     avec différentes apps/icônes pour varier ce qui s'affiche. ---
    [Header("Test - notifs de démo (remplis avec différentes apps/icônes)")]
    [SerializeField] private List<NotificationData> testNotifications = new List<NotificationData>();
    private int testIndex = 0;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            TestNextNotification();
        }
    }

    private void TestNextNotification()
    {
        if (testNotifications == null || testNotifications.Count == 0)
        {
            Debug.LogWarning("NotificationManager : la liste 'Test Notifications' est vide, ajoute des entrées dans l'Inspector.");
            return;
        }

        NotificationData template = testNotifications[testIndex];

        // Copie les données du template, mais avec l'heure réelle du moment
        // (le champ Timestamp de la liste dans l'Inspector n'est donc plus utilisé)
        NotificationData liveData = new NotificationData
        {
            appName = template.appName,
            appIcon = template.appIcon,
            senderName = template.senderName,
            message = template.message,
            timestamp = System.DateTime.Now.ToString("HH:mm")
        };

        Enqueue(liveData);
        testIndex = (testIndex + 1) % testNotifications.Count;
    }

    [ContextMenu("Test Next Notification")]
    private void TestNextNotificationContextMenu() => TestNextNotification();
}