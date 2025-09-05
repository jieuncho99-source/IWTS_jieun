using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    [Header("Move (+Z forward)")]
    [SerializeField] private float speed = 5f;   // m/s
    [SerializeField] private bool moveOnAwake = true;

    [Header("Despawn")]
    [SerializeField] private bool useEndZ = true;
    [SerializeField] private float endZ = 100f;

    private bool _moving;

    /// <summary>
    /// CloudMover 호환 Init. 속도/종료Z 세팅 후 이동 시작.
    /// </summary>
    public void Init(float speed, float endZ)
    {
        this.speed = speed;
        this.endZ = endZ;
        this.useEndZ = true;
        _moving = true;
    }

    private void Awake()
    {
        _moving = moveOnAwake;
    }

    private void Update()
    {
        if (!_moving) return;

        // +Z 방향으로만 전진
        transform.position += Vector3.forward * speed * Time.deltaTime;

        // z가 endZ 넘으면 파괴
        if (useEndZ && transform.position.z >= endZ)
        {
            Destroy(gameObject);
        }
    }

    // 컨트롤용 보조 메서드
    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void StopMove() => _moving = false;
    public void ResumeMove() => _moving = true;
    public void SetEndZ(float z) { endZ = z; useEndZ = true; }
}
