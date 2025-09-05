using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

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
    [Tooltip("대시 시 순간적으로 유지할 평면 속도(m/s).")]
    [SerializeField] private float _dashSpeed = 14f;
    [Tooltip("대시가 유지되는 시간(초).")]
    [SerializeField] private float _dashDuration = 0.15f;
    [Tooltip("대시 후 다시 사용할 때까지의 쿨다운(초).")]
    [SerializeField] private float _dashCooldown = 0.30f;

    #endregion

    #region 내부 상태 변수

    private Rigidbody _rb;
    private Vector2 _moveInput;

    // Dash
    private bool _dashRequested = false;      // 입력 콜백/폴백에서 true로 셋
    private bool _isDashing = false;          // 현재 대시 중?
    private float _dashTimer = 0f;            // 남은 대시 시간
    private float _dashCooldownLeft = 0f;     // 남은 쿨다운

    // 기타
    private bool _isCollided = false;

    #endregion

    #region 유니티 라이프사이클

    private void Awake()
    {
        _isCollided = false;
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        // 스페이스 폴백(액션 세팅이 없어도 작동)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            _dashRequested = true;

        // 타이머 갱신
        if (_dashCooldownLeft > 0f)
            _dashCooldownLeft -= Time.deltaTime;

        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
                _isDashing = false;
        }
    }

    private void FixedUpdate()
    {
        // 1) 대시 진입/유지
        if (TryApplyOrMaintainDash())
            return;                 // 대시 프레임에는 이동을 스킵(속도 덮어쓰기 방지)

        // 2) 일반 이동
        HandleMovement();
    }

    #endregion

    #region 입력 처리 (PlayerInput에서 호출)

    // Move 액션 콜백
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    // Dash 액션 콜백 (있으면 사용, 없으면 스페이스 폴백으로 동작)
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        _dashRequested = true;
    }

    #endregion

    #region 물리 처리

    /// <summary>
    /// 대시 요청이 있으면 대시를 시작하고, 대시 중이면 속도를 유지한다.
    /// true를 반환하면 이 FixedUpdate 프레임에서는 이동을 스킵한다.
    /// </summary>
    private bool TryApplyOrMaintainDash()
    {
        // 대시 시작 조건: 요청 + 쿨다운 종료 + 현재 비대시 상태
        if (!_isDashing && _dashRequested && _dashCooldownLeft <= 0f)
        {
            _dashRequested = false;
            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashCooldownLeft = _dashCooldown;

            // 바라보는 방향(수평)으로 대시 속도 적용
            Vector3 dir = transform.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 v = _rb.velocity; // 표준 Rigidbody 속도 사용
            _rb.velocity = new Vector3(dir.x * _dashSpeed, v.y, dir.z * _dashSpeed);

            return true; // 이 프레임은 이동 스킵
        }

        // 대시 유지: 대시 중에는 같은 방향/속도를 강제 유지(손맛 일정)
        if (_isDashing)
        {
            Vector3 dir = transform.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 v = _rb.velocity;
            _rb.velocity = new Vector3(dir.x * _dashSpeed, v.y, dir.z * _dashSpeed);
            return true; // 이동 스킵
        }

        // 대시 아님
        _dashRequested = false; // 잔여 요청 정리
        return false;
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);

        // 1) 목표 평면 속도
        Vector3 targetVelocity = moveDirection * _moveSpeed;

        // 2) 가감속 선택
        float accel = moveDirection.sqrMagnitude > 0.01f ? _acceleration : _deceleration;

        // 3) 현재 평면 속도
        Vector3 currentPlanarVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        // 4) 보간된 새 평면 속도
        Vector3 newPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetVelocity,
            accel * Time.fixedDeltaTime
        );

        // 5) 실제 속도에 반영 (Y 보존)
        _rb.velocity = new Vector3(newPlanarVelocity.x, _rb.velocity.y, newPlanarVelocity.z);

        // 6) 입력이 있을 때만 바라보는 방향 회전
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }
    }

    #endregion

    #region 골/히든 처리 (기존 유지)

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
