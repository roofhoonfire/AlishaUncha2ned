using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardHoverPreview : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    [Header("Auto-link to Preview Target")]
    [Tooltip("씬의 큰 프리뷰 카드에 부여할 태그 이름")]
    public string previewTag = "CardPreview";
    [Tooltip("찾은 레퍼런스를 모든 카드 인스턴스가 공유(캐싱)")]
    public bool cacheAcrossInstances = true;

    [Header("Timing")]
    public float hoverDelay = 1.5f;
    public float fadeDuration = 0.18f;
    public bool liveSyncDuringHover = false;

    [Header("필드 이름 매핑(자식 오브젝트 이름)")]
    public string timeClockName = "TimeClock";
    public string defenseName = "Defense";
    public string rumbleName = "Rumble";
    public string nameName = "name";
    public string ptName = "pt";

    [Header("Artwork Sync")]
    [Tooltip("여기 붙은 Image의 sprite를 프리뷰 카드의 ActualImage에 복사")]
    [SerializeField] private GameObject sourceImage;
    [Tooltip("프리뷰 카드 하위에서 스프라이트를 표시할 타겟 오브젝트 이름")]
    [SerializeField] private string previewImageName = "ActualImage";

    [Header("Behavior")]
    public bool keepPreviewActive = true;

    // ★ 추가: 호버 차단용 오브젝트 (활성화되면 프리뷰 비활성화)
    [Header("Hover Blocker")]
    [Tooltip("이 오브젝트가 활성(activeInHierarchy)이면 호버 프리뷰를 막습니다.")]
    [SerializeField] private GameObject hoverBlocker;  // ← 인스펙터 드래그

    private static GameObject _cachedPreviewCard;
    private static CanvasGroup _cachedCG;

    private GameObject previewCard;
    private CanvasGroup _previewCG;

    private Dictionary<string, TextMeshProUGUI> _src = new();
    private Dictionary<string, TextMeshProUGUI> _dst = new();

    private Image _srcImg;
    private Image _dstImg;

    private Coroutine _hoverCo;
    private Coroutine _syncCo;
    private Tween _fadeTween;
    private bool _isShowing;

    void Awake()
    {
        CacheTexts(transform, _src);
        EnsurePreview();

        _srcImg = GetImageFromGO(sourceImage);

        if (previewCard)
        {
            if (keepPreviewActive) previewCard.SetActive(true);
            else previewCard.SetActive(false);

            _previewCG.alpha = 0f;
            _previewCG.blocksRaycasts = false;
            _previewCG.interactable = false;

            if (_dst.Count == 0)
                CacheTexts(previewCard.transform, _dst, allowInactive: true);

            _dstImg = FindImageByName(previewCard.transform, previewImageName, includeInactive: true);
        }
    }

    void OnEnable()
    {
        // ★ 차단 상태라면 즉시 숨김 유지
        if (IsHoverBlocked()) HideImmediate();
    }

    void OnDisable()
    {
        KillAll();
        HideImmediate();
    }

    // ===== Pointer Events =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ★ 차단 중이면 아무 것도 하지 않음
        if (IsHoverBlocked()) return;

        if (_hoverCo != null) StopCoroutine(_hoverCo);
        _hoverCo = StartCoroutine(HoverCountdown());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        KillHoverOnly();
        Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        KillHoverOnly();
        HideImmediate();
    }

    private IEnumerator HoverCountdown()
    {
        // ★ 대기 중에도 차단 상태를 계속 감시
        float t = 0f;
        while (t < hoverDelay)
        {
            if (IsHoverBlocked()) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Show();
    }

    // ===== Show/Hide =====
    private void Show()
    {
        // ★ 마지막 방어선: Show 시점에도 차단 확인
        if (IsHoverBlocked()) return;
        if (!EnsurePreview()) return;

        if (_src.Count == 0) CacheTexts(transform, _src);
        if (_dst.Count == 0) CacheTexts(previewCard.transform, _dst, allowInactive: true);

        CopyTextsOnce();
        CopyImageOnce();

        if (_isShowing) return;
        _isShowing = true;

        _fadeTween?.Kill();

        if (!keepPreviewActive) previewCard.SetActive(true);

        _previewCG.blocksRaycasts = false;
        _previewCG.interactable = false;

        _previewCG.alpha = 0f;
        _fadeTween = _previewCG.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);

        if (liveSyncDuringHover)
            _syncCo = StartCoroutine(LiveSyncLoop());
    }

    private void Hide()
    {
        if (!_isShowing || !previewCard) return;

        _isShowing = false;
        if (_syncCo != null) { StopCoroutine(_syncCo); _syncCo = null; }

        _fadeTween?.Kill();
        _previewCG.blocksRaycasts = false;
        _previewCG.interactable = false;

        _fadeTween = _previewCG.DOFade(0f, fadeDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (!_isShowing && !keepPreviewActive)
                    previewCard.SetActive(false);
            });
    }

    private void HideImmediate()
    {
        if (!previewCard) return;

        _isShowing = false;
        _fadeTween?.Kill();
        _previewCG.alpha = 0f;
        _previewCG.blocksRaycasts = false;
        _previewCG.interactable = false;

        if (!keepPreviewActive)
            previewCard.SetActive(false);

        if (_syncCo != null) { StopCoroutine(_syncCo); _syncCo = null; }
    }

    private void KillHoverOnly()
    {
        if (_hoverCo != null) { StopCoroutine(_hoverCo); _hoverCo = null; }
    }

    private void KillAll()
    {
        KillHoverOnly();
        if (_syncCo != null) { StopCoroutine(_syncCo); _syncCo = null; }
        _fadeTween?.Kill();
    }

    // ===== Locate & Cache Preview =====
    private bool EnsurePreview()
    {
        if (previewCard && _previewCG) return true;

        if (cacheAcrossInstances && _cachedPreviewCard && _cachedCG)
        {
            previewCard = _cachedPreviewCard;
            _previewCG = _cachedCG;
        }
        else
        {
            var go = GameObject.FindWithTag(previewTag);
            if (!go)
            {
                var anchor = Object.FindObjectOfType<CardPreviewAnchor>(true);
                if (anchor) go = anchor.gameObject;
            }

            if (!go)
            {
                Debug.LogWarning($"[CardHoverPreview] 태그 '{previewTag}'(또는 CardPreviewAnchor)로 프리뷰 카드를 찾을 수 없음.");
                return false;
            }

            previewCard = go;
            _previewCG = previewCard.GetComponent<CanvasGroup>();
            if (!_previewCG) _previewCG = previewCard.AddComponent<CanvasGroup>();

            previewCard.SetActive(false);
            _previewCG.alpha = 0f;
            _previewCG.blocksRaycasts = false;
            _previewCG.interactable = false;

            if (cacheAcrossInstances)
            {
                _cachedPreviewCard = previewCard;
                _cachedCG = _previewCG;
            }
        }

        if (_dstImg == null && previewCard != null)
            _dstImg = FindImageByName(previewCard.transform, previewImageName, includeInactive: true);

        return true;
    }

    // ===== Copy / Sync =====
    private void CopyTextsOnce()
    {
        CopyIfExists(timeClockName);
        CopyIfExists(defenseName);
        CopyIfExists(rumbleName);
        CopyIfExists(nameName);
        CopyIfExists(ptName);
    }

    private void CopyImageOnce()
    {
        if (_srcImg == null) _srcImg = GetImageFromGO(sourceImage);
        if (_dstImg == null && previewCard != null)
            _dstImg = FindImageByName(previewCard.transform, previewImageName, includeInactive: true);

        if (_srcImg != null && _dstImg != null)
        {
            _dstImg.sprite = _srcImg.sprite;
            _dstImg.enabled = (_dstImg.sprite != null);
            // _dstImg.SetNativeSize(); // 필요 시
        }
    }

    private IEnumerator LiveSyncLoop()
    {
        var wait = new WaitForEndOfFrame();
        while (_isShowing)
        {
            // ★ 라이브 동기화 중에도 차단되면 즉시 숨김
            if (IsHoverBlocked())
            {
                HideImmediate();
                yield break;
            }
            CopyTextsOnce();
            CopyImageOnce();
            yield return wait;
        }
    }

    private void CopyIfExists(string fieldName)
    {
        if (_src.TryGetValue(fieldName, out var s) && s != null &&
            _dst.TryGetValue(fieldName, out var d) && d != null)
        {
            d.text = s.text;
        }
    }

    // ===== Utility =====
    private void CacheTexts(Transform root, Dictionary<string, TextMeshProUGUI> map, bool allowInactive = false)
    {
        map.Clear();
        var wanted = new string[] { timeClockName, defenseName, rumbleName, nameName, ptName };

        foreach (var key in wanted)
        {
            var t = root.Find(key);
            TextMeshProUGUI tmp = null;

            if (t != null) tmp = t.GetComponent<TextMeshProUGUI>();

            if (tmp == null)
            {
                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(allowInactive);
                foreach (var x in tmps)
                    if (x.name == key) { tmp = x; break; }
            }

            if (tmp != null) map[key] = tmp;
        }
    }

    private static Image GetImageFromGO(GameObject go)
    {
        if (!go) return null;
        var img = go.GetComponent<Image>();
        if (img) return img;
        return go.GetComponentInChildren<Image>(true);
    }

    private static Image FindImageByName(Transform root, string name, bool includeInactive)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;

        var t = root.Find(name);
        if (t)
        {
            var img = t.GetComponent<Image>();
            if (img) return img;
        }

        var imgs = root.GetComponentsInChildren<Image>(includeInactive);
        foreach (var x in imgs)
            if (x.name == name) return x;

        return null;
    }

    // ★ 차단 상태 확인 헬퍼
    private bool IsHoverBlocked()
    {
        return hoverBlocker != null && hoverBlocker.activeInHierarchy;
    }
}

// 선택형: 프리뷰 카드에 달아두면 자동 탐색에 사용됨(태그 대신/보조)
public class CardPreviewAnchor : MonoBehaviour
{
    void Reset() { gameObject.tag = "CardPreview"; }
}
