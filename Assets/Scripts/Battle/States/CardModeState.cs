// CardModeState.cs (원파일 통짜 교체본)
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

    // Buffer
    public ActionData curAction;
    public int actionClockBuffer = 0;
    public List<GameObject> spawnedCards = new List<GameObject>();

    public bool isActive = false;

    public bool isInteractiveSession = false; // 추가: 인터랙티브(ChooseLoop) 진입 여부


    private Coroutine _selectCardCoroutine;
    public ActionPacketData apDataRef;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SetActive(bool active, ActionPacketData apData)
    {
        // 기존 호출 호환: ChooseLoop 경로는 인터랙티브로 취급
        SetActive(active, apData, interactive: true);
    }

    // ★ 추가 오버로드: 진입 성격 명시
    public void SetActive(bool active, ActionPacketData apData, bool interactive)
    {
        if (active == isActive) return;
        isActive = active;
        isInteractiveSession = interactive;

        if (isActive) StartSelectCardLoop(apData);
        else StopSelectCardLoop(null);
    }
    private void StartSelectCardLoop(ActionPacketData apdata)
    {
        // ★ 인터랙티브일 때만 생각 모션 트리거
        if (isInteractiveSession)
        {
            var me = PhotonNetwork.LocalPlayer.ActorNumber;
            if (LocalState.Instance?.PlayerObDic != null &&
                LocalState.Instance.PlayerObDic.TryGetValue(me, out var meGo) &&
                meGo != null)
            {
                var anim = meGo.GetComponentInChildren<Animator>();
                if (anim) anim.SetTrigger("Trig_Think");
            }
        }

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

    public void CancelAndReturn()
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

        // ★ 인터랙티브일 때만 Think_Done 트리거 + ChooseLoop 복귀
        if (isInteractiveSession)
        {
            var me = PhotonNetwork.LocalPlayer.ActorNumber;
            if (LocalState.Instance?.PlayerObDic != null &&
                LocalState.Instance.PlayerObDic.TryGetValue(me, out var meGo) &&
                meGo != null)
            {
                var anim = meGo.GetComponentInChildren<Animator>();
                if (anim) anim.SetTrigger("Trig_Think_Done");
            }

            LocalState.Instance.ReturnToChooseLoop();
        }
    }

    public void StopSelectCardLoop(ActionData action)
    {
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        // ★ 인터랙티브일 때만 Think_Done 트리거
        if (isInteractiveSession)
        {
            var me = PhotonNetwork.LocalPlayer.ActorNumber;
            if (LocalState.Instance?.PlayerObDic != null &&
                LocalState.Instance.PlayerObDic.TryGetValue(me, out var meGo) &&
                meGo != null)
            {
                var anim = meGo.GetComponentInChildren<Animator>();
                if (anim) anim.SetTrigger("Trig_Think_Done");
            }
        }

        StartCoroutine(CloseHandsThenSubmit(action));
    }


    private IEnumerator CloseHandsThenSubmit(ActionData action)
    {
        ClearAllCards();
        yield return BothArmsAnimator.Instance.PlayExit();

        if (action != null)
        {
            if (LocalState.Instance?.alim) LocalState.Instance.alim.SetActive(false);
            Overmind.Instance.SubmitSelection(
                action, action.actionClock,
                PhotonNetwork.LocalPlayer.ActorNumber,
                LocalState.Instance.btmPacket
            );

            // ★ 실제 선택을 제출했을 때만 ChooseLoop 종료 플래그 내리기
            LocalState.Instance?.EndChoosePhase();
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
                Debug.Log("장님련 ㅋㅋ");

                // VisualRoot → BloodShed 경로로 찾기 (자식의 자식 대응)
                Transform visualRoot = cardGO.transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Transform blood = visualRoot.Find("BloodShed");
                    if (blood != null)
                    {
                        blood.gameObject.SetActive(true);
                        Debug.Log("안보여유 ㅋㅋ");
                    }
                    else
                    {
                        Debug.LogWarning("VisualRoot 아래에 BloodShed 없음");
                    }
                }
                else
                {
                    Debug.LogWarning("VisualRoot 없음");
                }
            }

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo 없음"); continue; }

            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);

            //여기가 바뀟다
            var anim = cardGO.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                var deferrer = cardGO.GetComponent<CardApplyDeferrer>();
                if (deferrer == null) deferrer = cardGO.AddComponent<CardApplyDeferrer>();
                deferrer.Bind(
                    info, anim,
                    idleStateName: "Card_Actioin_Idle",
                    layerIndex: 0,
                    requireInitStateName: "Card_Actioin_Init" // ★ Init을 실제로 본 뒤에만 적용
                );
            }
            else
            {
                info.ApplyCardData();
            }
            //음 여기가 말이지
        }

        Debug.Log("손패 생성완료");
    }

    public void ClearAllCards()
    {
        for (int i = cardContentArea.childCount - 1; i >= 0; i--)
            Destroy(cardContentArea.GetChild(i).gameObject);

        spawnedCards.Clear();
    }

    private void SelectCard()
    {
        // 드래그핸들러에서 StopSelectCardLoop 호출하는 기존 구조 사용
    }
    public void ActivatePachingOnCodeZero()
    {
        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var info = card.GetComponentInChildren<EachCardInfo>(true);
            if (info == null || info.cardData == null) continue;

            // code가 "0"인 카드만 대상
            if (info.cardData.cardType == 0)
            {
                // 직계에서 먼저 찾고, 없으면 모든 하위에서 이름으로 탐색
                Transform paching = card.transform.Find("Paching");
                if (paching == null)
                {
                    foreach (var t in card.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Paching") { paching = t; break; }
                    }
                }
                
                if (paching != null)
                {
                    Debug.Log("파칭 찾음");

                    paching.gameObject.SetActive(true);
                }
                // else: 못 찾으면 아무 것도 안 함 (요청대로 다른 동작 불필요)
            }
        }
    }

    public void ActionPacketUpgrade(ActionPacketData apData)
    {

        Debug.Log("손패 업글 눈에 보이지예?");

        int delta_cast = apData.permCast + apData.tempCast;
        int delta_def = apData.tempDef + apData.permDef;

        if (delta_cast == 0 && delta_def == 0) return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var info = card.GetComponentInChildren<EachCardInfo>();
            if (info == null || info.cardData.cardType == 1) continue; // 카드타입 1은 스킵

            // 이름 기준 깊이 탐색으로 TMP 바로 가져오기 (자식의 자식 대응)
            var tmpTime = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("TimeClock");
            var tmpDef = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("Defense");

            if (tmpTime != null && int.TryParse(tmpTime.text, out int v1))
                tmpTime.text = Mathf.Max(apData.CastingMinumum, v1 + delta_cast).ToString();

            if (tmpDef != null && int.TryParse(tmpDef.text, out int v2))
                tmpDef.text = Mathf.Max(0, v2 + delta_def).ToString();
        }
    }
}

// ===== 여기부터 같은 파일 바깥(전역)에 두는 확장 메서드 유틸 =====
public static class TransformUtil
{
    /// <summary>
    /// 이름이 targetName인 자손 노드 어디에서든 컴포넌트 T를 찾아 반환.
    /// 존재하지 않으면 null. 비활성 포함(true).
    /// </summary>
    public static T FindComponentByNameDeep<T>(this Transform root, string targetName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;
        foreach (var c in root.GetComponentsInChildren<T>(true))
            if (c != null && c.name == targetName) return c;
        return null;
    }
}
