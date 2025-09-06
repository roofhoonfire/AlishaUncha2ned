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
    [SerializeField] private Vector2 padding = new Vector2(12, 8); // 마우스로부터 약간 띄우고 싶다면
    [SerializeField] private float openX = 0.12f;   // X 확장 시간
    [SerializeField] private float openY = 0.15f;   // Y 확장 시간
    [SerializeField] private Ease easeX = Ease.OutCubic;
    [SerializeField] private Ease easeY = Ease.OutBack;

    private Sequence playing;

    public void SetText(string s)
    {
        text.text = s ?? "";
        // 레이아웃 강제 갱신 (실제 사이즈 확보)
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    /// <summary> 마우스 스크린 좌표 기준으로 표시 + 모서리 결정 + 펼치기 </summary>
    public void OpenAt(Canvas canvas, Vector2 screenPos)
    {
        var canvasRect = canvas.transform as RectTransform;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 레이아웃 갱신 후 실제 사이즈
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        var size = root.rect.size;

        // 화면 밖이면 피벗 전환
        Vector2 newPivot = DecidePivot(canvasRect, screenPos, size);
        root.pivot = newPivot;

        // 화면 좌표 → 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos + OffsetFromPivot(newPivot), cam, out var localPos);
        root.anchoredPosition = localPos;

        // 애니메이션
        root.DOKill();
        playing?.Kill();
        cg.alpha = 1f;                 // 투명도는 항상 보이게
        root.localScale = Vector3.zero; // 모서리(피벗)에서 0→1

        playing = DOTween.Sequence()
            .Append(root.DOScaleX(1f, openX).SetEase(easeX))
            .Append(root.DOScaleY(1f, openY).SetEase(easeY))
            .SetUpdate(true); // 타임스케일 무시 원하면 true 유지
        transform.SetAsLastSibling();   // 항상 맨 위에
    }

    /// <summary> 마우스 이동 시 위치 업데이트 + 필요 시 피벗 다시 전환 </summary>
    public void MoveTo(Canvas canvas, Vector2 screenPos)
    {
        var canvasRect = canvas.transform as RectTransform;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 현재 사이즈 기준으로 재판정
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        var size = root.rect.size;
        Vector2 pivot = DecidePivot(canvasRect, screenPos, size);
        if (pivot != root.pivot) root.pivot = pivot;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos + OffsetFromPivot(pivot), cam, out var localPos);
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

    // 화면 밖 잘림 예측하여 모서리(피벗) 결정: TL(0,1), TR(1,1), BL(0,0), BR(1,0)
    private Vector2 DecidePivot(RectTransform canvasRect, Vector2 screenPos, Vector2 tipSize)
    {
        // Canvas 기준 경계 계산
        var canvasSize = canvasRect.rect.size;
        // 스크린 좌표로 판정(간단): 마우스 오른쪽/아래 공간이 부족하면 반대쪽으로 펼친다
        bool overflowRight = screenPos.x + tipSize.x > Screen.width;
        bool overflowBottom = screenPos.y - tipSize.y < 0f;

        float px = overflowRight ? 1f : 0f; // 오른쪽이 모자라면 오른쪽 모서리를 핀(우측으로 펼치지 않게)
        float py = overflowBottom ? 0f : 1f; // 아래가 모자라면 아래 모서리를 핀(아래로 펼치지 않게)
        return new Vector2(px, py);
    }

    // 마우스와 박스 사이 약간의 패딩을 피벗 방향에 맞춰 더해준다
    private Vector2 OffsetFromPivot(Vector2 pivot)
    {
        // pivot=(0,1) → 오른쪽/아래로 펼침 ⇒ x:+padding.x, y:-padding.y
        float ox = (pivot.x < 0.5f) ? padding.x : -padding.x;
        float oy = (pivot.y > 0.5f) ? -padding.y : padding.y;
        return new Vector2(ox, oy);
    }
}
