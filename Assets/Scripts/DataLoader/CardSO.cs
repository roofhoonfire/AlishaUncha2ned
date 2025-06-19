using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;


[System.Serializable]

public class Card
{
    public string code;
    public int cardType; // 0: Action, 1: Support, 2: ImmediateSupport
    public string name;

    public Sprite sprite;
    public List<AnimationClip> animations;  // 변경: 단일 → 리스트

    public int actionClock;
    public int rumblePoint;
    public int defense;
    public int disappear;
    public int mana;
    public int tileType;
    public int zoneIndex;
    public int energy;
    public int damage;

    public string cardText;
}
public enum HookType { Activate, Priority, IQA, Counter, Guard ,BeforeRumble, RumbleWin, RumbleLose, Combo, Support }
public enum EffectType { Damage, whenAttackedFlagOn, Move, Heal, StackDamage,  GetDefense, DamageMeBangMoo, AddDamage, MoveToSelectedTile, OpNextActionisMoveFlagOn,
    whenDamagedFlagOn, NotRumbleFlagOn, ReplaceNextOpsMovetoStun, StunRecovery, SelectActionClockChange, UseEnergy, ReduceMyNextTurnActionClock}


public class CardEffect
{
    public HookType hookType;
    public EffectType effectType;
    public int amount;
    public int flag; //이거 사실상 의미 없음 추후에 다른 칸으로 쓰도록 하자 

    public CardEffect(HookType h, EffectType e, int a, int f)
    {
        hookType = h;
        effectType = e;
        amount = a;
        flag = f;
    }

