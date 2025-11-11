// CardModeState.cs (원파일 통짜 교체본)
using DG.Tweening;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
static class TmpParseUtil
{
    static readonly Regex RichTag = new Regex("<.*?>", RegexOptions.Compiled);

    public static bool TryParseInt(TMP_Text t, out int value)
    {
        value = 0;
        if (t == null || string.IsNullOrEmpty(t.text)) return false;
        var raw = RichTag.Replace(t.text, "");   // <color=...>12</color> → 12
        return int.TryParse(raw, out value);
    }
}
public class CardModeState : MonoBehaviour
{
    public static CardModeState Instance;

    [Header("Card UI")]
    public GameObject cardPrefab;
    public RectTransform cardContentArea;
    [Header("Input Locks")]
    [SerializeField] private bool cancelLocked = false;  // ← 필드(Inspector 노출)
    public bool CancelLocked => cancelLocked;
    // Buffer
    public ActionData curAction;
    public int actionClockBuffer = 0;
    public List<GameObject> spawnedCards = new List<GameObject>();

    public bool isActive = false;

    public bool isInteractiveSession = false; // 추가: 인터랙티브(ChooseLoop) 진입 여부


    private Coroutine _selectCardCoroutine;
    public ActionPacketData apDataRef;
    private int _populateVersion = 0;

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

