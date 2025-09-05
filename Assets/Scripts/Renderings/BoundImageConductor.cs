// BoundImageConductor.cs (변경/추가 부분)
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
    [SerializeField] private Animator animTarget;           // ★ 래퍼 오브젝트의 Animator(권장)
    [SerializeField] private string triggerName = "Trig_BoundSwap";
    [SerializeField] private string watchStateTag = "BoundSwap";
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private float maxWaitSeconds = 3f;

    [Header("Self Fade (애니 재생 구간에만 적용)")]
    [SerializeField] private bool fadeSelf = true;          // 페이드 on/off
    [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0f; // 애니 중 알파
    [SerializeField] private float fadeOutDuration = 0.12f; // 애니 시작 시 1→0
    [SerializeField] private float fadeInDuration = 0.15f; // 애니 종료 후 0→1
    [SerializeField] private bool useUnscaledTime = true;

    // 내부 상태
    private readonly List<string> _bounds = new();
    private int _index = -1;
    private bool _initialized = false;
    private Coroutine _running;

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        // 초기 알파 보정(첫 CrossFade 안전)
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
        ApplySpriteImmediate(key);  // 초기화는 즉시
        SetAlphaImmediate(1f);      // 보이는 상태 보장
        return key;
    }

    public void UpdateByIndex(int newIndex)
    {
        if (!_initialized) { Debug.LogWarning("[BoundImageConductor] Not initialized."); return; }
        if (newIndex < 0 || newIndex >= _bounds.Count) { Debug.LogWarning("[BoundImageConductor] Index out of range."); return; }
        if (newIndex == _index) return; // 동일 인덱스 → 무시

        string key = _bounds[newIndex];
        if (!TryGetSprite(key, out var newSprite))
        {
            Debug.LogWarning($"[BoundImageConductor] No sprite mapped for '{key}'.");
            return;
        }

        bool canWatch = animTarget != null && HasTrigger(animTarget, triggerName) && !string.IsNullOrEmpty(watchStateTag);

        if (_running != null) StopCoroutine(_running);

        if (canWatch)
            _running = StartCoroutine(CoSwapAfterAnim(animTarget, newSprite, newIndex));
        else
        {
            // 애니 감시 불가 → 바로 교체 (원하면 여기서도 페이드 사용해도 됨)
            ApplySpriteImmediate(newSprite);
            SetAlphaImmediate(1f);
            _index = newIndex;
        }
    }

    IEnumerator CoSwapAfterAnim(Animator anim, Sprite newSprite, int newIndex)
    {
        anim.ResetTrigger(triggerName);
        anim.SetTrigger(triggerName);

        int tagHash = Animator.StringToHash(watchStateTag);
        float deadline = Time.time + maxWaitSeconds;

        // 1) 태그 상태로 '진입' 대기
        bool entered = false;
        while (Time.time < deadline)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            if (st.tagHash == tagHash) { entered = true; break; }
            yield return null;
        }

        // 진입했으면 α를 0으로 페이드아웃
        if (entered && fadeSelf) FadeTo(fadedAlpha, fadeOutDuration);

        if (!entered)
        {
            // 태그 진입 실패 → 안전 폴백: 즉시 교체 + 보이기
            ApplySpriteImmediate(newSprite);
            SetAlphaImmediate(1f);
            _index = newIndex;
            _running = null;
            yield break;
        }

        // 2) 태그 상태가 '끝날 때까지' 대기
        while (Time.time < deadline)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);

            bool finishedInSame = (st.tagHash == tagHash && st.normalizedTime >= 0.999f && !anim.IsInTransition(animatorLayer));
            bool exited = (st.tagHash != tagHash && !anim.IsInTransition(animatorLayer));
            if (finishedInSame || exited) break;

            yield return null;
        }

        // 3) 스프라이트 교체 → α 다시 1로
        ApplySpriteImmediate(newSprite);
        if (fadeSelf) FadeTo(1f, fadeInDuration); else SetAlphaImmediate(1f);

        _index = newIndex;
        _running = null;
    }

    // --- 유틸들 ---

    bool TryGetSprite(string key, out Sprite s)
    {
        s = null;
        if (database == null) return false;
        return database.TryGetSprite(key, out s);
    }

    void ApplySpriteImmediate(string key)
    {
        if (targetImage == null) return;
        if (string.IsNullOrWhiteSpace(key)) { targetImage.overrideSprite = null; targetImage.sprite = null; return; }
        if (TryGetSprite(key, out var sp)) ApplySpriteImmediate(sp);
        else { targetImage.overrideSprite = null; }
    }

    void ApplySpriteImmediate(Sprite sp)
    {
        if (targetImage == null) return;
        // overrideSprite로 우선권 확보
        targetImage.overrideSprite = sp;
        targetImage.SetVerticesDirty();
    }

    void SetAlphaImmediate(float a)
    {
        if (targetImage == null) return;
        var c = targetImage.color;
        targetImage.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
        targetImage.canvasRenderer.SetAlpha(targetImage.color.a); // 초기 CrossFade 안정화
    }

    void FadeTo(float a, float duration)
    {
        if (targetImage == null) return;
        a = Mathf.Clamp01(a);
        if (duration <= 0f) { SetAlphaImmediate(a); return; }
        // Graphic.CrossFadeAlpha는 overrideSprite와 충돌 없음
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
