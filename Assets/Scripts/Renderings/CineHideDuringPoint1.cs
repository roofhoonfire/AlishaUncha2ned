using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CineHideDuringPoint1 : MonoBehaviour
{
    public enum HideMode
    {
        RendererOnly,      // SpriteRenderer/MeshRenderer 등 renderer.enabled=false
        CanvasToggle,      // Canvas.enabled=false (World/ScreenSpace 공통)
        CanvasGroupAlpha,  // CanvasGroup.alpha=0 (raycast off) → 복귀 시 원복
        GameObject,        // SetActive(false) (루트/자기 자신 비활성) ※ 스크립트 갱신 멈춤
        LayerSwap          // 임시 레이어로 교체 (오버레이가 안 보는 레이어)
    }

    [Header("What to hide")]
    public HideMode mode = HideMode.RendererOnly;

    [Tooltip("자식까지 포함할지")]
    public bool includeChildren = false;

    [Header("LayerSwap (mode=LayerSwap일 때)")]
    public string hiddenLayerName = "Ignore Raycast";

    // --- backups ---
    private struct RendB { public Renderer r; public bool enabled; }
    private struct CanvasB { public Canvas c; public bool enabled; }
    private struct GroupB { public CanvasGroup g; public float alpha; public bool blocks; public bool interact; }
    private struct LayerB { public Transform t; public int layer; }

    private List<RendB> _rend = new();
    private List<CanvasB> _canvas = new();
    private List<GroupB> _group = new();
    private List<LayerB> _layers = new();
    private GameObject _go;
    private bool _wasActive;

    public void Hide()
    {
        _rend.Clear(); _canvas.Clear(); _group.Clear(); _layers.Clear();

        if (mode == HideMode.GameObject)
        {
            _go = gameObject;
            _wasActive = _go.activeSelf;
            _go.SetActive(false);
            return;
        }

        if (mode == HideMode.RendererOnly)
        {
            var arr = includeChildren ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>();
            foreach (var r in arr) { _rend.Add(new RendB { r = r, enabled = r.enabled }); r.enabled = false; }
            return;
        }

        if (mode == HideMode.CanvasToggle)
        {
            var arr = includeChildren ? GetComponentsInChildren<Canvas>(true) : GetComponents<Canvas>();
            foreach (var c in arr) { _canvas.Add(new CanvasB { c = c, enabled = c.enabled }); c.enabled = false; }
            return;
        }

        if (mode == HideMode.CanvasGroupAlpha)
        {
            var arr = includeChildren ? GetComponentsInChildren<CanvasGroup>(true) : GetComponents<CanvasGroup>();
            foreach (var g in arr)
            {
                _group.Add(new GroupB { g = g, alpha = g.alpha, blocks = g.blocksRaycasts, interact = g.interactable });
                g.alpha = 0f; g.blocksRaycasts = false; g.interactable = false;
            }
            return;
        }

        if (mode == HideMode.LayerSwap)
        {
            int hid = LayerMask.NameToLayer(hiddenLayerName);
            if (hid < 0) { Debug.LogWarning($"[CineHideDuringPoint1] Hidden layer '{hiddenLayerName}' 없음"); return; }
            var ts = includeChildren ? GetComponentsInChildren<Transform>(true) : new[] { transform };
            foreach (var t in ts) { _layers.Add(new LayerB { t = t, layer = t.gameObject.layer }); t.gameObject.layer = hid; }
            return;
        }
    }

    public void Show()
    {
        if (mode == HideMode.GameObject && _go != null) { _go.SetActive(_wasActive); return; }

        foreach (var b in _rend) if (b.r) b.r.enabled = b.enabled;
        foreach (var b in _canvas) if (b.c) b.c.enabled = b.enabled;
        foreach (var b in _group) if (b.g) { b.g.alpha = b.alpha; b.g.blocksRaycasts = b.blocks; b.g.interactable = b.interact; }
        foreach (var b in _layers) if (b.t) b.t.gameObject.layer = b.layer;

        _rend.Clear(); _canvas.Clear(); _group.Clear(); _layers.Clear();
    }
}
