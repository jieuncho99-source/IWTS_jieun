// AchievementManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Stage")]
    [Tooltip("스테이지 ID (비우면 현재 씬 이름 사용)")]
    public string stageId;

    [Header("Runtime (ReadOnly)")]
    public int totalItems;
    public int collectedItems;
    public bool allStarAchieved;

    [Header("Events")]
    public UnityEvent<int, int> onProgressChanged; // (collected, total)
    public UnityEvent onAllStarAchieved;

    private readonly HashSet<string> _knownItemIds = new HashSet<string>();

    public string StageId => string.IsNullOrEmpty(stageId)
        ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        : stageId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (string.IsNullOrEmpty(stageId))
            stageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        InitializeStageSnapshot();
    }

    private void InitializeStageSnapshot()
    {
        var all = FindObjectsOfType<CollectibleItem>(true);
        totalItems = 0;
        collectedItems = 0;
        _knownItemIds.Clear();

        foreach (var it in all)
        {
            if (it.stageId != StageId) continue;
            if (string.IsNullOrEmpty(it.itemId)) continue;

            totalItems++;
            _knownItemIds.Add(it.itemId);

            if (AchievementStorage.IsItemCollected(StageId, it.itemId))
                collectedItems++;
        }

        allStarAchieved = AchievementStorage.IsAllStar(StageId);
        MaybeCheckAllStar();
        onProgressChanged?.Invoke(collectedItems, totalItems);
    }

    public void ReportCollected(CollectibleItem item)
    {
        if (!_knownItemIds.Contains(item.itemId)) return;
        if (!AchievementStorage.IsItemCollected(StageId, item.itemId)) return;

        collectedItems = Mathf.Clamp(collectedItems + 1, 0, totalItems);
        onProgressChanged?.Invoke(collectedItems, totalItems);
        MaybeCheckAllStar();
    }

    private void MaybeCheckAllStar()
    {
        if (allStarAchieved) return;
        if (totalItems == 0) return;
        if (collectedItems < totalItems) return;

        allStarAchieved = true;
        AchievementStorage.MarkAllStar(StageId);
        onAllStarAchieved?.Invoke();
    }

    // 테스트/디버그: 현재 스테이지 진행도 초기화 (이번 "세션" 기준)
    [ContextMenu("Reset Progress For Current Stage")]
    public void ResetProgressForCurrentStage()
    {
        var all = FindObjectsOfType<CollectibleItem>(true);
        foreach (var it in all)
        {
            if (it.stageId != StageId) continue;
            AchievementStorage.ClearItemCollected(StageId, it.itemId);
        }
        AchievementStorage.ClearAllStar(StageId);

        InitializeStageSnapshot();

        // 씬 내 시각 상태도 리셋
        foreach (var it in all)
        {
            if (it.stageId != StageId) continue;
            it.isCollected = false;
            if (it.visualRoot != null) it.visualRoot.SetActive(true);
            else it.gameObject.SetActive(true);
        }
    }
}
