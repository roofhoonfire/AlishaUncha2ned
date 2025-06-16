using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public enum JujuType
{
  handAdd,
  Reinforce,
}
public class Juju
{

    public string jujuCode;
    public JujuType type;
    public string jujuName;
    public Sprite sprite;
    public string addingCard;
    public int boundPoint;
    public int onlyOnce; //0이면 여러번 1이면 한번 밖에 못쓰는 주술
    public string require;
    public int isUsed; //0이면 아직 안씀, 1이면 씀
    public string Text;

    public void Apply()
    {
        var actorNum     = PhotonNetwork.LocalPlayer.ActorNumber; 
        var localData = LocalState.Instance.localPlayers[actorNum];
        switch(type)
        {

            case JujuType.handAdd:

                localData.hands.Add(addingCard);
                break;


            case JujuType.Reinforce:
                localData.defaultMoveCast = 0;
                break;
        }



    }

}

