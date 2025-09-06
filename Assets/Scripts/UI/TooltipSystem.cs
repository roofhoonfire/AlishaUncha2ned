using UnityEngine;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance { get; private set; }

    [SerializeField] private Canvas rootCanvas;     // UI ·çÆ® Äµ¹ö½º
    [SerializeField] private TooltipView prefab;    // À§ ÇÁ¸®ÆÕ
    [SerializeField] private bool followMouse = true;

    private TooltipView _inst;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Show(string text, Vector2 screenPos)
    {
        if (_inst == null)
        {
            _inst = Instantiate(prefab, rootCanvas.transform);
            _inst.gameObject.SetActive(false);
        }
        _inst.gameObject.SetActive(true);
        _inst.SetText(text);
        _inst.OpenAt(rootCanvas, screenPos);
    }

    public void Move(Vector2 screenPos)
    {
        if (_inst != null && _inst.gameObject.activeSelf)
            _inst.MoveTo(rootCanvas, screenPos);
    }

    public void Hide()
    {
        if (_inst != null && _inst.gameObject.activeSelf)
            _inst.Close();
    }
}
