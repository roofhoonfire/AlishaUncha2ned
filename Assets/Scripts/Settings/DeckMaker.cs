using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckMaker : MonoBehaviour
{
    [Header("Prefabs & Areas")]
    public GameObject card_action;           // 카드 프리팹
    public GameObject card_support;
    public RectTransform pageArea;          // PageArea (GridLayoutGroup 부착)
    public GameObject Panel;

    [Header("Navigation UI")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageLabel;              // 선택

    [Header("Grid Settings (2×3 페이지)")]
    public Vector2 cellSize = new Vector2(300, 400);
    public Vector2 spacing = new Vector2(40, 40);
    public int columns = 3;                 // 3 열 고정
    private const int PAGE_CAPACITY = 6;    // 2×3

    private List<Card> _cards = new();
    private int _currentPage = 0;
    private int _totalPages = 0;

    private GridLayoutGroup _grid;

    [Header("개발자 모드")]
    public bool dev = true;


    private void Awake()
    {
        _grid = pageArea.GetComponent<GridLayoutGroup>();
        if (_grid == null)
            _grid = pageArea.gameObject.AddComponent<GridLayoutGroup>();

        // 여기만 값 교체
        _grid.cellSize = cellSize;
        _grid.spacing = spacing;
        _grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;

        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);
    }

    public void CardLoad()
    {
        Panel.SetActive(true);
        var so = CardCSVLoader.Instance?.runtimeCardSO;
        if (so == null || so.cards == null || so.cards.Length == 0)
        {
            Debug.LogWarning("[DeckMaker] 카드가 없습니다.");
            ClearPage();
            UpdateNav(0, 0);
            return;
        }

        _cards.Clear();
        _cards.AddRange(so.cards);

        _totalPages = Mathf.CeilToInt(_cards.Count / (float)PAGE_CAPACITY);
        _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, _totalPages - 1));

        RenderPage(_currentPage);
    }

    private void RenderPage(int pageIndex)
    {
        ClearPage();

        int start = pageIndex * PAGE_CAPACITY;
        int end = Mathf.Min(start + PAGE_CAPACITY, _cards.Count);

        for (int i = start; i < end; i++)
        {
            GameObject go = null;
            if (_cards[i].cardType == 0)
                go = Instantiate(card_action, pageArea);
            else
            {
                if (dev == false)
                    continue;
                go = Instantiate(card_support, pageArea);

            }
            go.transform.localScale = Vector3.one; // 안전 보정

            var info = go.GetComponent<EachCardInfo>();
            if (info != null)
            {
                info.cardData = _cards[i];
                info.ApplyCardData();
            }
        }

        UpdateNav(pageIndex, _totalPages);
        Debug.Log($"[DeckMaker] 페이지 {pageIndex + 1}/{_totalPages} 렌더 (카드 {end - start}장).");
    }

    private void ClearPage()
    {
        for (int i = pageArea.childCount - 1; i >= 0; i--)
            Destroy(pageArea.GetChild(i).gameObject);
    }

    private void UpdateNav(int pageIndex, int total)
    {
        if (prevButton) prevButton.interactable = pageIndex > 0;
        if (nextButton) nextButton.interactable = pageIndex < total - 1;
        if (pageLabel) pageLabel.text = (total == 0) ? "0 / 0" : $"{pageIndex + 1} / {total}";
    }

    public void NextPage()
    {
        Debug.Log("버튼눌림 앙");
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RenderPage(_currentPage);
        }
    }

    public void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RenderPage(_currentPage);
        }
    }
}