        if (isActive)
        {
            cancelLocked = false;   // ← 필드로 접근
            StartSelectCardLoop(apData);
        }
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
       // ActionPacketUpgrade(apdata);

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
                if (!cancelLocked) CancelAndReturn();     // ← 필드로 체크
                else Debug.Log("[CardMode] Cancel locked after using support/bless.");
                yield break;
            }

            SelectCard();
            yield return null;
        }
    }

    public void CancelAndReturn()
    {
        if (cancelLocked) { Debug.Log("[CardMode] Cancel locked; ignoring."); return; }

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
                Transform visualRoot = cardGO.transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Transform blood = visualRoot.Find("BloodShed");
                    if (blood != null) blood.gameObject.SetActive(true);
                }
            }

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo 없음"); continue; }

            info.cardData = CardCSVLoader.Instance.GetCardByCode(code);

            var anim = cardGO.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                var deferrer = cardGO.GetComponent<CardApplyDeferrer>();
                if (deferrer == null) deferrer = cardGO.AddComponent<CardApplyDeferrer>();
                deferrer.Bind(
                    info, anim,
                    idleStateName: "Card_Actioin_Idle",
                    layerIndex: 0,
                    requireInitStateName: "Card_Actioin_Init"
                );
            }
            else
            {
                info.ApplyCardData(); // 애니 없으면 즉시 적용
            }
        }

        // ★ 여기 추가: 이번 생성 라운드 버전 고유값 부여 후, 모두 적용될 때까지 기다렸다가 업그레이드 호출
        int myVersion = ++_populateVersion;
        StartCoroutine(WaitCardsAppliedThenUpgrade(apData, myVersion));

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
        int delta_cast = apData.permCast + apData.tempCast;
        int delta_def = apData.tempDef + apData.permDef;
        int delta_dam = apData.tempDam + apData.permDam;
        int delta_rum = apData.tempRum;

        if (delta_cast == 0 && delta_def == 0 && delta_dam == 0 && delta_rum == 0) return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var info = card.GetComponentInChildren<EachCardInfo>();
            if (info == null || info.cardData == null || info.cardData.cardType == 1) continue;

            // 1) 데이터(모델) 기준으로 신규 값 계산
            int baseClock = info.cardData.actionClock;
            int baseDef = info.cardData.defense;
            int baseDam = info.cardData.damage;
            int baseRum = info.cardData.rumblePoint;

            int newClock = Mathf.Max(apData.CastingMinumum, baseClock + delta_cast);
            int newDef = Mathf.Max(0, baseDef + delta_def);
            int newDam = baseDam + delta_dam;
            int newRum = baseRum + delta_rum;

            // 2) UI 참조(이름기반 깊이 탐색 그대로 유지)
            var tmpTime = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("TimeClock");
            var tmpDef = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("Defense");
            var tmpPT = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("pt");
            var tmpRum = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("Rumble");

            // 3) 색상 적용 표기 (정책은 GetColoredValue에 위임)
            if (tmpTime != null)
                tmpTime.text = CardFieldColorizer.GetColoredValue(info.cardData.code, "actionClock", newClock);

            if (tmpDef != null)
                tmpDef.text = CardFieldColorizer.GetColoredValue(info.cardData.code, "defense", newDef);


            if (tmpRum != null)
                tmpRum.text = CardFieldColorizer.GetColoredValue(info.cardData.code, "rumblePoint", newRum);

            // 설명문은 기존 로직대로 damage만 델타/컬러 반영
            if (tmpPT != null)
                tmpPT.text = info.cardData.GetDisplayText("damage", delta_dam);
        }
    }
    public void ActionPacketUpgrade_Bless(ActionPacketData apData, int delta_rumb, int delta_cast, int delta_def, int thisBlessDam)
    {

        Debug.Log("손패 업글 눈에 보이지예?");

      //  int delta_cast = apData.permCast + apData.tempCast;
      //  int delta_def = apData.tempDef + apData.permDef;
     int delta_dam = apData.tempDam + apData.permDam;
        if (delta_cast == 0 && delta_def == 0 && delta_dam == 0 && delta_rumb == 0 && thisBlessDam == 0) return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var info = card.GetComponentInChildren<EachCardInfo>();
            if (info == null || info.cardData.cardType == 1) continue; // 카드타입 1은 스킵

            // 이름 기준 깊이 탐색으로 TMP 바로 가져오기 (자식의 자식 대응)
            var tmpTime = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("TimeClock");
            var tmpDef = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("Defense");
            var tmpPT = card.transform.FindComponentByNameDeep<TextMeshProUGUI>("pt");

            // TimeClock
            if (tmpTime != null && TmpParseUtil.TryParseInt(tmpTime, out var baseClock))
            {
                Debug.Log($"{baseClock}무챠 로코");
                var newClock = Mathf.Max(apData.CastingMinumum, baseClock + delta_cast);
                tmpTime.text = CardFieldColorizer.GetColoredValue(info.cardData.code, "actionClock", newClock);
            }

            // Defense
            if (tmpDef != null && TmpParseUtil.TryParseInt(tmpDef, out var baseDef))
            {
                var newDef = Mathf.Max(0, baseDef + delta_def);
                tmpDef.text = CardFieldColorizer.GetColoredValue(info.cardData.code, "defense", newDef);
            }
            if (thisBlessDam != 0) {
                tmpPT.text = info.cardData.GetDisplayText("damage", delta_dam);

            }
        }
    }


    public void LockCancel(string why = null) { cancelLocked = true; if (!string.IsNullOrEmpty(why)) Debug.Log($"[CardMode] Cancel locked: {why}"); }
    public void UnlockCancel() { cancelLocked = false; Debug.Log("[CardMode] Cancel unlocked."); }


    private IEnumerator WaitCardsAppliedThenUpgrade(ActionPacketData apData, int version)
    {
        // 한 프레임 정도는 애니 초기 진입을 위해 양보
        yield return null;

        float start = Time.realtimeSinceStartup;
        const float TIMEOUT = 5f;

        // 모든 카드에서 CardApplyDeferrer가 사라질 때까지 대기
        while (true)
        {
            // 다른 PopulateCards 호출로 version 바뀌면 중단
            if (version != _populateVersion) yield break;

            bool anyPending = false;
            foreach (var go in spawnedCards)
            {
                if (go == null) continue;
                if (go.GetComponent<CardApplyDeferrer>() != null) { anyPending = true; break; }
            }

            if (!anyPending) break;                    // 모두 적용 완료
            if (Time.realtimeSinceStartup - start > TIMEOUT)
            {
                Debug.LogWarning("[WaitCardsAppliedThenUpgrade] Timeout. 강제 진행.");
                break;
            }

            yield return null;
        }

        // 마지막으로 한 프레임 더 양보해 텍스트/바인딩 정착
        yield return null;

        // 여전히 최신 라운드인지 확인 후 실행
        if (version == _populateVersion)
            ActionPacketUpgrade(apData);
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
