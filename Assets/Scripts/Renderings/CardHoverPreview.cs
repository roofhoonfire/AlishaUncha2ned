using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using UnityEngine.UI; // ★ Image 사용
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
    [SerializeField] private GameObject sourceImage; // ★ 소스 이미지 오브젝트( Image 컴포넌트 필요 )
    [Tooltip("프리뷰 카드 하위에서 스프라이트를 표시할 타겟 오브젝트 이름")]
    [SerializeField] private string previewImageName = "ActualImage";

    // 내부// 추가
    [Header("Behavior")]
    public bool keepPreviewActive = true;

    private static GameObject _cachedPreviewCard;
    private static CanvasGroup _cachedCG;

    private GameObject previewCard;   // ← 인스펙터 할당 불필요(런타임 자동 연결)
    private CanvasGroup _previewCG;

    private Dictionary<string, TextMeshProUGUI> _src = new();
    private Dictionary<string, TextMeshProUGUI> _dst = new();

    // ★ 이미지 캐시
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

        // 소스 이미지 캐시
        _srcImg = GetImageFromGO(sourceImage);

        if (previewCard)
        {
            if (keepPreviewActive)
                previewCard.SetActive(true);     // 부모는 항상 활성
            else
                previewCard.SetActive(false);    // (옵션 B 사용시)

            _previewCG.alpha = 0f;
            _previewCG.blocksRaycasts = false;   // 미리보기는 클릭 가로채지 않게
            _previewCG.interactable = false;

            if (_dst.Count == 0)
                CacheTexts(previewCard.transform, _dst, allowInactive: true);

            // 프리뷰 쪽 타깃 이미지 캐시
            _dstImg = FindImageByName(previewCard.transform, previewImageName, includeInactive: true);
        }
    }

    void OnDisable()
    {
        KillAll();
        HideImmediate();
    }

    // ===== Pointer Events =====
    public void OnPointerEnter(PointerEventData eventData)
    {
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
        yield return new WaitForSeconds(hoverDelay);
        Show();
    }

    // ===== Show/Hide =====
    private void Show()
    {
        if (!EnsurePreview()) return;

        if (_src.Count == 0) CacheTexts(transform, _src);
        if (_dst.Count == 0) CacheTexts(previewCard.transform, _dst, allowInactive: true);

        // 텍스트 & 이미지 1회 복사
        CopyTextsOnce();
        CopyImageOnce();

        if (_isShowing) return;
        _isShowing = true;

        _fadeTween?.Kill();

        if (!keepPreviewActive) previewCard.SetActive(true); // 항상활성 모드라면 토글 불필요

        // 레이캐스트는 계속 OFF (프리뷰가 입력 방해 X)
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
                    previewCard.SetActive(false); // 항상활성 모드면 비활성화 금지
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
            previewCard.SetActive(false); // 항상활성 모드면 비활성화 금지

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
                // 보조 루트: CardPreviewAnchor 마커를 붙였을 때 자동 탐색
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

            // 처음 한 번은 확실히 숨겨둔다
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

        // 프리뷰 찾았으면 타깃 이미지도 캐시 시도
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
        // 소스/타깃 캐시가 없다면 다시 시도
        if (_srcImg == null) _srcImg = GetImageFromGO(sourceImage);
        if (_dstImg == null && previewCard != null)
            _dstImg = FindImageByName(previewCard.transform, previewImageName, includeInactive: true);

        if (_srcImg != null && _dstImg != null)
        {
            _dstImg.sprite = _srcImg.sprite;
            _dstImg.enabled = (_dstImg.sprite != null);
            // 필요시 이미지 크기 보정이 있으면 여기서 SetNativeSize() 등 사용 가능
            // _dstImg.SetNativeSize();
        }
    }

    private IEnumerator LiveSyncLoop()
    {
        var wait = new WaitForEndOfFrame();
        while (_isShowing)
        {
            CopyTextsOnce();
            CopyImageOnce(); // ★ 라이브 동기화 옵션일 때 이미지도 계속 동기화
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
        // 자기 자신 우선
        var img = go.GetComponent<Image>();
        if (img) return img;
        // 자식 중 첫 Image
        return go.GetComponentInChildren<Image>(true);
    }

    private static Image FindImageByName(Transform root, string name, bool includeInactive)
    {
        if (!root || string.IsNullOrEmpty(name)) return null;

        // 1차: 직계 이름으로
        var t = root.Find(name);
        if (t)
        {
            var img = t.GetComponent<Image>();
            if (img) return img;
        }

        // 2차: 전체 하위 탐색
        var imgs = root.GetComponentsInChildren<Image>(includeInactive);
        foreach (var x in imgs)
            if (x.name == name) return x;

        return null;
    }
}

// 선택형: 프리뷰 카드에 달아두면 자동 탐색에 사용됨(태그 대신/보조)
public class CardPreviewAnchor : MonoBehaviour
{
    void Reset() { gameObject.tag = "CardPreview"; }
}
