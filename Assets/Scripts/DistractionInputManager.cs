using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Point d'entrée unique pour les distractions clavier de la session PC.
/// Centralise la détection des touches ET le logging, au lieu d'avoir
/// chaque script de distraction (PopupTrigger, NotificationManager,
/// OfficePhoneTrigger, CarHonk) lire sa propre touche indépendamment.
///
/// Utilise le nouvel Input System (Keyboard.current), comme le reste du
/// projet — PAS l'ancien Input.GetKeyDown : aucun script du projet ne
/// l'utilise, ce qui indique qu'Active Input Handling est réglé sur
/// "Input System Package (New)" uniquement, où l'ancien Input ne
/// fonctionne pas.
///
///   L -> popup mail sur l'écran ordi      -> PopupTrigger.ShowPopup()
///   N -> notif Insta/Messenger portable   -> NotificationManager.TestNextNotification()
///   P -> sonnerie du téléphone fixe       -> OfficePhoneTrigger.RingPhone()
///   H -> honk de voiture dehors           -> CarHonk.PlayHonk()
///
/// La bascule scène 1/2 (bouton B, dans DNDToggle) et le passage à la
/// Result Scene (dans NBackTaskController.HandleResultSceneTransitionInput)
/// appellent directement LogSceneToggle() / LogResultsTransitionAndStopLogging()
/// ci-dessous — ce script ne relit pas ces boutons pour éviter de dupliquer
/// une lecture d'input qui existe déjà ailleurs.
/// </summary>
public class DistractionInputManager : MonoBehaviour
{
    public static DistractionInputManager Instance { get; private set; }

    [System.Serializable]
    public class DistractionBinding
    {
        public Key key;
        [Tooltip("Écrit tel quel dans la colonne Details du CSV du SessionLogger")]
        public string eventLabel;
        public UnityEvent onTrigger;
    }

    [Header("Distractions clavier (session PC)")]
    [Tooltip("Une entrée par distraction : touche, libellé pour le CSV, action à déclencher. " +
             "Branche onTrigger sur ShowPopup() / TestNextNotification() / RingPhone() / PlayHonk() dans l'Inspector.")]
    public List<DistractionBinding> distractions = new List<DistractionBinding>();

    void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        foreach (var d in distractions)
        {
            if (Keyboard.current[d.key].wasPressedThisFrame)
            {
                d.onTrigger?.Invoke();
                LogSafe("Distraction", d.eventLabel);
            }
        }
    }

    /// <summary>
    /// À appeler depuis DNDToggle.ToggleDNDMode(), à chaque bascule
    /// scène 1 <-> scène 2 (dans les deux sens).
    /// </summary>
    public void LogSceneToggle(string details = "")
    {
        LogSafe("SceneToggle", details);
    }

    /// <summary>
    /// À appeler depuis NBackTaskController.HandleResultSceneTransitionInput(),
    /// au moment du passage vers la Result Scene. Ferme aussi le CSV : plus
    /// besoin d'enregistrer une fois sur la scène de résultats.
    /// </summary>
    public void LogResultsTransitionAndStopLogging()
    {
        LogSafe("ResultsSceneTransition", "");
        SessionLogger.Instance?.EndSession();
    }

    void LogSafe(string eventType, string details)
    {
        if (SessionLogger.Instance != null)
            SessionLogger.Instance.LogEvent(eventType, details);
        else
            Debug.LogWarning($"DistractionInputManager : SessionLogger.Instance est null, événement '{eventType}/{details}' non loggé.");
    }
}