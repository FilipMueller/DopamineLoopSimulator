using UnityEngine;

public class NBackInputLinker : MonoBehaviour
{
    // A
    public void SendResponse()
    {
        if (NBackTaskController.Instance != null)
        {
            NBackTaskController.Instance.HandleMainInput();
        }
    }

    // B
    public void SendCancel()
    {
        if (NBackTaskController.Instance != null)
        {
            NBackTaskController.Instance.HandleCancelInput();
        }
    }
}