using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScreenFlashFX : MonoBehaviour
{
    public static ScreenFlashFX Instance;

    [Header("Optional override")]
    public Image flashImage;           // 미지정 시 런타임 생성
    public int sortingOrder = 9999;    // 항상 최상단으로

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (flashImage == null) flashImage = CreateRuntimeOverlay();
        SetAlpha(0f);
    }

    Image CreateRuntimeOverlay()
    {
        var goCanvas = new GameObject("[FX]ScreenFlashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(goCanvas);
        var canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var goImg = new GameObject("Flash", typeof(Image));
        goImg.transform.SetParent(goCanvas.transform, false);
        var img = goImg.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }

    void SetAlpha(float a)
    {
        if (flashImage == null) return;
        var c = flashImage.color; c.a = a; flashImage.color = c;
    }

    /// <summary>
    /// 화면 플래시 (타임스케일 무시).
    /// </summary>
    /// <param name="color">플래시 색</param>
    /// <param name="fadeIn">급상승 시간</param>
    /// <param name="hold">머무는 시간</param>
    /// <param name="fadeOut">감쇠 시간</param>
    /// <param name="maxAlpha">최대 알파</param>
    public void Flash(Color color, float fadeIn = 0.02f, float hold = 0.04f, float fadeOut = 0.10f, float maxAlpha = 0.6f)
    {
        if (flashImage == null) return;
        flashImage.color = new Color(color.r, color.g, color.b, 0f);

        DOTween.Kill(flashImage); // 같은 타겟의 이전 트윈 정리
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(flashImage.DOFade(maxAlpha, Mathf.Max(0f, fadeIn)));
        if (hold > 0f) seq.AppendInterval(hold);
        seq.Append(flashImage.DOFade(0f, Mathf.Max(0f, fadeOut)));
    }
}
