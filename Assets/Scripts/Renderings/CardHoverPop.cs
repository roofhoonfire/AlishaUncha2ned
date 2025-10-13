using System.Collections.Generic; // ← 이것 추가
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using Photon.Pun; // ★ 추가

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
    public bool addTempCanvasForSorting = true;
    public int tempSortingOrder = 2000;

    [Header("Targets")]
    [Tooltip("루트(그리드용)는 건드리지 말고, 이 자식만 스케일")]
    public RectTransform scaleTarget;

    [Header("기타")]
    public bool respectInputGate = true;

    private Vector3 _origScale;
    private Tween _scaleTween;
    private Canvas _tempCanvas;
    private bool _isHovering;

    void Awake()
    {
        if (!scaleTarget) scaleTarget = transform as RectTransform;
        _origScale = scaleTarget.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (respectInputGate && UIInputGate.IsLocked) return;

        _isHovering = true;

        if (addTempCanvasForSorting) AddTempCanvasToScaleTarget();

        _scaleTween?.Kill();
        _scaleTween = scaleTarget
            .DOScale(_origScale * hoverScale, growDuration)
            .SetEase(growEase);

        // ★ 스킬 범위 미리보기 + 글로우
        TryPreviewSkillRange();
    }

    public void OnPointerExit(PointerEventData eventData) => HoverOff();
    public void OnBeginDrag(PointerEventData eventData) => HoverOff(true);

    private void HoverOff(bool immediate = false)
    {
        if (!_isHovering) return;
        _isHovering = false;

        // ★ 타일 색상/글로우 원복
        GridManagement.Instance?.ResetAllTiles();
        GridManagement.Instance?.HideAllGlow();

        _scaleTween?.Kill();
        if (immediate) scaleTarget.localScale = _origScale;
        else _scaleTween = scaleTarget.DOScale(_origScale, shrinkDuration).SetEase(shrinkEase);

        if (addTempCanvasForSorting) RemoveTempCanvasFromScaleTarget();
    }

    private void AddTempCanvasToScaleTarget()
    {
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

        // ★ 비활성화 시에도 원복
        GridManagement.Instance?.ResetAllTiles();
        GridManagement.Instance?.HideAllGlow();
    }

    // =========================
    // ★ 여기부터 미리보기 로직
    // =========================

    private void TryPreviewSkillRange()
    {
        // 카드 데이터 획득
        var info = GetComponentInParent<EachCardInfo>(true);
        if (info == null || info.cardData == null) return;

        var card = info.cardData;

        // 카드 타입 0(공격/행동 카드)만 미리보기
        if (card.cardType != 0) return;

        int tileType = card.tileType;
        int zoneIndex = card.zoneIndex;

        // 내 현재 위치 인덱스
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        int curIndex = LocalRenderingStatic.localRenderingDatas.TryGetValue(actor, out var lr)
            ? lr.curpos : -1;
        if (curIndex < 0) return;

        // 항상 시작은 초기화 + 글로우 끄기
        GridManagement.Instance.ResetAllTiles();
        GridManagement.Instance.HideAllGlow();

        if (tileType == -1)
        {
            // 미리보기 없음
            return;
        }
        else if (tileType == 0)
        {
            // 6방향 모두 칠하기(파랑) + 글로우
            var colored = PreviewDirectionalShapeAllSix(zoneIndex, curIndex, Color.cyan);

            // ★ 글로우 적용
            var glowSet = new HashSet<int>(colored);
            GridManagement.Instance.ShowGlowForIndices(glowSet, Color.cyan);
        }
        else if (tileType >= 1 && tileType <= 10)
        {
            // 리처블 칠하기(파랑)
            GridManagement.Instance.HighlightReachableTilesFrom(
                curIndex,
                tileType,
                Color.cyan,
                "Skill"
            );

            // ★ reachable 타일들에 글로우 부여
            var glowSet = new HashSet<int>();
            foreach (var kv in GridManagement.Instance.tileObjects)
            {
                var t = kv.Value.GetComponent<EachTile>();
                if (t != null && t.canMove) glowSet.Add(kv.Key);
            }
            GridManagement.Instance.HideAllGlow();
            GridManagement.Instance.ShowGlowForIndices(glowSet, Color.cyan);
        }
        else if (tileType >= 11)
        {
            // 선형 리처블 칠하기(파랑)
            GridManagement.Instance.HighlightReachableTilesFrom(
                curIndex,
                tileType,            // 내부에서 길이 = tileType - 10
                Color.cyan,
                "LinearSkill"
            );

            // ★ reachable 타일들에 글로우 부여
            var glowSet = new HashSet<int>();
            foreach (var kv in GridManagement.Instance.tileObjects)
            {
                var t = kv.Value.GetComponent<EachTile>();
                if (t != null && t.canMove) glowSet.Add(kv.Key);
            }
            GridManagement.Instance.HideAllGlow();
            GridManagement.Instance.ShowGlowForIndices(glowSet, Color.cyan);
        }
    }

    /// <summary>
    /// zoneIndex 모양을 기준으로 6방향 모두를 색칠(파랑)하고,
    /// 실제로 칠해진 타일 인덱스 집합을 반환.
    /// </summary>
    private HashSet<int> PreviewDirectionalShapeAllSix(int zoneIndex, int startIndex, Color color)
    {
        var grid = GridManagement.Instance;
        var result = new HashSet<int>();
        if (grid == null) return result;

        var origin = grid.GetCoordFromIndex(startIndex);
        var baseShape = SkillTileDatabase.skillShapes[zoneIndex];

        foreach (var dir in HexSkill.Directions)
        {
            var coords = HexSkill.GetSkillTargets(baseShape, dir, origin);
            foreach (var c in coords)
            {
                int idx = grid.GetIndexFromCoord(c);
                if (idx >= 0) result.Add(idx);
            }
        }

        // 실제 칠하기
        foreach (var kv in grid.tileObjects)
        {
            int idx = kv.Key;
            var go = kv.Value;
            if (!go) continue;

            var tile = go.GetComponent<EachTile>();
            var sr = go.GetComponent<SpriteRenderer>();
            if (!sr) continue;

            if (result.Contains(idx))
            {
                tile.defaultColor = color;
                sr.color = color;
                tile.canMove = true; // 글로우 집합 분류에 쓰일 수 있어 추가 표시
            }
        }

        return result;
    }
}
