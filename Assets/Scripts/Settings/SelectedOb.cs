using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // TextMeshPro 사용
using Unity.VisualScripting;

public class SelectedOb : MonoBehaviour, IPointerClickHandler
{
    public bool isSelected = false;

    // 인스펙터에서 연결할 텍스트 컴포넌트
    public TextMeshProUGUI cardText;
    public GameObject theCard;

    // 카드 로더의 덱 리스트 참조
    private myDeckList deckList;

    void Start()
    {
        // CardLoader 오브젝트 찾기
        GameObject cardLoader = GameObject.Find("CardLoader");
        if (cardLoader != null)
        {
            deckList = cardLoader.GetComponent<myDeckList>();
            if (deckList == null)
            {
                Debug.LogError("CardLoader에 myDeckList 스크립트가 없음!");
            }
        }
        else
        {
            Debug.LogError("CardLoader 오브젝트를 찾을 수 없음!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = !isSelected;

        if (deckList != null && cardText != null)
        {
            string cardName = cardText.text;

            if (isSelected)
            {
                if (!deckList.deckList.Contains(cardName))
                {
                    deckList.deckList.Add(cardName);
                    deckList.codeList.Add(theCard.GetComponent<EachCardInfo>().cardData.code);
                    deckList.RefreshSelectedCardListUI(); // 덱리에 프리팹생성 _> 실제 인스턴시에이트는 myDecklist에서

                    Debug.Log($"[SelectedOb] 선택됨 → {cardName} 추가");
                }
            }
            else
            {
                if (deckList.deckList.Contains(cardName))
                {
                    deckList.deckList.Remove(cardName);
                    deckList.codeList.Remove(theCard.GetComponent<EachCardInfo>().cardData.code);
                    deckList.RefreshSelectedCardListUI(); // 제거됨

                    Debug.Log($"[SelectedOb] 선택 해제 → {cardName} 제거");
                }
            }
        }
    }
}
