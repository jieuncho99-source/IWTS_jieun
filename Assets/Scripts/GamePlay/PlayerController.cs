using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

// 플레이어 캐릭터의 이동과 점프를 제어하는 스크립트입니다.
// Rigidbody 기반의 물리 계산을 사용하며, 일관된 단일 점프 로직을 가집니다.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    #region 인스펙터 변수

    [Header("MOVEMENT SETTINGS")]
    [Tooltip("캐릭터의 최대 이동 속도입니다.")]
    [SerializeField] private float _moveSpeed = 7f;
    [Tooltip("최대 속도에 도달하기까지의 가속도입니다. 높을수록 빠르게 최대 속도에 도달합니다.")]
    [SerializeField] private float _acceleration = 80f;
    [Tooltip("입력이 없을 때 정지하기까지의 감속도입니다. 높을수록 빠르게 멈춥니다.")]
    [SerializeField] private float _deceleration = 120f;
    [Tooltip("캐릭터가 이동 방향으로 회전하는 속도입니다.")]
    [SerializeField] private float _rotationSpeed = 1080f;

    [Header("DASH SETTINGS")]
    [Tooltip("대시 시 캐릭터가 앞으로 튀어나가는 속도입니다.")]
    [SerializeField] private float _dashSpeed = 16f;
    [Tooltip("대시 쿨다운(초)입니다.")]
    [SerializeField] private float _dashCooldown = 0.15f;

    #endregion

    #region 내부 상태 변수

    // Rigidbody 컴포넌트(이 스크립트가 제어하는 물리 몸체)
    private Rigidbody _rb;

    // 입력값 및 상태
    private Vector2 _moveInput;            // 최신 이동 입력값 (x: 좌/우, y: 앞/뒤)

    // 대시 상태
    private bool _dashRequested = false;   // 입력 콜백에서 표시
    private float _dashCooldownLeft = 0f;  // 쿨다운 타이머

    private bool _isCollided = false;

    #endregion

    #region 유니티 라이프사이클

    // Awake: 컴포넌트 초기화, Rigidbody 제약 설정
    private void Awake()
    {
        _isCollided = false;
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // Enable interpolation so rendered transform stays smooth between physics updates
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // Update: 입력/상태 타이머 업데이트
    private void Update()
    {
        // (참고) 현재 스크립트에선 별도 상태 갱신 없음
        if (_dashCooldownLeft > 0f)
            _dashCooldownLeft -= Time.deltaTime;
    }

    // FixedUpdate: 물리 연산 처리
    private void FixedUpdate()
    {
        // 1) 대시를 먼저 처리. 적용됐다면 이 프레임엔 이동 스킵
        if (HandleDash()) return;

        // 2) 이동 처리
        HandleMovement();
    }

    #endregion

    #region 입력 처리 (PlayerInput에서 호출)

    // Move 액션 콜백: 이동 입력 업데이트
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    // Dash 액션 콜백: '의도'만 표시 (실제 물리 적용은 FixedUpdate에서)
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        _dashRequested = true;
    }

    #endregion

    #region 물리 처리 (대시, 이동)

    /// <summary>
    /// 단발 대시를 '그 물리 프레임 한 번만' 적용한다.
    /// 적용되면 true를 반환(같은 프레임의 이동 보간이 대시 속도를 덮어쓰지 않도록 이동 스킵 신호).
    /// </summary>
    private bool HandleDash()
    {
        if (!_dashRequested) return false;   // 요청 없으면 패스
        _dashRequested = false;              // 요청 소모

        if (_dashCooldownLeft > 0f) return false; // 쿨다운 중이면 패스

        // 바라보는 방향의 수평 성분으로 대시
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward; // 안전장치
        dir.Normalize();

        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(dir.x * _dashSpeed, v.y, dir.z * _dashSpeed);

        _dashCooldownLeft = _dashCooldown;
        return true;
    }

    // 이동 처리: 월드 기준 입력을 사용하여 목표 속도로 부드럽게 보간
    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);

        // 1) 목표 평면 속도
        Vector3 targetVelocity = moveDirection * _moveSpeed;

        // 2) 가감속 선택
        float accel = moveDirection.sqrMagnitude > 0.01f ? _acceleration : _deceleration;

        // 3) 현재 평면 속도
        Vector3 currentPlanarVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        // 4) 보간된 새 평면 속도
        Vector3 newPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetVelocity,
            accel * Time.fixedDeltaTime
        );

        // 5) 실제 속도에 반영 (Y는 보존)
        _rb.linearVelocity = new Vector3(newPlanarVelocity.x, _rb.linearVelocity.y, newPlanarVelocity.z);

        // 6) 입력이 있을 때만 바라보는 방향 회전
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }
    }

    #endregion

    #region 골/히든 처리 (기존 그대로)

    private async void OnParticleCollision(GameObject goal)
    {
        if (_isCollided) return;

        _isCollided = true;

        if (goal.CompareTag("Hidden"))
        {
            Debug.Log("히든 골 도달");
            if (!GameManager.Accomplishment.IsUnlocked((int)AchievementKey.HIDDEN))
            {
                await GameManager.Accomplishment.UnLock((int)AchievementKey.HIDDEN);

                if (UnitySceneManager.GetActiveScene().name != Scenes.START)
                {
                    GameManager.Scene.LoadScene(Scenes.START);

                    return;
                }
            }
        }

        if (goal.CompareTag("Goal"))
        {
            var currentStageName = UnitySceneManager.GetActiveScene().name;
            Debug.Log($"골인 지점 도달 {currentStageName}");
            GameManager.Stage.ClearedStage(currentStageName);
        }
    }

    #endregion
}
