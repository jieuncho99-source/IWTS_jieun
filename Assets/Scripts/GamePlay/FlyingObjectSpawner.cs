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

    [Header("Spawn Plane (���� ��ǥ��)")]
    [Tooltip("�÷��̾� ������ �󸶳� ������ Z���� �������� (player.z + �� ��)")]
    public float forwardDistanceZ = 12f;

    [Tooltip("���� X ��ǥ�� ���� ����")]
    public Vector2 worldXRange = new Vector2(-6f, 6f);

    [Tooltip("�����Ǵ� ���� Y ��ǥ(������)")]
    public float spawnY = 1.0f;   // �� �ν����Ϳ��� ���⸸ �ٲٸ� Y�� ������

    [Header("Motion")]
    [Tooltip("���� Z�����θ� �̵�(�÷��̾� ��). ���� �� -player.forward�� �̵�")]
    public bool worldZOnly = true;

    [Tooltip("�ӵ� ����(�ʴ� m)")]
    public Vector2 speedRange = new Vector2(8f, 14f);

    [Tooltip("Z�� ���� �̵��� �� X/Y�� ������ ��¥ Z�� �����̰� ��")]
    public bool freezeXYWhenWorldZ = true;

    [Tooltip("�̵� ������ �ٶ󺸰� ȸ��")]
    public bool alignToMotion = false;

    [Header("Lifetime")]
    [Min(0f)] public float maxLifeTime = 8f; // 0�̸� ���� ����

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
            Debug.LogWarning("[FlyingObjectSpawner] player �Ǵ� projectilePrefabs ������ �ʿ��մϴ�.");
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
        // 1) ���� ��ġ (X ����, Y ����, Z�� �÷��̾� ����)
        float x = Random.Range(worldXRange.x, worldXRange.y);
        float y = spawnY; // �� ���� Y
        float z = player.position.z + forwardDistanceZ;
        Vector3 spawnPos = new Vector3(x, y, z);

        // 2) ������ ����
        var prefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 3) �̵� ���� & �ӵ�
        Vector3 dir;
        if (worldZOnly)
        {
            // ������ �÷��̾� �����̸� -Z��, �����̸� +Z��(�׻� �÷��̾� ��)
            dir = (spawnPos.z >= player.position.z) ? Vector3.back : Vector3.forward;
        }
        else
        {
            // �÷��̾��� ���� ����(������ ȸ���ϴ� ��� ����). Y ���� ���ŷ� ��� �̵�.
            dir = -player.forward; dir.y = 0f; dir.Normalize();
        }

        float speed = Random.Range(speedRange.x, speedRange.y);

        // 4) Rigidbody ���� (���� �̵�)
        if (!go.TryGetComponent<Rigidbody>(out var rb))
            rb = go.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = dir * speed;

        if (worldZOnly && freezeXYWhenWorldZ)
        {
            // X/Y ���� �� Z�θ� �̵�
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (alignToMotion) go.transform.rotation = Quaternion.LookRotation(dir);

        // 5) ���� ����
        if (maxLifeTime > 0f) Destroy(go, maxLifeTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!player) return;

        float z = player.position.z + forwardDistanceZ;

        // ������(X ���� @ ���� Y, Z����)
        Vector3 a = new Vector3(worldXRange.x, spawnY, z);
        Vector3 b = new Vector3(worldXRange.y, spawnY, z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.06f);
        Gizmos.DrawSphere(b, 0.06f);

        // �̵� ���� ȭ��ǥ(�÷��̾� ��)
        Gizmos.color = Color.red;
        Vector3 mid = (a + b) * 0.5f;
        Vector3 tip = mid + ((z >= player.position.z) ? Vector3.back : Vector3.forward) * 2f;
        Gizmos.DrawLine(mid, tip);
    }
#endif
}
