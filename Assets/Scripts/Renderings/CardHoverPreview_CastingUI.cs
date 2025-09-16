using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEditor.Rendering;

public class CardHoverPreview_CastingUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Codes")]
    public string myCardCode;
    public string opCardCode;
    public bool isOp;

    [Header("Preview Target (항상 활성 상태 권장)")]
    [Tooltip("씬의 CardPreview 루트. 이 오브젝트는 활성 상태로 두고, CanvasGroup 알파로만 제어합니다.")]
    [SerializeField] private GameObject previewRoot;

    [Header("Timing")]
    [SerializeField] private float hoverDelay = 0.5f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Fade (DOTween, 알파로만 제어)")]
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


    [Header("툴팁 디스크립션")]
    [SerializeField] private TooltipTriggerUI tooltiptriggerDis; // "TimeClock"

    private Coroutine _showRoutine;
    private CanvasGroup _cg;
    private Tween _fadeTween;



    private void Awake()
    {
        // SetActive 토글 제거: 프리뷰 루트는 항상 활성 상태로 둔다.
        ResolveRefsIfNeeded();
        EnsureCanvasGroup();
        ClearFields();

        if (_cg != null)
        {
            _cg.alpha = 0f;                 // 완전히 숨김 상태로 시작
            _cg.blocksRaycasts = false;     // 입력 방해 금지
            _cg.interactable = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        HidePreview(); // 알파 페이드 아웃만
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

    private void ShowPreviewNow()
    {
        if (previewRoot == null) return;
        ResolveRefsIfNeeded();
        EnsureCanvasGroup();

        _fadeTween?.Kill();

        // 활성/비활성 토글 없이, 텍스트 초기화 → 데이터 채움 → 알파 페이드 인
        ClearFields();

        string code = isOp ? opCardCode : myCardCode;

        if (string.IsNullOrEmpty(code)) {

          //  tooltiptriggerDis.description = "캐스팅 중인 액션이 없습니다";
                return;

        }

        if (!string.IsNullOrEmpty(code) && CardCSVLoader.Instance != null)
        {
            var card = CardCSVLoader.Instance.GetCardByCode(code);
            if (card != null)
            {
                if (timeClockText) timeClockText.text = card.actionClock.ToString();
                if (defenseText) defenseText.text = card.defense.ToString();
                if (rumbleText) rumbleText.text = card.rumblePoint.ToString();
                if (nameText) nameText.text = card.name ?? string.Empty;
                var desc = card.cardText ?? string.Empty;
                // CSV/외부 입력의 이스케이프를 실제 개행으로 정규화
                desc = desc.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("<br>", "\n").Replace("<br/>", "\n");

                if (ptText) ptText.text = desc;
             //   if (tooltiptriggerDis)  tooltiptriggerDis.description =  $"{card.name}의 캐스팅까지 남은시간";
            }
        }

        _cg.blocksRaycasts = false;
        _cg.interactable = false;

        // 알파만 0 → 1
        _cg.alpha = 0f;
        _fadeTween = _cg.DOFade(1f, fadeInDuration)
                     .SetEase(fadeInEase)
                     .SetUpdate(useUnscaledTime);
    }

    private void HidePreview()
    {
        if (previewRoot == null || _cg == null) return;

        _fadeTween?.Kill();

        // 알파만 1 → 0 (완료 후 텍스트 정리)
        _fadeTween = _cg.DOFade(0f, fadeOutDuration)
                     .SetEase(fadeOutEase)
                     .SetUpdate(useUnscaledTime)
                     .OnComplete(() =>
                     {
                         ClearFields();       // 시각적으로 완전히 사라진 뒤 초기화
                         _cg.blocksRaycasts = false;
                         _cg.interactable = false;
                     });
    }

    private void OnDisable()
    {
        if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        _fadeTween?.Kill();
        if (_cg != null) _cg.alpha = 0f;     // 알파만 0으로
        ClearFields();
    }

    private void ResolveRefsIfNeeded()
    {
        if (previewRoot == null) return;
        timeClockText ??= FindTMP("TimeClock");
        defenseText ??= FindTMP("Defense");
        rumbleText ??= FindTMP("Rumble");
        nameText ??= FindTMP("name");
        ptText ??= FindTMP("pt");
    }

    private TMP_Text FindTMP(string childName)
    {
        var t = previewRoot.transform.Find(childName);
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
    }
}
