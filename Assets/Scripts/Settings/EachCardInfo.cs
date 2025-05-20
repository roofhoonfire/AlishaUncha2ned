using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EachCardInfo : MonoBehaviour
{
    public Card cardData; // Card SO에서 설정된 데이터
    public TextMeshProUGUI nameText; // 인스펙터에서 드래그 할당
    public TextMeshProUGUI playtext;
    public TextMeshProUGUI rumble;
    public TextMeshProUGUI defense;
    public TextMeshProUGUI clock;


    // cardData를 외부에서 할당한 뒤 이 메서드를 호출할 것
    public void ApplyCardData()
    {
        if (cardData != null && nameText != null)
        {
            nameText.text = cardData.name;
            playtext.text = cardData.cardText.Replace("\\n", "\n"); ;
            rumble.text = cardData.rumblePoint.ToString();
            defense.text = cardData.defense.ToString();
            clock.text= cardData.actionClock.ToString();
        }
        else
        {
            Debug.LogWarning("[EachCardInfo] CardData 또는 nameText가 비어있습니다.");
        }
    }
}
