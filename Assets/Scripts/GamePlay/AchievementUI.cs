// AchievementUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI progressText; // TMP 쓰면 TextMeshProUGUI로 교체

    void Start()
    {
        var mgr = AchievementManager.Instance;
        if (mgr != null)
        {
            mgr.onProgressChanged.AddListener(UpdateUI);
            UpdateUI(mgr.collectedItems, mgr.totalItems);
            if (mgr.allStarAchieved)
                ShowAllStarToast();
            mgr.onAllStarAchieved.AddListener(ShowAllStarToast);
        }
    }

    private void UpdateUI(int collected, int total)
    {
        if (progressText != null)
            progressText.text = $"Collected {collected}/{total}";
    }

    private void ShowAllStarToast()
    {
        Debug.Log("올스타 달성!");
        // 여기서 토스트 UI/연출 호출
    }
}
