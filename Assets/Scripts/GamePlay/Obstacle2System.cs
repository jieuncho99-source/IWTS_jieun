using UnityEngine;

public class Obstacle2System : MonoBehaviour, IObstacle
{
    public bool IsStop => false;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;   // 왕복 속도 계수(값이 클수록 빨라짐)
    [SerializeField] private float moveRange = 3f;   // 시작 지점 기준 좌우 거리
    [SerializeField] private bool useWorldSpace = true; // 월드 X축 기준 이동 여부

    private Vector3 _worldStartPos;
    private Vector3 _localStartPos;
    private float _startX;

    void Awake()
    {
        _worldStartPos = transform.position;
        _localStartPos = transform.localPosition;
        _startX = useWorldSpace ? _worldStartPos.x : _localStartPos.x;
    }

    void Update()
    {
        if (!IsStop)
        {
            Movement();
        }
    }

    public void Movement()
    {
        // -moveRange ~ +moveRange 사이를 일정 속도로 왕복
        float offset = Mathf.PingPong(Time.time * moveSpeed, moveRange * 2f) - moveRange;

        if (useWorldSpace)
        {
            Vector3 p = _worldStartPos;
            p.x = _startX + offset;
            transform.position = p;
        }
        else
        {
            Vector3 p = _localStartPos;
            p.x = _startX + offset;
            transform.localPosition = p;
        }
    }

#if UNITY_EDITOR
    // 에디터에서 선택 시 이동 경로 표시
    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? _worldStartPos : transform.position;
        Vector3 a = start; a.x += -moveRange;
        Vector3 b = start; b.x += moveRange;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.06f);
        Gizmos.DrawSphere(b, 0.06f);
    }
#endif
}
