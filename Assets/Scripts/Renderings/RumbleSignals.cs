using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RumbleSignals : MonoBehaviour
{
    [SerializeField] private RumbleContext ctx; // ← 인스펙터에 연결

    // Start is called before the first frame update
    public void Alisha_Anim_Back2Idle()
    {
        Debug.Log("응 럼블 시그널 왓어");


        var dic = LocalState.Instance.PlayerObDic;

        foreach (var chargo in dic.Values) {

            var animatortt = chargo.GetComponentInChildren<Animator>();
            if (animatortt == null)
            {
                Debug.LogError("럼블,  Animator 없음");

                return;
            }
            animatortt.SetTrigger("Trig_AfterRumble");

        }

    }

    private Transform tileIndextoPosition(int tileindex)
    {

        //이거 로컬에서도 그리드 초기화 해야함 
        return GridManagement.Instance?.tileObjects[tileindex].transform.Find("charpoint");
        //리턴 된 놈은 Transform으로 받고 .transform.position으로 써야 작동

    }

    public void During_Rumble_Position_Change()
    {
        if (ctx == null) { Debug.LogError("[RumbleSignals] RumbleContext 참조가 없음 그래서 못움직임"); return; }

        LocalState.Instance.PlayerObDic[ctx.data1.actorNum].transform.position = tileIndextoPosition(ctx.data1.curpos).position;
        LocalState.Instance.PlayerObDic[ctx.data2.actorNum].transform.position = tileIndextoPosition(   ctx.data2.curpos).position;
        Debug.Log("이동완료");


    }

    public void Alisha_Anim_LetsRumble()
    {
        Debug.Log("응 럼블 시그널 왓어");


        var dic = LocalState.Instance.PlayerObDic;

        foreach (var chargo in dic.Values)
        {

            var animatortt = chargo.GetComponentInChildren<Animator>();
            if (animatortt == null)
            {
                Debug.LogError("럼블,  Animator 없음");

                return;
            }
            animatortt.SetTrigger("Trig_LetsRumble");

        }

    }


}
