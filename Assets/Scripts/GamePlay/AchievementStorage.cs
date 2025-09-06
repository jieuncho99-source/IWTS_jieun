// AchievementStorage.cs
using UnityEngine;

public static class AchievementStorage
{
    private static string ItemKey(string stageId, string itemId)
        => $"ACH_ITEM_{stageId}_{itemId}";

    private static string AllStarKey(string stageId)
        => $"ACH_ALLSTAR_{stageId}";

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

    // 테스트/디버그용: 스테이지 진행도 초기화
    public static void ResetStage(string stageId)
    {
        // 실제 게임에선 키를 모두 순회하기 어렵기 때문에
        // 매니저에서 아이템 목록을 받아 초기화하는 헬퍼를 따로 둔다.
        // 여기선 빈 함수로 두고, AchievementManager.ResetProgressForCurrentStage() 제공.
    }
}
