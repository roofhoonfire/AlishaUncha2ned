using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckMaker : MonoBehaviour
{
    public GameObject cardPrefab;
    public RectTransform cardContentArea; // ScrollView 안의 Content

    [Header("카드 사이 간격")]
    public float spacingX = 50f;

    public void CardLoad()
    {
        /* var loader = GameObject.Find("OverM1nd")?.GetComponent<CardCSVLoader>();
         if (loader == null || loader.runtimeCardSO == null || loader.runtimeCardSO.cards == null)
         {
             Debug.LogError("[DeckMaker] CardCSVLoader 또는 runtimeCardSO를 찾을 수 없습니다.");
             return;
         }

         var cards = loader.runtimeCardSO.cards;
        */


        var cards = CardCSVLoader.Instance?.runtimeCardSO.cards;
        // 기존 카드 제거
        foreach (Transform child in cardContentArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardContentArea);

            EachCardInfo info = cardGO.GetComponent<EachCardInfo>();
            if (info != null)
            {
                info.cardData = cards[i];
                info.ApplyCardData(); //카드 프리팹 값 초기화
            }
        }

        Debug.Log($"[DeckMaker] 가로 스크롤로 카드 {cards.Length}장 로드 완료");
    }


}
