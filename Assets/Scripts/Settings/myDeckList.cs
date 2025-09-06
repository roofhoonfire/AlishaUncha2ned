using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UIElements; // 필요없으면 삭제

public class myDeckList : MonoBehaviour
{
    [Header("Deck Data (병렬 리스트)")]
    public List<string> deckList = new(); // 카드 이름
    public List<string> codeList = new(); // 카드 코드

    [Header("ScrollView References")]
    public RectTransform selectedListContent; // ScrollRect의 Content
    public GameObject cardNameItemPrefab;     // 아이템 프리팹

    [Header("UI")]
    public TextMeshProUGUI deckCountText;     // ← 인스펙터에서 끌어다 놓기 (예: "0/20")

    private const int MAX_DECK = 20;

    // 코드별 현재 장수
    public int CountByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return 0;
        int cnt = 0;
        for (int i = 0; i < codeList.Count; i++)
            if (codeList[i] == code) cnt++;
        return cnt;
    }

    // 1장 추가 (코드당 2장, 총 20장 제한)
    public bool AddOne(string name, string code)
    {
        // 총합 제한 먼저 체크
        if (deckList.Count >= MAX_DECK)
        {
            Debug.Log("[myDeckList] 덱이 가득 찼습니다. 더 이상 카드를 추가할 수 없습니다.");
            return false;
        }

        // 코드별 2장 제한
        if (CountByCode(code) >= 2)
            return false;

        deckList.Add(name);
        codeList.Add(code);
        RefreshSelectedCardListUI();
        return true;
    }

    // 코드에 해당하는 첫 1장 제거
    public bool RemoveOne(string code)
    {
        int idx = codeList.FindIndex(c => c == code);
        if (idx < 0) return false;

        codeList.RemoveAt(idx);
        deckList.RemoveAt(idx);
        RefreshSelectedCardListUI();
        return true;
    }

    public void RefreshSelectedCardListUI()
    {
        // 기존 아이템 삭제
        for (int i = selectedListContent.childCount - 1; i >= 0; i--)
            Destroy(selectedListContent.GetChild(i).gameObject);

        // 병렬 인덱스 기준 생성
        for (int i = 0; i < deckList.Count && i < codeList.Count; i++)
        {
            var item = Instantiate(cardNameItemPrefab, selectedListContent);

            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = deckList[i];

            var clickable = item.GetComponent<SelectedListItem>();
            if (!clickable) clickable = item.AddComponent<SelectedListItem>();
            clickable.Setup(this, deckList[i], codeList[i]);
        }

        // 카운터 갱신: "아이템 오브젝트 개수 / 20"
        UpdateDeckCountLabel();
        // (선택) 초기 프레임 정렬 삑사리 방지
        LayoutRebuilder.ForceRebuildLayoutImmediate(selectedListContent);
    }

    private void UpdateDeckCountLabel()
    {
        if (deckCountText == null) return;
        int itemCount = selectedListContent != null ? selectedListContent.childCount : deckList.Count;
        deckCountText.text = $"{deckList.Count}/{MAX_DECK}";
    }
}
