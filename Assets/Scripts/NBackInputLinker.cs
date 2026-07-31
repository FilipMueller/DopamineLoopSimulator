using UnityEngine;

public class NBackInputLinker : MonoBehaviour
{
   
    public void SendResponse()
    {
        if (NBackTaskController.Instance != null)
        {
           NBackTaskController.Instance.HandleMainInput();
        }
    }
}