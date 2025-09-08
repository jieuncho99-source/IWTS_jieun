using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class AchievementState
{
    public int id;
    public bool unlocked;
}

[System.Serializable]
public class AchievementSave
{
    public List<AchievementState> stateList = new List<AchievementState>();
}

public class AccomplishmentManager : IManager
{

    private string fileName = "achievements.json";
    private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    public Dictionary<int, AchievementState> _states { get; private set; } = new();
    public Dictionary<int, AchievementDefinition> _defs { get; private set; } = new();

    private GameObject _UI;

    public void Initialize()
    {
        LoadDefinitionsFromResources();
        LoadOrInitStates();

        // _UI = GameObject.Find("UI_Accom");
       // Object.DontDestroyOnLoad(_UI);
    }

    private void LoadDefinitionsFromResources()
    {
        _defs.Clear();
        var defs = Resources.LoadAll<AchievementDefinition>("Achievements");
        foreach (var def in defs)
        {
            if (_defs.ContainsKey(def.id))
            {
                Debug.LogWarning("업적 중복 ID");
                continue;
            }
            _defs[def.id] = def;
        }
        Debug.Log("업적 에셋 로드 완료");
    }

    private void LoadOrInitStates()
    {
        _states.Clear();

        AchievementSave save = null;
        if(File.Exists(FilePath))
        {
            try
            {
                var json = File.ReadAllText(FilePath);
                save = JsonUtility.FromJson<AchievementSave>(json);
            }catch(System.Exception e)
            {
                Debug.LogError($"업적 저장 파일 파싱 실패, 새로 만듭니다...{e}");
            }
        }
        if (save == null) save = new AchievementSave();

        foreach(var s in save.stateList)
        {
            _states[s.id] = s;
        }

        foreach(var id in _defs.Keys)
        {
            if (!_states.ContainsKey(id))
            {
                _states[id] = new AchievementState { id = id, unlocked = false };
            }
        }

        Save();
    }

    private void Save()
    {
        var save = new AchievementSave { stateList = new List<AchievementState>(_states.Values) };
        var json = JsonUtility.ToJson(save, true);
        File.WriteAllText(FilePath, json);
    }

    public async UniTask UnLock(int id)
    {
        if (!_defs.ContainsKey(id)) return;

        if (!_states.TryGetValue(id, out var state))
        {
            state = new AchievementState { id = id, unlocked = false };
            _states[id] = state;
        }

        if (!state.unlocked)
        {
            state.unlocked = true;
            Save();
            await OnUnlocked(_defs[id].achievementTitle);
        }
    }

    private async UniTask OnUnlocked(string text)
    {
        var AchievePopup = Resources.Load<GameObject>("UI/accomplishmentPanel");
        var instance = Object.Instantiate(AchievePopup, _UI.transform);
        instance.GetComponent<AchievePopupUI>().SetText(text);
        Object.DontDestroyOnLoad(instance);
        instance.SetActive(true);

        await UniTask.Delay(1000, DelayType.UnscaledDeltaTime);

        if(instance != null)
        {
            Object.Destroy(instance);
        }
    }

    public bool IsUnlocked(int id) => _states.TryGetValue(id, out var state) && state.unlocked;

    public void Release()
    {
        
    }
}
