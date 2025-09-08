using System.Collections;
using TMPro;
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

    [Header("DASH FX (선택)")]
    [Tooltip("대시 시작 순간 한 번만 재생할 속도선 파티클")]
    [SerializeField] public DashSpeedLines dashSpeedLines; // 자식의 ParticleSystem 연결

    [Header("CLEAR RULE")]
    [Tooltip("이 스테이지에서 아이템 수집을 요구할지 여부(튜토리얼에서는 보통 끔).")]
    [SerializeField] private bool requireItemsThisStage = true;
    [Tooltip("튜토리얼 씬 이름. 이름이 일치하면 수집 요구를 자동으로 건너뜁니다.")]
    [SerializeField] private string tutorialSceneName = Scenes.TUTORIAL;

    [Header("Toast UI")]
    [SerializeField] private GameObject toastPanel;     // 패널
    [SerializeField] private TextMeshProUGUI toastText; // 텍스트
    [SerializeField] private float toastDuration = 2f;  // 몇 초간 보일지

    #endregion

    #region 내부 상태 변수

    private Rigidbody _rb;
    private Vector2 _moveInput;

    // Dash
    private bool _dashRequested = false;
    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _dashCooldownLeft = 0f;

    // FX 상태 트래킹
    private bool _wasDashing = false;   // 직전 프레임 대시 여부

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

        _wasDashing = _isDashing;
    }

    private void FixedUpdate()
    {
        // 1) 대시 진입/유지
        if (TryApplyOrMaintainDash())
            return; // 대시 프레임에는 일반 이동 스킵

        // 2) 일반 이동
        HandleMovement();
    }

    #endregion

    #region 입력 처리 (PlayerInput에서 호출)

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

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
        // 대시 시작
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

            Vector3 v = _rb.velocity;
            _rb.velocity = new Vector3(dir.x * _dashSpeed, v.y, dir.z * _dashSpeed);

            // 대시 시작 FX
            if (dashSpeedLines != null) dashSpeedLines.OnDashStart();

            return true;
        }

        // 대시 유지
        if (_isDashing)
        {
            Vector3 dir = transform.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 v = _rb.velocity;
            _rb.velocity = new Vector3(dir.x * _dashSpeed, v.y, dir.z * _dashSpeed);
            return true;
        }

        // 대시 아님
        _dashRequested = false;
        return false;
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);

        Vector3 targetVelocity = moveDirection * _moveSpeed;

        float accel = moveDirection.sqrMagnitude > 0.01f ? _acceleration : _deceleration;

        Vector3 currentPlanarVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        Vector3 newPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetVelocity,
            accel * Time.fixedDeltaTime
        );

        _rb.velocity = new Vector3(newPlanarVelocity.x, _rb.velocity.y, newPlanarVelocity.z);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }
    }

    #endregion

    #region 골/히든 처리

    private bool ShouldRequireItems()
    {
        if (!requireItemsThisStage) return false;

        string active = UnitySceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(tutorialSceneName) && active == tutorialSceneName)
            return false;

        return true;
    }

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
            // 튜토리얼/조건 비활성 스테이지는 바로 클리어
            if (!ShouldRequireItems())
            {
                var stageName0 = UnitySceneManager.GetActiveScene().name;
                Debug.Log($"골인 지점 도달 {stageName0} (튜토리얼/수집 요구 비활성)");
                GameManager.Stage.ClearedStage(stageName0);
                return;
            }

            var mgr = AchievementManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("AchievementManager가 없습니다.");
                _isCollided = false;
                ShowToast("시스템 오류: 업적 매니저 없음");
                return;
            }

            bool canClear = (mgr.totalItems > 0) && (mgr.collectedItems >= mgr.totalItems);
            if (!canClear)
            {
                Debug.Log($"클리어 조건 미달: {mgr.collectedItems}/{mgr.totalItems}");
                _isCollided = false; // 재충돌 허용
                ShowToast("You Need To Collect Every Star!!");
                return;
            }

            var currentStageName = UnitySceneManager.GetActiveScene().name;
            Debug.Log($"골인 지점 도달 {currentStageName} (모두 수집)");
            GameManager.Stage.ClearedStage(currentStageName);
        }
    }

    #endregion

    #region 토스트

    private void ShowToast(string message)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.LogWarning("Toast UI가 연결되지 않았습니다.");
            return;
        }

        StopAllCoroutines(); // 여러 번 겹치지 않게
        StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastPanel.SetActive(true);

        yield return new WaitForSeconds(toastDuration);

        toastPanel.SetActive(false);
    }

    #endregion
}
