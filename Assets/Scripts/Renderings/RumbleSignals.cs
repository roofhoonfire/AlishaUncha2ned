using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RumbleSignals : MonoBehaviour
{
    // Start is called before the first frame update
   public  void Alisha_Anim_Back2Idle()
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


    public void During_Rumble_Position_Change()
    {


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
