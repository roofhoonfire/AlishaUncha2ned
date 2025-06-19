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
            .LastOrDefault(tuple => tuple.actorNumber == actorNum);

        int remaining = found.remainingCost;
        int def = found.Item2 != null ? found.Item2.defense :
                  fallbackAction != null ? fallbackAction.defense : 0;

        return new LocalRenderingData
        {
            actorNum = actorNum,
            curpos = player.curpos,
            hp = player.HP,
            defense = def,
            bounds = new List<string>(player.Bounds),
            remainingCost = remaining,
            boundIndex = player.boundIndex,
            hands = new List<string>(player.hands),
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
    //여기 애니메이션도 들어가야함
    
}
public class LocalRenderingStatic : MonoBehaviour
{
    public static Dictionary<int, LocalRenderingData> localRenderingDatas = new Dictionary<int, LocalRenderingData>();


   

}
