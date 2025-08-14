using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Photon.Realtime;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;

public class CardDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite arrowSprite;
    public Sprite defaultSprite;



    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image uiImage;
    private Canvas canvas;
    private RectTransform dropZone;

    private LayoutElement layoutElement;
    private Vector2 dragOffset;
    private Vector2 originalAnchoredPos;
    private Vector2 originalSizeDelta;
    private Vector3 originalScale;

    // Character and shape data
    private GameObject myChara;
    public Vector3Int playerCoord;
    public List<Vector3Int> debugYong;
    public float angle;
    

    // Card data
    private Card thisCardData;

    // Tile highlighting
    private List<int> prevHighlighted = new List<int>();

    private int actorNum;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        uiImage = GetComponent<Image>();
        defaultSprite = uiImage.sprite;
        originalSizeDelta = rectTransform.sizeDelta;
        originalScale = transform.localScale;

        layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        var zone = GameObject.FindWithTag("dropzone");
        if (zone != null)
            dropZone = zone.GetComponent<RectTransform>();

        actorNum =  PhotonNetwork.LocalPlayer.ActorNumber;
        myChara = LocalState.Instance?.PlayerObDic[actorNum];
        playerCoord = GridManagement.Instance.GetCoordFromIndex(LocalRenderingStatic.localRenderingDatas[actorNum].curpos);

    
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvas = GetComponentInParent<Canvas>();//원래 어웨이크에서 해주려고 할라했는데 그게 호출 순서때문에 그럴 수 없다
        thisCardData = GetComponent<EachCardInfo>().cardData;
        canvasGroup.blocksRaycasts = false;
        originalAnchoredPos = rectTransform.anchoredPosition;
        layoutElement.ignoreLayout = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        dragOffset = localPoint - originalAnchoredPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint - dragOffset;

        bool inside = IsInsideDropZone(eventData.position);
        SetDragVisual(inside);
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;

        if (!IsInsideDropZone(eventData.position))
        {
            rectTransform.DOAnchorPos(originalAnchoredPos, 0.25f).SetEase(Ease.OutQuad);
            transform.DOScale(originalScale, 0.25f).SetEase(Ease.OutQuad);
            uiImage.sprite = defaultSprite;
          
            //피뿌리기 추가 라인
            if (CardModeState.Instance.apDataRef.isBlinded)
            {
                Transform blood = transform.Find("BloodShed");
                if (blood != null)
                    blood.gameObject.SetActive(true);
            }


        }


        else //카드 내려놓기
        {
            var apData = CardModeState.Instance.apDataRef;
            if (thisCardData.cardType == 0)
            {
                CardModeState.Instance.curAction.actionId = 1;
                CardModeState.Instance.curAction.defense = Mathf.Max(0,thisCardData.defense +apData.tempDef+apData.permDef ) ;
                CardModeState.Instance.curAction.rumblePoint = thisCardData.rumblePoint;
                CardModeState.Instance.curAction.cardcode = thisCardData.code;
                CardModeState.Instance.curAction.animations = thisCardData.animations;
                CardModeState.Instance.curAction.actionClock = Mathf.Max(apData.CastingMinumum, thisCardData.actionClock + apData.permCast+apData.tempCast);
                CardModeState.Instance.curAction.cardname = thisCardData.name;
                CardModeState.Instance.curAction.zoneIndex = thisCardData.zoneIndex;
                CardModeState.Instance.curAction.tileType = thisCardData.tileType;
                CardModeState.Instance.curAction.effects.AddRange(CardDEffectDatabase.GetEffects(thisCardData.code));
                CardModeState.Instance.curAction.damage = Mathf.Max(0,thisCardData.damage + apData.tempDam + apData.permDam);

                btmPacketAdd(thisCardData.code);
                CardModeState.Instance.StopSelectCardLoop(CardModeState.Instance.curAction);
            }
            else if (thisCardData.cardType == 1) {
              
                  //  LocalState.Instance.localPlayers[PhotonNetwork.LocalPlayer.ActorNumber].energy -= thisCardData.energy; 
                    CardModeState.Instance.curAction.effects.AddRange(CardDEffectDatabase.GetEffects(thisCardData.code));

                //해보자
                //되면 temp rumble도 추가
                apData.tempDef += thisCardData.defense;
                apData.tempDam += thisCardData.damage;
                apData.tempCast += thisCardData.actionClock;

                CardModeState.Instance.ActionPacketUpgrade(apData);

                btmPacketAdd(thisCardData.code);

                Destroy(gameObject);
                //}
            }
            //나중에 에너지 관련 싹다 없애면됨 ㅎ. 
            else if (thisCardData.cardType == 2)
            {
                    var effectsList = CardDEffectDatabase.GetEffects(thisCardData.code);
                    foreach (var e in effectsList)
                    {
                        if (e.hookType == HookType.Support)
                        {
                            e.Apply(0, null, 0, null, null, null);
                            Debug.Log("ImSupport 즉발");
                        }
                        else
                        {
                            CardModeState.Instance.curAction.effects.Add(e); //에너지 줄이는 용도인듯 ;\
                            Debug.Log("support added");
                        }
                    }
                btmPacketAdd(thisCardData.code);

                Destroy(gameObject);
                //}
                
            }
            
            
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickScaleAnimation();
    }

    /*private void SetDragVisual(bool inside)
    {
        uiImage.sprite = inside ? arrowSprite : defaultSprite;
        rectTransform.sizeDelta = originalSizeDelta;
    }
    */
    private void SetDragVisual(bool inside)
    {
        uiImage.sprite = inside ? arrowSprite : defaultSprite;
        rectTransform.sizeDelta = originalSizeDelta;

        //  Blinded일 경우 피를 걷거나 다시 덮기
        if (CardModeState.Instance.apDataRef.isBlinded)
        {
            Transform blood = transform.Find("BloodShed");
            if (blood != null)
                blood.gameObject.SetActive(!inside); // 드래그 진입 시 false, 나갈 때 true
        }
    }
    private bool IsInsideDropZone(Vector2 screenPos) =>
        dropZone != null && RectTransformUtility.RectangleContainsScreenPoint(dropZone, screenPos);

    private void PlayClickScaleAnimation()
    {
        transform.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * 1.1f, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, 0.08f).SetEase(Ease.InQuad));
    }

    


    /// <summary>
    /// Converts a list of Vector3Int coords to their corresponding grid indices.
    /// </summary>
    private List<int> CoordsToIndices(List<Vector3Int> coords)
    {
        var indices = new List<int>(coords.Count);
        foreach (var coord in coords)
        {
            int idx = GridManagement.Instance.GetIndexFromCoord(coord);
            if (idx >= 0)
                indices.Add(idx);
        }
        return indices;
    }

    private void btmPacketAdd(string code)
    {

        LocalState.Instance.btmPacket.usedCard.Add(code);

    }
}
