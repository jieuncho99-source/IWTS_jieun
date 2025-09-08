using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0.01f, 1.58f, -9.8f);
    [SerializeField] float followSpeed = 5f;

    private float maxY = 4f;
    private float minZ = -9.87f;

    // --- Yaw 오버라이드 기능 ---
    private bool useYawOverride = false;
    private float yawOverride = 0f;
    private bool snapYaw = false;  // true면 바로 회전, false면 부드럽게

    [SerializeField] private float yawLerpSpeed = 180f; // 부드럽게 돌릴 때 속도

    // 외부에서 호출하는 함수
    public void SetYawOverride(float yaw, bool snap = false)
    {
        useYawOverride = true;
        yawOverride = yaw;
        snapYaw = snap;
    }

    public void ClearYawOverride()
    {
        useYawOverride = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 위치 이동
        Vector3 desirePosition = target.position + offset;
        desirePosition.y = Mathf.Min(desirePosition.y, maxY);
        desirePosition.z = Mathf.Max(minZ, desirePosition.z);
        transform.position = Vector3.Lerp(transform.position, desirePosition, followSpeed * Time.deltaTime);

        // 회전 처리
        if (useYawOverride)
        {
            if (snapYaw)
            {
                // 즉시 스냅
                var e = transform.eulerAngles;
                e.y = yawOverride;
                transform.eulerAngles = e;
                snapYaw = false; // 한 번만 스냅 적용
            }
            else
            {
                // 부드럽게 보간
                float currentY = transform.eulerAngles.y;
                float newY = Mathf.MoveTowardsAngle(currentY, yawOverride, yawLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, newY, 0f);
            }
        }
    }
}
