using UnityEngine;
using DG.Tweening;

public class HandActionManager : MonoBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;
    public GameObject CardArea;

    public int Sonpay;

    private bool isActive = false;

    private RectTransform leftRT;
    private RectTransform rightRT;
    private RectTransform cardRT;

    private Vector2 originalLeftPos;
    private Vector2 originalRightPos;
    private Vector2 originalCardSize;

    void Start()
    {
        leftRT = LeftHand.GetComponent<RectTransform>();
        rightRT = RightHand.GetComponent<RectTransform>();
        cardRT = CardArea.GetComponent<RectTransform>();

        // 초기 위치 및 사이즈 저장
        originalLeftPos = leftRT.anchoredPosition;
        originalRightPos = rightRT.anchoredPosition;
        originalCardSize = cardRT.sizeDelta;

        // 최초 비활성화
        LeftHand.SetActive(false);
        RightHand.SetActive(false);
        CardArea.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isActive)
            {
                // 🔥 소환
                LeftHand.SetActive(true);
                RightHand.SetActive(true);
                CardArea.SetActive(true);
                isActive = true;

                Debug.Log("세 오브젝트가 동시에 깨어났다...");

                if (Sonpay > 2)
                {
                    int extra = Sonpay - 2;
                    float offset = extra * 75f;

                    Vector2 targetSize = new Vector2(originalCardSize.x + offset * 2, originalCardSize.y);
                    cardRT.DOSizeDelta(targetSize, 1.0f).SetEase(Ease.OutQuad);

                    leftRT.DOAnchorPosX(originalLeftPos.x - offset, 1.0f).SetEase(Ease.OutQuad);
                    rightRT.DOAnchorPosX(originalRightPos.x + offset, 1.0f).SetEase(Ease.OutQuad);

                    Debug.Log($"CardArea 확장: {offset * 2}px | 손 이동: {offset}px 좌우");
                }
            }
            else
            {
                // 🔥 봉인
                cardRT.DOSizeDelta(originalCardSize, 1.0f).SetEase(Ease.InOutQuad);
                leftRT.DOAnchorPos(originalLeftPos, 1.0f).SetEase(Ease.InOutQuad);
                rightRT.DOAnchorPos(originalRightPos, 1.0f).SetEase(Ease.InOutQuad);

                // 1초 후 비활성화
                DOVirtual.DelayedCall(1.0f, () =>
                {
                    LeftHand.SetActive(false);
                    RightHand.SetActive(false);
                    CardArea.SetActive(false);
                    Debug.Log("세 오브젝트가 봉인되었다...");
                });

                isActive = false;
            }
        }
    }
}
