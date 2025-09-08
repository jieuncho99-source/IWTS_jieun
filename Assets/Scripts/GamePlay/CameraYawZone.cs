using UnityEngine;

public class CameraYawZone : MonoBehaviour
{
    public float targetYaw = 90f;
    public bool snapOnEnter = false;
    public bool clearOnExit = true;
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var camController = FindObjectOfType<CameraController>();
        if (camController != null)
        {
            camController.SetYawOverride(targetYaw, snapOnEnter);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (clearOnExit)
        {
            var camController = FindObjectOfType<CameraController>();
            if (camController != null)
            {
                camController.ClearYawOverride();
            }
        }
    }
}
