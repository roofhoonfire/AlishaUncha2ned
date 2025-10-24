using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Photon.Realtime;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;

public class CardDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Animator")]
    public Animator animator;                     // 카드 오브젝트(또는 자식)에 Animator 달아두기
    [SerializeField] private string trigInZone = "Trig_InZone";
    [SerializeField] private string trigOffZone = "Trig_OffZone";

    [Header("FX")]
    [Tooltip("드롭 존 위에 있을 때 켜질 이펙트(예: 불꽃, 하이라이트)")]
    [SerializeField] private GameObject inZoneFlame; // ★ 인스펙터에서 드래그&드롭

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform dropZone;

    private LayoutElement layoutElement;
    private Vector2 dragOffset;
    private Vector2 originalAnchoredPos;
    private Vector2 originalSizeDelta;
    private Vector3 originalScale;

    // Character and shape data
    private GameObject myChara;
    public Vector3Int playerCoord;
    public List<Vector3Int> debugYong;
    public float angle;

    // Card data
    private Card thisCardData;

    // Tile highlighting
    private List<int> prevHighlighted = new List<int>();

    private int actorNum;

    // 내부 상태
    private bool wasInside = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalSizeDelta = rectTransform.sizeDelta;
        originalScale = transform.localScale;

        layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        var zone = GameObject.FindWithTag("dropzone");
        if (zone != null)
            dropZone = zone.GetComponent<RectTransform>();

        actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        myChara = LocalState.Instance?.PlayerObDic[actorNum];
        playerCoord = GridManagement.Instance.GetCoordFromIndex(
            LocalRenderingStatic.localRenderingDatas[actorNum].curpos
        );

        // Animator 자동 참조(없으면 null 허용)
        if (!animator) animator = GetComponent<Animator>();

        // 시작 시 꺼 둔다
        if (inZoneFlame) inZoneFlame.SetActive(false);
    }

    private void OnDisable()
    {
        // 비활성화 시 흔적 정리
        if (inZoneFlame) inZoneFlame.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CardModeState.Instance == null || !CardModeState.Instance.isActive || !CardModeState.Instance.isInteractiveSession)
            return;
        canvas = GetComponentInParent<Canvas>(); // 호출 순서 고려해서 여기서 획득
        thisCardData = GetComponent<EachCardInfo>().cardData;

        canvasGroup.blocksRaycasts = false;
        originalAnchoredPos = rectTransform.anchoredPosition;
        layoutElement.ignoreLayout = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        dragOffset = localPoint - originalAnchoredPos;

        // 트리거 정리 & 시작 상태 초기화
        wasInside = false;
        if (animator)
        {
            animator.ResetTrigger(trigInZone);
            animator.ResetTrigger(trigOffZone);
        }

        // 드래그 시작 시 꺼 둔다 (안전장치)
        if (inZoneFlame) inZoneFlame.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CardModeState.Instance == null || !CardModeState.Instance.isActive || !CardModeState.Instance.isInteractiveSession)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint - dragOffset;

        bool inside = IsInsideDropZone(eventData.position);
        HandleZoneAnimation(inside);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CardModeState.Instance == null || !CardModeState.Instance.isActive || !CardModeState.Instance.isInteractiveSession)
            return;
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;

        if (!IsInsideDropZone(eventData.position))
        {
            // 드롭 실패 → 원위치 복귀
            rectTransform.DOAnchorPos(originalAnchoredPos, 0.25f).SetEase(Ease.OutQuad);
            transform.DOScale(originalScale, 0.25f).SetEase(Ease.OutQuad);

            // 상태를 확실히 Off로
            if (animator)
            {
                animator.ResetTrigger(trigInZone);
                animator.SetTrigger(trigOffZone);
            }

            // 이펙트도 확실히 끄기
            if (inZoneFlame) inZoneFlame.SetActive(false);

            // Blinded: 피 다시 덮기
            if (CardModeState.Instance.apDataRef.isBlinded)
            {
                Transform blood = transform.Find("BloodShed");
                if (blood != null)
                    blood.gameObject.SetActive(true);
            }
        }
        else // 드롭 성공
        {
            var apData = CardModeState.Instance.apDataRef;

            if (thisCardData.cardType == 0)
            {
                CardModeState.Instance.curAction.actionId = 1;
                CardModeState.Instance.curAction.defense = Mathf.Max(0, thisCardData.defense + apData.tempDef + apData.permDef);
                CardModeState.Instance.curAction.rumblePoint = thisCardData.rumblePoint;
                CardModeState.Instance.curAction.cardcode = thisCardData.code;
                CardModeState.Instance.curAction.animations = thisCardData.animations;
                CardModeState.Instance.curAction.actionClock = Mathf.Max(apData.CastingMinumum, thisCardData.actionClock + apData.permCast + apData.tempCast);
                CardModeState.Instance.curAction.cardname = thisCardData.name;
                CardModeState.Instance.curAction.zoneIndex = thisCardData.zoneIndex;
                CardModeState.Instance.curAction.tileType = thisCardData.tileType;
                CardModeState.Instance.curAction.effects.AddRange(CardDEffectDatabase.GetEffects(thisCardData.code));
                CardModeState.Instance.curAction.damage = Mathf.Max(0, thisCardData.damage + apData.tempDam + apData.permDam);

                btmPacketAdd(thisCardData.code);

                Debug.Log("로컬 핸드를 위한 apdata 초기화!!");

                // ── 여기부터 추가 ─────────────────────────────────────────────
                // 1) 로컬 핸드 AP 임시 수치 리셋
                if (LocalState.Instance?.LastApData != null)
                {
                    LocalState.Instance.LastApData.tempDef = 0;
                    LocalState.Instance.LastApData.tempCast = 0;
                    LocalState.Instance.LastApData.tempDam = 0;

                    // 2) usedCard 목록을 멀티셋 방식으로 hands에서 제거
                    RemoveUsedFromHands(LocalState.Instance.LastApData, LocalState.Instance.btmPacket?.usedCard);
                }
                // ───────────────────────────────────────────────────────────────
                CardModeState.Instance.StopSelectCardLoop(CardModeState.Instance.curAction);
            }
            else if (thisCardData.cardType == 1)
            {
                CardModeState.Instance.curAction.effects.AddRange(CardDEffectDatabase.GetEffects(thisCardData.code));

                apData.tempDef += thisCardData.defense;
                apData.tempDam += thisCardData.damage;
                apData.tempCast += thisCardData.actionClock;

                CardModeState.Instance.ActivatePachingOnCodeZero();

                CardModeState.Instance.ActionPacketUpgrade(apData);

                btmPacketAdd(thisCardData.code);
                Destroy(gameObject);

                //쓴 블레스 추가 

                CardModeState.Instance.curAction.blessList.Add(thisCardData.code);

                LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber]
                    .GetComponentInChildren<Animator>()
                    .SetTrigger("Trig_Support_Buff");
            }
            else if (thisCardData.cardType == 2)
            {
                var effectsList = CardDEffectDatabase.GetEffects(thisCardData.code);
                foreach (var e in effectsList)
                {
                    if (e.hookType == HookType.Support)
                    {
                        e.Apply(0, null, 0, null, null, null);
                        Debug.Log("ImSupport 즉발");
                    }
                    else
                    {
                        CardModeState.Instance.curAction.effects.Add(e);
                        Debug.Log("support added");
                    }
                }
                CardModeState.Instance.curAction.blessList.Add(thisCardData.code);

                CardModeState.Instance.ActionPacketUpgrade(apData);

                btmPacketAdd(thisCardData.code);


                

                Destroy(gameObject);
            }




        }
    }
    private static void RemoveUsedFromHands(ActionPacketData ap, List<string> used)
    {
        if (ap == null || ap.hands == null || used == null) return;

        // 멀티셋 제거: used 에 있는 각 코드마다 hands에서 "첫 번째 매칭"만 제거
        foreach (var code in used)
        {
            int idx = ap.hands.IndexOf(code);
            if (idx >= 0) ap.hands.RemoveAt(idx);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickScaleAnimation();
    }

    private void HandleZoneAnimation(bool inside)
    {
        // 상태 변화시에만 트리거/이펙트 발동
        if (inside != wasInside)
        {
            if (animator)
            {
                if (inside)
                {
                    AnimTriggerManager.Instance.FireByLabel("myCasting", "Trig_Open");
                    animator.ResetTrigger(trigOffZone);
                    animator.SetTrigger(trigInZone);
                }
                else
                {
                    AnimTriggerManager.Instance.FireByLabel("myCasting", "Trig_Close");
                    animator.ResetTrigger(trigInZone);
                    animator.SetTrigger(trigOffZone);
                }
            }

            // 인존 이펙트 온/오프
            if (inZoneFlame) inZoneFlame.SetActive(inside);

            // Blinded일 때 피 오버레이 토글(존 안에선 걷고, 밖에선 덮기)
            if (CardModeState.Instance.apDataRef.isBlinded)
            {
                Transform blood = transform.Find("BloodShed");
                if (blood != null)
                    blood.gameObject.SetActive(!inside);
            }

            wasInside = inside;
        }
    }

    private bool IsInsideDropZone(Vector2 screenPos) =>
        dropZone != null && RectTransformUtility.RectangleContainsScreenPoint(dropZone, screenPos);

    private void PlayClickScaleAnimation()
    {
        transform.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * 1.1f, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, 0.08f).SetEase(Ease.InQuad));
    }

    private void btmPacketAdd(string code)
    {
        LocalState.Instance.btmPacket.usedCard.Add(code);
    }
}
