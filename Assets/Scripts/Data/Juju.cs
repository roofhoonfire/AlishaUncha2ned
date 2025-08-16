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


    public Juju() { }

    public Juju(Juju other)
    {
        jujuCode = other.jujuCode;
        type = other.type;
        jujuName = other.jujuName;
        sprite = other.sprite; // Sprite는 리소스 공유이므로 얕은 복사로 충분
        addingCard = other.addingCard;
        boundPoint = other.boundPoint;
        onlyOnce = other.onlyOnce;
        require = other.require;
        isUsed = other.isUsed;
        Text = other.Text;
    }

    public void Apply(int actorNum)
    {
        var playerData = Overmind.Instance.players[actorNum];
        switch(type)
        {

            case JujuType.handAdd:

                playerData.hands.Add(addingCard);
                break;


            case JujuType.Reinforce:
                playerData.forActionPacket[apProp.defaultMoveCast] = 0;
                break;
        }



    }

}

