using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoundImageConductor : MonoBehaviour
{
    [Header("DB & Targets")]
    [SerializeField] private BoundSpriteDB database;
    [SerializeField] private Image targetImage; // 비우면 GetComponent<Image>() 사용

    [Header("Animation Watch (선택)")]
    [SerializeField] private Animator animTarget;
    [SerializeField] private string triggerName = "Trig_BoundSwap";
    [SerializeField] private string watchStateTag = "BoundSwap";
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float maxWaitSeconds = 3f;

    [Header("Self Fade")]
    [SerializeField] private bool fadeSelf = true;
    [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0f;
    [SerializeField] private float fadeOutDuration = 0.12f;
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("상대 바운드면 물음표 표시")]
    [SerializeField] private bool isOp = false;

    [Header("Tooltip 연동 (선택)")]
    [SerializeField] private TooltipTriggerUI tooltipTrigger; // ★ 메인 큰 이미지용

    [Header("바운드예차크")]
    [SerializeField] public List<string> boundGuesses; // ★ isOp==true일 때 사용

    // ★★★ 추가: 그리드 및 프리팹
    [Header("프리뷰 그리드 & 프리팹")]
    [Tooltip("내 바운드 프리뷰가 들어갈 GridLayoutGroup의 RectTransform")]
    [SerializeField] private RectTransform myGridRoot;
    [Tooltip("상대 바운드 프리뷰가 들어갈 GridLayoutGroup의 RectTransform")]
    [SerializeField] private RectTransform opGridRoot;
    [Tooltip("각 칸에 깔릴 100x100 프리팹 (Image + TooltipTriggerUI 포함)")]
    [SerializeField] private BoundIconItem iconPrefab;
    public GameObject myGridArea;
    public GameObject opGridArea;


    // 내부 상태
    private readonly List<string> _bounds = new();
    private int _index = -1;
    private bool _initialized = false;
    private Coroutine _running;

    // ★ 생성한 프리뷰 아이콘들 보관(설명 업데이트 용)
    private readonly List<BoundIconItem> _myIcons = new();
    private readonly List<BoundIconItem> _opIcons = new();

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null)
        {
            var c = targetImage.color;
            targetImage.color = new Color(c.r, c.g, c.b, targetImage.color.a);
        }
    }

    public string InitBounds(IList<string> boundsFromData)
    {
        // 초기화
        _bounds.Clear();
        if (boundsFromData != null) _bounds.AddRange(boundsFromData);
        _initialized = true;
        _index = 0;

        // 그리드/아이콘 초기화
        ClearGrid(myGridRoot, _myIcons);
        ClearGrid(opGridRoot, _opIcons);

        // isOp 여부에 따라 프리뷰 생성
        if (!isOp)
        {
            // 내 바운드: 실제 키로 프리뷰 고정 생성
            BuildMyGridPreview(_bounds);
        }
        else
        {
            // 상대 바운드: b0 이미지 + 초기 guess로 생성
            if (boundGuesses == null) boundGuesses = new List<string>();
            boundGuesses.Clear();
            for (int i = 0; i < _bounds.Count; i++)
                boundGuesses.Add("상대의 행동에 집중해서 바운드를 예측하자");

            BuildOpGridPreview(_bounds.Count, boundGuesses);
        }

        // 메인 큰 이미지 적용키 결정
        string key = (_bounds.Count > 0) ? _bounds[0] : null;
        if (isOp) key = "b0";   // 상대는 가림

        // ★ 핵심: 먼저 프리뷰들을 만들고, 그 다음 메인 이미지/툴팁을 적용
        ApplyByKeyImmediate(key, 0);
        SetAlphaImmediate(1f);
        return key;
    }

    public void UpdateByIndex(int newIndex)
    {
        if (!_initialized) { Debug.LogWarning("[BoundImageConductor] Not initialized."); return; }
        if (newIndex < 0 || newIndex >= _bounds.Count) { Debug.LogWarning("[BoundImageConductor] Index out of range."); return; }
        if (newIndex == _index) return;

        string key = _bounds[newIndex];
        if (isOp) key = "b0"; // 상대는 가림

        ApplyByKeyImmediate(key, newIndex);
        SetAlphaImmediate(1f);
        _index = newIndex;
    }

    // ───────── 내부 유틸 ─────────

    void ApplyByKeyImmediate(string key, int index)
    {
        if (targetImage == null) return;

        if (string.IsNullOrWhiteSpace(key) || database == null || !database.TryGet(key, out var entry))
        {
            targetImage.overrideSprite = null;
            UpdateTooltipDesc(null, index);
            return;
        }

        ApplyEntryImmediate(entry, index);
    }

    void ApplyEntryImmediate(BoundSpriteDB.Entry entry, int index)
    {
        if (targetImage == null) return;
        targetImage.overrideSprite = entry.sprite;
        targetImage.SetVerticesDirty();
        UpdateTooltipDesc(entry.description, index);
    }

    void ApplySpriteImmediate(Sprite sp)
    {
        if (targetImage == null) return;
        targetImage.overrideSprite = sp;
        targetImage.SetVerticesDirty();
        // 설명은 키가 없으면 갱신 불가
    }

    void UpdateTooltipDesc(string desc, int index)
    {
        // 메인(큰) 이미지의 툴팁
        if (isOp)
        {
            if (tooltipTrigger != null && boundGuesses != null && index >= 0 && index < boundGuesses.Count)
                tooltipTrigger.description = boundGuesses[index];
        }
        else
        {
            if (tooltipTrigger != null)
                tooltipTrigger.description = desc ?? string.Empty;
        }

        // ★ 추가: 프리뷰 그리드 아이콘의 해당 인덱스 툴팁도 동기화
        if (isOp)
        {
            int indexBefore = (index + 5) % 6;

            if (index >= 0 && index < _opIcons.Count && _opIcons[index] != null && _opIcons[index].tooltip != null)
                Debug.Log("상수정이되");
                _opIcons[indexBefore].tooltip.description = boundGuesses[indexBefore];
        }
      /*  else
        {
            if (index >= 0 && index < _myIcons.Count && _myIcons[index] != null && _myIcons[index].tooltip != null)
                _myIcons[index].tooltip.description = desc ?? string.Empty;
        }*/
    }

    void SetAlphaImmediate(float a)
    {
        if (targetImage == null) return;
        var c = targetImage.color;
        targetImage.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
        targetImage.canvasRenderer.SetAlpha(targetImage.color.a);
    }

    void FadeTo(float a, float duration)
    {
        if (targetImage == null) return;
        a = Mathf.Clamp01(a);
        if (duration <= 0f) { SetAlphaImmediate(a); return; }
        targetImage.CrossFadeAlpha(a, duration, useUnscaledTime);
    }

    bool HasTrigger(Animator anim, string name)
    {
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
                return true;
        return false;
    }

    // ───────── 프리뷰 빌드 유틸 ─────────

    void BuildMyGridPreview(IList<string> keys)
    {
        if (myGridRoot == null || iconPrefab == null) return;
        Debug.Log("여긴아님1");
        ConfigureGrid(myGridRoot, keys?.Count ?? 0);

        if (keys == null) return;
        Debug.Log("여긴아님3");

        foreach (var key in keys)
        {
            Sprite sp = null; string desc = null;
            if (database != null && database.TryGet(key, out var e))
            {
                sp = e.sprite; desc = e.description;
            }
            var item = Instantiate(iconPrefab, myGridRoot, false);
            item.Set(sp, desc);
            _myIcons.Add(item);
        }
    }

    void BuildOpGridPreview(int count, IList<string> guesses)
    {
        if (opGridRoot == null || iconPrefab == null) return;

        ConfigureGrid(opGridRoot, count);

        Sprite spB0 = null;
        if (database != null && database.TryGet("b0", out var eB0))
            spB0 = eB0.sprite;

        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(iconPrefab, opGridRoot, false);
            var desc = (guesses != null && i < guesses.Count) ? guesses[i] : string.Empty;
            item.Set(spB0, desc); // 전부 b0 이미지
            _opIcons.Add(item);
        }
    }

    void ConfigureGrid(RectTransform grid, int count)
    {
        if (grid == null) return;
        Debug.Log("여긴아님2");

        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();

        glg.cellSize = new Vector2(100, 100);
        glg.spacing = new Vector2(10, 10);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;

        // 부모 박스 크기(최대 6개, 3열 → 최대 2행 기준)
        int columns = 3;
        int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Min(count, 6) / (float)columns));

        float width = glg.padding.left + glg.padding.right
                     + glg.cellSize.x * columns
                     + glg.spacing.x * (columns - 1);

        float height = glg.padding.top + glg.padding.bottom
                     + glg.cellSize.y * rows
                     + glg.spacing.y * (rows - 1);

        // 고정 사이즈로 딱 맞게
        var rt = grid;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    void ClearGrid(RectTransform root, List<BoundIconItem> cache)
    {
        if (cache != null) cache.Clear();
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; --i)
            Destroy(root.GetChild(i).gameObject);
    }

    public void setBoundListAreaActive()
    {

        if (isOp)
            opGridArea.SetActive(true);
        else
        {
            myGridArea.SetActive(true);
        }
    }
}
