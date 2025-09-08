using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class IconSlot
{
    public Image image;                 // 이 슬롯이 표시될 UI Image
    public Sprite uncollectedSprite;    // 안 먹었을 때 보여줄 스프라이트
    public Sprite collectedSprite;      // 먹었을 때 보여줄 스프라이트
    public bool preserveAspect = true;  // 필요 시 비율 유지
}

public class CollectibleIconsUI : MonoBehaviour
{
    [Header("Icon Slots (index = uiIndex)")]
    [SerializeField] private List<IconSlot> slots = new List<IconSlot>();

    // uiIndex -> CollectibleItem
    private Dictionary<int, CollectibleItem> _itemsByIndex;

    private void Start()
    {
        // 씬에서 수집 아이템 수집
        var allItems = FindObjectsOfType<CollectibleItem>(true);

        _itemsByIndex = new Dictionary<int, CollectibleItem>();
        foreach (var it in allItems)
        {
            if (it.uiIndex < 0) continue;
            if (_itemsByIndex.ContainsKey(it.uiIndex))
            {
                Debug.LogWarning($"중복 uiIndex: {it.uiIndex} in {it.name}");
                continue;
            }
            _itemsByIndex[it.uiIndex] = it;
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("slots가 비어 있습니다. 인스펙터에서 아이콘 슬롯을 설정하세요.");
        }

        // 최초 갱신
        RefreshAll();

        // 진행 이벤트 구독
        var mgr = AchievementManager.Instance;
        if (mgr != null)
        {
            mgr.onProgressChanged.AddListener((_, __) => RefreshAll());
            mgr.onAllStarAchieved.AddListener(RefreshAll);
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.image == null) continue;

            bool collected = false;

            if (_itemsByIndex != null && _itemsByIndex.TryGetValue(i, out var item))
            {
                collected = AchievementStorage.IsItemCollected(item.stageId, item.itemId);
            }
            else
            {
                // 해당 uiIndex에 매핑된 아이템이 없다면 미수집으로 취급
                collected = false;
            }

            // 스프라이트 교체
            var targetSprite = collected ? slot.collectedSprite : slot.uncollectedSprite;

            if (targetSprite == null)
            {
                Debug.LogWarning($"슬롯 {i}의 {(collected ? "collected" : "uncollected")} 스프라이트가 비어 있습니다.");
            }

            slot.image.sprite = targetSprite;

            // 알파는 항상 1로 고정 (투명도 조절 사용 안 함)
            var c = slot.image.color;
            c.a = 1f;
            slot.image.color = c;

            // 비율 유지 옵션
            slot.image.preserveAspect = slot.preserveAspect;
        }
    }
}
