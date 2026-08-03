using UnityEngine;
// On retire UnityEngine.InputSystem car on n'en a plus besoin ici

public class PopupTrigger : MonoBehaviour
{
    [Header("Le Pop-up à afficher")]
    public GameObject popup;

    private void Start()
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }
    }

    /// <summary>
    /// Fonction publique appelée par le DistractionInputManager
    /// </summary>
    public void ShowPopup()
    {
        if (popup != null)
        {
            popup.SetActive(true);
            Debug.Log("Le pop-up s'affiche via le Manager !");
        }
        else
        {
            Debug.LogWarning("Attention : Le champ Popup est vide dans l'inspecteur !");
        }
    }
}