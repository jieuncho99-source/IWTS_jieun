using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0.01f, 1.58f, -9.8f); // 기준 Yaw(0도)에서의 오프셋
    [SerializeField] float followLerp = 5f;
    [SerializeField] Vector3 lookAtOffset = Vector3.zero; // 머리/가슴 등 원하는 포인트 보정

    [Header("Yaw")]
    [SerializeField] float yawLerpSpeed = 180f; // deg/sec
    [SerializeField] float defaultYaw = 0f;     // 오버라이드 없을 때 기본 바라보는 각

    [Header("Optional clamps (월드 기준)")]
    [SerializeField] float maxY = 4f;
    [SerializeField] bool clampY = true;

    // Yaw 오버라이드
    private bool useYawOverride = false;
    private float yawOverride = 0f;
    private bool snapYawOnce = false;

    public void SetYawOverride(float yawDeg, bool snap = false)
    {
        useYawOverride = true;
        yawOverride = yawDeg;
        snapYawOnce = snap;
    }

    public void ClearYawOverride()
    {
        useYawOverride = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1) 현재 사용할 Yaw 계산
        float currentY = transform.eulerAngles.y;

        float targetYaw = useYawOverride ? yawOverride : defaultYaw;

        float nextYaw;
        if (snapYawOnce)
        {
            nextYaw = targetYaw;
            snapYawOnce = false;
        }
        else
        {
            nextYaw = Mathf.MoveTowardsAngle(currentY, targetYaw, yawLerpSpeed * Time.deltaTime);
        }

        // 2) 해당 Yaw 기준으로 offset을 회전시켜 카메라 위치 산출
        Vector3 rotatedOffset = Quaternion.Euler(0f, nextYaw, 0f) * offset;
        Vector3 desiredPos = target.position + rotatedOffset;

        if (clampY)
            desiredPos.y = Mathf.Min(desiredPos.y, maxY);

        // 3) 위치 보간
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        // 4) 타깃을 항상 바라보기 (중앙 정렬 핵심)
        Vector3 lookPoint = target.position + lookAtOffset;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}
