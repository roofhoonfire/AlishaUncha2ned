using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(EachLogIconInfo))]
public class LogIconHoverPreview_ActionData : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Preview Target (항상 활성 권장)")]
    [Tooltip("씬의 CardPreview 루트. 활성은 유지하고 CanvasGroup 알파만 제어")]
    [SerializeField] private GameObject previewRoot;

    [Header("Timing")]
    [SerializeField] private float hoverDelay = 0.5f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Fade (DOTween, 알파만)")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.12f;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    [Header("Text Refs (비우면 자동 탐색)")]
    [SerializeField] private TMP_Text timeClockText; // "TimeClock"
    [SerializeField] private TMP_Text defenseText;   // "Defense"
    [SerializeField] private TMP_Text rumbleText;    // "Rumble"
    [SerializeField] private TMP_Text nameText;      // "name"
    [SerializeField] private TMP_Text ptText;        // "pt"

    [Header("Artwork (Drag & Drop)")]
    [Tooltip("프리뷰 카드 일러스트 Image (드래그&드롭)")]
    [SerializeField] private Image previewImage;

    [Header("Bless Slots (under PreviewRoot)")]
    [Tooltip("각 Bless 루트: Image 컴포넌트 보유, 자식에 name/pt(TMP) 존재. 미사용은 비활성 유지")]
    [SerializeField] private List<GameObject> blessRoots = new List<GameObject>();

    private EachLogIconInfo _iconInfo;
    private CanvasGroup _cg;
    private Coroutine _showRoutine;
    private Tween _fadeTween;

    // 내부 캐시: Bless 슬롯 구성요소
    private bool _blessResolved = false;
    private struct BlessRefs
    {
        public GameObject root;
        public Image img;
        public TMP_Text name;
        public TMP_Text pt;
    }
    private readonly List<BlessRefs> _blessSlots = new List<BlessRefs>(8);

    private void Awake()
    {
        _iconInfo = GetComponent<EachLogIconInfo>();
        ResolveRefsIfNeeded();
        EnsureCanvasGroup();
        ResolveBlessRefs(); // 미리 캐시 해두면 매호출 비용↓
        ClearFields();

        if (_cg != null)
        {
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }
    }

    // ===== Pointer =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        HidePreview();
    }

    private IEnumerator ShowAfterDelay()
    {
        if (hoverDelay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(hoverDelay);
            else yield return new WaitForSeconds(hoverDelay);
        }
        ShowPreviewNow();
    }

    // ===== Core =====
    private void ShowPreviewNow()
    {
        if (previewRoot == null) return;
        ResolveRefsIfNeeded();
        EnsureCanvasGroup();
        if (!_blessResolved) ResolveBlessRefs();

        _fadeTween?.Kill();
        ClearFields();

        var ad = _iconInfo != null ? _iconInfo.actionData : null;
        if (ad == null) return;

        // --- 1) 수치: ActionData에서 직접 ---
        if (timeClockText) timeClockText.text = ad.actionClock.ToString();
        if (defenseText) defenseText.text = ad.defense.ToString();
        if (rumbleText) rumbleText.text = ad.rumblePoint.ToString();

        // --- 2) 텍스트/스프라이트: SO에서 불변 데이터 채우기 ---
        string code = ad.cardcode;
        if (!string.IsNullOrEmpty(code) && CardCSVLoader.Instance != null)
        {
            var card = CardCSVLoader.Instance.GetCardByCode(code);
            if (card != null)
            {
                if (nameText)
                {
                    string nm = !string.IsNullOrEmpty(ad.cardname) ? ad.cardname : (card.name ?? string.Empty);
                    nameText.text = nm;
                }

                if (ptText)
                {
                    var desc = card.cardText ?? string.Empty;
                    desc = NormalizeDesc(desc);
                    ptText.text = desc;
                }

                if (previewImage != null)
                {
                    previewImage.sprite = card.sprite;
                    previewImage.enabled = (previewImage.sprite != null);
                }
            }
            else
            {
                if (nameText) nameText.text = ad.cardname ?? string.Empty;
            }
        }
        else
        {
            if (nameText) nameText.text = ad.cardname ?? string.Empty;
        }

        // --- 3) Bless 채우기: ActionData.blessList → SO 참조 후 슬롯 채움 ---
        FillBlessSlots(ad);

        // CanvasGroup 페이드 인
        _cg.blocksRaycasts = false;
        _cg.interactable = false;
        _cg.alpha = 0f;

        _fadeTween = _cg.DOFade(1f, fadeInDuration)
                     .SetEase(fadeInEase)
                     .SetUpdate(useUnscaledTime);
    }

    private void HidePreview()
    {
        if (previewRoot == null || _cg == null) return;
        _fadeTween?.Kill();

        _fadeTween = _cg.DOFade(0f, fadeOutDuration)
                     .SetEase(fadeOutEase)
                     .SetUpdate(useUnscaledTime)
                     .OnComplete(() =>
                     {
                         ClearFields();
                         _cg.blocksRaycasts = false;
                         _cg.interactable = false;
                     });
    }

    private void OnDisable()
    {
        if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        _fadeTween?.Kill();
        if (_cg != null) _cg.alpha = 0f;
        ClearFields();
    }

    // ===== Bless 처리 =====
    private void ResolveBlessRefs()
    {
        _blessSlots.Clear();

        if (blessRoots != null)
        {
            foreach (var root in blessRoots)
            {
                if (root == null)
                {
                    _blessSlots.Add(default);
                    continue;
                }

                var img = root.GetComponent<Image>();
                TMP_Text nm = null;
                TMP_Text pt = null;

                var nameTf = root.transform.Find("name");
                if (nameTf) nm = nameTf.GetComponent<TMP_Text>();
                var ptTf = root.transform.Find("pt");
                if (ptTf) pt = ptTf.GetComponent<TMP_Text>();

                // 시작 상태는 비활성화 권장
                root.SetActive(false);

                _blessSlots.Add(new BlessRefs
                {
                    root = root,
                    img = img,
                    name = nm,
                    pt = pt
                });
            }
        }
        _blessResolved = true;
    }

    private void FillBlessSlots(ActionData ad)
    {
        // 모두 비활성화로 초기화
        for (int i = 0; i < _blessSlots.Count; i++)
            if (_blessSlots[i].root) _blessSlots[i].root.SetActive(false);

        if (ad?.blessList == null || ad.blessList.Count == 0) return;
        if (CardCSVLoader.Instance == null) return;

        int count = Mathf.Min(ad.blessList.Count, _blessSlots.Count);
        for (int i = 0; i < count; i++)
        {
            var slot = _blessSlots[i];
            if (slot.root == null) continue;

            string blessCode = ad.blessList[i];
            if (string.IsNullOrEmpty(blessCode))
            {
                slot.root.SetActive(false);
                continue;
            }

            var blessCard = CardCSVLoader.Instance.GetCardByCode(blessCode);
            if (blessCard == null)
            {
                // 코드가 잘못됐으면 슬롯 숨김
                slot.root.SetActive(false);
                continue;
            }

            // 이미지
            if (slot.img != null)
            {
                slot.img.sprite = blessCard.sprite;
                slot.img.enabled = (slot.img.sprite != null);
            }

            // 이름
            if (slot.name != null)
            {
                slot.name.text = blessCard.name ?? string.Empty;
            }

            // 설명
            if (slot.pt != null)
            {
                var desc = blessCard.cardText ?? string.Empty;
                slot.pt.text = NormalizeDesc(desc);
            }

            slot.root.SetActive(true);
        }
        // 남는 슬롯은 비활성화 (위에서 이미 모두 false 처리)
    }

    // ===== Helpers =====
    public void SetPreviewRoot(GameObject root) => previewRoot = root;
    public void SetPreviewImage(Image img) => previewImage = img;
    public void SetBlessRoots(List<GameObject> roots)
    {
        blessRoots = roots ?? new List<GameObject>();
        _blessResolved = false; // 다음 Show에서 다시 캐시
    }

    private void ResolveRefsIfNeeded()
    {
        if (previewRoot == null) return;
        timeClockText ??= FindTMP("TimeClock");
        defenseText ??= FindTMP("Defense");
        rumbleText ??= FindTMP("Rumble");
        nameText ??= FindTMP("name");
        ptText ??= FindTMP("pt");
        // previewImage는 드래그&드롭 전용
    }

    private TMP_Text FindTMP(string childName)
    {
        var t = previewRoot != null ? previewRoot.transform.Find(childName) : null;
        return t ? t.GetComponent<TMP_Text>() : null;
    }

    private void EnsureCanvasGroup()
    {
        if (previewRoot == null) return;
        _cg ??= previewRoot.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = previewRoot.AddComponent<CanvasGroup>();
    }

    private void ClearFields()
    {
        if (timeClockText) timeClockText.text = string.Empty;
        if (defenseText) defenseText.text = string.Empty;
        if (rumbleText) rumbleText.text = string.Empty;
        if (nameText) nameText.text = string.Empty;
        if (ptText) ptText.text = string.Empty;

        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
        }

        // Bless 클리어 & 비활성화
        for (int i = 0; i < _blessSlots.Count; i++)
        {
            var slot = _blessSlots[i];
            if (slot.name) slot.name.text = string.Empty;
            if (slot.pt) slot.pt.text = string.Empty;
            if (slot.img)
            {
                slot.img.sprite = null;
                slot.img.enabled = false;
            }
            if (slot.root) slot.root.SetActive(false);
        }
    }

    private static string NormalizeDesc(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br>", "\n");
    }
}
