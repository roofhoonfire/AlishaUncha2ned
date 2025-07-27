using JetBrains.Annotations;
using System;
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
    public void Make_Op_Burn(int hOpActorNum, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            int clock = (i + 1) * 5;

            ActionData burn = new ActionData
            {
                actionId = 99,
                actionClock = clock,
                rumblePoint = 0,
                defense = 0,
                hasOtherExecutedSinceInsertion = false,
                cardname = "화상",
                nthaction = ++Overmind.Instance.globalaction,
                tileType = -1,
                zoneIndex = 4,
                cardcode = "미싱노",
                Dot_to = hOpActorNum
            };

            burn.effects.Add(new CardEffect(HookType.Dot, EffectType.Dot_Burn, 5, 0));

            Overmind.Instance.actionQueue.Add((0, burn, clock));
        }

        Overmind.Instance.actionQueue.Sort((a, b) =>
            a.remainingCost != b.remainingCost
                ? a.remainingCost.CompareTo(b.remainingCost)
                : a.action.nthaction.CompareTo(b.action.nthaction)
        );
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

    public void Add_ActionClock_Op_intheQ(int hActorNum, int amount)
    {
        for (int i = 0; i < Overmind.Instance.actionQueue.Count; i++)
        {
            if (Overmind.Instance.actionQueue[i].actorNumber == hActorNum)
            {
                var tuple = Overmind.Instance.actionQueue[i];
                Overmind.Instance.actionQueue[i] = (tuple.actorNumber, tuple.action, tuple.remainingCost + amount);
            }
        }

        Overmind.Instance.actionQueue.Sort((a, b) =>
            a.remainingCost != b.remainingCost
                ? a.remainingCost.CompareTo(b.remainingCost)
                : a.action.nthaction.CompareTo(b.action.nthaction));
    }
    public void Add_Defense_Op_intheQ(int hActorNum, int amount)
    {
        for (int i = 0; i < Overmind.Instance.actionQueue.Count; i++)
        {
            if (Overmind.Instance.actionQueue[i].actorNumber == hActorNum)
            {
                var tuple = Overmind.Instance.actionQueue[i];

                tuple.action.defense = Mathf.Max(0, tuple.action.defense + amount);
                Overmind.Instance.actionQueue[i] = tuple; // 구조체 재할당 필요
                return;
            }
        }

    }
    public void Add_ActionClock_Op_ApPacket(int hActorNum, int amount)
    {
        var packet = Overmind.Instance.players[hActorNum].forActionPacket;

        if (packet.ContainsKey(apProp.tempCast))
        {
            packet[apProp.tempCast] += amount;
        }
        else
        {
            Debug.LogWarning($"[Add_ActionClock_Op_ApPacket] tempCast 항목이 존재하지 않습니다! actorNum: {hActorNum}");
        }
    }
    public void MoveChara(int actorNum, int amount) {
        Overmind.Instance.players[actorNum].curpos = amount; //destindex로 할지 amount로 할지 고민중
        //amount로 쓰고 actionData의 destindex는 쓰지 않도록 해보자 
    }
    public void FlagOn(int actorNum, ActionData myaction, int amount)
    {
        myaction.flags.Add(amount);

    }

    public void FlagOff(int actorNum, ActionData myaction, int amount)
    {
        myaction.flags.Remove(amount);

    }

    public void Add_Defense_Op_intheQ()
    {



    }
    public void Get_Element(int actorNum, int amount)
    {
        if (!Overmind.Instance.players.ContainsKey(actorNum))
        {
            Debug.LogWarning($"플레이어 {actorNum}를 찾을 수 없습니다.");
            return;
        }

        List<apProp> list = Overmind.Instance.players[actorNum].forActionPacket_elem_List;

        apProp elemToAdd = amount switch
        {
            1 => apProp.elem_fire,
            2 => apProp.elem_ice,
            3 => apProp.elem_wind,
            4 => apProp.elem_earth,
            _ => throw new ArgumentException($"잘못된 amount 값: {amount}")
        };

        // 리스트 길이가 4면 가장 앞의 요소 제거
        if (list.Count >= 4)
        {
            list.RemoveAt(0);
        }

        // 새 요소 추가
        list.Add(elemToAdd);
    }
    public void DealDamage(int hActorNum,int hOpActorNum, ActionData hAction,  List<int> effectTiles,
        ActionData hOpMainAction) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
    {
        int realdamage =  hAction.damage;
        int reduceDamage = hOpMainAction.defense ;

        if (Overmind.Instance.players[hOpActorNum].isInvincible)
        {
            Debug.Log("상대 플레이어는 무적이다. 딜 안들어간다");
            return;
        }

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
 /*   public void TrueDamage(int hActorNum, int hOpActorNum, ActionData hAction,  List<int> effectTiles) //이새낀 어차피 데미지 스텝만 처리하니까 단순하게
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

    */
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

    public void NextTurn_AddDamage(int actorNum, int amount)
    {

        Overmind.Instance.players[actorNum].forActionPacket[apProp.tempDam] += amount;
    }
    public void MoveToSelectedTile(int actorNum, int tileIndex)
    {


        Overmind.Instance.players[actorNum].curpos = tileIndex;
    }
}
