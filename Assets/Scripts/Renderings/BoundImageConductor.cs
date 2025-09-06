// BoundImageConductor.cs (발췌/변경)
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
    [SerializeField] private TooltipTriggerUI tooltipTrigger; // ★ 인스펙터에 드롭

    // 내부 상태
    private readonly List<string> _bounds = new();
    private int _index = -1;
    private bool _initialized = false;
    private Coroutine _running;

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
        _bounds.Clear();
        if (boundsFromData != null) _bounds.AddRange(boundsFromData);
        _initialized = true;
        _index = 0;

        string key = (_bounds.Count > 0) ? _bounds[0] : null;

        if (isOp) key = "b0"; // 상대는 가림

        ApplyByKeyImmediate(key);  // ★ 키 기반으로 스프라이트+툴팁 동시 세팅
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

        // 애니 생략 분기(지금은 즉시 적용)
        ApplyByKeyImmediate(key);
        SetAlphaImmediate(1f);
        _index = newIndex;
    }

    // ───────── 내부 유틸 ─────────

    // ★ 키로 DB에서 엔트리 통째로 가져와 스프라이트+툴팁 동시 반영
    void ApplyByKeyImmediate(string key)
    {
        if (targetImage == null) return;

        if (string.IsNullOrWhiteSpace(key) || database == null || !database.TryGet(key, out var entry))
        {
            targetImage.overrideSprite = null;
            UpdateTooltipDesc(null);
            return;
        }

        ApplyEntryImmediate(entry);
    }

    // ★ 엔트리 단위 적용: 스프라이트 + 툴팁 설명
    void ApplyEntryImmediate(BoundSpriteDB.Entry entry)
    {
        if (targetImage == null) return;
        targetImage.overrideSprite = entry.sprite;
        targetImage.SetVerticesDirty();
        UpdateTooltipDesc(entry.description);
    }

    // (기존 스프라이트 전용 함수는 남겨두되, 설명 갱신은 없음)
    void ApplySpriteImmediate(Sprite sp)
    {
        if (targetImage == null) return;
        targetImage.overrideSprite = sp;
        targetImage.SetVerticesDirty();
        // 설명은 키가 없으면 갱신 불가 → 필요 시 CallSite에서 ApplyByKeyImmediate 사용
    }

    void UpdateTooltipDesc(string desc)
    {
        if (tooltipTrigger != null)
            tooltipTrigger.description = desc ?? string.Empty;
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
}
