using UnityEngine;
using TMPro;

public class StageIntroUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private float showTime = 2.5f; // 몇 초간 보이게 할지
    [SerializeField] private string prefix = "Stage";

    private void Start()
    {
        if (stageText == null) return;

        /* 현재 스테이지 이름이나 번호 가져오기
        string stageName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        stageText.text = $"{prefix} {stageName}";
        */

        // 보이게 한 뒤 일정 시간 지나면 숨기기
        stageText.gameObject.SetActive(true);
        Invoke(nameof(HideText), showTime);
    }

    private void HideText()
    {
        if (stageText != null)
            stageText.gameObject.SetActive(false);
    }
}
