using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimingSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class SwitchEntry
    {
        [Header("Targets")]
        [Tooltip("꺼졌다 켜질 오브젝트(여러 개 가능). 이 스크립트를 가진 오브젝트 자신은 넣지 마세요.")]
        public List<GameObject> targets = new List<GameObject>();

        [Header("Timing (sec)")]
        [Min(0f)] public float onSeconds = 1.5f;
        [Min(0f)] public float offSeconds = 1.0f;
        [Min(0f)] public float initialDelay = 0f;
        [Min(0f)] public float startPhaseJitter = 0f; // 시작 지연에 랜덤 가산

        [Header("Options")]
        public bool useUnscaledTime = false; // 일시정지 중에도 동작하게 하려면 체크
        public bool startOn = true;          // 시작 상태 (켜짐/꺼짐)
        public bool restoreOnDisable = true; // 컴포넌트 꺼질 때 '켜짐'으로 복구
    }

    [Tooltip("각 항목마다 타겟과 시간을 따로 지정하세요.")]
    public List<SwitchEntry> entries = new List<SwitchEntry>();

    private readonly List<Coroutine> _runners = new List<Coroutine>();

    private void OnEnable()
    {
        StopAllRunners();

        foreach (var e in entries)
        {
            if (e == null || e.targets == null || e.targets.Count == 0) continue;
            _runners.Add(StartCoroutine(RunEntry(e)));
        }
    }

    private void OnDisable()
    {
        StopAllRunners();

        // 종료 시 상태 복구 옵션
        foreach (var e in entries)
        {
            if (e == null || e.targets == null) continue;
            if (e.restoreOnDisable)
                SetActiveFor(e.targets, true); // 보통 켜진 상태로 복구
        }
    }

    private void StopAllRunners()
    {
        for (int i = 0; i < _runners.Count; i++)
        {
            if (_runners[i] != null) StopCoroutine(_runners[i]);
        }
        _runners.Clear();
    }

    private IEnumerator RunEntry(SwitchEntry e)
    {
        // 시작 지연(+지터)
        float delay = Mathf.Max(0f, e.initialDelay)
                    + (e.startPhaseJitter > 0f ? Random.Range(0f, e.startPhaseJitter) : 0f);
        if (delay > 0f) yield return WaitSeconds(delay, e.useUnscaledTime);

        // 시작 상태 설정
        bool state = e.startOn;
        SetActiveFor(e.targets, state);

        while (true)
        {
            float wait = state ? e.onSeconds : e.offSeconds;
            if (wait > 0f) yield return WaitSeconds(wait, e.useUnscaledTime);

            state = !state;
            SetActiveFor(e.targets, state);
        }
    }

    private static void SetActiveFor(List<GameObject> list, bool active)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (go) go.SetActive(active);
        }
    }

    // IEnumerator로 구현 (WaitForSeconds / Realtime 모두 지원)
    private IEnumerator WaitSeconds(float seconds, bool unscaled)
    {
        if (unscaled) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }
}
