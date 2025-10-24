using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Photon.Pun;

// 빈 매니저 오브젝트에 붙여서 사용
public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

    [Header("Log Capacity")]
    [Tooltip("최대 저장 개수 (기본 8칸, 초과 시 '가장 오래된' 항목 제거)")]
    [SerializeField] private int capacity = 8;

    [Header("UI References")]
    [Tooltip("50x50 셀의 GridLayoutGroup이 붙은 컨테이너")]
    [SerializeField] private Transform logArea;         // LogArea(그리드) 트랜스폼
    [Tooltip("아이콘 프리팹 (자식에 Visual 이름의 Transform 권장)")]
    [SerializeField] private GameObject logIconPrefab;  // LogIcon 프리팹

    [Header("Spawn Animation (DOTween)")]
    [SerializeField] private float popScale = 1.08f;    // 생성 시 '뚜왕' 크기
    [SerializeField] private float popUpTime = 0.10f;   // 커지는 시간
    [SerializeField] private float settleTime = 0.10f;  // 1.08 -> 1.0 되돌아오는 시간
    [SerializeField] private Ease popUpEase = Ease.OutBack;
    [SerializeField] private Ease settleEase = Ease.InOutQuad;

    [Header("Sprites (Drag & Drop 이미지 파일)")]
    [Tooltip("기본 액션/발동(Activate)")]
    public Sprite action;
    [Tooltip("카운터/대처(Counter)")]
    public Sprite counter;
    [Tooltip("도트/지속 피해(Dot)")]
    public Sprite dot;
    [Tooltip("가드/방어(Guard)")]
    public Sprite guard;

    // === Hovering Preview ===
    [Header("Hovering Preview")]
    [Tooltip("씬에 항상 활성 상태로 존재하는 카드 프리뷰 루트(알파로만 제어)")]
    [SerializeField] private GameObject previewRoot;
    [Tooltip("프리뷰 카드 일러스트 이미지 슬롯")]
    [SerializeField] private Image previewImage;

    [Header("Hovering Preview - Bless Slots (under PreviewRoot)")]
    [Tooltip("Bless 루트 오브젝트들 (각각 Image 컴포넌트 보유, 자식에 name/pt TMP_Text 존재)")]
    [SerializeField] private List<GameObject> blessRoots = new List<GameObject>();

    // 내부 상태
    private readonly List<ActionData> _actions = new List<ActionData>();
    private readonly List<GameObject> _icons = new List<GameObject>(); // 인스턴스된 프리팹 추적

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LogManager] Duplicate instance. Destroying this.");
            Destroy(this);
            return;
        }
        Instance = this;

        if (capacity < 1) capacity = 1;

        // 안전 장치: 그리드가 없다면 경고
        if (logArea == null)
            Debug.LogWarning("[LogManager] LogArea is not assigned.");
        else
        {
            var grid = logArea.GetComponent<GridLayoutGroup>();
            if (grid == null)
                Debug.LogWarning("[LogManager] LogArea has no GridLayoutGroup.");
        }

        if (logIconPrefab == null)
            Debug.LogWarning("[LogManager] LogIcon Prefab is not assigned.");

        // 프리뷰 레퍼런스 점검
        if (previewRoot == null)
            Debug.LogWarning("[LogManager] PreviewRoot is not assigned (Hovering Preview).");
        if (previewImage == null)
            Debug.LogWarning("[LogManager] PreviewImage is not assigned (Hovering Preview).");

        // Bless 슬롯 점검 (없어도 동작은 하지만 경고)
        if (blessRoots == null || blessRoots.Count == 0)
            Debug.Log("[LogManager] Bless roots are empty (optional).");
    }

    /// <summary>
    /// 외부에서 호출 — ActionData를 큐잉하고 코루틴 시작
    /// </summary>
    public void Push(ActionData data, HookType h)
    {
        if (data == null)
        {
            Debug.LogWarning("[LogManager] Push called with null ActionData.");
            return;
        }

        Debug.Log($"[LogManager] Push — HookType: {h}, name: {data.cardname}, code: {data.cardcode}");
        StartCoroutine(AddLogRoutine(data, h));
    }

    /// <summary>
    /// 리스트/아이콘 추가 + 용량 관리 + 생성 애니 + 스프라이트 적용 + 프리뷰 주입
    /// </summary>
    private IEnumerator AddLogRoutine(ActionData data, HookType h)
    {
        // 1) 용량 관리 (FIFO)
        if (_actions.Count >= capacity)
        {
            _actions.RemoveAt(0);
            if (_icons.Count > 0)
            {
                var oldestIcon = _icons[0];
                _icons.RemoveAt(0);
                if (oldestIcon != null) Destroy(oldestIcon);
            }
        }

        // 2) 새 데이터 추가
        _actions.Add(data);

        // 3) UI 아이콘 인스턴스
        if (logIconPrefab != null && logArea != null)
        {
            GameObject go = Instantiate(logIconPrefab, logArea);

            // (0) 액션데이터 주입
            var info = go.GetComponent<EachLogIconInfo>();
            if (info != null) info.actionData = data;

            // (A) 생성 애니
            Transform visual = FindVisual(go.transform);
            if (visual != null)
            {
                visual.DOKill(true);
                visual.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();
                seq.Append(visual.DOScale(Vector3.one * popScale, popUpTime).SetEase(popUpEase))
                   .Append(visual.DOScale(Vector3.one, settleTime).SetEase(settleEase));
            }

            // (B) 훅 스프라이트
            TryApplyHookSprite(go, h);

            // (C) Hover 프리뷰 주입 (previewRoot / previewImage / blessRoots)
            TryBindHoverPreview(go);

            _icons.Add(go);
        }

        // (선택) 렌더링 완료 콜백
        Overmind.Instance.Submit_RenderingDone(PhotonNetwork.LocalPlayer.ActorNumber);

        yield return null;
    }

    /// <summary>
    /// 생성된 아이콘의 LogIconHoverPreview_ActionData에 Preview 참조 주입
    /// </summary>
    private void TryBindHoverPreview(GameObject iconGO)
    {
        if (iconGO == null) return;
        var hover = iconGO.GetComponent<LogIconHoverPreview_ActionData>();
        if (hover == null) return;

        // 권장: 세터 메서드로 주입
        if (previewRoot != null) hover.SetPreviewRoot(previewRoot);
        if (previewImage != null) hover.SetPreviewImage(previewImage);
        if (blessRoots != null && blessRoots.Count > 0) hover.SetBlessRoots(blessRoots);

        // 추가 방어: 세터 미구현 구버전 폴백(리플렉션)
        // (네 스크립트가 private [SerializeField]이면 무시됨)
        var t = hover.GetType();
        var fRoot = t.GetField("previewRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var fImg = t.GetField("previewImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var fBless = t.GetField("blessRoots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (fRoot != null && previewRoot != null) fRoot.SetValue(hover, previewRoot);
        if (fImg != null && previewImage != null) fImg.SetValue(hover, previewImage);
        if (fBless != null && blessRoots != null) fBless.SetValue(hover, blessRoots);
    }

    /// <summary>
    /// HookType → Sprite 매핑
    /// </summary>
    private Sprite GetSpriteForHook(HookType h)
    {
        switch (h)
        {
            case HookType.Activate: return action != null ? action : null;
            case HookType.Guard: return guard != null ? guard : null;
            case HookType.Dot: return dot != null ? dot : null;
            case HookType.Counter: return counter != null ? counter : null;
            default: return action;
        }
    }

    /// <summary>
    /// 아이콘 프리팹의 Hook Image에 스프라이트 적용
    /// </summary>
    private void TryApplyHookSprite(GameObject iconGO, HookType h)
    {
        var info = iconGO.GetComponent<EachLogIconInfo>();
        if (info == null)
        {
            Debug.LogWarning("[LogManager] EachLogIconInfo not found on instantiated icon.");
            return;
        }

        var img = info.ResolveHookImage();
        if (img == null)
        {
            Debug.LogWarning("[LogManager] Hook Image not found. Check Hook assignment or child path.");
            return;
        }

        var sprite = GetSpriteForHook(h);
        if (sprite == null)
        {
            Debug.LogWarning($"[LogManager] Sprite for HookType {h} is null. Assign in inspector.");
            return;
        }

        img.sprite = sprite;
        img.preserveAspect = true;
        img.enabled = true;
    }

    /// <summary>
    /// 자식 중 이름이 "Visual"인 Transform을 찾아 반환(없으면 루트)
    /// </summary>
    private Transform FindVisual(Transform root)
    {
        var direct = root.Find("Visual");
        if (direct != null) return direct;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Visual") return t;
        }
        return root;
    }

    // 디버그/유틸
    public void ClearAll()
    {
        _actions.Clear();
        foreach (var go in _icons)
            if (go != null) Destroy(go);
        _icons.Clear();
    }

    public IReadOnlyList<ActionData> GetLogs() => _actions.AsReadOnly();
}
