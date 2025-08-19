// CardModeState.cs 발췌/패치
using DG.Tweening;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardModeState : MonoBehaviour
{
    public static CardModeState Instance;

    [Header("Card UI")]
    public GameObject cardPrefab;
    public RectTransform cardContentArea;

    //[Header("Hands Orchestrator")]
    //public BothArmsAnimator hands; // 인스펙터에 BothArmsAnimator 할당

    // Buffer
    public ActionData curAction;
    public int actionClockBuffer = 0;
    public List<GameObject> spawnedCards = new List<GameObject>();

    public bool isActive = false;
    private Coroutine _selectCardCoroutine;
    public ActionPacketData apDataRef;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return;
        isActive = active;

        if (isActive) StartSelectCardLoop(apData);
        else StopSelectCardLoop(null);
    }

    private void StartSelectCardLoop(ActionPacketData apdata)
    {
        apDataRef = apdata;
        if (_selectCardCoroutine == null)
            _selectCardCoroutine = StartCoroutine(SelectCardFlow(apdata));
    }

    private IEnumerator SelectCardFlow(ActionPacketData apdata)
    {
        // 1) 손 등장 애니 (handCount = 손패 장수)
        int handCount = apdata?.hands != null ? apdata.hands.Count : 0;
        yield return BothArmsAnimator.Instance.PlayEnter(handCount);

        // 2) 카드 생성 (등장 완료 후 실행)
        PopulateCards(apdata);
        InitBuffer();
        ActionPacketUpgrade(apdata);

        // 3) 선택 루프
        yield return StartCoroutine(SelectCardLoop());

        _selectCardCoroutine = null;
    }

    public void InitBuffer()
    {
        curAction = new ActionData();
        curAction.effects = new List<CardEffect>();
    }

    private IEnumerator SelectCardLoop()
    {
        while (isActive)
        {
            // 손/전환 애니 중에는 입력 무시
            if (UIInputGate.IsLocked)
            {
                yield return null;
                continue;
            }

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

    private void CancelAndReturn()
    {
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        // 카드 제거 → 손 사라짐 애니 → ChooseLoop 복귀
        StartCoroutine(CloseHandsThenReturn());
    }

    private IEnumerator CloseHandsThenReturn()
    {
        ClearAllCards();
        yield return BothArmsAnimator.Instance.PlayExit();

        if (LocalState.Instance?.alim) LocalState.Instance.alim.SetActive(false);
        LocalState.Instance.ReturnToChooseLoop();
    }

    public void StopSelectCardLoop(ActionData action)
    {
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        StartCoroutine(CloseHandsThenSubmit(action));
    }

    private IEnumerator CloseHandsThenSubmit(ActionData action)
    {
        // 1) 카드 UI 제거
        ClearAllCards();

        // 2) 손 사라짐 애니 끝까지
        yield return BothArmsAnimator.Instance.PlayExit();

        // 3) 제출/정리
        if (action != null)
        {
            if (LocalState.Instance?.alim) LocalState.Instance.alim.SetActive(false);
            Overmind.Instance.SubmitSelection(
                action, action.actionClock,
                PhotonNetwork.LocalPlayer.ActorNumber,
                LocalState.Instance.btmPacket
            );
        }
    }

    public void PopulateCards(ActionPacketData apData)
    {
        spawnedCards.Clear();

        foreach (string code in apData.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO);

            if (apData.isBlinded)
            {
                Transform blood = cardGO.transform.Find("BloodShed");
                if (blood != null) blood.gameObject.SetActive(true);
            }

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo 없음"); continue; }

            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }

        Debug.Log("손패 생성완료");
    }

    public void ClearAllCards()
    {
        for (int i = cardContentArea.childCount - 1; i >= 0; i--)
        {
            Destroy(cardContentArea.GetChild(i).gameObject);
        }
        spawnedCards.Clear();
    }

    private void SelectCard()
    {
        // 너의 기존 로직 그대로 사용 (드래그핸들러에서 StopSelectCardLoop 호출)
    }

    public void ActionPacketUpgrade(ActionPacketData apData)
    {
        int delta_cast = apData.permCast + apData.tempCast;
        int delta_def = apData.tempDef + apData.permDef;

        if (delta_cast == 0 && delta_def == 0) return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var timeTf = card.transform.Find("TimeClock");
            var defTf = card.transform.Find("Defense");
            if (timeTf == null || defTf == null) continue;

            var tmp = timeTf.GetComponent<TextMeshProUGUI>();
            var tmpDef = defTf.GetComponent<TextMeshProUGUI>();
            if (tmp != null && int.TryParse(tmp.text, out int v1))
                tmp.text = Mathf.Max(apData.CastingMinumum, v1 + delta_cast).ToString();
            if (tmpDef != null && int.TryParse(tmpDef.text, out int v2))
                tmpDef.text = Mathf.Max(0, v2 + delta_def).ToString();
        }
    }
}
