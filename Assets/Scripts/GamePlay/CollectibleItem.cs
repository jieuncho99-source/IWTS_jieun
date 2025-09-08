// CollectibleItem.cs
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CollectibleItem : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("스테이지 ID (비우면 현재 씬 이름 사용)")]
    public string stageId;

    [Tooltip("아이템 고유 ID (복제/프리팹마다 서로 달라야 함)")]
    public string itemId;

    [Header("Setup")]
    [Tooltip("시각만 끄고 싶다면 여기에 모델/스프라이트 루트를 넣기")]
    public GameObject visualRoot;

    [Tooltip("플레이어가 지닌 태그명")]
    public string playerTag = "Player";

    [Header("Events")]
    public UnityEvent onCollected; // 파티클, 사운드 등 연결

    [Header("UI")]
    [Tooltip("아이콘 슬롯 인덱스 (0부터 시작). UI의 아이콘 배열과 매칭")]
    public int uiIndex = -1;

    [NonSerialized] public bool isCollected; // 런타임 상태

    void Reset()
    {
        // 에디터에서 컴포넌트 추가 시 한 번 호출: 기본값 설정
        stageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        GenerateNewId();
        if (visualRoot == null) visualRoot = gameObject;
    }

    [ContextMenu("Generate New ItemId")]
    public void GenerateNewId()
    {
        // 영구 저장에 안전한 난수 ID
        itemId = Guid.NewGuid().ToString("N");
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(stageId))
            stageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(itemId))
            GenerateNewId();

        // 저장된 진행도 반영
        isCollected = AchievementStorage.IsItemCollected(stageId, itemId);
        ApplyVisual(isCollected);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag(playerTag)) return;

        // 수집 처리
        isCollected = true;
        AchievementStorage.MarkItemCollected(stageId, itemId);
        onCollected?.Invoke();
        ApplyVisual(true);

        // 매니저에 보고
        var mgr = AchievementManager.Instance;
        if (mgr != null && mgr.StageId == stageId)
            mgr.ReportCollected(this);
    }

    private void ApplyVisual(bool collected)
    {
        if (visualRoot != null)
            visualRoot.SetActive(!collected); // 먹었으면 숨김
    }
}
