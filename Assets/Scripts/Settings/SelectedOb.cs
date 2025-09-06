using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SelectedOb : MonoBehaviour, IPointerClickHandler
{
    // 시각용으로 쓰고 싶으면 유지, 로직엔 사용하지 않는다.
    public bool isSelected = false;

    public TextMeshProUGUI cardText; // 카드 표시명
    public GameObject theCard;       // EachCardInfo 보유

    private myDeckList deckList;

    void Start()
    {
        var cardLoader = GameObject.Find("CardLoader");
        if (!cardLoader)
        {
            Debug.LogError("CardLoader 오브젝트를 찾을 수 없음!");
            return;
        }
        deckList = cardLoader.GetComponent<myDeckList>();
        if (!deckList)
            Debug.LogError("CardLoader에 myDeckList 스크립트가 없음!");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!deckList || !theCard) return;

        var info = theCard.GetComponent<EachCardInfo>();
        if (info == null || info.cardData == null) return;

        string code = info.cardData.code;
        string name = (cardText != null) ? cardText.text : info.cardData.name;

        int count = deckList.CountByCode(code);
        if (count >= 2)
        {
            Debug.Log($"[SelectedOb] '{name}'는 이미 2장. 더 추가 불가.");
            return;
        }

        deckList.AddOne(name, code); // 내부에서 UI 리프레시
        // (선택) 시각 표시 갱신: isSelected = (deckList.CountByCode(code) > 0);
    }
}
