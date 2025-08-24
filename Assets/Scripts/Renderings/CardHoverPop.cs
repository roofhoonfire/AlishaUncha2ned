using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CardHoverPop : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    [Header("Hover Pop Settings")]
    public float hoverScale = 1.08f;
    public float growDuration = 0.12f;
    public float shrinkDuration = 0.10f;
    public Ease growEase = Ease.OutQuad;
    public Ease shrinkEase = Ease.InQuad;

    [Header("Z-Order / 겹침 제어")]
    public bool addTempCanvasForSorting = true; // 루트 순서 변경은 금지(그리드 흔들림 원인)
    public int tempSortingOrder = 2000;

    [Header("Targets")]
    [Tooltip("루트(그리드용)는 건드리지 말고, 이 자식만 스케일")]
    public RectTransform scaleTarget; // <- 카드 비주얼 래퍼(없으면 루트로 대체)

    [Header("기타")]
    public bool respectInputGate = true;

    private Vector3 _origScale;
    private Tween _scaleTween;
    private Canvas _tempCanvas; // scaleTarget 쪽에 붙임
    private bool _isHovering;

    void Awake()
    {
        if (!scaleTarget) scaleTarget = transform as RectTransform; // 안전장치
        _origScale = scaleTarget.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (respectInputGate && UIInputGate.IsLocked) return;

        _isHovering = true;

        if (addTempCanvasForSorting) AddTempCanvasToScaleTarget();

        _scaleTween?.Kill();
        _scaleTween = scaleTarget.DOScale(_origScale * hoverScale, growDuration).SetEase(growEase);
    }

    public void OnPointerExit(PointerEventData eventData) => HoverOff();
    public void OnBeginDrag(PointerEventData eventData) => HoverOff(true);

    private void HoverOff(bool immediate = false)
    {
        if (!_isHovering) return;
        _isHovering = false;

        _scaleTween?.Kill();
        if (immediate) scaleTarget.localScale = _origScale;
        else _scaleTween = scaleTarget.DOScale(_origScale, shrinkDuration).SetEase(shrinkEase);

        if (addTempCanvasForSorting) RemoveTempCanvasFromScaleTarget();
    }

    private void AddTempCanvasToScaleTarget()
    {
        // 루트가 아닌 scaleTarget에만 Canvas를 붙여 ‘보이는 순서’만 올림
        _tempCanvas = scaleTarget.GetComponent<Canvas>();
        if (!_tempCanvas) _tempCanvas = scaleTarget.gameObject.AddComponent<Canvas>();
        _tempCanvas.overrideSorting = true;
        _tempCanvas.sortingOrder = tempSortingOrder;

        if (!scaleTarget.GetComponent<GraphicRaycaster>())
            scaleTarget.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void RemoveTempCanvasFromScaleTarget()
    {
        if (_tempCanvas)
        {
            _tempCanvas.overrideSorting = false;
            Destroy(_tempCanvas);
            _tempCanvas = null;
        }
    }

    void OnDisable()
    {
        _scaleTween?.Kill();
        scaleTarget.localScale = _origScale;
        if (addTempCanvasForSorting) RemoveTempCanvasFromScaleTarget();
        _isHovering = false;
    }
}
