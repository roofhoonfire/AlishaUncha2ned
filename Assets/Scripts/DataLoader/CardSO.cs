using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
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
public enum HookType { Activate, Priority, IQA, Counter, Guard ,BeforeRumble, RumbleWin, RumbleLose, Combo, Support, Dot }
public enum EffectType {
    Add_ActionClock_Op, Add_Defense_Op,Add_Defense_Op_intheQ,
    Burn_Op,StartDot_Heal,StartDot_Stealth_Off,Stealth_Off,
    Blind_Op,
    Dot_Heal, Dot_Burn, Dot_Stealth_Off, Dot_Blind_Off,

    whenDamaged_FlagOn,  Element_Check_FlagOn,  intheRange_FlagOn, 
    Flag_Off, Remove_Element,Invincible_forOneAction,Stealth, Damage, whenAttackedFlagOn, Move, Get_Element, Heal, StackDamage,  GetDefense, DamageMeBangMoo, AddDamage, MoveToSelectedTile, OpNextActionisMoveFlagOn,
     NotRumbleFlagOn, ExtraSelect_Kawari,NextTurn_AddDamage, ReplaceNextOpsMovetoStun, StunRecovery, SelectActionClockChange, UseEnergy, TrueDamage, MakeItTrue, PrevCycleClockFlagOn,  ReduceMyNextTurnActionClock, 
}


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
    public IEnumerator Norm_Apply(
    int hActorNum,
    int hOpActorNum,
    ActionData hAction,
    ActionData hOpMainAction,
    ActionData hMainAction,
    ActionData ttAction)
    {
        switch (effectType)
        {



            case EffectType.StartDot_Heal:
                {
                    CardAction.Instance.StartDot_Heal(hActorNum, amount);
                    Debug.Log($"플레이어 {hActorNum}에게 찔끔힐 DOT을 부여했다");
                    break;
                }

            case EffectType.StartDot_Stealth_Off:
                {
                    CardAction.Instance.StartDot_Stealth_Off(hActorNum, amount);
                    Debug.Log($"플레이어 {hActorNum}에게 찔끔힐 DOT을 부여했다");
                    break;
                }

            case EffectType.Add_Defense_Op_intheQ:
                {
                    if (hOpMainAction != null) {
                        CardAction.Instance.Add_Defense_Op_intheQ(hOpActorNum, amount);
                        Debug.Log($"{hOpActorNum}의 방어도가 {amount}만큼 더해진다 후후");
                    }

                    break;
                }

            case EffectType.Element_Check_FlagOn:
                {
                    // amount 분해
                    int depp = amount % 10;
                    int raw = amount / 10;

                    // 요구 원소 개수 파싱
                    Dictionary<apProp, int> required = new Dictionary<apProp, int>();
                    for (int i = 0; i < 4; i++)
                    {
                        int digit = raw % 10;
                        raw /= 10;

                        if (digit == 0) continue;

                        apProp prop = digit switch
                        {
                            1 => apProp.elem_fire,
                            2 => apProp.elem_ice,
                            3 => apProp.elem_wind,
                            4 => apProp.elem_earth,
                        };

                        if (required.ContainsKey(prop))
                            required[prop]++;
                        else
                            required[prop] = 1;
                    }

                    // 플레이어의 엘리먼트 리스트에서 보유 현황 집계
                    var elementList = Overmind.Instance.players[hActorNum].forActionPacket_elem_List;
                    Dictionary<apProp, int> has = new Dictionary<apProp, int>();
                    foreach (var elem in elementList)
                    {
                        if (has.ContainsKey(elem))
                            has[elem]++;
                        else
                            has[elem] = 1;
                    }

                    // 요구 조건 충족 여부 확인
                    bool canActivate = true;
                    foreach (var kvp in required)
                    {
                        if (!has.ContainsKey(kvp.Key) || has[kvp.Key] < kvp.Value)
                        {
                            canActivate = false;
                            Debug.Log($"플레이어 {hActorNum}는 스킬 조건을 못채움 ㅋ ");

                            break;
                        }
                    }

                    if (canActivate)
                    {
                        Debug.Log($"플레이어 {hActorNum}는 스킬 조건을 채웟다1 ");
                        CardAction.Instance.FlagOn(hActorNum, hAction, depp);
                    }

                    break;
                }

            case EffectType.Flag_Off:
            {
                    CardAction.Instance.FlagOff(hActorNum, hAction, amount);
                    Debug.Log("플래그는 제 역할을 다했다 ㅂㅂ");
                break;
            }
            case EffectType.Add_ActionClock_Op:
                {
                    bool existsInQueue = Overmind.Instance.actionQueue
                        .Any(q => q.actorNumber == hOpActorNum);

                    if (existsInQueue)
                        CardAction.Instance.Add_ActionClock_Op_intheQ(hOpActorNum, amount);
                    else
                        CardAction.Instance.Add_ActionClock_Op_ApPacket(hOpActorNum, amount);

                    break;
                }
            case EffectType.Remove_Element:
                {
                    List<apProp> list = Overmind.Instance.players[hActorNum].forActionPacket_elem_List;

                    int temp = amount;
                    List<apProp> toRemove = new List<apProp>();

                    // amount → 제거해야 할 원소 리스트로 변환
                    while (temp > 0)
                    {
                        int digit = temp % 10;
                        temp /= 10;

                        apProp prop = digit switch
                        {
                            1 => apProp.elem_fire,
                            2 => apProp.elem_ice,
                            3 => apProp.elem_wind,
                            4 => apProp.elem_earth,
                            // 이론상 나올 수 없음
                        };

                        toRemove.Add(prop);
                    }

                    // 제거 처리
                    foreach (var elem in toRemove)
                    {
                        // 해당 원소가 리스트에 있으면 하나 제거
                        if (list.Contains(elem))
                        {
                            list.Remove(elem);
                        }
                        else
                        {
                            Debug.LogWarning($"[Remove_Element] {elem} 원소가 부족하여 제거 실패함 (actor: {hActorNum})");
                        }
                    }

                    break;
                }
            case EffectType.Heal:

                var data = Overmind.Instance.players[hActorNum];
                data.prevHP = data.HP;
                data.HP += amount;
                Debug.Log($"플레이어 {hActorNum}이 {amount}의 체력을 회복한다 ");
                
                break;
            case EffectType.Dot_Heal:

                var datar = Overmind.Instance.players[ttAction.Dot_to];
                datar.prevHP = datar.HP;
                datar.HP += amount;
                Debug.Log($"플레이어 {hActorNum}이 {amount}의 체력을 회복한다 ");

                break;


            case EffectType.Burn_Op:
                CardAction.Instance.Make_Op_Burn(hOpActorNum, amount);
                Debug.Log($"플레이어 {hActorNum}가 상대를 불태운다 ");

                break;
            case EffectType.Stealth:

                var dataa = Overmind.Instance.players[hActorNum];
                dataa.isStealthed = true;
                Debug.Log($"플레이어 {hActorNum}이 은신을 얻는다 ");

                break;
            case EffectType.Dot_Stealth_Off:
                {
                     Overmind.Instance.players[ttAction.Dot_to].isStealthed = false;
                                 Debug.Log($"플레이어 {hActorNum}이 은신을 잃는다 ");
                    break;
                }
            case EffectType.Dot_Blind_Off:
                Overmind.Instance.players[ttAction.Dot_to].isBlinded = false;
                Debug.Log("눈깔을 회복햇다");
                break;
            case EffectType.Blind_Op:
                Overmind.Instance.players[hOpActorNum].isBlinded = true;
                CardAction.Instance.StartDot_Blind_Off(hOpActorNum, amount);
                Debug.Log("눈깔을 파버렸다 ㄷ ㄷ");
                break;

            case EffectType.Get_Element:
                CardAction.Instance.Get_Element(hActorNum, amount);
                Debug.Log($"플레이어 {hActorNum}가 원소을 얻는다 ");

                break;

            case EffectType.Dot_Burn:
                CardAction.Instance.DamageMeBangMoo(ttAction.Dot_to, amount);
                Debug.Log($"플레이어 {ttAction.Dot_to}가 화상 피해를 입는다 ");

                break;
            case EffectType.Move:
                CardAction.Instance.MoveChara(hActorNum, hAction.destindex);
                Debug.Log($"플레이어 {hActorNum}이 {hAction.destindex}로 이동한다");
                break;
            case EffectType.Invincible_forOneAction:
                Overmind.Instance.players[hActorNum].isInvincible = true;
                Debug.Log($"플레이어 {hActorNum}이 이번 턴 피해를 입지 않는다");
                break;
            case EffectType.Damage:
                CardAction.Instance.DealDamage(hActorNum, hOpActorNum, hAction, hAction.effectTiles, hOpMainAction);
                Debug.Log($"플레이어 {hActorNum}이 플레이어{hOpActorNum}에게 {hAction.damage}의 피해를 입힌다");
                break;
            case EffectType.intheRange_FlagOn:
                {

                    foreach (var tile in hAction.effectTiles)
                    {
                        if (tile == Overmind.Instance.players[hOpActorNum].curpos)
                        {
                            Debug.Log($" 플레이어{hOpActorNum}가 피격 범위 내이므로 플래그 온즈섹스");

                            CardAction.Instance.FlagOn(hActorNum, hAction, amount);

                            break;
                        }

                    }

                    break;
                }
           /* case EffectType.TrueDamage:
                CardAction.Instance.TrueDamage(hActorNum, hOpActorNum, hAction, hAction.effectTiles);
                Debug.Log($"플레이어 {hActorNum}이 플레이어{hOpActorNum}에게 {hAction.damage}의 고정 피해를 입힌다");
                break;
           */
            case EffectType.MakeItTrue:
                CardAction.Instance.MakeItTrue(hAction, hOpMainAction);
                Debug.Log($"플레이어 {hActorNum}의 피해 {hAction.damage}는 이제 고정뎀이다 ㄷㄷ");
                break;

            case EffectType.StackDamage:
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
                }
                else
                {
                    Debug.Log($"이동이 아니기 때문에 플래그 {amount}가 추가되지 않음");
                }
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
                Debug.Log($"[플레이어 {hActorNum}] energy now {Overmind.Instance.players[hActorNum].energy}");
                break;

            case EffectType.ReduceMyNextTurnActionClock:
                CardAction.Instance.NextTurn_CastingChange(hActorNum, amount);
                Debug.Log($"플레리어 {hActorNum}의 다음 턴 메인 액션의 캐스팅이 {amount}만큼 줄어든다]");
                break;

            case EffectType.MoveToSelectedTile:
                int tileIndex = hAction.effectTiles[amount];
                hAction.destindex = tileIndex;
                CardAction.Instance.MoveToSelectedTile(hActorNum, tileIndex);
                Debug.Log($"공중 강습이다!");
                break;

            case EffectType.whenAttackedFlagOn:
                {
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
                }
                break;

            case EffectType.whenDamaged_FlagOn:
                {
                    // amount 예시: 3512 → 기준피해량: 35, 플래그: 12
                    int damageThreshold = amount / 100;
                    int flagToAdd = amount % 100;

                    bool isInRange = false;

                    foreach (var tile in ttAction.effectTiles)
                    {
                        if (tile == Overmind.Instance.players[hActorNum].curpos)
                        {
                            isInRange = true;
                            break;
                        }
                    }

                    if (!isInRange)
                    {
                        Debug.Log($"플레이어 {hActorNum}의 위치는 공격 범위에 없으므로 플래그 추가 안 함");
                        break;
                    }

                    int netDamage = ttAction.damage - hMainAction.defense;
                    if (netDamage >= damageThreshold)
                    {
                        CardAction.Instance.FlagOn(hActorNum, hAction, flagToAdd);
                        Debug.Log($"플레이어 {hActorNum}이 {netDamage} 피해를 입었고, 기준 {damageThreshold} 이상이므로 플래그 {flagToAdd} 추가");
                    }
                    else
                    {
                        Debug.Log($"플레이어 {hActorNum}이 받은 피해 {netDamage}가 기준 {damageThreshold} 미만이므로 플래그 추가 안 함");
                    }
                }
                break;

            case EffectType.PrevCycleClockFlagOn:
                if (Overmind.Instance.players[hActorNum].constraintStats.prevCycleActionClocks.Sum() >= amount)
                {
                    CardAction.Instance.FlagOn(hActorNum, hAction, 22);
                }
                break;

            case EffectType.NextTurn_AddDamage:
                CardAction.Instance.NextTurn_AddDamage(hActorNum, amount);
                break;
            case EffectType.ExtraSelect_Kawari:
                Overmind.Instance.extraSelectionJson = null;
                Overmind.Instance.ExtraSelection_Caller(ExtraSelection.Kawari, hActorNum);
                Debug.Log("바꿔치기 술법의 발동이다!");
                yield return new WaitUntil (() => Overmind.Instance.extraSelectionJson != null);
                hAction.destindex = JsonConvert.DeserializeObject<int>(Overmind.Instance.extraSelectionJson);
                
                Overmind.Instance.extraSelectionJson =null;
                //여기서 json 컨버터로 stirng 인트로 바꾼담에 action의 dest에 박아 넣어주면됨
                break;
        }

        // 지금은 yield가 필요 없지만, 확장성을 위해 여기 추가
        yield break;
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

            case EffectType.whenDamaged_FlagOn:
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