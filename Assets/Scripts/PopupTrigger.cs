using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Update()
    {
        // On teste avec la touche L pour voir si ça évite les conflits
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (popup != null)
            {
                popup.SetActive(true);
                Debug.Log("Touche L pressée : Le pop-up s'affiche !");
            }
            else
            {
                Debug.LogWarning("Attention : Le champ Popup est vide dans l'inspecteur !");
            }
        }
    }
}
