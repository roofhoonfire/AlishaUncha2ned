using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;


[System.Serializable]

public class Card
{
    public string code;
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

    public string cardText;
}
public enum HookType { Activate, Priority, IQA, Counter, Guard ,BeforeRumble, RumbleWin, RumbleLose, Combo }
public enum EffectType { Damage, Move, Heal, StackDamage,  GetDefense, DamageMeBangMoo, AddDamage, OpNextActionisMoveFlagOn,
    whenDamagedFlagOn, NotRumbleFlagOn, ReplaceNextOpsMovetoStun, StunRecovery}


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
                CardAction.Instance.MoveChara(actorNum, amount);
                Debug.Log($"플레이어 {actorNum}이 {amount}로 이동한다");

                break;


            case EffectType.Damage:
                // actorId가 공격자, opponent.ownerId가 피해 대상이라 가정
                CardAction.Instance.DealDamage(actorNum, oppActorNum, myAction, amount, myAction.effectTiles, rightnextopponent, rightnowOP);
                Debug.Log($"플레이어 {actorNum}이 플레이어{oppActorNum}에게 {amount}의 피해를 입힌다");
                break;
            case EffectType.StackDamage: //피해 저장하는 대처 같은 넘
                CardAction.Instance.StackDamage(actorNum, myAction);
                Debug.Log($"플레이어 {actorNum}이 {myAction.plusAlpha}의 피해를 저장한다");
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

            case EffectType.whenDamagedFlagOn:
                if (Overmind.Instance.players[actorNum].prevHP == Overmind.Instance.players[actorNum].HP)
                {
                    Debug.Log($"플레이어 {actorNum}이 처맞지 않았기 때문에 플래그는 추가 되지 않는다");
                    break;
                }
                CardAction.Instance.FlagOn(actorNum, myAction, amount);
                Debug.Log($"플레이어 {actorNum}이 처맞았기 때문에 플래그 {amount}가 추가된다");
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
                CardAction.Instance.StunOp(rightnextopponent, amount, oppActorNum);
                Debug.Log($"크하하 플레이어 {oppActorNum}은 이제 {amount}동안 기절이다 꼴 좋군!");
                break;

            case EffectType.StunRecovery:
                CardAction.Instance.StunRecovery(actorNum);
                Debug.Log($"크하하 플레이어 {actorNum}은 기절로 부터 회복했다");
                break;


        }
    }
}

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Object/CardSO")]
public class CardSO : ScriptableObject
{
    public Card[] cards;
}