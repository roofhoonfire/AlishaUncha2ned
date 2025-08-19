using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class BothArmsAnimator : MonoBehaviour
{
    public static BothArmsAnimator Instance { get; private set; }

    [Header("Refs")]
    public RectTransform leftHand;
    public RectTransform rightHand;
    public RectTransform cardArea;

    [Header("Layout Rules (인스펙터 조절)")]
    [Tooltip("N장까지는 기본 간격/크기 유지")]
    public int baseCountN = 2;
    [Tooltip("T장 이상은 더 이상 벌어지지 않음")]
    public int maxResponsiveT = 7;
    [Tooltip("N 초과 1장당 양손 사이 추가 간격(한쪽 손 기준)")]
    public float handSpacingPerExtra = 75f;       // ex) 75 → 좌/우 각각 75px
    [Tooltip("N 초과 1장당 CardArea 좌우 확대량(한쪽 기준)")]
    public float cardAreaHalfGrowPerExtra = 75f;  // ex) 75 → 총 폭 +150px

    [Header("Timings")]
    public float enterDuration = 1.0f;
    public float exitDuration = 1.0f;
    public Ease enterEase = Ease.OutQuad;
    public Ease exitEase = Ease.InOutQuad;

    [Header("Idle Loop")]
    public bool idleLoop = true;
    public float idleBobPixels = 6f;
    public float idlePeriodSecs = 1.2f;

    // --- internal ---
    private Vector2 _leftOrigPos, _rightOrigPos;
    private Vector2 _cardOrigSize;
    private bool _cached;

    private Sequence _idleSeq;
    private Tween _twLeft, _twRight, _twCard;
    public bool IsBusy { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        CacheOriginals();
        DeactivateAll();
    }

    private void CacheOriginals()
    {
        if (_cached) return;
        _leftOrigPos = leftHand.anchoredPosition;
        _rightOrigPos = rightHand.anchoredPosition;
        _cardOrigSize = cardArea.sizeDelta;
        _cached = true;
    }

    private int ExtraCount(int handCount)
    {
        int capped = Mathf.Min(handCount, Mathf.Max(maxResponsiveT, baseCountN));
        return Mathf.Max(0, capped - baseCountN);
    }

    public IEnumerator PlayEnter(int handCount)
    {
        CacheOriginals();
        KillAllTweens();

        // 활성화 + 입력 잠금
        ActivateAll();
        UIInputGate.Push();
        IsBusy = true;

        int extra = ExtraCount(handCount);
        float off = extra * handSpacingPerExtra;
        float halfG = extra * cardAreaHalfGrowPerExtra;

        // 카드 영역은 반드시 Pivot/Anchor가 중앙(0.5,0.5) 추천
        _twCard = cardArea.DOSizeDelta(
            new Vector2(_cardOrigSize.x + halfG * 2f, _cardOrigSize.y),
            enterDuration).SetEase(enterEase);
        _twLeft = leftHand.DOAnchorPosX(_leftOrigPos.x - off, enterDuration).SetEase(enterEase);
        _twRight = rightHand.DOAnchorPosX(_rightOrigPos.x + off, enterDuration).SetEase(enterEase);

        yield return _twCard.WaitForCompletion();

        IsBusy = false;
        UIInputGate.Pop();
        StartIdle();
    }

    public IEnumerator PlayExit()
    {
        StopIdle();
        KillMoveTweens();

        UIInputGate.Push();
        IsBusy = true;

        _twCard = cardArea.DOSizeDelta(_cardOrigSize, exitDuration).SetEase(exitEase);
        _twLeft = leftHand.DOAnchorPos(_leftOrigPos, exitDuration).SetEase(exitEase);
        _twRight = rightHand.DOAnchorPos(_rightOrigPos, exitDuration).SetEase(exitEase);

        yield return _twCard.WaitForCompletion();

        DeactivateAll();
        IsBusy = false;
        UIInputGate.Pop();
    }

    public void KillAllTweens()
    {
        StopIdle();
        KillMoveTweens();
    }

    private void KillMoveTweens()
    {
        _twLeft?.Kill(); _twLeft = null;
        _twRight?.Kill(); _twRight = null;
        _twCard?.Kill(); _twCard = null;
    }

    private void StartIdle()
    {
        if (!idleLoop) return;
        StopIdle();

        // 가벼운 y-바운스 (원점 기준 왕복)
        _idleSeq = DOTween.Sequence();
        _idleSeq.Append(leftHand.DOAnchorPosY(_leftOrigPos.y + idleBobPixels, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));
        _idleSeq.Join(rightHand.DOAnchorPosY(_rightOrigPos.y + idleBobPixels, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));
        _idleSeq.Join(cardArea.DOAnchorPosY(cardArea.anchoredPosition.y + idleBobPixels, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));

        _idleSeq.Append(leftHand.DOAnchorPosY(_leftOrigPos.y, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));
        _idleSeq.Join(rightHand.DOAnchorPosY(_rightOrigPos.y, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));
        _idleSeq.Join(cardArea.DOAnchorPosY(cardArea.anchoredPosition.y, idlePeriodSecs / 2f).SetEase(Ease.InOutSine));

        _idleSeq.SetLoops(-1);
    }

    private void StopIdle()
    {
        if (_idleSeq != null) { _idleSeq.Kill(); _idleSeq = null; }
    }

    private void ActivateAll()
    {
        if (!leftHand.gameObject.activeSelf) leftHand.gameObject.SetActive(true);
        if (!rightHand.gameObject.activeSelf) rightHand.gameObject.SetActive(true);
        if (!cardArea.gameObject.activeSelf) cardArea.gameObject.SetActive(true);
    }

    private void DeactivateAll()
    {
        if (leftHand) leftHand.gameObject.SetActive(false);
        if (rightHand) rightHand.gameObject.SetActive(false);
        if (cardArea) cardArea.gameObject.SetActive(false);
    }
}
