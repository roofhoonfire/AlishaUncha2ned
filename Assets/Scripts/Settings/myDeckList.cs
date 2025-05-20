using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;  // <-- 추가

public class myDeckList : MonoBehaviour //지금 사실 덱 리스트가 중복으로 저장이 되어있음. 이거 해결할 필요 잇음
{
    public List<string> deckList = new List<string>();
    public List<string> codeList = new List<string>();

    public GameObject cardNameItemPrefab;
    public Transform selectedCardListParent;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("deckcode", out object deckObj))
            {
                string[] codes = deckObj as string[];
                if (codes != null)
                {
                    Debug.Log($"[LocalPlayer] deckcode 프로퍼티: {string.Join(", ", codes)}");
                }
                else
                {
                    Debug.Log("deckcode 프로퍼티는 존재하지만 형식이 string[]이 아님");
                }
            }
            else
            {
                Debug.Log("deckcode 프로퍼티가 설정되어 있지 않음");
            }
        }
    }

    public void RefreshSelectedCardListUI()
    {
        foreach (Transform child in selectedCardListParent)
            Destroy(child.gameObject);

        foreach (string cardName in deckList)
        {
            GameObject item = Instantiate(cardNameItemPrefab, selectedCardListParent);
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = cardName;
        }
    }

   
}
