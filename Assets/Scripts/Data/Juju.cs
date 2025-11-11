using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public enum JujuType
{
  handAdd,
  Reinforce,
  heal_15,
  handAddRan,
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

            case JujuType.heal_15:
                if (playerData.HP >= 85)
                {
                    playerData.prevHP = playerData.HP;
                    playerData.HP = 100;
                    break;
                }
                
                playerData.prevHP = playerData.HP;
                playerData.HP += 15;
                break;

            case JujuType.handAddRan:
                {
                    if (string.IsNullOrEmpty(addingCard))
                    {
                        Debug.LogWarning("[Juju] handAddRan: addingCard 비어있음");
                        break;
                    }

                    // "c3,c5,c6" → ["c3","c5","c6"] (공백/빈 토큰 제거)
                    string[] tokens = addingCard.Split(',');
                    List<string> options = new List<string>(tokens.Length);
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string s = tokens[i]?.Trim();
                        if (!string.IsNullOrEmpty(s)) options.Add(s);
                    }

                    if (options.Count == 0)
                    {
                        Debug.LogWarning("[Juju] handAddRan: 유효한 카드 코드가 없음");
                        break;
                    }

                    int pick = UnityEngine.Random.Range(0, options.Count);
                    string chosen = options[pick];

                    playerData.hands.Add(chosen);
                    // 필요하면 로그:
                    // Debug.Log($"[Juju] handAddRan: '{chosen}' 추가");
                    break;
                }

        }



    }

}

