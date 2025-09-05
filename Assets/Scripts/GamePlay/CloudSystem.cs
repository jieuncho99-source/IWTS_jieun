using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Prefabs")]
    [SerializeField] private GameObject[] cloudPrefabs;

    [Header("Spawn Timing (sec)")]
    [SerializeField][Min(0f)] private float minInterval = 3f;
    [SerializeField][Min(0f)] private float maxInterval = 5f;

    [Header("Spawn Area")]
    [SerializeField] public float minX = -20f;
    [SerializeField] public float maxX = 20f;
    [SerializeField] public float yFixed = 15f;
    [SerializeField] public float zStart = -15f;
    [SerializeField] public float zEnd = 120f;

    [Header("Movement")]
    public float cloudSpeed = 0.5f; // 공개 속도

    private Coroutine _spawnLoop;

    // 현재 순서를 기록할 인덱스
    private int currentIndex = 0;

    private void OnEnable()
    {
        _spawnLoop = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_spawnLoop != null) StopCoroutine(_spawnLoop);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float a = Mathf.Min(minInterval, maxInterval);
            float b = Mathf.Max(minInterval, maxInterval);
            float wait = Random.Range(a, b);
            yield return new WaitForSeconds(wait);

            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0) return;

        // 순서대로 prefab 선택
        GameObject prefab = cloudPrefabs[currentIndex];
        currentIndex = (currentIndex + 1) % cloudPrefabs.Length;

        float x = Random.Range(Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        Vector3 pos = new Vector3(x, yFixed, zStart);

        GameObject cloud = Instantiate(prefab, pos, Quaternion.identity, this.transform);

        // 이동 컴포넌트 부착 및 초기화
        CloudMover mover = cloud.GetComponent<CloudMover>();
        if (mover == null) mover = cloud.AddComponent<CloudMover>();
        mover.Init(cloudSpeed, zEnd);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 스폰/소멸 z라인 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(minX, yFixed, zStart), new Vector3(maxX, yFixed, zStart));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(minX, yFixed, zEnd), new Vector3(maxX, yFixed, zEnd));
    }
#endif
}
