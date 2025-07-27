using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public enum BoundType {
    DistanceShouldBe,
PreTile,
DefenseShouldBe,
DamageShouldBe,
CardUsedShouldBe,
LastActionShouldBe,
ActionClockDifferenceShouldBe,
YouShouldHaveMoved
}
public class Bound
{    // Start is called before the first frame update
    public BoundType boundType;
    public int amount;
    public string boundName;
    public Sprite sprite;
    public string text;
    public int boundPoint;
    
    public bool Apply(int actorNum)
    {

        var playerData = Overmind.Instance.players;
        switch (boundType)
        {

            case BoundType.DistanceShouldBe:

                Vector3Int coordA = GridManagement.Instance.GetCoordFromIndex(playerData[1].curpos) ;
                Vector3Int coordB = GridManagement.Instance.GetCoordFromIndex(playerData[2].curpos);

                int dx = coordA.x - coordB.x;
                int dy = coordA.y - coordB.y;
                int dz = coordA.z - coordB.z;

                int distance = (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz)) / 2;
                if (distance > amount)
                {
                    return false;       

                }
                else
                {
                    return true;

                }

            case BoundType.PreTile:

                return true;


         /*   case BoundType.DefenseShouldBe:

                ActionData result1 = Overmind.Instance.actionQueue.LastOrDefault(entry => entry.actorNumber == actorNum).action;

                if (result1.defense == amount)
                    return true;
                else
                    return false;
         */
            case BoundType.DefenseShouldBe:
                {
                    var lastEntry = Overmind.Instance.actionQueue
                        .LastOrDefault(entry => entry.actorNumber == actorNum);

                    return lastEntry.action != null && lastEntry.action.defense == amount;
                }
            case BoundType.DamageShouldBe:
                var ddtc = playerData[actorNum].constraintStats.damageDealtThisCycle;
                if (ddtc== amount)
                    return true;
                else return false;


            case BoundType.CardUsedShouldBe: //안됨
                var cusb = playerData[actorNum].constraintStats.actionUsedHowmany;
                if (cusb == amount)
                    return true;
                else return false;

           /* case BoundType.LastActionShouldBe:

                ActionData result = Overmind.Instance.actionQueue.LastOrDefault(entry => entry.actorNumber == actorNum).action;
                if (result.actionId == amount)
                    return true;
                else return false;
            */
            
            case BoundType.LastActionShouldBe:
                {
                    var lastEntry = Overmind.Instance.actionQueue
                        .LastOrDefault(entry => entry.actorNumber == actorNum);

                    return lastEntry.action != null && lastEntry.action.actionId == amount;
                }

            //amount이상인 경우다
            case BoundType.ActionClockDifferenceShouldBe: //안됨
                bool result2 =playerData[actorNum].constraintStats.actionClockDiffer.All(val => val >= amount);


                return result2;

                //amount가 0이면 이동햇어야함 1이면 ㄴㄴ
            case BoundType.YouShouldHaveMoved: //안됨

                bool result3 = false;

                if ((amount == 0 && playerData[actorNum].constraintStats.moved == false) || (amount == 1 && playerData[actorNum].constraintStats.moved == true))
                {
                    result3 = true;
                }


                return result3;

            default: return false;






        }

    }
}
