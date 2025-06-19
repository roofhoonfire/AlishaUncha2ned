using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public static class ActionPacketConverter
{

    public static ActionPacketData FromPlayer(PlayerData player)
    {
        return new ActionPacketData
        {

            defaultMove = player.forActionPacket[apProp.defaultMove],
            defaultMoveCast = player.forActionPacket[apProp.defaultMoveCast],
            permDef = player.forActionPacket[apProp.permDef],
            tempDef = player.forActionPacket[apProp.tempDef],
            permCast = player.forActionPacket[apProp.permCast],
            tempCast = player.forActionPacket[apProp.tempCast],
            permDam = player.forActionPacket[apProp.permDam],
            tempDam = player.forActionPacket[apProp.tempDam],
            canMove = player.canMove, //이거 apProp으로 나주엥 코드 싹 바꿔주덩가.. 
            hands = new List<string>(player.hands),
            CastingMinumum = player.forActionPacket[apProp.CastingMinimum],


        };





    }


}


public class BacktoMaster
{

    public List<string> usedCard;



}
public class ActionPacketData
{
    public int defaultMove;
    public int defaultMoveCast;
    public int permDef;
    public int tempDef;
    public int permCast;
    public int tempCast;
    public int permDam;
    public int tempDam;
    public bool canMove; //1이면 가능 0이면 ㄴㄴ
    public int CastingMinumum;
    public List<string> hands;


    //여기 템프 럼블 같은 것도 넣으면 된다
}