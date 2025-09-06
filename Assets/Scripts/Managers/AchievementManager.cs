// AchievementManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

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
        // 씬 내 모든 CollectibleItem 스캔
        var all = FindObjectsOfType<CollectibleItem>(true);
        totalItems = 0;
        collectedItems = 0;
        _knownItemIds.Clear();

        foreach (var it in all)
        {
            if (it.stageId != StageId) continue; // 다른 스테이지 ID는 무시
            if (string.IsNullOrEmpty(it.itemId)) continue;

            totalItems++;
            _knownItemIds.Add(it.itemId);

            // Awake에서 이미 본인 시각 상태는 반영됨
            if (AchievementStorage.IsItemCollected(StageId, it.itemId))
                collectedItems++;
        }

        allStarAchieved = AchievementStorage.IsAllStar(StageId);
        MaybeCheckAllStar();
        onProgressChanged?.Invoke(collectedItems, totalItems);
    }

    public void ReportCollected(CollectibleItem item)
    {
        // 중복 보고 방지
        if (!_knownItemIds.Contains(item.itemId)) return;

        // 이미 카운트에 포함됐는지 다시 검사
        if (!AchievementStorage.IsItemCollected(StageId, item.itemId))
            return;

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

    // 테스트/디버그: 현재 스테이지 진행도 초기화
    [ContextMenu("Reset Progress For Current Stage")]
    public void ResetProgressForCurrentStage()
    {
        var all = FindObjectsOfType<CollectibleItem>(true);
        foreach (var it in all)
        {
            if (it.stageId != StageId) continue;
            PlayerPrefs.DeleteKey($"ACH_ITEM_{StageId}_{it.itemId}");
        }
        PlayerPrefs.DeleteKey($"ACH_ALLSTAR_{StageId}");
        PlayerPrefs.Save();

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
