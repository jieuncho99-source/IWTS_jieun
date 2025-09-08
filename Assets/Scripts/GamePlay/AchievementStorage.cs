// AchievementStorage.cs
using UnityEngine;

public static class AchievementStorage
{
    // 이번 앱 실행(세션) 동안만 유지되는 세션 ID
    public static string SessionId { get; private set; }

    // 앱 로드 직전에 한 번만 호출되어 세션 ID를 만든다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitSession()
    {
        SessionId = System.Guid.NewGuid().ToString("N"); // 32자리
        // Debug.Log($"[AchievementStorage] SessionId = {SessionId}");
    }

    // 세션 스코프를 키에 포함
    private static string ItemKey(string stageId, string itemId)
        => $"ACH_ITEM_{stageId}_{itemId}_{SessionId}";

    private static string AllStarKey(string stageId)
        => $"ACH_ALLSTAR_{stageId}_{SessionId}";

    public static bool IsItemCollected(string stageId, string itemId)
        => PlayerPrefs.GetInt(ItemKey(stageId, itemId), 0) == 1;

    public static void MarkItemCollected(string stageId, string itemId)
    {
        PlayerPrefs.SetInt(ItemKey(stageId, itemId), 1);
        PlayerPrefs.Save();
    }

    public static bool IsAllStar(string stageId)
        => PlayerPrefs.GetInt(AllStarKey(stageId), 0) == 1;

    public static void MarkAllStar(string stageId)
    {
        PlayerPrefs.SetInt(AllStarKey(stageId), 1);
        PlayerPrefs.Save();
    }

    // 이번 "세션" 기준으로만 삭제
    public static void ClearItemCollected(string stageId, string itemId)
    {
        PlayerPrefs.DeleteKey(ItemKey(stageId, itemId));
        PlayerPrefs.Save();
    }

    public static void ClearAllStar(string stageId)
    {
        PlayerPrefs.DeleteKey(AllStarKey(stageId));
        PlayerPrefs.Save();
    }

    // 매니저에서 아이템 목록을 받아 초기화하는 대신 제공했던 헬퍼(주석 유지)
    public static void ResetStage(string stageId)
    {
        // 비워둠: AchievementManager.ResetProgressForCurrentStage() 사용
    }
}
