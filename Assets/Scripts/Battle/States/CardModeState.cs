using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering;


public class CardModeState : MonoBehaviour
{

    public GameObject cardPrefab;
    public RectTransform cardContentArea; // ScrollView 안의 Content

   
    public static CardModeState Instance;
    public bool isActive = false;
    private Coroutine _selectCardCoroutine;
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }


    public void SetActive(bool active)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectCardLoop();
        else StopSelectCardLoop(null);
    }


    private void StartSelectCardLoop()
    {
        PopulateCards();//손패 쫘자작

        if (_selectCardCoroutine == null)
            _selectCardCoroutine = StartCoroutine(SelectCardLoop());


    }

    private IEnumerator SelectCardLoop()
    {
        while (isActive)
        {

            SelectCard();
            yield return null; //SelectCard()가 무한이 아닌 매 프레임마다 한번씩만 호출 되게끔!
        }

    }
    private void SelectCard()
    {

        //



        //선택하고 나면

        //isActive = false
        //Overmind.Instance?.SubmitSelection(액션, 코스트)
        //이 때 액션에 초기화 되어야하는 값은 격돌 포인트, 애니메이션,등의 메타 데이타다
        //이펙트는 액션이 큐에 들어갈 때 알아서 Init만나서 처기화됨 


    }
    public void StopSelectCardLoop(ActionData action)
    {
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }

        if (action != null)
        {
            isActive = false;
            ClearAllCards();
            LocalState.Instance.alim.SetActive(false);
            Overmind.Instance.SubmitSelection(action, action.actionClock);
        }
    }
    public void PopulateCards()
    {

        //애니메이션도 넣고

        //이거 로직 나중에 많이 뜯어 고치자

        // 1) 로컬 플레이어 덱 가져오기
        var actor = PhotonNetwork.LocalPlayer.ActorNumber;
        var deck = LocalState.Instance.localPlayers[actor].DeckCodes;
        int deckIndex = LocalState.Instance.localPlayers[actor].deckIndexStart;

        if (deckIndex >= deck.Count)
            deckIndex = 0;

        // 2) 뽑을 카드의 끝 인덱스 계산 (deckIndex + 5, 또는 deck.Count 중 작은 값)
        int endIndex = Mathf.Min(deckIndex + 5, deck.Count);

        // 3) Instantiate 반복
        for (int i = deckIndex; i < endIndex; i++)
        {
            // 빈 카드 오브젝트 생성
            var cardGO = Instantiate(cardPrefab, cardContentArea);

            // 메타 정보 적용
            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null)
            {
                Debug.LogWarning("EachCardInfo 컴포넌트가 없습니다!");
                continue;
            }

            string code = deck[i];
            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }
        LocalState.Instance.localPlayers[actor].deckIndexStart = endIndex ;
    }


    public void ClearAllCards()
    {

        //애니메이션 이쁜거 넣기 ㅎ
        // childCount 대신 Transform을 순회하여 안전하게 제거
        for (int i = cardContentArea.childCount - 1; i >= 0; i--)
        {
            Transform child = cardContentArea.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}
