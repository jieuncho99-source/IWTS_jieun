using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class StageManager : IManager
{
    private static readonly HashSet<string> clearedStages = new();
    public List<string> _allStages;

    public void Initialize()
    {
        _allStages = new List<string>
        {
            Scenes.TUTORIAL,
            Scenes.STEP1,
            Scenes.STEP2,
            Scenes.STEP3,
            Scenes.FINAL,
        };
    }

    public void Release()
    {
        clearedStages.Clear();
    }

    /// <summary>
    /// 스테이지 클리어 시 호출
    /// </summary>
    public async void ClearedStage(string stageName)
    {
        Debug.Log($"{stageName} 클리어");
        clearedStages.Add(stageName);

        int currentStageIdx = _allStages.IndexOf(stageName);

        // 마지막 스테이지일 경우
        if (stageName == Scenes.FINAL)
        {
            await FinalClear();
            return;
        }

        //다음 스테이지가 있는 경우
        if (currentStageIdx != -1 && currentStageIdx + 1 < _allStages.Count)
        {
            string nextStageName = _allStages[currentStageIdx + 1];

            var player = GameObject.Find("FinalPlayer");
            var hs = player?.GetComponent<HealthSystem>();

            // 업적 조건 체크
            if (hs != null && hs.currentHealth > 70 && stageName == Scenes.STEP1 &&
                !GameManager.Accomplishment.IsUnlocked((int)AchievementKey.POTENTIAL))
            {
                // 업적 달성 팝업 끝까지 기다린 후 다음 씬 로드
                await GameManager.Accomplishment.UnLock((int)AchievementKey.POTENTIAL);
            }

            // 씬 전환은 무조건 한 번만
            if(UnitySceneManager.GetActiveScene().name != nextStageName)
            {
                GameManager.Scene.LoadScene(nextStageName);
            }
        }
    }

    public bool IsStageCleared(string stageName) => clearedStages.Contains(stageName);

    /// <summary>
    /// 최종 스테이지 클리어 처리
    /// </summary>
    private async Task FinalClear()
    {
        // ALIVE 업적
        if (!GameManager.Accomplishment.IsUnlocked((int)AchievementKey.ALIVE))
        {
            await GameManager.Accomplishment.UnLock((int)AchievementKey.ALIVE);
        }

        // STRONGER 업적
        var player = GameObject.Find("FinalPlayer");
        var hs = player?.GetComponent<HealthSystem>();

        if (hs != null && hs.currentHealth > 50 &&
            !GameManager.Accomplishment.IsUnlocked((int)AchievementKey.STRONGER))
        {
            await GameManager.Accomplishment.UnLock((int)AchievementKey.STRONGER);
        }

        // 엔딩 연출 실행
        var ending = GameObject.FindObjectOfType<EndingEffect>();
        if (ending != null)
        {
            ending.PlayEnding();

            // 연출 끝날 때까지 대기
            await UniTask.Delay(4000);
        }

        //마지막에 딱 한 번만 씬 전환
        if (UnitySceneManager.GetActiveScene().name != Scenes.START)
        {
            GameManager.Scene.LoadScene(Scenes.START);
        }
    }
}
