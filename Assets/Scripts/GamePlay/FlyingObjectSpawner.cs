using System.Collections;
using UnityEngine;

public class FlyingObjectSpawner : MonoBehaviour
{
    [Header("Target / Prefabs")]
    public Transform player;
    public GameObject[] projectilePrefabs;

    [Header("Spawn Timing")]
    [Min(0.05f)] public float spawnInterval = 1.0f;
    [Range(0f, 0.9f)] public float intervalJitter = 0.15f;

    [Header("Spawn Plane (월드 좌표계)")]
    [Tooltip("플레이어 앞으로 얼마나 떨어진 Z에서 스폰할지 (player.z + 이 값)")]
    public float forwardDistanceZ = 12f;

    [Tooltip("스폰 X 좌표의 월드 범위")]
    public Vector2 worldXRange = new Vector2(-6f, 6f);

    [Tooltip("스폰되는 월드 Y 좌표(고정값)")]
    public float spawnY = 1.0f;   // ★ 인스펙터에서 여기만 바꾸면 Y가 고정됨

    [Header("Motion")]
    [Tooltip("월드 Z축으로만 이동(플레이어 쪽). 해제 시 -player.forward로 이동")]
    public bool worldZOnly = true;

    [Tooltip("속도 범위(초당 m)")]
    public Vector2 speedRange = new Vector2(8f, 14f);

    [Tooltip("Z축 전용 이동일 때 X/Y를 고정해 진짜 Z만 움직이게 함")]
    public bool freezeXYWhenWorldZ = true;

    [Tooltip("이동 방향을 바라보게 회전")]
    public bool alignToMotion = false;

    [Header("Lifetime")]
    [Min(0f)] public float maxLifeTime = 8f; // 0이면 제한 없음

    [Header("Misc")]
    public bool autoFindPlayerByTag = true;

    private Coroutine _loop;

    void OnEnable()
    {
        if (player == null && autoFindPlayerByTag)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (player == null || projectilePrefabs == null || projectilePrefabs.Length == 0)
        {
            Debug.LogWarning("[FlyingObjectSpawner] player 또는 projectilePrefabs 설정이 필요합니다.");
            enabled = false; return;
        }

        _loop = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOne();
            float jitter = 1f + Random.Range(-intervalJitter, intervalJitter);
            yield return new WaitForSeconds(spawnInterval * jitter);
        }
    }

    private void SpawnOne()
    {
        // 1) 스폰 위치 (X 랜덤, Y 고정, Z는 플레이어 앞쪽)
        float x = Random.Range(worldXRange.x, worldXRange.y);
        float y = spawnY; // ★ 고정 Y
        float z = player.position.z + forwardDistanceZ;
        Vector3 spawnPos = new Vector3(x, y, z);

        // 2) 프리팹 생성
        var prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 3) 이동 방향 & 속도
        Vector3 dir;
        if (worldZOnly)
        {
            // 스폰이 플레이어 앞쪽이면 -Z로, 뒤쪽이면 +Z로(항상 플레이어 쪽)
            dir = (spawnPos.z >= player.position.z) ? Vector3.back : Vector3.forward;
        }
        else
        {
            // 플레이어의 전방 기준(게임이 회전하는 경우 유용). Y 성분 제거로 평면 이동.
            dir = -player.forward; dir.y = 0f; dir.Normalize();
        }

        float speed = Random.Range(speedRange.x, speedRange.y);

        // 4) Rigidbody 설정 (직선 이동)
        if (!go.TryGetComponent<Rigidbody>(out var rb))
            rb = go.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.velocity = dir * speed;

        if (worldZOnly && freezeXYWhenWorldZ)
        {
            // X/Y 고정 → Z로만 이동
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (alignToMotion) go.transform.rotation = Quaternion.LookRotation(dir);

        // 5) 수명 관리
        if (maxLifeTime > 0f) Destroy(go, maxLifeTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!player) return;

        float z = player.position.z + forwardDistanceZ;

        // 스폰선(X 범위 @ 고정 Y, Z고정)
        Vector3 a = new Vector3(worldXRange.x, spawnY, z);
        Vector3 b = new Vector3(worldXRange.y, spawnY, z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.06f);
        Gizmos.DrawSphere(b, 0.06f);

        // 이동 방향 화살표(플레이어 쪽)
        Gizmos.color = Color.red;
        Vector3 mid = (a + b) * 0.5f;
        Vector3 tip = mid + ((z >= player.position.z) ? Vector3.back : Vector3.forward) * 2f;
        Gizmos.DrawLine(mid, tip);
    }
#endif
}
