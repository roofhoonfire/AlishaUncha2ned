using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;



//로컬 스테이트의 오브젝트를 참조하거나 하셈 
public enum status { Poisoned, Stunned, }

public static class RenderingConverter
{
    public static LocalRenderingData FromPlayer(PlayerData player)
    {
        return FromPlayerInternal(player, null);
    }

    public static LocalRenderingData FromPlayer_ChooseTile(PlayerData player, ActionData action)
    {
        return FromPlayerInternal(player, action);
    }

    private static LocalRenderingData FromPlayerInternal(PlayerData player, ActionData fallbackAction)
    {
        int actorNum = player.ActorNumber;

        var found = Overmind.Instance.actionQueue
      .LastOrDefault(t => t.actorNumber == actorNum);

        var chosenAction = found.action ?? fallbackAction;   // ← 안전한 선택
        int remaining = found.remainingCost;              // 못 찾으면 0
        int def = chosenAction?.defense ?? 0;
        string myCard = chosenAction?.cardcode;           // ← 핵심

        return new LocalRenderingData
        {
            actorNum = actorNum,
            curpos = player.curpos,
            hp = player.HP,
            defense = def,
            bounds = player.Bounds != null ? new List<string>(player.Bounds) : new List<string>(),
            remainingCost = remaining,
            boundIndex = player.boundIndex,
            hands = player.hands != null ? new List<string>(player.hands) : new List<string>(),
            isStealthed = player.isStealthed,
            elements = player.forActionPacket_elem_List != null ? new List<apProp>(player.forActionPacket_elem_List) : new List<apProp>(),
            rightOrLeft = player.rightOrLeft,
            whosCycle = Overmind.Instance.cycleState,
            myCard = myCard
        };

    }
}
public class LocalRenderingData
{
    public int actorNum;
    public int curpos;
    public int hp;
    public int defense;
    public List<string> hands; //
    public List<string> bounds;
    public List<status> statuses;
    public int remainingCost;
    public int boundIndex;
    public bool isStealthed;
    public bool isBlinded;
    public List<apProp> elements;
    public string rightOrLeft;
    public string myCard;
    public int whosCycle;

    //여기 애니메이션도 들어가야함
    
}
public class LocalRenderingStatic : MonoBehaviour
{
    public static Dictionary<int, LocalRenderingData> localRenderingDatas = new Dictionary<int, LocalRenderingData>();
    public static Dictionary <int, ActionData> localRenderingActions = new Dictionary<int, ActionData>();

   

}
