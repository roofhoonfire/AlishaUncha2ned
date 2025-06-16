using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro; // 반드시 있어야 함

using UnityEngine.Rendering;


public class CardModeState : MonoBehaviour
{

    public GameObject cardPrefab;
    public RectTransform cardContentArea; // ScrollView 안의 Content

   
    public static CardModeState Instance;
    
    // Buffer
    public ActionData curAction;
    public int actionClockBuffer = 0;
    public List<GameObject> spawnedCards = new List<GameObject>();

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

       InitBuffer();

        ActionClockManipulated();

        if (_selectCardCoroutine == null)
            _selectCardCoroutine = StartCoroutine(SelectCardLoop());


    }
    
    
    private void ActionClockManipulated()
    {
        //애니메도 넣으면 좋다 
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        int delta = LocalState.Instance.localPlayers[actor].actionClockManuplate;

        if (delta == 0)
            return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            Transform timeClockTransform = card.transform.Find("TimeClock");
            if (timeClockTransform == null)
            {
                Debug.LogWarning("TimeClock 자식 오브젝트가 없습니다.");
                continue;
            }

            TextMeshProUGUI tmp = timeClockTransform.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogWarning("TimeClock 오브젝트에 TextMeshProUGUI가 없습니다.");
                continue;
            }

            // 현재 텍스트가 숫자일 때만 처리
            if (int.TryParse(tmp.text, out int originalValue))
            {
                int newValue = Mathf.Max(1, originalValue + delta);
                tmp.text = newValue.ToString();
            }
            else
            {
                Debug.LogWarning($"TimeClock 텍스트가 숫자가 아닙니다: {tmp.text}");
            }
        }
    }
    
    public void InitBuffer()
    {
        // Reset buffers
        curAction = new ActionData();
        curAction.effects = new List<CardEffect>();
        actionClockBuffer = 0;
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
        spawnedCards.Clear(); // 이전 것들 제거

        var actor = PhotonNetwork.LocalPlayer.ActorNumber;
        var player = LocalState.Instance.localPlayers[actor];

        // 부족한 만큼 덱에서 뽑기
        int neededCount = player.handsJangSoo - player.hands.Count;

        for (int i = 0; i < neededCount; i++)
        {
            if (player.DeckCodes.Count == 0)
            {
                // 덱이 비었으면 trash에서 refill 시도
                RefillDeckFromTrash(player);

                // refill 후에도 비어 있으면 더 이상 뽑을 수 없음
                if (player.DeckCodes.Count == 0)
                {
                    Debug.LogWarning("[PopulateCards] Deck is empty even after refill.");
                    break;
                }
            }
            // 덱에서 한 장 뽑아서 hands에 추가
            string drawnCard = player.DeckCodes[0];
            player.DeckCodes.RemoveAt(0);
            player.hands.Add(drawnCard);
        }

        // hands의 카드 코드들로 카드 오브젝트 Instantiate
        foreach (string code in player.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO); // 참조 저장

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null)
            {
                Debug.LogWarning("EachCardInfo 컴포넌트가 없습니다!");
                continue;
            }

            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }

        Debug.Log($"[PopulateCards] Player {actor}: Hands = {player.hands.Count}, Deck = {player.DeckCodes.Count}");



        /* spawnedCards.Clear(); // 이전 것들 제거

        var actor = PhotonNetwork.LocalPlayer.ActorNumber;
        var deck = LocalState.Instance.localPlayers[actor].DeckCodes;
        int deckIndex = LocalState.Instance.localPlayers[actor].deckIndexStart;

        if (deckIndex >= deck.Count)
            deckIndex = 0;

        int endIndex = Mathf.Min(deckIndex + 5, deck.Count);

        for (int i = deckIndex; i < endIndex; i++)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO); // 여기서 참조 저장

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

        LocalState.Instance.localPlayers[actor].deckIndexStart = endIndex;
   */
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



    //여기서 주술 카드는 안뽑게 만들어야함 ㅎ ㅎ .ㅎ .ㅎ .ㅎ .ㅎ .
    private void RefillDeckFromTrash(PlayerData player)
    {
        if (player.trash.Count == 0)
        {
            Debug.Log("[RefillDeckFromTrash] Trash is empty, cannot refill deck.");
            return;
        }

        // Trash → DeckCodes로 이동
        player.DeckCodes.AddRange(player.trash);
        player.trash.Clear();

        // Shuffle
        Shuffle(player.DeckCodes);

        Debug.Log($"[RefillDeckFromTrash] Deck refilled with {player.DeckCodes.Count} cards.");


    }

    private void Shuffle(List<string> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;

        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            string tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

}
