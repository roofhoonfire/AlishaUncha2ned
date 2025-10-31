using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class EachCardInfo : MonoBehaviour
{
    public Card cardData; // Card SO에서 설정된 데이터
    public TextMeshProUGUI nameText; // 인스펙터에서 드래그 할당
    public TextMeshProUGUI playtext;
    public TextMeshProUGUI rumble;
    public TextMeshProUGUI defense;
    public TextMeshProUGUI clock;
    [Header("Artwork")]
    [Tooltip("카드 일러스트가 표시될 UI Image")]
    [SerializeField] private Image artworkImage;   // ★ 인스펙터 드래그&드롭
    [SerializeField] private GameObject artworkImageMom;   // ★ 인스펙터 드래그&드롭


    // cardData를 외부에서 할당한 뒤 이 메서드를 호출할 것
    public void ApplyCardData()
    {

        if (cardData.cardType == 0) {
            if (cardData != null && nameText != null)
            {
                nameText.text = cardData.name;
                playtext.text = cardData.GetDisplayText();
                rumble.text = cardData.rumblePoint.ToString();
                defense.text = cardData.defense.ToString();
                clock.text = cardData.actionClock.ToString();
                if (cardData.sprite != null && artworkImage !=null && artworkImageMom !=null)
                {
                    artworkImageMom.SetActive(true);
                    artworkImage.sprite = cardData.sprite;

                }
            }
            else
            {
                Debug.LogWarning("[EachCardInfo] CardData 또는 nameText가 비어있습니다.");
            }
        }

        else
        {
            if (cardData != null && nameText != null)
            {
                nameText.text = cardData.name;
                playtext.text = cardData.cardText.Replace("\\n", "\n"); ;
                if (cardData.sprite != null && artworkImage != null && artworkImageMom != null)
                {
                    artworkImageMom.SetActive(true);
                    artworkImage.sprite = cardData.sprite;

                }
            }
            else
            {
                Debug.LogWarning("[EachCardInfo] CardData 또는 nameText가 비어있습니다.");
            }


        }
        
    }
}
