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
            
            
            //이즈 블라인디드, 캔무브는 플레이어 데이터에 잇는 값을 액션 패킷으로 넘겨주는 거임 둘의 변수이름은 같지만
            //좌측은 액션패킷 데이터의 멤버 변수, 우측은 플레이어 데이터의 멤버변수임
            isBlinded = player.isBlinded,


            canMove = player.canMove, //이거 apProp으로 나주엥 코드 싹 바꿔주덩가.. 
            hands = new List<string>(player.hands),
            CastingMinumum = player.forActionPacket[apProp.CastingMinimum],

            //원소도 추가
            elements = new List<apProp>(player.forActionPacket_elem_List)

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
    public bool isBlinded;
    public bool canMove; //1이면 가능 0이면 ㄴㄴ
    public int CastingMinumum;
    public List<string> hands;
    public List<apProp> elements;

    //여기 템프 럼블 같은 것도 넣으면 된다
}