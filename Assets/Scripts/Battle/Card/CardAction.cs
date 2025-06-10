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
    public void StunOp(ActionData opNext, int amount, int opactorNum)
    {

        Overmind.Instance.players[opactorNum].isStunned = true;
        //effect activate에 스턴 해제 (players 데이터 값 바꾸는거)
        //해가지고 애니메이션 렌더링 해주면 딱일듯
        //걍 액션 새로 만들어서 넣는게 더 나을지도? opNext는 걍 큐에서 지워버리고
        var toRemove =
        Overmind.Instance.actionQueue.Find(entry => object.ReferenceEquals(entry.action, opNext));
        if (toRemove != default)
        {
            
            Overmind.Instance.actionQueue.Remove(toRemove);
        }

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
        
        Overmind.Instance.actionQueue.Add((opactorNum, stun, amount));
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
    public void DealDamage(int actorNum,int OppActorNum, ActionData myaction, int amount, List<int> effectTiles,
        ActionData opsrightnextAction, ActionData rightnowOp) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
    {
        //actorNum = 때리는 놈

        //OppActorNum = 맞는 새끼

        //Amount = 데미지

        //effectTiles = 어느 칸인지
        int reduceDamage = 0;
        if (rightnowOp != null)
        {
            reduceDamage = rightnowOp.defense;
        }
        else
        {
            reduceDamage = opsrightnextAction.defense;
        }

        foreach (var tile in effectTiles)
        {
            if (tile == Overmind.Instance.players[OppActorNum].curpos)
            {
                Overmind.Instance.players[OppActorNum].prevHP = Overmind.Instance.players[OppActorNum].HP;
                if (myaction.damage - reduceDamage>0)
                    Overmind.Instance.players[OppActorNum].HP -= (myaction.damage - reduceDamage);
                break;

            }
            
        }
    }

    public void AddDamage(int actorNum, ActionData action, int amount)
    {
        action.damage += amount;
    }

    
    public void StackDamage(int actorNum, ActionData actionData)
    {
        actionData.damage += (Overmind.Instance.players[actorNum].prevHP - Overmind.Instance.players[actorNum].HP);
    }

    public void GetDefense(int actorNum, ActionData myaction, int amount) {
        myaction.defense += amount;
        Overmind.Instance.players[actorNum].defense += amount;
    }

    public void DamageMeBangMoo(int actorNum, int amount)
    {

        Overmind.Instance.players[actorNum].prevHP = Overmind.Instance.players[actorNum].HP;
        Overmind.Instance.players[actorNum].HP -= (amount  );

    }
    //타일위에잇는지 tf로 리턴하는 함수 만들기 

     public void ReduceMyNExtTurnActionClock(int actorNum, int amount)
    {
        //이거 그냥 players 딕셔너리에 변수하나 둬서 beginchoose 할 때 체크 하고
        //있으면 거기서 초기화 하고, buffer에 데이터 추가하는 식으로 가야할 드 ㅅ
        //
        Overmind.Instance.players[actorNum].actionClockManuplate = amount;
    }

    public void MoveToSelectedTile(int actorNum, int tileIndex)
    {


        Overmind.Instance.players[actorNum].curpos = tileIndex;
    }
}