    //데미지 계산시의 수비는 모두 MainAction 시리즈
    public void Norm_Apply(int hActorNum, int hOpActorNum, int ttActorNUm, ActionData hAction,  ActionData hOpMainAction, ActionData hMainAction, ActionData ttAction)
    {

        //if (rumbleOp != null)
        //{
        //   isRumble = true;
        //}
        // else
        // {
        //   isRumble = false;
        //}
        switch (effectType)
        {

            case EffectType.Move:
                // Overmind.Instance.players[actorNum].canMove = false;
                CardAction.Instance.MoveChara(hActorNum, amount);
                Debug.Log($"플레이어 {hActorNum}이 {amount}로 이동한다");

                break;


            case EffectType.Damage:
                // actorId가 공격자, opponent.ownerId가 피해 대상이라 가정
                CardAction.Instance.DealDamage(hActorNum, hOpActorNum, hAction, amount, hAction.effectTiles, hOpMainAction);
                Debug.Log($"플레이어 {hActorNum}이 플레이어{hOpActorNum}에게 {hAction.damage}의 피해를 입힌다");
                break;
            case EffectType.StackDamage: //피해 저장하는 대처 같은 넘
                CardAction.Instance.StackDamage(hActorNum, hAction);
                Debug.Log($"플레이어 {hActorNum}의 데미지 업데이트 : {hAction.damage}");
                break;
            case EffectType.GetDefense:
                CardAction.Instance.GetDefense(hActorNum, hAction, amount);
                Debug.Log($"플레이어 {hActorNum}이 {amount}의 방어도를 추가한다");

                break;
            case EffectType.DamageMeBangMoo:
                CardAction.Instance.DamageMeBangMoo(hActorNum, amount);
                Debug.Log($"플레이어 {hActorNum}이 {amount}의 고정 데미지를 스스로 입는다 ㅋㅋ 병신");

                break;
            case EffectType.AddDamage:
                CardAction.Instance.AddDamage(hActorNum, hAction, amount);
                Debug.Log($"플레이어 {hActorNum}이 {amount}를 추가 피해 하려고한다");
                break;

           
            case EffectType.OpNextActionisMoveFlagOn:
                if (hOpMainAction.actionId == 0)
                {
                    CardAction.Instance.FlagOn(hActorNum, hAction, amount);
                    Debug.Log($"상대의 다음 행동이 이동이므로 플래그 {amount}가 추가된다");
                    break;
                }

                Debug.Log($"이동이 아니기 때문에 플래그 {amount}가 추가되지 아늠");

                break;
            case EffectType.ReplaceNextOpsMovetoStun:
                CardAction.Instance.StunOp(amount, hOpActorNum);
                Debug.Log($"크하하 플레이어 {hOpActorNum}은 이제 {amount}동안 기절이다 꼴 좋군!");
                break;

            case EffectType.StunRecovery:
                CardAction.Instance.StunRecovery(hActorNum);
                Debug.Log($"크하하 플레이어 {hActorNum}은 기절로 부터 회복했다");
                break;
            case EffectType.SelectActionClockChange:
                CardMultipleChoice.Instance.ChoiceStart(amount);
                Debug.Log($"Immediate support card만 가질수 있는 특성이다, Local에서 호출된다");
                break;
            case EffectType.UseEnergy:
                Overmind.Instance.players[hActorNum].energy -= amount;
                Debug.Log($"[플레이어 {hActorNum}] energy now {Overmind.Instance.players[hActorNum].energy} ");
                break;
            case EffectType.ReduceMyNextTurnActionClock:

                CardAction.Instance.NextTurn_CastingChange(hActorNum, amount);
                Debug.Log($"플레리어 {hActorNum}의 다음 턴 메인 액션의 캐스팅이 {amount}만큼 줄어든다]");
                break;

            case EffectType.MoveToSelectedTile:
                int tileIndex = hAction.effectTiles[amount]; //어마운트 번째 인덱스의 타일로 슝좍한다~
                hAction.destindex = tileIndex;
                CardAction.Instance.MoveToSelectedTile(hActorNum, tileIndex);
                Debug.Log($"공중 강습이다!");

                break;
            case EffectType.whenAttackedFlagOn:
                bool found = false;
                foreach (var tile in ttAction.effectTiles)
                {
                    if (tile == Overmind.Instance.players[hActorNum].curpos)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    CardAction.Instance.FlagOn(hActorNum, hAction, amount);
                    Debug.Log($"플레이어 {hActorNum}이 처맞았기 때문에 플래그 {amount}가 추가된다");
                }
                else
                {
                    Debug.Log($"플레이어 {hActorNum}의 위치는 공격 범위에 없으므로 플래그 추가 안 함");
                }
                break;

            case EffectType.whenDamagedFlagOn:
                bool damaged = false;
                foreach (var tile in ttAction.effectTiles)
                {
                    if (tile == Overmind.Instance.players[hActorNum].curpos)
                    {
                        //액션마다 데미지를 넣자 
                        if (ttAction.damage > hMainAction.defense)
                            damaged = true;
                        break;
                    }
                }

                if (damaged)
                {
                    CardAction.Instance.FlagOn(hActorNum, hAction, amount);
                    Debug.Log($"플레이어 {hActorNum}이 피해를 입었기 때무네 때문에 플래그 {amount}가 추가된다");
                }
                else
                {
                    Debug.Log($"플레이어 {hActorNum}의 위치는 공격 범위에 없으므로 플래그 추가 안 함");
                }
                break;

        }
    }
    public void Apply(int actorNum, ActionData myAction,  int oppActorNum, ActionData rightnextopponent , ActionData rightnowOP, ActionData myrightnextAction)
    {

        //if (rumbleOp != null)
        //{
        //   isRumble = true;
        //}
        // else
        // {
        //   isRumble = false;
        //}
        switch (effectType)
        {

            case EffectType.Move:
               // Overmind.Instance.players[actorNum].canMove = false;
                CardAction.Instance.MoveChara(actorNum, amount);
                Debug.Log($"플레이어 {actorNum}이 {amount}로 이동한다");

                break;


            case EffectType.Damage:
                // actorId가 공격자, opponent.ownerId가 피해 대상이라 가정
               // CardAction.Instance.DealDamage(actorNum, oppActorNum, myAction, amount, myAction.effectTiles, rightnextopponent, rightnowOP);
                Debug.Log($"플레이어 {actorNum}이 플레이어{oppActorNum}에게 {myAction.damage}의 피해를 입힌다");
                break;
            case EffectType.StackDamage: //피해 저장하는 대처 같은 넘
                CardAction.Instance.StackDamage(actorNum, myAction);
                Debug.Log($"플레이어 {actorNum}의 데미지 업데이트 : {myAction.damage}");
                break;
            case EffectType.GetDefense:
                CardAction.Instance.GetDefense(actorNum, myAction, amount);
                Debug.Log($"플레이어 {actorNum}이 {amount}의 방어도를 추가한다");

                break;
            case EffectType.DamageMeBangMoo:
                CardAction.Instance.DamageMeBangMoo(actorNum, amount);
                Debug.Log($"플레이어 {actorNum}이 {amount}의 고정 데미지를 스스로 입는다 ㅋㅋ 병신");

                break;
            case EffectType.AddDamage:
                CardAction.Instance.AddDamage(actorNum, myAction, amount);
                Debug.Log($"플레이어 {actorNum}이 {amount}를 추가 피해 하려고한다");
                break;

    

            case EffectType.NotRumbleFlagOn:
                if (rightnowOP == null) { 
                    CardAction.Instance.FlagOn(actorNum, myAction, amount);
                Debug.Log($"럼블이 아닌 일반 타격이므로 플래그 {amount}가 추가된다");
                    break;
                }

                Debug.Log($"럼블이기 때문에 플래그 {amount}가 추가되지 아늠");

                break;
            case EffectType.OpNextActionisMoveFlagOn:
                if (rightnextopponent.actionId == 0)
                {
                    CardAction.Instance.FlagOn(actorNum, myAction, amount);
                    Debug.Log($"상대의 다음 행동이 이동이므로 플래그 {amount}가 추가된다");
                    break;
                }

                Debug.Log($"이동이 아니기 때문에 플래그 {amount}가 추가되지 아늠");

                break;
            case EffectType.ReplaceNextOpsMovetoStun:
                CardAction.Instance.StunOp( amount, oppActorNum);
                Debug.Log($"크하하 플레이어 {oppActorNum}은 이제 {amount}동안 기절이다 꼴 좋군!");
                break;

            case EffectType.StunRecovery:
                CardAction.Instance.StunRecovery(actorNum);
                Debug.Log($"크하하 플레이어 {actorNum}은 기절로 부터 회복했다");
                break;
            case EffectType.SelectActionClockChange:
                CardMultipleChoice.Instance.ChoiceStart(amount);
                Debug.Log($"Immediate support card만 가질수 있는 특성이다, Local에서 호출된다");
                break;
            case EffectType.UseEnergy:
                Overmind.Instance.players[actorNum].energy -= amount;
                Debug.Log($"[플레이어 {actorNum}] energy now {Overmind.Instance.players[actorNum].energy} ");
                break;
            case EffectType.ReduceMyNextTurnActionClock:
                
                CardAction.Instance.NextTurn_CastingChange(actorNum, amount);
                Debug.Log($"플레리어 {actorNum}의 다음 턴 메인 액션의 캐스팅이이 {amount}만큼 줄어든다]");
                break;

            case EffectType.MoveToSelectedTile:
                int tileIndex = myAction.effectTiles[amount]; //어마운트 번째 인덱스의 타일로 슝좍한다~
                myAction.destindex = tileIndex; 
                CardAction.Instance.MoveToSelectedTile(actorNum, tileIndex);
                Debug.Log($"공중 강습이다!");

                break;
            case EffectType.whenAttackedFlagOn:
                bool found = false;
                foreach (var tile in rightnowOP.effectTiles)
                {
                    if (tile == Overmind.Instance.players[actorNum].curpos)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    CardAction.Instance.FlagOn(actorNum, myAction, amount);
                    Debug.Log($"플레이어 {actorNum}이 처맞았기 때문에 플래그 {amount}가 추가된다");
                }
                else
                {
                    Debug.Log($"플레이어 {actorNum}의 위치는 공격 범위에 없으므로 플래그 추가 안 함");
                }
                break;

            case EffectType.whenDamagedFlagOn:
                bool damaged = false;
                foreach (var tile in rightnowOP.effectTiles)
                {
                    if (tile == Overmind.Instance.players[actorNum].curpos)
                    {
                        //액션마다 데미지를 넣자 
                        if (rightnowOP.damage > myAction.defense)
                            damaged = true;
                        break;
                    }
                }

                if (damaged)
                {
                    CardAction.Instance.FlagOn(actorNum, myAction, amount);
                    Debug.Log($"플레이어 {actorNum}이 피해를 입었기 때무네 때문에 플래그 {amount}가 추가된다");
                }
                else
                {
                    Debug.Log($"플레이어 {actorNum}의 위치는 공격 범위에 없으므로 플래그 추가 안 함");
                }
                break;

        }
    }
}

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Object/CardSO")]
public class CardSO : ScriptableObject
{
    public Card[] cards;
}