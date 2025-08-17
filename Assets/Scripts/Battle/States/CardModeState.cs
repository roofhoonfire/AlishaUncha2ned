using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro; // 반드시 있어야 함

using UnityEngine.Rendering;
using UnityEditor;

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
    public ActionPacketData apDataRef;

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
    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return;
        isActive = active;
        if (isActive) StartSelectCardLoop(apData);
        else StopSelectCardLoop(null);   // null이면 제출 안 함
    }

    //추가 됨낄렵
    /*
    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectCardLoop(apData);
        else StopSelectCardLoop(null);
    }
    */

  
    private void CancelAndReturn()
    {
        // 코루틴만 멈추면 카드가 남으니, 확실히 비워주자
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        ClearAllCards();                    // ★ 손패 UI 제거
        if (LocalState.Instance?.alim) LocalState.Instance.alim.SetActive(false);

        // 제출 없이 복귀
        LocalState.Instance.ReturnToChooseLoop();
    }
    private void StartSelectCardLoop(ActionPacketData apdata)
    {
        apDataRef = apdata;
        PopulateCards(apdata);//손패 쫘자작

        InitBuffer();
        ActionPacketUpgrade(apdata);

        if (_selectCardCoroutine == null)
            _selectCardCoroutine = StartCoroutine(SelectCardLoop());


    }



    // 코스트, 방어도 데미지 강화된거 렌더링 하는 함수 
    public void ActionPacketUpgrade(ActionPacketData apData)
    {
        //애니메도 넣으면 좋다 
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        int delta_cast = apData.permCast + apData.tempCast;
        int delta_def = apData.tempDef + apData.permDef;

        //여기에 방어도 코드도 넣으면 된다 
        //지금은 넘어간다 귀찮으ㅡ므로


        if (delta_cast == 0 || delta_def == 0)
            return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            Transform timeClockTransform = card.transform.Find("TimeClock");
            Transform defenseTransform = card.transform.Find("Defense");
            if (timeClockTransform == null)
            {
                Debug.LogWarning("TimeClock 자식 오브젝트가 없습니다.");
                continue;
            }
            if (defenseTransform == null)
            {
                Debug.LogWarning("디펜스 자식 오브젝트가 없습니다.");
                continue;
            }

            TextMeshProUGUI tmp = timeClockTransform.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI tmpdef = defenseTransform.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogWarning("TimeClock 오브젝트에 TextMeshProUGUI가 없습니다.");
                continue;
            }

            // 현재 텍스트가 숫자일 때만 처리
            if (int.TryParse(tmp.text, out int originalValue))
            {
                int newValue = Mathf.Max(apData.CastingMinumum, originalValue + delta_cast);
                tmp.text = newValue.ToString();
            }
            else
            {
                Debug.LogWarning($"TimeClock 텍스트가 숫자가 아닙니다: {tmp.text}");
            }

            if (int.TryParse(tmpdef.text, out int ogval))
            {
                int newValue = Mathf.Max(0, ogval + delta_def);
                tmpdef.text = newValue.ToString();
            }
            else
            {
                Debug.LogWarning($"TimeClock 텍스트가 숫자가 아닙니다: {tmpdef.text}");
            }
        }
    }

    public void InitBuffer()
    {
        // Reset buffers
        curAction = new ActionData();
        curAction.effects = new List<CardEffect>();
    }

    //추가됨 낄렵
    private IEnumerator SelectCardLoop()
    {
        while (isActive)
        {
            // ESC 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturn();
                yield break;
            }

            SelectCard();
            yield return null;
        }
    }

    /*
    private IEnumerator SelectCardLoop()
    {
        while (isActive)
        {

            SelectCard();
            yield return null; //SelectCard()가 무한이 아닌 매 프레임마다 한번씩만 호출 되게끔!
        }

    }*/
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
            Overmind.Instance.SubmitSelection(action, action.actionClock, PhotonNetwork.LocalPlayer.ActorNumber, LocalState.Instance.btmPacket);
        }
    }
    public void PopulateCards(ActionPacketData apData)
    {
        spawnedCards.Clear(); // 이전 것들 제거



        // hands의 카드 코드들로 카드 오브젝트 Instantiate
        foreach (string code in apData.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO); // 참조 저장
            if (apData.isBlinded)
            {
                Transform blood = cardGO.transform.Find("BloodShed");
                if (blood != null)
                {
                    blood.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("BloodShed 자식 오브젝트를 찾을 수 없습니다!");
                }
            }
            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null)
            {
                Debug.LogWarning("EachCardInfo 컴포넌트가 없습니다!");
                continue;
            }

            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }

        Debug.Log($"손패 생성완료 ");




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
