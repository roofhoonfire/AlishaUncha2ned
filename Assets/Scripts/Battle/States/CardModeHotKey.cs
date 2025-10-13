using UnityEngine;
using Photon.Pun;

public class CardModeHotkey : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            var ls = LocalState.Instance;
            var cms = CardModeState.Instance;
            if (ls == null || cms == null) return;

            // ★ ChooseLoop 중이거나 이미 카드모드가 켜져 있으면 핫키로 열지 않음
            if (ls.IsChooseLoopActive || cms.isActive) return;

            var ap = ls.LastApData;
            if (ap == null) return;

            // 프리뷰(비인터랙티브)로만 오픈
            cms.SetActive(true, ap, interactive: false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var ls = LocalState.Instance;
            var cms = CardModeState.Instance;
            if (cms == null || !cms.isActive) return;

            if (ls != null && ls.IsChooseLoopActive)
                cms.CancelAndReturn();      // ChooseLoop 복귀
            else
                cms.StopSelectCardLoop(null); // 그냥 닫기
        }
    }

}
