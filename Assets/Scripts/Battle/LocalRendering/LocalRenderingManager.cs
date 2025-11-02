using DG.Tweening;
using JetBrains.Annotations;
using Microlight.MicroBar;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LocalRenderingManager : MonoBehaviour
{
    public TextMeshPro myHP;
    public TextMeshPro opHP;
    public TextMeshPro mydefense;
    public TextMeshPro opdefense;

    public TextMeshProUGUI opponencostRemainTxt;
    public TextMeshProUGUI mycostRemainTxt;


    public TextMeshProUGUI myBound;
    public TextMeshProUGUI OpBound;

    public GameObject paching_Op;
    public GameObject paching_Me;


    public static LocalRenderingManager Instance;
    [SerializeField] BoundImageConductor myBoundConductor;
    [SerializeField] BoundImageConductor opBoundConductor;

    private Coroutine _runningRAAS; // 애프터 액션 추즈를 위한 (코스트 등좡  똭을 위한 코루튄)

    public CardHoverPreview_CastingUI myCardCode;
    public CardHoverPreview_CastingUI opCardCode;

    [SerializeField] private GameObject globalVolume;   // 드래그&드롭
    [SerializeField] private bool globalvolumetest = false; 
    
    public class RenderDiff
    {
        public int actorNum;
        public Dictionary<string, (object oldValue, object newValue)> changedFields = new();
    }
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




    public void Rendering_On_Action_Select(LocalRenderingData data1, LocalRenderingData data2)
    {
        //여기서 할꺼 싸이클 업뎃이랑

        //방어도 업뎃


    }


    public  void Rendering_JujuSync(LocalRenderingData data1, LocalRenderingData data2)
    {
        //바운드 인덱스 넘어가는 거 보여줌 

        //주술 선택 애니메이션 출력 등 

        myBoundConductor.UpdateByIndex(GetMine(data1, data2).boundIndex);
        opBoundConductor.UpdateByIndex(GetOp(data1, data2).boundIndex);


        //data1과 기존의 LocalRenderingData.localRenderingDatas 의 값과 다른 것들 을 애니메로 촤촤촤
        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);



        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    /*
    public  void Rendering_AfterActionSelect(LocalRenderingData data1, LocalRenderingData data2, int whoselect)
    {

        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        ApplyDiffsToLocalRenderingData(diffs);


        //이건깎이기보단 뚜왕 하고 나타나는 느낌으로..다가 . .
        //StartCoroutine(AnimateStatChange("defense", diffs));
        //StartCoroutine(AnimateStatChange("remainingCost", diffs));
        //ApplyDiffsToLocalRenderingData(diffs);
        //Debug.Log("문제없다");






        //뚜왕 하는 느낌으루다가!
        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        //이거 하고 잠깐 멈췃다 다가 뚜가가가
        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }*/
    public void Rendering_AfterActionSelect(LocalRenderingData data1, LocalRenderingData data2, int whoselect)
    {
        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;


        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        ApplyDiffsToLocalRenderingData(diffs);

        //myCardCode.myCardCode = LocalRenderingStatic.localRenderingDatas[myActor].myCard;
        //opCardCode.opCardCode = LocalRenderingStatic.localRenderingDatas[Overmind.Instance.GetOtherPlayerNumber(myActor)].myCard;


        // 진행 중이면 정리하고 새로 시작 (원하면 Kill 생략 가능)
        if (_runningRAAS != null) StopCoroutine(_runningRAAS);
        _runningRAAS = StartCoroutine(Rendering_AfterActionSelect_Coroutine(data1, data2, whoselect));
    }
    public IEnumerator Rendering_AfterActionSelect_AndWait(LocalRenderingData data1, LocalRenderingData data2, int whoselect)
    {
        yield return Rendering_AfterActionSelect_Coroutine(data1, data2, whoselect);
    }

    // 3) 실제 로직은 코루틴에 둔다 (여기서만 연출 완료까지 대기)
    private IEnumerator Rendering_AfterActionSelect_Coroutine(LocalRenderingData data1, LocalRenderingData data2, int whoselect)
    {
        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;


        //        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        //      ApplyDiffsToLocalRenderingData(diffs);

        //    myCardCode.myCardCode = LocalRenderingStatic.localRenderingDatas[myActor].myCard;
        //opCardCode.opCardCode = LocalRenderingStatic.localRenderingDatas[Overmind.Instance.GetOtherPlayerNumber(myActor)].myCard;



        // “뚜왕” 연출: 끝날 때까지 대기
        if (whoselect != PhotonNetwork.LocalPlayer.ActorNumber)
        { 
            paching_Op.SetActive(true);
        
            if(whoselect <= 0)
                paching_Me.SetActive(true);

        }
        else {
            paching_Me.SetActive(true);
                }
        yield return BounceRemainingCost(data1, data2, whoselect);

        // 연출이 끝난 뒤에만 UI 적용
        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        //추가 해야하는 거 카드 코드 전달 
        
        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
        _runningRAAS = null;
    }

    private IEnumerator BounceRemainingCost(LocalRenderingData data1, LocalRenderingData data2, int whoselect)
    {
        if (mycostRemainTxt == null && opponencostRemainTxt == null)
            yield break;

        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

        bool d1IsMine = data1 != null && data1.actorNum == myActor;
        bool d2IsMine = data2 != null && data2.actorNum == myActor;

        int myRemain = d1IsMine ? data1.remainingCost : (d2IsMine ? data2.remainingCost : 0);
        int opRemain = d1IsMine ? (data2 != null ? data2.remainingCost : 0)
                                : (data1 != null ? data1.remainingCost : 0);

        int oppActor = d1IsMine ? (data2 != null ? data2.actorNum : -1)
                                : (data1 != null ? data1.actorNum : -1);

        bool playMy = (whoselect == 0) || (whoselect == myActor);
        bool playOp = (whoselect == 0) || ((oppActor != -1) && (whoselect == oppActor));

        // 텍스트 갱신
        if (mycostRemainTxt != null) mycostRemainTxt.text = myRemain.ToString();
        if (opponencostRemainTxt != null) opponencostRemainTxt.text = opRemain.ToString();

        // 시퀀스 구성 + “붙인 개수”로 판단
        int joinCount = 0;
        var master = DOTween.Sequence();

        if (playMy && mycostRemainTxt != null)
        {
            master.Join(BuildBounceSeq(mycostRemainTxt.rectTransform));
            joinCount++;
        }
        if (playOp && opponencostRemainTxt != null)
        {
            master.Join(BuildBounceSeq(opponencostRemainTxt.rectTransform));
            joinCount++;
        }

        if (joinCount == 0)
            yield break; // 붙인 트윈이 없으면 바로 종료

        yield return master.WaitForCompletion();
    }

    private static Sequence BuildBounceSeq(RectTransform rt, float upScale = 1.5f, float durUp = 0.4f, float durDown = 0.6f)
    {
        // 중복 트윈으로 스케일 꼬임 방지하고 싶다면 아래 한 줄 활성화:
        // rt.DOKill(true);

        Vector3 baseScale = rt.localScale;
        return DOTween.Sequence()
            .Append(rt.DOScale(baseScale * upScale, durUp).SetEase(Ease.OutBack))
            .Append(rt.DOScale(baseScale, durDown).SetEase(Ease.InOutQuad));
    }
    public void ApplyImmediateUI(LocalRenderingData data)
    {
        bool isMine = data.actorNum == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isMine)
        {
            myHP.text = data.hp.ToString();
            mydefense.text = data.defense.ToString();
         //   myBound.text = data.bounds[data.boundIndex].ToString(); // 필요하면 다른 방식으로 포맷
        }
        else
        {
            opHP.text = data.hp.ToString();
            opdefense.text = data.defense.ToString();
            //  OpBound.text = data.bounds[data.boundIndex].ToString(); // 마찬가지
        }
    }
    // 딱 2개 비교해서 로컬 플레이어와 actorNum이 같은 객체를 리턴
    public static LocalRenderingData GetMine(LocalRenderingData a, LocalRenderingData b)
    {
        var my = PhotonNetwork.LocalPlayer.ActorNumber;
        if (a != null && a.actorNum == my) return a;
        if (b != null && b.actorNum == my) return b;
        return null; // 못 찾으면 null
    }

    public static LocalRenderingData GetOp(LocalRenderingData a, LocalRenderingData b)
    {
        var my = PhotonNetwork.LocalPlayer.ActorNumber;
        if (a != null && a.actorNum == my) return b;
        if (b != null && b.actorNum == my) return a;
        return null; // 못 찾으면 null
    }
    public void Rendering_GameStart(LocalRenderingData data1, LocalRenderingData data2)
    {

        //게임시작 렌더링 연출 넣고 싶은거 집어 옇어라 스발아



        ApplyFacingFromRightOrLeft(data1.actorNum, data1);
        ApplyFacingFromRightOrLeft(data2.actorNum, data2);





        ApplyImmediateUI(data1);
        ApplyImmediateUI(data2);

        myBoundConductor.InitBounds(GetMine(data1, data2).bounds);
        opBoundConductor.InitBounds(GetOp(data1, data2).bounds);


        //싱크는 아래서 넣어준다
        //
        //Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);

        StartCoroutine(CineManager.Instance.PlayGameStartAndWait());
    }
    public void Rendering_FaceOff_Start(int nthFaceOff)
    {

       // AlertDialogue.Instance.StartDialogue(null, 0, 0, nthFaceOff, DialogueType.FaceOff);

    }
    public void Rendering_Rumble_Single_Action(int winner, LocalRenderingData data1, LocalRenderingData data2)
    {

        //
        StartCoroutine(CineManager.Instance.PlayRumbleAndWait(winner, data1, data2));



    }

    public void Rendering_Norm_Action(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h, ActionData OpAction, bool GA)
    {

        if (globalvolumetest && globalVolume != null)
            globalVolume.SetActive(false);

        StartCoroutine(Rendering_Norm_Action_Co(actorNum, data1, data2, action, h, OpAction, GA));
    }
    private IEnumerator Rendering_Norm_Action_Co(
     int actorNum,
     LocalRenderingData data1,
     LocalRenderingData data2,
     ActionData action,
     HookType h,
     ActionData otherActionData,                 // [MOD] 로컬 기준 상대편 ActionData (마스터에서 전달)
     bool shouldGuardAnimationPlay               // [MOD] Guard 시네마틱 재생 여부(추가 안전장치)
 )
    {
        LocalRenderingData actorData = null;
        if (data1 != null && data1.actorNum == actorNum) actorData = data1;
        else if (data2 != null && data2.actorNum == actorNum) actorData = data2;

        ApplyFacingFromRightOrLeft(actorNum, actorData);
        Debug.Log($"{actorData.rightOrLeft}를 바라 볼 겁니다 이제");

        bool isMoveAction = (action != null && action.actionId == 0);

        var router = CardAnimationRouter.Instance; // [MOD] 라우터 캐시(null 가능)

        // [MOD] 액터 GO 캐시 (HideEmAll 등에 사용)
        GameObject actorGO = null;
        if (LocalState.Instance != null && LocalState.Instance.PlayerObDic != null)
            LocalState.Instance.PlayerObDic.TryGetValue(actorNum, out actorGO);

        // [MOD] 상대 액터 넘버/카드코드 해석 (Guard 프롤로그용)
        int ResolveOpponentActorNum(int self)
        {
            // ActionData에는 actorNum이 없다고 했으니, Overmind의 헬퍼로 상대 액터 넘버를 얻는다.
            if (Overmind.Instance != null) return Overmind.Instance.GetOtherPlayerNumber(self);

            // 안전빵: Overmind가 없으면 씬에 있는 "다른" 플레이어를 픽업
            if (LocalState.Instance != null && LocalState.Instance.PlayerObDic != null)
            {
                foreach (var kv in LocalState.Instance.PlayerObDic)
                    if (kv.Key != self) return kv.Key;
            }
            return self; // fallback
        }
        string ResolveOpponentCardCode() => otherActionData != null ? otherActionData.cardcode : null;

        if (isMoveAction)
        {
            // ★ 이동 액션 전용 애니메이션(대시/점프 + 지정 프레임 워프)
            yield return StartCoroutine(
                MoveAnimationRouter.Instance.PlayMoveCo(actorNum, data1, data2, action)
            );
        }
        else
        {
            // ============================ 메인 액션(이동 아님) ============================

            if (router == null || action == null)
            {
                Debug.LogWarning("[Rendering] Router or Action is null. Skip animation.");
            }
            else if (h == HookType.Guard)
            {
                // ---------- Guard 렌더링(피격자 = actorNum) ----------
                bool hasExactGuard = CardAnimationRouter.Instance != null &&
                                     CardAnimationRouter.Instance.HasExactEntry(action.cardcode, HookType.Guard);

                if (shouldGuardAnimationPlay && hasExactGuard)
                {
                    int activateActorNum = ResolveOpponentActorNum(actorNum);
                    string activateCardCode = ResolveOpponentCardCode();

                    // [중요] Guard에서는 피격자 HideEmAll 하지 않음!
                    yield return StartCoroutine(
                        CardAnimationRouter.Instance.PlayCo(
                             data1,
                            data2,
                            action.cardcode,                 // 지금(피격자) 카드
                            actorNum,                        // 지금(피격자) actor
                            HookType.Guard,
                            victimActorNum: activateActorNum,                // 상대는 공격자
                            forcedOutcome: null,
                            shouldGuardCinematic: true,                      // Guard 프롤로그 실행(공격자 Prep 재생 + 컷라인 끝난 뒤 Wait!)
                            skipPoint1DueToGuard: false,
                            opponentActivateCardCode: activateCardCode,      // 공격자 Activate 카드로 프롤로그 꾸밈
                            opponentActorNum: activateActorNum
                        )
                    );
                }
                else
                {
                    Debug.Log("[Rendering] Skip Guard cinematic (flag false or no DB)");
                }
            }
            else if (h == HookType.Activate)
            {
                // ---------- Activate 렌더링(공격자) ----------
                bool hasExactAct = CardAnimationRouter.Instance != null &&
                                   CardAnimationRouter.Instance.HasExactEntry(action.cardcode, HookType.Activate);

                if (hasExactAct)
                {
                    // Activate 쪽은 기존처럼 숨김 가능(선택): Point1 스킵이면 숨김 효과도 거의 안 보임
                    if (CameraLovesAlisha.Instance != null && actorGO != null)
                        CameraLovesAlisha.Instance.HideEmAll(actorGO);

                    // 메인 액션(어제 작업한 라우터 그대로)
                    HitResolution? forcedOutcome = EvaluateHitOutcome(actorNum, data1, data2, action);

                    yield return StartCoroutine(
                        CardAnimationRouter.Instance.PlayCo(
                             data1,
                            data2,
                            action.cardcode,
                            actorNum,            // 공격자
                            HookType.Activate,
                            victimActorNum: null,
                            forcedOutcome: forcedOutcome,
                            shouldGuardCinematic: false,
                            skipPoint1DueToGuard: shouldGuardAnimationPlay,  // ★ 직전에 Guard를 보여줬다면 Point1 스킵 → Point2부터
                            opponentActivateCardCode: null,
                            opponentActorNum: null
                        )
                    );
                }
                else
                {
                    Debug.Log($"[Rendering] Skip Activate: No exact anim for card='{action.cardcode}'.");
                }
            }
            else
            {
                // ---------- Priority/Counter 등 기타 훅 ----------
                bool hasExact = router.HasExactEntry(action.cardcode, h); // [MOD] 엄격 검사
                if (hasExact)
                {
                    if (CameraLovesAlisha.Instance != null && actorGO != null)
                        CameraLovesAlisha.Instance.HideEmAll(actorGO);

                    yield return StartCoroutine(
                        CardAnimationRouter.Instance.PlayCo(
                            data1,
                            data2,
                            action.cardcode, actorNum, h,
                            victimActorNum: null,
                            forcedOutcome: null
                        )
                    );
                }
                else
                {
                    Debug.Log($"[Rendering] Skip {h}: No exact anim for card='{action.cardcode}'.");
                }
            }
        }
        // ★ 추가: 연출 직후 글로벌 볼륨 다시 켜기
        if (globalvolumetest && globalVolume != null)
            globalVolume.SetActive(true);
        Debug.Log("자자 노멀 액션 시퀀스 잘봣니?");



     

        // ===== 여기부터는 공통 사후 처리(기존 유지) =====
        var diffs = CopyandDifferences(data1, data2);
        yield return StartCoroutine(AnimateStatChange("defense", diffs));

        HPBarManager.Instance.DamageMe(GetMine(data1,data2).hp);
        HPBarManager.Instance.DamageOp(GetOp(data1, data2).hp);


        Debug.Log($"{GetMine(data1, data2).hp}가 내 체력 {GetOp(data1, data2).hp}가 네 체력 ");
        yield return StartCoroutine(AnimateStatChange("hp", diffs));
        yield return StartCoroutine(AnimateStatChange("remainingCost", diffs));

        Debug.Log("자자 UI 바뀐거 잘밧지?");

        // 위치 최종 스냅(이동 라우터에서 이미 워프했더라도 동일 좌표로 한번 더 정렬 → 문제 없음)
        //여기서 넉백류 애니메이션 넣으면 좋을 듯 ㅎㅎ
        //LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        //LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;

        ApplyDiffsToLocalRenderingData(diffs);

        if (actorNum == PhotonNetwork.LocalPlayer.ActorNumber && h == HookType.Activate)
            myCardCode.myCardCode = null;
        
        else if(actorNum != PhotonNetwork.LocalPlayer.ActorNumber && h == HookType.Activate)
            opCardCode.opCardCode = null;


        StealthPlayer(diffs);
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);
        Debug.Log("자자 노멀 액션 렌더링 다 끝, 이제 렌더링 섭밑만 하면됨");
   
        // [MOD] 혹시 백드롭을 안 썼거나 중간 스킵 경로였을 때를 대비한 안전 복구
        CameraLovesAlisha.Instance?.UnhideAutoHiddenNow();

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public IEnumerator Rendering_After_Anim_Pack(LocalRenderingData data1, LocalRenderingData data2)
    {
        
        var diffs = CopyandDifferences(data1, data2);
        yield return StartCoroutine(AnimateStatChange("defense", diffs));

        HPBarManager.Instance.DamageMe(GetMine(data1, data2).hp);
        HPBarManager.Instance.DamageOp(GetOp(data1, data2).hp);

        yield return StartCoroutine(AnimateStatChange("hp", diffs));
        yield return StartCoroutine(AnimateStatChange("remainingCost", diffs));

        Debug.Log("자자 UI 바뀐거 잘밧지?");

        // 위치 최종 스냅(이동 라우터에서 이미 워프했더라도 동일 좌표로 한번 더 정렬 → 문제 없음)
        //여기서 넉백류 애니메이션 넣으면 좋을 듯 ㅎㅎ
        //LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        //LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;

        ApplyDiffsToLocalRenderingData(diffs);
        StealthPlayer(diffs);
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);

        myCardCode.myCardCode = null;
        opCardCode.opCardCode = null;

        Debug.Log("자자 노멀 액션 렌더링 다 끝, 이제 렌더링 섭밑만 하면됨");

        // [MOD] 혹시 백드롭을 안 썼거나 중간 스킵 경로였을 때를 대비한 안전 복구
        CameraLovesAlisha.Instance?.UnhideAutoHiddenNow();

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
    }


    public void Rendering_Dot_Action(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h)
    {
        StartCoroutine(RenderDotAndContinue(actorNum, data1, data2, action, h));
    }

    private IEnumerator RenderDotAndContinue(int actorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action, HookType h)
    {
        if (action.cardcode == "dot_burn")
        {
            if (LocalState.Instance.PlayerObDic.TryGetValue(action.Dot_to, out var tgt) && tgt != null)
            {
                var anim = tgt.GetComponentInChildren<Animator>();
                if (anim != null)
                    yield return StartCoroutine(PlayTriggerAndWaitExit(anim, "Trig_Dot_Burn", 0, 0.75f, 10f));
            }
        }
        Debug.Log("으악 도트 ");
        // 여기부터 원래 이어지던 작업 수행
        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("defense", diffs));
        HPBarManager.Instance.DamageMe(GetMine(data1, data2).hp);
        HPBarManager.Instance.DamageOp(GetOp(data1, data2).hp);

        StartCoroutine(AnimateStatChange("hp", diffs));
        StartCoroutine(AnimateStatChange("remainingCost", diffs));

        LocalState.Instance.PlayerObDic[data1.actorNum].transform.position = tileIndextoPosition(data1.curpos).position;
        LocalState.Instance.PlayerObDic[data2.actorNum].transform.position = tileIndextoPosition(data2.curpos).position;

        ApplyDiffsToLocalRenderingData(diffs);
        StealthPlayer(diffs);
        ElementRenderer.Instance.RenderElementsFromDiffs(diffs);

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private void StealthPlayer(List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (diff.changedFields.TryGetValue("isStealthed", out var stealthChange))
            {
                bool newValue = (bool)stealthChange.Item2;

                if (LocalState.Instance.PlayerObDic.TryGetValue(diff.actorNum, out var playerOb))
                {
                    var sr = playerOb.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var color = sr.color;

                        if (newValue) // 스텔스 On
                        {
                            color.a = (diff.actorNum == PhotonNetwork.LocalPlayer.ActorNumber) ? 0.4f : 0f;
                        }
                        else // 스텔스 Off
                        {
                            color.a = 1f;
                        }

                        sr.color = color;
                    }
                }
            }
        }
    }
    private Transform tileIndextoPosition(int tileindex)
    {

        //이거 로컬에서도 그리드 초기화 해야함 
        return GridManagement.Instance?.tileObjects[tileindex].transform.Find("charpoint");
        //리턴 된 놈은 Transform으로 받고 .transform.position으로 써야 작동

    }

    public ActionData Rendering_Before_Tile_Choose_ShowDown(List<(int actorNum, ActionData action)> actionList, LocalRenderingData data1, LocalRenderingData data2)
    {
        
            paching_Op.SetActive(true);
            paching_Me.SetActive(true);
        
        int myActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        var myActionTuple = actionList.FirstOrDefault(pair => pair.actorNum == myActorNum);

        if (myActionTuple.action == null)
        {
            Debug.LogError($"로컬 플레이어의 액션을 찾을 수 없습니다. (ActorNumber: {myActorNum})");
            return null;
        }

        ActionData myAction = myActionTuple.action;

     //   if (myAction.actionId == 1)
      //  {
       //     AlertDialogue.Instance.StartDialogue(myAction, 0, 0, 0, DialogueType.TileChoose);

        //}/
       // else
        //{
          //  AlertDialogue.Instance.StartDialogue(null, 0, 0, 0, DialogueType.Wait);
       // }


        List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("remainingCost", diffs));

        ApplyDiffsToLocalRenderingData(diffs);
        //myCardCode.myCardCode = LocalRenderingStatic.localRenderingDatas[PhotonNetwork.LocalPlayer.ActorNumber].myCard;
        //opCardCode.opCardCode = LocalRenderingStatic.localRenderingDatas[Overmind.Instance.GetOtherPlayerNumber(PhotonNetwork.LocalPlayer.ActorNumber)].myCard;

        return myAction;
    }

    public void Rendering_Before_Tile_Choose(LocalRenderingData data1, LocalRenderingData data2, int whoSelect)
    {
        
            if (whoSelect != PhotonNetwork.LocalPlayer.ActorNumber)
                paching_Op.SetActive(true);
            else
            {
                paching_Me.SetActive(true);
            }

            List<RenderDiff> diffs = CopyandDifferences(data1, data2);
        StartCoroutine(AnimateStatChange("remainingCost", diffs));
        ApplyDiffsToLocalRenderingData(diffs);
        //myCardCode.myCardCode = LocalRenderingStatic.localRenderingDatas[PhotonNetwork.LocalPlayer.ActorNumber].myCard;
        //opCardCode.opCardCode = LocalRenderingStatic.localRenderingDatas[Overmind.Instance.GetOtherPlayerNumber(PhotonNetwork.LocalPlayer.ActorNumber)].myCard;

        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);


    }
    public void Rendering_Tile_Choose(int actorNum, ActionData action)
    {



        if (actorNum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
        //    AlertDialogue.Instance.StartDialogue(action, 0, 0, 0, DialogueType.TileChoose);

        }
        else
        {
      //      AlertDialogue.Instance.StartDialogue(null, 0, 0, 0, DialogueType.Wait);

        }



    }
    private IEnumerator AnimateStatChange(string fieldName, List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (!diff.changedFields.TryGetValue(fieldName, out var change))
                continue;

            if (change.oldValue is not int oldVal || change.newValue is not int newVal)
                continue;

            bool isMyActor = diff.actorNum == PhotonNetwork.LocalPlayer.ActorNumber;

            // 타겟 텍스트 할당
            TMP_Text target = null;
            switch (fieldName)
            {
                case "defense":
                    target = isMyActor ? mydefense : opdefense;
                    break;
                case "remainingCost":
                    target = isMyActor ? mycostRemainTxt : opponencostRemainTxt;
                    break;
                case "hp":
                    target = isMyActor ? myHP : opHP;
                    break;
            }

            if (target == null) continue;

            float duration = 0.4f;
            float elapsed = 0f;

            // 숫자 애니메이션(띠리리링)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(oldVal, newVal, t));
                target.text = current.ToString();
                yield return null;
            }

            // 애니메이션 종료 후 보정/후처리
            if (fieldName == "remainingCost" && isMyActor && newVal == 0)
            {
                // 최종 0을 잠깐 찍은 뒤 즉시 지움
                target.text = "0";
                target.text = string.Empty;

                // 트리거 발사 (라벨 기반)
                AnimTriggerManager.Instance?.FireByLabel("myCasting", "Trig_End");
            }
            else
            {
                target.text = newVal.ToString(); // 일반 보정
            }
        }
    }

    private List<RenderDiff> CopyandDifferences(LocalRenderingData data1, LocalRenderingData data2)
    {
        List<RenderDiff> results = new();

        void CompareWithLocal(LocalRenderingData incomingData)
        {
            int actorNum = incomingData.actorNum;
            if (!LocalRenderingStatic.localRenderingDatas.TryGetValue(actorNum, out var localData))
                return;

            var diff = new RenderDiff { actorNum = actorNum };

            void Compare<T>(string fieldName, T oldVal, T newVal)
            {
                if (!EqualityComparer<T>.Default.Equals(oldVal, newVal))
                    diff.changedFields[fieldName] = (oldVal, newVal);
            }

            Compare("curpos", localData.curpos, incomingData.curpos);
            Compare("hp", localData.hp, incomingData.hp);
            Compare("defense", localData.defense, incomingData.defense);
            Compare("remainingCost", localData.remainingCost, incomingData.remainingCost);
            Compare("boundIndex", localData.boundIndex, incomingData.boundIndex);
            Compare("isStealthed", localData.isStealthed, incomingData.isStealthed);

            // ★ 추가: 네가 요청한 4개
            Compare("isBlinded", localData.isBlinded, incomingData.isBlinded);
            Compare("rightOrLeft", localData.rightOrLeft, incomingData.rightOrLeft);
            Compare("myCard", localData.myCard, incomingData.myCard);
            Compare("whosCycle", localData.whosCycle, incomingData.whosCycle);

            bool ListDiff<T>(List<T> a, List<T> b)
                => !(a?.SequenceEqual(b) ?? b == null);

            if (ListDiff(localData.hands, incomingData.hands))
                diff.changedFields["hands"] = (localData.hands, incomingData.hands);

            if (ListDiff(localData.bounds, incomingData.bounds))
                diff.changedFields["bounds"] = (localData.bounds, incomingData.bounds);

            if (ListDiff(localData.elements, incomingData.elements))
                diff.changedFields["elements"] = (localData.elements, incomingData.elements);

            if (diff.changedFields.Count > 0)
                results.Add(diff);
        }

        CompareWithLocal(data1);
        CompareWithLocal(data2);
        return results;
    }


    public void ApplyDiffsToLocalRenderingData(List<RenderDiff> diffs)
    {
        foreach (var diff in diffs)
        {
            if (!LocalRenderingStatic.localRenderingDatas.TryGetValue(diff.actorNum, out var targetData))
                continue;

            foreach (var kvp in diff.changedFields)
            {
                string fieldName = kvp.Key;
                object newVal = kvp.Value.newValue;

                switch (fieldName)
                {
                    case "curpos": targetData.curpos = (int)newVal; break;
                    case "hp": targetData.hp = (int)newVal; break;
                    case "defense": targetData.defense = (int)newVal; break;
                    case "remainingCost": targetData.remainingCost = (int)newVal; break;
                    case "boundIndex": targetData.boundIndex = (int)newVal; break;
                    case "hands": targetData.hands = new List<string>((List<string>)newVal); break;
                    case "bounds": targetData.bounds = new List<string>((List<string>)newVal); break;
                    case "isStealthed": targetData.isStealthed = (bool)newVal; break;
                    case "elements": targetData.elements = new List<apProp>((List<apProp>)newVal); break;

                    // ★ 추가: 네가 요청한 4개
                    case "isBlinded": targetData.isBlinded = (bool)newVal; break;
                    case "rightOrLeft": targetData.rightOrLeft = (string)newVal; break;
                    case "myCard": targetData.myCard = (string)newVal; break;
                    case "whosCycle": targetData.whosCycle = (int)newVal; break;
                }
            }
        }
    }

    private void ApplyFacingFromRightOrLeft(int actorNum, LocalRenderingData d)
    {
        if (d == null) return;
        if (!LocalState.Instance.PlayerObDic.TryGetValue(actorNum, out var go) || go == null) return;

        // "right"면 오른쪽을 보게(기본은 왼쪽을 봄)
        bool faceRight = string.Equals(d.rightOrLeft, "right", System.StringComparison.OrdinalIgnoreCase);

        // 우선 SpriteRenderer.flipX로 처리 (여러 파츠가 있으면 전부 뒤집기)
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs != null && srs.Length > 0)
        {
            foreach (var sr in srs) sr.flipX = faceRight;
            return;
        }

        // 스프라이트가 아니라면(혹은 렌더러가 없다면) 스케일로 폴백
        var t = go.transform;
        var ls = t.localScale;
        ls.x = Mathf.Abs(ls.x) * (faceRight ? -1f : 1f); // 기본 왼쪽(+), 오른쪽은 -로 뒤집기
        t.localScale = ls;
    }








    //뚜드려패기 ㅎ판정용

    private HitResolution EvaluateHitOutcome(int attackerActorNum, LocalRenderingData data1, LocalRenderingData data2, ActionData action)
    {
        // 1) 타일형 -1 → 자동 Miss
        if (action.tileType == -1)
            return HitResolution.Missed;

        // 2) 상대 actorNumber (2인 전제)
        int victimActorNum = Overmind.Instance.GetOtherPlayerNumber(attackerActorNum);

        // 3) 상대 렌더링데이터 찾기
        var victimData =
            (data1 != null && data1.actorNum == victimActorNum) ? data1 :
            (data2 != null && data2.actorNum == victimActorNum) ? data2 : null;

        if (victimData == null)
            return HitResolution.Missed; // 안전빵

        int victimIndex = victimData.curpos;

        // 4) 효과 범위 타일
        var effectTiles = GetEffectTilesSafe(action);

        // 5) 범위 밖이면 Miss
        if (effectTiles == null || effectTiles.Count == 0 || !effectTiles.Contains(victimIndex))
            return HitResolution.Missed;

        // 6) 방어/데미지 비교
        int dmg = Mathf.Max(0, action.damage);
        int def = GetDefenseFromRenderingData(victimData);

        return (dmg <= def) ? HitResolution.Defended : HitResolution.Damage;
    }

    private HashSet<int> GetEffectTilesSafe(ActionData action)
    {
        // ActionData에 effectTiles가 채워져 있다고 했으니 그걸 그대로 사용
        if (action.effectTiles != null && action.effectTiles.Count > 0)
            return new HashSet<int>(action.effectTiles);

        // 없으면 빈 집합 → Miss로 처리됨
        return new HashSet<int>();
    }

    private int GetDefenseFromRenderingData(LocalRenderingData rd)
    {
        // 네가 준 필드명 그대로
        return rd.defense;
    }
    private IEnumerator PlayTriggerAndWaitExit(
    Animator anim, string trigger, int layer = 0,
    float enterTimeout = 0.75f, float maxWait = 10f)
    {
        if (anim == null) yield break;

        // 이전 상태 해시 저장
        int prevHash = anim.GetCurrentAnimatorStateInfo(layer).fullPathHash;

        // 트리거 세팅
        anim.ResetTrigger(trigger);
        anim.SetTrigger(trigger);

        // --- 상태 진입 대기(트리거로 바뀌는 첫 상태를 잡아냄) ---
        int playedHash = -1;
        float enterEnd = Time.realtimeSinceStartup + Mathf.Max(0.05f, enterTimeout);
        while (Time.realtimeSinceStartup < enterEnd)
        {
            var cur = anim.GetCurrentAnimatorStateInfo(layer);
            if (cur.fullPathHash != prevHash)
            {
                playedHash = cur.fullPathHash;
                break;
            }
            if (anim.IsInTransition(layer))
            {
                var next = anim.GetNextAnimatorStateInfo(layer);
                if (next.fullPathHash != prevHash)
                {
                    playedHash = next.fullPathHash;
                    break;
                }
            }
            yield return null;
        }

        // --- 재생 종료 또는 상태 이탈 대기 ---
        float end = Time.realtimeSinceStartup + Mathf.Max(0.2f, maxWait);
        while (Time.realtimeSinceStartup < end)
        {
            var cur = anim.GetCurrentAnimatorStateInfo(layer);

            // 대상 상태를 아직 재생 중이면 normalizedTime 1.0 이상이 되고 전이가 아니면 종료
            if (cur.fullPathHash == playedHash)
            {
                if (!anim.IsInTransition(layer) && cur.normalizedTime >= 1f)
                    break;
            }
            else
            {
                // 대상 상태에서 이미 벗어났으면 종료 (컨트롤러가 다음 상태로 넘김)
                if (!anim.IsInTransition(layer))
                    break;
            }
            yield return null;
        }

        // 뒷정리
        anim.ResetTrigger(trigger);
    }

}
