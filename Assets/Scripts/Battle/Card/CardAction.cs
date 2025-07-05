using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CardAction : MonoBehaviour
{
    public static CardAction Instance;
   
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    public void StunRecovery(int actorNum)
    {
        Overmind.Instance.players[actorNum].isStunned = false;


    }

    public void StunOp( int amount, int hOpActorNum)
    {

        Overmind.Instance.players[hOpActorNum].isStunned = true;
        //effect activate에 스턴 해제 (players 데이터 값 바꾸는거)
        //해가지고 애니메이션 렌더링 해주면 딱일듯
        //걍 액션 새로 만들어서 넣는게 더 나을지도? opNext는 걍 큐에서 지워버리고
        Overmind.Instance.actionQueue.RemoveAll(entry => entry.actorNumber == hOpActorNum);


        Overmind.Instance.globalaction++;

        ActionData stun = new ActionData()
        {
            actionId = 1,
            actionClock = amount,
            rumblePoint = 0,
            defense = 0,
            hasOtherExecutedSinceInsertion = false,
            cardname = "기절회복",
            nthaction = Overmind.Instance.globalaction,
            tileType =-1,
            zoneIndex = 4,
            cardcode = "미싱노",
        }
        ;
        stun.actionClock = amount;
        stun.effects.Add(new CardEffect(HookType.Activate, EffectType.StunRecovery, 0, 0));
        
        Overmind.Instance.actionQueue.Add((hOpActorNum, stun, amount));
        Overmind.Instance.actionQueue.Sort((a, b) =>
            a.remainingCost != b.remainingCost
                ? a.remainingCost.CompareTo(b.remainingCost)
                : a.action.nthaction.CompareTo(b.action.nthaction)
        );


    }
    public void MoveChara(int actorNum, int amount) {
        Overmind.Instance.players[actorNum].curpos = amount; //destindex로 할지 amount로 할지 고민중
        //amount로 쓰고 actionData의 destindex는 쓰지 않도록 해보자 
    }
    public void FlagOn(int actorNum, ActionData myaction, int amount)
    {
        myaction.flags.Add(amount);

    }
    public void DealDamage(int hActorNum,int hOpActorNum, ActionData hAction,  List<int> effectTiles,
        ActionData hOpMainAction) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
    {
        int realdamage =  hAction.damage;
        int reduceDamage = hOpMainAction.defense ;
        

        foreach (var tile in effectTiles)
        {
            if (tile == Overmind.Instance.players[hOpActorNum].curpos)
            {
                Overmind.Instance.players[hOpActorNum].prevHP = Overmind.Instance.players[hOpActorNum].HP;
                if (hAction.damage - reduceDamage > 0)
                {
                    Overmind.Instance.players[hOpActorNum].HP -= (realdamage - reduceDamage);
                    //summary//
                    //데미지 제약 체커//
                    Overmind.Instance.players[hActorNum].constraintStats.damageDealtThisCycle += (hAction.damage - reduceDamage);
                }
                break;

            }
            
        }
    }
    public void MakeItTrue(ActionData hAction,ActionData hOpMainAction) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
    {
        hAction.damage += hOpMainAction.defense;
    }
    public void TrueDamage(int hActorNum, int hOpActorNum, ActionData hAction,  List<int> effectTiles) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
    {
        int realdamage = hAction.damage;
        
        foreach (var tile in effectTiles)
        {
            if (tile == Overmind.Instance.players[hOpActorNum].curpos)
            {
                Overmind.Instance.players[hOpActorNum].prevHP = Overmind.Instance.players[hOpActorNum].HP;
                if (hAction.damage  > 0)
                {
                    Overmind.Instance.players[hOpActorNum].HP -= (realdamage );
                    //summary//
                    //데미지 제약 체커//
                    Overmind.Instance.players[hActorNum].constraintStats.damageDealtThisCycle += (hAction.damage);
                }
                break;

            }

        }
    }


    public void AddDamage(int hActorNum, ActionData hAction, int amount)
    {
        hAction.damage += amount;
    }

    
    public void StackDamage(int actorNum, ActionData actionData)
    {
        actionData.damage += (Overmind.Instance.players[actorNum].prevHP - Overmind.Instance.players[actorNum].HP);
    
        
    }

    public void GetDefense(int hActorNum, ActionData hAction, int amount) {
        hAction.defense = Mathf.Max(0, hAction.defense + amount);
      //  Overmind.Instance.players[actorNum].defense += amount;
    }

    public void DamageMeBangMoo(int actorNum, int amount)
    {

        Overmind.Instance.players[actorNum].prevHP = Overmind.Instance.players[actorNum].HP;
        Overmind.Instance.players[actorNum].HP -= (amount  );

    }
    //타일위에잇는지 tf로 리턴하는 함수 만들기 

     public void NextTurn_CastingChange(int actorNum, int amount)
    {
       //실제 다음 턴 캐스팅은 apProp에서 관리하므로 이게 맞다
        Overmind.Instance.players[actorNum].forActionPacket[apProp.tempCast] += amount;
    }

    public void MoveToSelectedTile(int actorNum, int tileIndex)
    {


        Overmind.Instance.players[actorNum].curpos = tileIndex;
    }
}
