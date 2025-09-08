using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraYawZone : MonoBehaviour
{
    [Tooltip("이 구역에 들어오면 카메라의 Y 회전을 이 각도로 유도합니다(월드 기준, 도).")]
    public float targetYaw = 90f;

    [Tooltip("들어오는 순간 즉시 스냅할지 여부(아니면 부드럽게 보간).")]
    public bool snapOnEnter = false;

    [Tooltip("구역에서 나가면 오버라이드를 해제할지 여부.")]
    public bool clearOnExit = true;

    [Tooltip("플레이어를 식별할 태그.")]
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 플레이어 안에 있는 카메라 컨트롤러를 찾는다.
        // 카메라가 플레이어 자식이라면 아래가 통할 가능성이 높음.
        var camController = other.GetComponentInChildren<CameraController>();
        if (!camController)
        {
            // 씬에 하나뿐인 카메라 컨트롤러를 찾는 대안
            camController = FindObjectOfType<CameraController>();
        }
        if (!camController) return;

        camController.SetYawOverride(targetYaw, snapOnEnter);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!clearOnExit) return;

        var camController = other.GetComponentInChildren<CameraController>();
        if (!camController) camController = FindObjectOfType<CameraController>();
        if (!camController) return;

        camController.ClearYawOverride();
    }
}
