using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class TooltipView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform root;     // 프리팹 루트 RectTransform
    [SerializeField] private TextMeshProUGUI text;   // 설명 텍스트
    [SerializeField] private Image bg;               // 배경 (Raycast Target 끄기 권장)
    [SerializeField] private CanvasGroup cg;         // 페이드/인터랙션 제어 (blocksRaycasts=false)

    [Header("Layout")]
    [SerializeField] private Vector2 padding = new Vector2(12, 8); // 마우스로부터 오프셋
    [SerializeField] private float openX = 0.12f;   // X 확장 시간
    [SerializeField] private float openY = 0.15f;   // Y 확장 시간
    [SerializeField] private Ease easeX = Ease.OutCubic;
    [SerializeField] private Ease easeY = Ease.OutBack;

    [Header("Auto Resize (by text)")]
    [Tooltip("텍스트와 테두리 사이 위/아래 여백(px)")]
    [SerializeField] private float verticalTextMargin = 15f;
    [Tooltip("텍스트 줄바꿈 기준 최대 폭(px). 0이면 현재 Text Rect의 폭을 사용")]
    [SerializeField] private float maxTextWidth = 0f;
    [Tooltip("최소/최대 높이 제한(0이면 무시)")]
    [SerializeField] private float minHeight = 0f, maxHeight = 0f;
    [Tooltip("리사이즈를 부드럽게 보간할지")]
    [SerializeField] private bool animateResize = false;
    [SerializeField] private float resizeDuration = 0.10f;
    [SerializeField] private Ease resizeEase = Ease.OutCubic;

    private Sequence playing;

    public void SetText(string s)
    {
        text.text = s ?? "";
        // 텍스트 기준으로 배경/루트 높이 맞추기
        FitToText();
    }

    /// <summary> 마우스 스크린 좌표 기준으로 표시 + 모서리 결정 + 펼치기 </summary>
    public void OpenAt(Canvas canvas, Vector2 screenPos)
    {
        var canvasRect = canvas.transform as RectTransform;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 레이아웃 갱신 + 리사이즈 보정
        FitToText();

        // 실제 사이즈
        var size = root.rect.size;

        // 화면 밖이면 피벗 전환
        Vector2 newPivot = DecidePivot(canvasRect, screenPos, size);
        root.pivot = newPivot;

        // 화면 좌표 → 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos + OffsetFromPivot(newPivot),
            cam,
            out var localPos
        );
        root.anchoredPosition = localPos;

        // 애니메이션
        root.DOKill();
        playing?.Kill();
        cg.alpha = 1f;                 // 항상 보이게
        root.localScale = Vector3.zero;

        playing = DOTween.Sequence()
            .Append(root.DOScaleX(1f, openX).SetEase(easeX))
            .Append(root.DOScaleY(1f, openY).SetEase(easeY))
            .SetUpdate(true); // 타임스케일 무시 유지
        transform.SetAsLastSibling();   // 맨 위
    }

    /// <summary> 마우스 이동 시 위치 업데이트 + 필요 시 피벗 다시 전환 </summary>
    public void MoveTo(Canvas canvas, Vector2 screenPos)
    {
        var canvasRect = canvas.transform as RectTransform;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 텍스트 바뀌었을 수 있으니 보정(비용 작음)
        FitToText();

        var size = root.rect.size;
        Vector2 pivot = DecidePivot(canvasRect, screenPos, size);
        if (pivot != root.pivot) root.pivot = pivot;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos + OffsetFromPivot(pivot),
            cam,
            out var localPos
        );
        root.anchoredPosition = localPos;
    }

    /// <summary> 접기 (선호 연출: Y→0, X→0) </summary>
    public void Close()
    {
        root.DOKill();
        playing?.Kill();
        playing = DOTween.Sequence()
            .Append(root.DOScaleY(0f, 0.10f).SetEase(Ease.InCubic))
            .Append(root.DOScaleX(0f, 0.08f).SetEase(Ease.InCubic))
            .OnComplete(() => gameObject.SetActive(false))
            .SetUpdate(true);
    }

    // ───────── 내부 유틸 ─────────

    /// <summary>
    /// 텍스트의 선호 높이 + 위/아래 마진(15px) + 9-slice 보더(top/bottom)를 더해서
    /// 배경/루트 높이를 맞춘다.
    /// </summary>
    private void FitToText()
    {
        if (!text || !root) return;

        // 1) 줄바꿈 기준 폭 결정
        var textRT = text.rectTransform;
        float width = maxTextWidth > 0f ? maxTextWidth : Mathf.Max(1f, textRT.rect.width);

        text.enableWordWrapping = true;

        // 2) TMP에 선호 사이즈 질의
        text.ForceMeshUpdate();
        // GetPreferredValues(string, width, height)
        Vector2 pref = text.GetPreferredValues(text.text, width, 0f);
        float textHeight = Mathf.Ceil(pref.y);

        // 텍스트 Rect 높이 적용(폭은 유지/혹은 지정 폭으로 고정)
        textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        // 3) 배경 스프라이트가 9-sliced면 보더 두께를 더해서 실제 안쪽 콘텐츠 마진이 정확히 15px가 되게 한다
        float borderTop = 0f, borderBottom = 0f;
        if (bg && bg.sprite)
        {
            // Unity Sprite.border: (left, bottom, right, top) in pixels
            Vector4 b = bg.sprite.border;
            float ppu = bg.pixelsPerUnit; // 보통 100
            borderBottom = b.y / ppu;
            borderTop = b.w / ppu;
        }

        float targetH = textHeight + (verticalTextMargin * 2f) + borderTop + borderBottom;

        if (minHeight > 0f) targetH = Mathf.Max(minHeight, targetH);
        if (maxHeight > 0f) targetH = Mathf.Min(maxHeight, targetH);

        // 4) root / bg 높이 반영 (앵커가 Stretch일 때 sizeDelta가 기준)
        void ApplyHeight(RectTransform rt)
        {
            if (!rt) return;
            var sd = rt.sizeDelta;
            float currentH = rt.rect.height;
            if (animateResize)
            {
                // sizeDelta.y를 목표값으로 보간 (Stretch 기준)
                rt.DOSizeDelta(new Vector2(sd.x, targetH - (rt.rect.height - sd.y)), resizeDuration)
                  .SetEase(resizeEase)
                  .SetUpdate(true);
            }
            else
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetH);
            }
        }

        ApplyHeight(root);
        if (bg) ApplyHeight(bg.rectTransform);

        // 레이아웃 강제 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    // 화면 밖 잘림 예측하여 모서리(피벗) 결정: TL(0,1), TR(1,1), BL(0,0), BR(1,0)
    private Vector2 DecidePivot(RectTransform canvasRect, Vector2 screenPos, Vector2 tipSize)
    {
        bool overflowRight = screenPos.x + tipSize.x > Screen.width;
        bool overflowBottom = screenPos.y - tipSize.y < 0f;

        float px = overflowRight ? 1f : 0f; // 오른쪽이 모자라면 오른쪽 피벗
        float py = overflowBottom ? 0f : 1f; // 아래가 모자라면 아래 피벗
        return new Vector2(px, py);
    }

    // 마우스와 박스 사이 약간의 패딩을 피벗 방향에 맞춰 더해준다
    private Vector2 OffsetFromPivot(Vector2 pivot)
    {
        float ox = (pivot.x < 0.5f) ? padding.x : -padding.x;
        float oy = (pivot.y > 0.5f) ? -padding.y : padding.y;
        return new Vector2(ox, oy);
    }
}
