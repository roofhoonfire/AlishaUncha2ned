using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class TileGlow : MonoBehaviour
{
    [Header("Overlay")]
    [Tooltip("글로우에 쓸 재질 (Unlit/Transparent 또는 Particles/Additive 권장)")]
    public Material additiveMat;
    [Tooltip("바닥 스프라이트보다 약간 크게")]
    public float scaleMul = 1.12f;

    [Header("Pulse")]
    public float alphaMin = 0.25f;
    public float alphaMax = 0.8f;
    public float pulseSec = 0.75f;

    private SpriteRenderer _baseSr;
    private SpriteRenderer _glowSr;   // child "~Glow"
    private Tween _pulseTween;

    void Awake()
    {
        _baseSr = GetComponent<SpriteRenderer>();
        if (!_baseSr) _baseSr = GetComponentInChildren<SpriteRenderer>();
        EnsureGlowChild();
        HideImmediate();
    }

    void OnDisable() => HideImmediate();

    void EnsureGlowChild()
    {
        if (_glowSr) return;

        var child = new GameObject("~Glow");
        child.transform.SetParent(_baseSr.transform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one * scaleMul;

        _glowSr = child.AddComponent<SpriteRenderer>();
        _glowSr.sprite = _baseSr.sprite;
        _glowSr.sortingLayerID = _baseSr.sortingLayerID;
        _glowSr.sortingOrder = _baseSr.sortingOrder + 1;
        _glowSr.material = additiveMat != null
            ? additiveMat
            : new Material(Shader.Find("Unlit/Transparent"));
        _glowSr.color = new Color(1, 1, 1, 0);
        _glowSr.enabled = false;
    }

    public void ShowGlow(Color c)
    {
        EnsureGlowChild();
        _pulseTween?.Kill();

        // 색상 지정(알파는 펄스에서 제어)
        c.a = alphaMin;
        _glowSr.color = c;
        _glowSr.enabled = true;

        _pulseTween = DOTween
            .To(() => _glowSr.color.a, a =>
            {
                var cc = _glowSr.color;
                cc.a = a;
                _glowSr.color = cc;
            }, alphaMax, pulseSec * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void HideGlow()
    {
        _pulseTween?.Kill();
        if (_glowSr) _glowSr.enabled = false;
    }

    private void HideImmediate()
    {
        _pulseTween?.Kill();
        if (_glowSr)
        {
            _glowSr.enabled = false;
            var c = _glowSr.color; c.a = 0; _glowSr.color = c;
        }
    }
}
