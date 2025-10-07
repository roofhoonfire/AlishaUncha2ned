using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Photon.Realtime;
using System;
using System.Linq;
using JetBrains.Annotations;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using System.Runtime.ExceptionServices;
using System.Net.Sockets;
using UnityEngine.UIElements;
using UnityEngine.Rendering.Universal;
using UnityEngine.Analytics;
using System.Numerics;

public enum apProp
{

   blinded, defaultMove,defaultMoveCast,tempCast,permCast,tempDef,permDef,tempDam,permDam, CastingMinimum, elem_fire, elem_ice, elem_wind, elem_earth
}

public enum ExtraSelection
{

    Kawari
}

public enum HitResolution
{
    Missed,    // 타일 미스(타겟이 범위 밖) 또는 타일형(-1)
    Defended,  // 범위 안이나 (action.damage < targetDefense)
    Damage     // 범위 안이고 (action.damage >= targetDefense)
}
public class ConstraintStats
{
    public int damageDealtThisCycle = 0;
    public int actionUsedHowmany = 0;
    public List<int> actionClockDiffer = new();
    public bool moved = false;
    public List <int> thisCycleActionClocks = new();
    public List<int> prevCycleActionClocks = new();
}
public class PlayerData //여기 변수 추가할 때마다 local의 SyncAll과 SyncPartial 업뎃 해야함
{
    public int ActorNumber;
    public int prevHP;
    public int HP ;//currentHp
    public int curpos;
    public int deckIndexStart; //몇번째 카드 부터 
    public int deckIndexEnd; //이 덱 정보는 지금 로컬에서만 관리되고 마스터로 보내주진않음 
    //즉 방어도 렌더링은 카드 내릴때, 한번 처음 해주고 그 뒤엔 localUIRendering에서 맘껏 건들면될듯 ;;
    public int energy;
    public bool canMove = true;
    public bool isBlinded = false;
    public bool isInvincible = false;
    public bool isStunned = false;
    public bool isStealthed = false;
    public int actionClockManuplate = 0;
    public int defenseManuplate = 0;
    public int timeBombSetinTomMotion = 0;
    public string rightOrLeft;

    public List<string> Bounds ; //바운드 아이디의 리스투이돵돠라돵돵
    public int boundIndex = 0;



    //수치관련//

    public int defaultMove = 3;
    public int defaultMoveCast = 99;






    //액션 패킷 강화용도

    public Dictionary<apProp, int> forActionPacket;
    public List<apProp> forActionPacket_elem_List;



    //summary//
    public ConstraintStats constraintStats = new();




    //summary//
    //덱관련//

    public List<string> DeckCodes;
    public List<string> hands;
    public List<string> trash;
    public int handsJangSoo = 5;

    //summary//
    //주술 관련

    public List<string> JujuCode;




    public PlayerData(int actorNumber, List<string> deckCodes, int initialHP = 100)
    {
        //세번째 매
        ActorNumber = actorNumber;
        DeckCodes = deckCodes;
        HP = initialHP;
        deckIndexStart = 0;
        prevHP = initialHP;
        energy = 3;

        //기찮으므로로로로루뢰뢰
        Bounds = new List<string> {"b1", "b3", "b4","b5","b6","b7" };
        JujuCode = new List<string> { "j4", "j10", "j3" };




        forActionPacket = new Dictionary<apProp, int>()
        {

            {apProp.defaultMove,3 },
            {apProp.defaultMoveCast,99 },
            {apProp.permDef,0 },
            {apProp.tempDef,0 },
            {apProp.tempCast, 0},
            {apProp.permCast,0},
            {apProp.permDam,0},
            {apProp.tempDam,0},
            {apProp.CastingMinimum,1},
          

        };
        forActionPacket_elem_List = new List<apProp>();






    }
}

public class ActionData //여기 뭐 추가할 거면 carddragHandler로 수정해야함
{

    public int actionId; //0 이면 이동 1이면 카드, 99면 도트 뎀 
    public int destindex = -10; // 이동 일 경우 목적지 인덱스를 저장// 액션이 destIndex가 있을 경우 해당 위치로 이동 (플레이어 딕셔너리 저장은 따로)//즉 렌더링 용 변수이다 
    public int defense;
    public int damage;
    public int rumblePoint;
    public int actionClock;
    public string cardcode;
    public int plusAlpha; //플러스 알파는 액션 단위로 관리해야함// 이거 이제 없애도 됨 //물론 바꾸긴 해야겟디만 ㅎ
    public List <int> flags = new List<int> () ;//
    public bool isfirstStrikeSuccess = false; //이것도 사실상 의미가 없어졌지만, 일단 냅두자
    public string cardname;
    public int tileType;
    public int zoneIndex;
    public List<AnimationClip> animations;//사실상 필요가 없어졌다
    public bool asCombo = false;    
    public List<CardEffect> effects = new();
    public int Dot_to; //도트 딜이 노리는 액터넘버
    public List<int> effectTiles = new();
    //public HitResolution? resolvedOutcome;

    [NonSerialized]
    public bool hasOtherExecutedSinceInsertion = false; //선공, 대처 결정용 변수

    public int nthaction =0;
    public bool isDot = false;

    public void InitializeEffects()
    {

        if (effects != null && effects.Count > 0)
            effects.AddRange(CardDEffectDatabase.GetEffects(cardcode));
        else
            effects = CardDEffectDatabase.GetEffects(cardcode);
        // 여기서 이펙트 리스트 채워넣고
        // 그다음에 럼블 포인트, 수비력 등 도 채워넣으면댐
    }



    public void ProcessHook(
        HookType hook,
        int actorNum,
        ActionData rightNextOpponent,
        int oppActorNum,
        ActionData rightNowOp,
        ActionData myRightNextMove)
    {
        if (effects == null)
            return;

        foreach (var e in effects)
        {
            // 훅 타입이 다르면 스킵
            if (e.hookType != hook)
                continue;

            // 선공 훅인데 이미 다른 행동이 실행된 적이 있으면 스킵 (flag 상관없이 적용)
            if (e.hookType == HookType.Priority && hasOtherExecutedSinceInsertion)
                continue;

            // flag가 0이 아닌 경우, flags 리스트에 포함되어야 적용
            if (e.flag != 0)
            {
                if (flags == null || !flags.Contains(e.flag))
                    continue;
            }
            //rightNowOp는 대처, 카운터, IQA시 사용되는 액션임
            //
            // 모든 조건 통과 시 효과 적용
            e.Apply(actorNum, this, oppActorNum, rightNextOpponent, rightNowOp, myRightNextMove);
            flags.Remove(e.flag);
        }

    }
    public IEnumerator Norm_ProcessHook(
     HookType hook,
     int hActorNum,
     int hOpActorNum,
     ActionData ttAction,
     ActionData hOpMainAction,
     ActionData hMainAction)
    {
        if (effects == null)
            yield break;

        foreach (var e in effects)
        {
            // 훅 타입이 다르면 스킵
            if (e.hookType != hook)
                continue;

            // 선공 훅인데 이미 다른 행동이 실행된 적이 있으면 스킵
            if (e.hookType == HookType.Priority && hasOtherExecutedSinceInsertion)
                continue;

            // flag 조건 확인
            if (e.flag != 0)
            {
                if (flags == null || !flags.Contains(e.flag))
                    continue;
            }

            // 효과 적용 (여기서 애니메이션 대기 등 비동기 처리가 필요하다면 yield 가능)
            yield return e.Norm_Apply(hActorNum, hOpActorNum, this, hOpMainAction, hMainAction, ttAction);
          //  flags.Remove(e.flag);

            // flag 제거
            // 영구 플래그 처리 필요하면 여기에 조건 추가
        }

    }










}

[RequireComponent(typeof(PhotonView))]
public class Overmind : MonoBehaviourPunCallbacks
{
    //잡변수들
    //드래그 드랍 참조
   

    public static Overmind Instance { get; private set; }
    
    
    
    public myDeckList localDeckLoader;


    public int globalaction = 0;
    // 플레이어 데이터
    public Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    // 선택 대기 및 실행 큐

    private Dictionary<int, (ActionData action, int cost)> pendingSelections;

    private List<(int actorNum, List<int> tiles)> pendingTiles;
    public List<(int actorNumber, ActionData action, int remainingCost)> actionQueue;
    public Dictionary<int, Dictionary<string, Juju>> playerJujuData = new Dictionary<int, Dictionary<string, Juju>>();

    private int syncCount = 0;



    public bool GA_On = false;

    public int initialHP = 100;



    public string extraSelectionJson;
    private int faceOffCount = 0;// 더 좋은 방법있음 나와보라그래
    
    public int cycleState = 0; // -1: Initial, 0: FaceOff, 1: Actor1, 2: Actor2
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            pendingTiles = new List<(int actorNum, List<int> tiles)> ();
            pendingSelections = new Dictionary<int, (ActionData, int)>();
            actionQueue = new List<(int, ActionData, int)>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    //로컬에서 방문 닫고 들어온놈이 호출
    public void RequestLoadBattleScene()
    {
        photonView.RPC(nameof(RPC_LoadBattleScene), RpcTarget.MasterClient);
    }


    //방문 닫고 들어온 놈의 요청을 받아서 Battle씬을 호출한다
    [PunRPC]
    private void RPC_LoadBattleScene()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.LoadLevel("Battle");
    }
    /// 로컬 플레이어의 덱 코드를 MasterClient에게 전송합니다.
    public void SubmitMyDeckCodes()
    {
        if (localDeckLoader == null)
        {
            Debug.LogWarning("Overmind: localDeckLoader가 할당되지 않았습니다!");
            return;
        }

        string[] codes = localDeckLoader.codeList.ToArray();
        photonView.RPC(
            nameof(RPC_ReceiveDeckCodes),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber,
            codes
        );
    }

    /// MasterClient에서 호출되어 각 클라이언트의 덱 코드를 PlayerData에 저장합니다.
    [PunRPC] //지금 덱코드를 넘겨주는 시점에서 처음으로 player 딕셔너리에 축아 된다
    private void RPC_ReceiveDeckCodes(int actorNumber, string[] codes)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var codeList = new List<string>(codes);

        if (!players.ContainsKey(actorNumber))
        {
            players[actorNumber] = new PlayerData(actorNumber, codeList, initialHP);
        }
        else
        {
            players[actorNumber].DeckCodes = codeList;
        }

        Debug.Log($"Overmind: Actor {actorNumber} 덱 코드 저장 - [{string.Join(", ", codes)}]");
    }

    


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (scene.name == "Battle")
        {
            InitGame();              // 초기 데이터 세팅 마스터클라이언트 저장
            InitGameSync();  // 모든 클라이언트에 동기화 요청
            // 코루틴 시작은 동기화 완료 후 NotifyRPC에서 실행
        }
    }

    void InitGame()
    {
        
        foreach (var kvp in players)
        {
            var player = kvp.Value;

            // 1) 위치 초기화
            player.curpos = (kvp.Key == PhotonNetwork.MasterClient.ActorNumber) ? 17 : 19;
            // 에너지, 체력 초기화가 필요하다면 여기서 추가
            if (player.curpos == 17)
                player.rightOrLeft = "right";
            else
                player.rightOrLeft = "left";


            // 2) 덱 셔플 
            ShuffleList(player.DeckCodes);


            // 2.5) 바운드 셔플 
            ShuffleList(player.Bounds);

            // 3) 핸드/트래시 초기화
            player.hands = new List<string>();
            player.trash = new List<string>();

            // 4) 핸드에 카드 드로우
            int drawCount = Math.Min(player.handsJangSoo, player.DeckCodes.Count);
            for (int i = 0; i < drawCount; i++)
            {
                string drawnCard = player.DeckCodes[0];
                player.DeckCodes.RemoveAt(0);
                player.hands.Add(drawnCard);
            }
            //주주 메타 데이터 초기화 -- 사용했던 주술, 혹은 나와선 안되는 주술 등
            //핵심, 깊은 복사를 해야한다 ㄷ ㄷ 
            playerJujuData[kvp.Key] = JujuLoader.jujuDataBase
                .ToDictionary(entry => entry.Key, entry => new Juju(entry.Value));



            // 카드 메타 데이터 초기화 -- 미라클 슬래쉬 같은거
            Debug.Log($"[최초 덱셔플] Player {player.ActorNumber}: hands({player.hands.Count}), deck({player.DeckCodes.Count})");
        }
    }

  

    void InitGameSync()

    {
        syncCount = 0;//syncCompleteCount = 0;
        var packet = new Dictionary<int, LocalRenderingData>(); 
        foreach (var playerData in players)
        {

            var temp = new LocalRenderingData()
            {
                actorNum = playerData.Key,
                curpos = playerData.Value.curpos,
                hp = 100,
                defense = 0,
                bounds = new List<string>(playerData.Value.Bounds), // 안전하게 복사
                remainingCost = 0,
                boundIndex = 0,
                rightOrLeft = playerData.Value.rightOrLeft

            };

            packet[playerData.Key] = temp;

        }
        string packetjson = JsonConvert.SerializeObject(packet);
        photonView.RPC(nameof(RPC_InitGameSync_M2C), RpcTarget.All, packetjson);
    }

    [PunRPC]
    void RPC_InitGameSync_M2C(string packetjson) //얜처음 한번만
    {
        var data = JsonConvert.DeserializeObject<Dictionary<int, LocalRenderingData>>(packetjson);
        LocalState.Instance?.GameStart(data);

        // 마스터 포함 모든 클라이언트가 완료 보고
       //시작 연출로 인한 위치 변경
        // photonView.RPC(nameof(RPC_InitGameSyncDone_C2M), RpcTarget.MasterClient,
         //   PhotonNetwork.LocalPlayer.ActorNumber);
    }




    [PunRPC]
    public void RPC_InitGameSyncDone_C2M(int actorNumber)
    {
        syncCount++;//' syncCompleteCount++;
        Debug.Log($"[Master] 플레이어 {actorNumber} 동기화 완료 ({syncCount}/{players.Count})");

        if (syncCount == players.Count)// syncCompleteCount==players.Count
        {
            //
            Debug.Log("[Master] 모든 클라이언트 동기화 완료 → 게임 루프 시작");
            syncCount = 0;
            StartCoroutine(TurnLoop());
        }
    }

  

    public void Hand_Gen(int actorNum)
    {
        var playerData = players[actorNum];
        int neededCount = playerData.handsJangSoo - playerData.hands
            .Count(code =>
            {
                var card = CardCSVLoader.Instance.GetCardByCode(code);
                return card != null && card.cardType == 0;
            });

        //여기서 서포트 카드는 제외하는 처리가 이미 되잇네 휴

        for (int i = 0; i < neededCount; i++)
        {
            if (playerData.DeckCodes.Count == 0)
            {
                // 덱이 비었으면 trash에서 refill 시도
                RefillDeckFromTrash(playerData);

                // refill 후에도 비어 있으면 더 이상 뽑을 수 없음
                if (playerData.DeckCodes.Count == 0)
                {
                    Debug.LogWarning("[PopulateCards] Deck is empty even after refill.");
                    break;
                }
            }
            // 덱에서 한 장 뽑아서 hands에 추가
            string drawnCard = playerData.DeckCodes[0];
            playerData.DeckCodes.RemoveAt(0);
            playerData.hands.Add(drawnCard);
        }
    }


    IEnumerator FaceOff()
    {
        faceOffCount++;
        yield return UpdateCycleState(0);
        //주술을 선택한 후에 기다려야하기 때문에~~



        // Begin selection for all players
        pendingSelections.Clear();
        foreach (int actor in players.Keys)
        {

            Hand_Gen(actor);
            ActionPacketData var1 = ActionPacketConverter.FromPlayer(players[actor]);
            
            Player targetP = PhotonNetwork.CurrentRoom.Players[actor];  //쇼다운RPC는 따로 만들기 혹은 매개변수로 조절 (연출용)
            string apjson = JsonConvert.SerializeObject(var1);
        
            
            photonView.RPC(nameof(RPC_FaceOff_ChooseAction_M2C), targetP,actor, faceOffCount,apjson);
        }

        yield return new WaitUntil(() => pendingSelections.Count == players.Count);

        // Register initial queue
      //  actionQueue.Clear();
        foreach (var sel in pendingSelections)
        {
            // 새로 들어오는 액션은, 아직 다른 액션이 실행된 적 없으므로 false로

            InitActionBeforeInsert(sel.Value.action, sel.Key);
            
           
            actionQueue.Add((sel.Key, sel.Value.action, sel.Value.cost));


        }
        actionQueue.Sort((a, b) =>
            a.remainingCost != b.remainingCost
                ? a.remainingCost.CompareTo(b.remainingCost)
                : a.action.nthaction.CompareTo(b.action.nthaction)
        ); pendingSelections.Clear();


    }

   
    IEnumerator After_Action_Selection()
    {


        

        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);

        photonView.RPC(nameof(RPC_After_Action_Selection_Sync_M2C), RpcTarget.All, var1json, var2json, cycleState);
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;


    }









    IEnumerator TurnLoop()
    {

        //생각해보니까
        while (true)
        {
            // New FaceOff phase
            yield return FaceOff();

            // Action processing
            while (actionQueue.Count > 0)
            {
                yield return After_Action_Selection();
           

                TimeGoesOn();

                int readyCount = 0;
                int tempIndex = 0;

                // 도트 뎀 처리 및, 이번 턴 어느 액션 발동 체크 
                while (tempIndex < actionQueue.Count)
                {
                    if (actionQueue[tempIndex].remainingCost != 0)
                    {
                        tempIndex++;
                        continue;
                    }

                    if (actionQueue[tempIndex].action.isDot ==true)
                    {
                        var dotAction = actionQueue[tempIndex].action;
                        var dotActorNum = dotAction.Dot_to;

                        yield return dotAction.Norm_ProcessHook(HookType.Dot, dotActorNum, GetOtherPlayerNumber(dotActorNum), dotAction, null, null);
                        yield return Dot_After_Hook((dotActorNum, dotAction), HookType.Dot);

                        actionQueue.RemoveAt(tempIndex);
                        // tempIndex는 증가하지 않음: RemoveAt 했으니 다음 요소가 자동으로 당겨짐
                    }
                    else
                    {
                        readyCount++;
                        tempIndex++;
                    }
                }
                // Find ready actions





                //readyCount 0이면 다시 continue로 코드 추가하깅 ㅎ
                if(readyCount == 0)
                {
                    continue;
                }





                //경우  1 : 쇼다운
                if (readyCount > 1)
                {
                    //액션 발동 되는 곳1
                    yield return ShowDown();
                    Turn_End_Call();


                    if (actionQueue.Count <= 0 ||
    actionQueue.All(a => a.actorNumber == 0)) // 0만 있는 경우 포함
                    {
                        break; // 다시 페이스 오프
                    }
                    else
                    {
                        var nonZeroActors = actionQueue
                            .Where(a => a.actorNumber != 0)
                            .Select(a => a.actorNumber)
                            .ToList();

                        if (nonZeroActors.Count > 0 && nonZeroActors.All(a => a == nonZeroActors[0]))
                        {
                            int loneActor = nonZeroActors[0];
                            int actorNum = GetOtherPlayerNumber(loneActor);

                            pendingSelections.Clear();

                            Hand_Gen(actorNum);
                            ActionPacketData var = ActionPacketConverter.FromPlayer(players[actorNum]);
                            string apjson = JsonConvert.SerializeObject(var);
                            photonView.RPC(nameof(RPC_Norm_ChooseAction_M2C), RpcTarget.All, actorNum, apjson);
                            yield return new WaitUntil(() => pendingSelections.ContainsKey(actorNum));
                            var newSel = pendingSelections[actorNum];
                            pendingSelections.Remove(actorNum);

                            InitActionBeforeInsert(newSel.action, actorNum);
                            actionQueue.Add((actorNum, newSel.action, newSel.cost));
                            actionQueue.Sort((a, b) =>
                                a.remainingCost != b.remainingCost
                                    ? a.remainingCost.CompareTo(b.remainingCost)
                                    : a.action.nthaction.CompareTo(b.action.nthaction)
                            );

                            continue;
                        }

                        // 이 아래는 (0,1,2 혼재 등) 아무 작업도 하지 않음
                    }
                }


                // 경우 2 : 안 격돌

                //다음 액션 뽑고
                var ttTuple = actionQueue[0];
                if (ttTuple.action.asCombo== false)
                    yield return UpdateCycleState(ttTuple.actorNumber);
              



                //선공 대처 결정
                (int actorNumber, ActionData action, int remainingCost) temp ;
                
                ActionData ttAction = ttTuple.action;
                int ttActorNum = ttTuple.actorNumber;
                
                ActionData ttOpMainAction = null; 
                ActionData ttMainAction = null; //ttAction이 asCombo가 아니라면 이녀석은 계속 널이다

              

                //선공 , 대처, 발동, 격돌은 모두 메인 액션에게서만 발동한다
                for (int i = 0; i < actionQueue.Count; i++)
                {
                    temp = actionQueue[i];
                    if (temp.actorNumber != ttActorNum)
                    {                        
                        if (temp.action.asCombo ==false)
                        {
                            ttOpMainAction = temp.action;
                            if (ttAction.actionId == 1)
                                ttOpMainAction.hasOtherExecutedSinceInsertion = true; //즉 단순 이동으로는 선공을 끊을 수 없다 ㅎ
                        }
                    }
                    else
                    {
                        if(temp.action.asCombo==false && ttAction.asCombo == true)
                        {
                            ttMainAction = temp.action;
                        }
                    }
                } 


                yield return Norm_chooseTile(ttTuple.actorNumber, ttTuple.action);
                //액션 결과 마스터 내부에서 연산해서 player데이터 바꾸는 함수
                yield return Norm_Action_Calc(ttActorNum,  ttAction, ttOpMainAction, ttMainAction);//경계와 발동 처리는 여기서 진행한다
                Turn_End_Call();
                actionQueue.RemoveAt(0);
              

                // Request next move if no pending action
                if (!actionQueue.Exists(a => a.actorNumber == ttActorNum))
                {
                    pendingSelections.Clear();






                    Hand_Gen(ttTuple.actorNumber);
                    ActionPacketData var = ActionPacketConverter.FromPlayer(players[ttTuple.actorNumber]);
                    string apjson = JsonConvert.SerializeObject(var);
                    Debug.Log($"늉늉플레이어 {ttTuple.actorNumber}의 이번 싸이클 값들 : " + string.Join(", ", players[ttTuple.actorNumber].constraintStats.thisCycleActionClocks));

                    Debug.Log($"늉늉플레이어 {ttTuple.actorNumber}의 이전 싸이클 값들 : " + string.Join(", ", players[ttTuple.actorNumber].constraintStats.prevCycleActionClocks));
                    photonView.RPC(nameof(RPC_Norm_ChooseAction_M2C), RpcTarget.All, ttTuple.actorNumber, apjson);
                    yield return new WaitUntil(() => pendingSelections.ContainsKey(ttTuple.actorNumber));


                    var selNew = pendingSelections[ttTuple.actorNumber];
                    pendingSelections.Remove(ttTuple.actorNumber);
                    InitActionBeforeInsert(selNew.action, ttTuple.actorNumber);
                    actionQueue.Add((ttTuple.actorNumber, selNew.action, selNew.cost));
                    actionQueue.Sort((a, b) =>
                        a.remainingCost != b.remainingCost
                            ? a.remainingCost.CompareTo(b.remainingCost)
                            : a.action.nthaction.CompareTo(b.action.nthaction)
                    );
                }
            }
        }
    }



    private void InitActionBeforeInsert(ActionData action, int actorNum)
    {
        globalaction++;

        action.nthaction = globalaction;
        if (action.actionId == 0)
            return;
        action.hasOtherExecutedSinceInsertion = false;
     //   action.InitializeEffects();

    }
    IEnumerator Rumble_Mult_After_Hook(int winner)
    {
      
        //lr 패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);
        //lr 패킷




        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
       
            photonView.RPC(nameof(RPC_Rumble_Play_Multi_Action_M2C), RpcTarget.All, winner, var1json, var2json);

        yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;

    }
    IEnumerator Rumble_Multi_Action_Calc(int ttActorNum1, int ttActorNum2, ActionData ttAction1, ActionData ttAction2, ActionData ttMainAction1, ActionData ttMainAction2, int winner)
        {
       


        // winner 는 액터 넘버 , 0일 경우 그냥 아무 일 도 없음, 3일 경우 둘다 아야함





        // GuardOrCounter는 현재 행동하는 액터의 상대 액터의 큐에 삽입되있는 바로 다음 액션임
        //action이 지금 발동되는 액션

       /* if (ttAction2 != ttMainAction2) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
        {
            yield return ttMainAction2.Norm_ProcessHook(HookType.Guard, ttActorNum2, ttActorNum1, ttAction1, ttMainAction1, ttMainAction2);
            yield return Norm_After_Hook((ttActorNum2, ttMainAction2), HookType.Guard, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌
        }
       */

        yield return ttAction1.Norm_ProcessHook(HookType.Activate, ttActorNum1, ttActorNum2, ttAction1, ttMainAction2, ttMainAction1);
        //yield return Norm_After_Hook((ttActorNum1, ttAction1), HookType.Activate, false, null);

        yield return ttAction2.Norm_ProcessHook(HookType.Activate, ttActorNum2, ttActorNum1, ttAction2, ttMainAction1, ttMainAction2);
        //yield return Norm_After_Hook((ttActorNum2, ttAction2), HookType.Activate, false, null);

        yield return Rumble_Mult_After_Hook(winner);

        //요부분만 노말 훅이아니라 럼블 훅으로 바꿔주면 될지도?


        /*  if (ttAction2 != ttMainAction2) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
          {

              yield return ttMainAction2.Norm_ProcessHook(HookType.Counter, ttActorNum2, ttActorNum1, ttAction1, ttMainAction1, ttMainAction2);
              yield return Norm_After_Hook((ttActorNum2, ttMainAction2), HookType.Counter, false, null);
          }
        */


        /*     if (ttAction1 != ttMainAction1) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
             {
                 yield return ttMainAction1.Norm_ProcessHook(HookType.Guard, ttActorNum1, ttActorNum2, ttAction2, ttMainAction2, ttMainAction1);
                 yield return Norm_After_Hook((ttActorNum1, ttMainAction1), HookType.Guard, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌
             }
        */



        //요부분만 노말 훅이아니라 럼블 훅으로 바꿔주면 될지도?

        /*
                if (ttAction1 != ttMainAction1) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
                {

                    yield return ttMainAction1.Norm_ProcessHook(HookType.Counter, ttActorNum1, ttActorNum2, ttAction2, ttMainAction2, ttMainAction1);
                    yield return Norm_After_Hook((ttActorNum1, ttMainAction1), HookType.Counter, false, null);
                }
        */




    }
    IEnumerator Rumble_Single_Action_Calc(int ttActorNum, ActionData ttAction, ActionData ttOpAction, ActionData ttOpMainAction, ActionData ttMainAction, int winnerNum)
    {
        // 1, 2 승자 존재: 승자 번호
        // 3: 동시발동
        // 4: 00후 처맞기
        // 5: 처맞고 00하기하기
        //6 : 아슬아슬 피하기
        //7: 비김
        //3 : 원래 안맞는 놈 , 두 플레이어 모두 이동 , 둘다 헛발질
        //애니메이션 재생 순서가 중요한 경우
        //00후 처맞기, 처맞고 00하기, 아슬아슬 피하기
        //그 외에는 두 액션 모두 동시에 출력되도 됨, 
        int ttOpActorNum = GetOtherPlayerNumber(ttActorNum);

        // GuardOrCounter는 현재 행동하는 액터의 상대 액터의 큐에 삽입되있는 바로 다음 액션임
        //action이 지금 발동되는 액션
/*
        if (ttOpAction != ttOpMainAction) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
        {      
            yield return ttOpMainAction.Norm_ProcessHook(HookType.Guard, ttOpActorNum, ttActorNum, ttAction, ttMainAction, ttOpMainAction);
            yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Guard, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌
        }

        */
        yield return ttAction.Norm_ProcessHook(HookType.Activate, ttActorNum, ttOpActorNum, ttAction, ttOpMainAction, ttMainAction);
        yield return Rumble_Single_After_Hook((ttActorNum, ttAction), HookType.Activate,  null);


        //요부분만 노말 훅이아니라 럼블 훅으로 바꿔주면 될지도?



     /*   if (ttOpAction != ttOpMainAction) //지금 격돌하는 상대의 액션이 메인 아닐때만 가드, 카운터 발동 
        {

        yield return ttOpMainAction.Norm_ProcessHook(HookType.Counter, ttOpActorNum, ttActorNum, ttAction, ttMainAction, ttOpMainAction);
        yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Counter, false, null);
        }*/
        //nth 액션이랑 비교해서 순서대로 실행하기 
        //여기선 action의 반대 플레이어가 카운터의 주체이므로 헷갈리지 말자 ㅎㅎ

        //  yield return MasterForEveryIntheQueAfterCalc(action, actorNum, myrightNextAction, GuardOrCounter);

        //애프터 액션 플래그 같은거 주면 될 듯 

        // Turn_End_Call();
    }
    IEnumerator Norm_Action_Calc(int ttActorNum, ActionData ttAction,  ActionData ttOpMainAction,ActionData ttMainAction)
    {
        //내일 여기 부터 작업 하면됨 .
        int ttOpActorNum = GetOtherPlayerNumber(ttActorNum);

        // GuardOrCounter는 현재 행동하는 액터의 상대 액터의 큐에 삽입되있는 바로 다음 액션임
        //action이 지금 발동되는 액션



        yield return ttOpMainAction.Norm_ProcessHook(HookType.Guard, ttOpActorNum, ttActorNum, ttAction, ttMainAction, ttOpMainAction);
        yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Guard, GA_On, ttAction); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌



        //action.ProcessHook(HookType.Activate, actorNum, )
        yield return ttAction.Norm_ProcessHook(HookType.Priority, ttActorNum, ttOpActorNum, ttAction, ttOpMainAction, ttMainAction);
        yield return ttAction.Norm_ProcessHook(HookType.Activate, ttActorNum, ttOpActorNum, ttAction, ttOpMainAction, ttMainAction);

        //어쩔수 없다 하드 코딩
        //히트 미스 디펜드
     /*   bool m_issed = true;
        foreach (var tile in ttAction.effectTiles)
        {
            if (tile == Overmind.Instance.players[ttOpActorNum].curpos)
            {
                m_issed = false;
                break;
            }
        }
        if (m_issed)
        {
            ttAction.resolvedOutcome = HitResolution.Missed;
        }
        else
        {
            if (ttAction.damage > ttOpMainAction.defense)
            {
                ttAction.resolvedOutcome = HitResolution.Damage;

            }
            else if (ttAction.damage <= ttOpMainAction.defense)
            {
                ttAction.resolvedOutcome = HitResolution.Defended;
            }


        }

        */
        yield return Norm_After_Hook((ttActorNum, ttAction), HookType.Activate, GA_On, null);

        yield return  ttOpMainAction.Norm_ProcessHook(HookType.Counter, ttOpActorNum, ttActorNum, ttAction, ttMainAction, ttOpMainAction);
        yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Counter, GA_On, null);

        //nth 액션이랑 비교해서 순서대로 실행하기 
        //여기선 action의 반대 플레이어가 카운터의 주체이므로 헷갈리지 말자 ㅎㅎ

        //  yield return MasterForEveryIntheQueAfterCalc(action, actorNum, myrightNextAction, GuardOrCounter);

        //애프터 액션 플래그 같은거 주면 될 듯 

        // Turn_End_Call();
        
        //급한대로 하드 코딩 어쩔 수 없다 
        //어차피 가드는 노멀 액션에서만 발동되니 걱정 ㄴ

        GA_On = false;
    }



    //이것도 그냥 딴데로 옮길까..
    private void Turn_End_Call()
    {
        foreach (var p in players)
        {
            // p.Value.prevHP = p.Value.HP;
            p.Value.isInvincible = false;

        }


    }
   


    /*public void MasterBeforeRumbleCalc(int actorNum, ActionData myaction, ActionData opaction)
    {
        //카드의 경우
        if (myaction.actionId == 1)
        {


            //여기서 넘겨주는 훅타입은 호출에 필요한 훅타입이여! 여기는 발동 부의 체커니까
            //action.ProcessHook(HookType.Activate, actorNum, )

            myaction.ProcessHook(HookType.BeforeRumble, actorNum, null, GetOtherPlayerNumber(actorNum),opaction, null);//다음 플레이어의 액션을 넘겨주는건 추가 함수 작성하자
//            yield return myaction.Norm_ProcessHook(HookType.BeforeRumble, ttActorNum2, ttActorNum1, ttAction1, ttMainAction1, ttMainAction2);

        }

    }
    */

    //이게 로컬 스테이트에서 실행되어야한다
    public void SubmitSelection(ActionData action, int cost, int actorNum, BacktoMaster btmPacket)
    {


        string actionjson = JsonConvert.SerializeObject(action);
        string btmjson = JsonConvert.SerializeObject(btmPacket);

        photonView.RPC(nameof(RPC_Receive_Action_Selection_C2M), RpcTarget.MasterClient, actorNum, actionjson, cost, btmjson);

    }




    [PunRPC]
    void RPC_Receive_Action_Selection_C2M(int actorNumber, string actionjson, int cost, string btmsjon)
    {

        var action = JsonConvert.DeserializeObject<ActionData>(actionjson);
        var btm = JsonConvert.DeserializeObject<BacktoMaster>(btmsjon);



        Action_Selection_Result_Calc(actorNumber, btm, action);



        pendingSelections[actorNumber] = (action, cost);

    }





    //summary//
    //해당 플레이어의 손패, 트래쉬, 제약 조건을 위한 변수 건드는 장소
    //레이스 컨디션 발동하지 않게 자기의 액터넘버만 건드려라
    private void Action_Selection_Result_Calc(int actorNum, BacktoMaster btm , ActionData action)
    {
        var myPlayerData = players[actorNum];
        var trash = new List <string> (btm.usedCard);
        // trash에 있는 카드들을 hands에서 하나씩만 제거하고, trash에 추가
        foreach (var code in trash)
        {
            if (myPlayerData.hands.Contains(code))
            {
                myPlayerData.hands.Remove(code);


                //메인 액션 만 트래쉬로 가게 해논거임
                //만약에 사용한 서포트카드나 
                if (CardCSVLoader.Instance.GetCardByCode(code).disappear == 0)
                    myPlayerData.trash.Add(code);
                else
                {
                    Debug.Log($"{CardCSVLoader.Instance.GetCardByCode(code).name}은 소멸 카드라 사라진다 트래쉬로도 안감;");

                }
            }
            else
            {
                Debug.Log($"레전드 상황발생 {code}란 카드는 손패에 없는데 버리려고한다 ;;");
            }
        }


        //constraints
        if (action.actionId != 0)
        {
            // summary //
            myPlayerData.constraintStats.actionUsedHowmany++;

            int opActorNum = GetOtherPlayerNumber(actorNum);
            int maxRemainingCost = actionQueue
                .Where(entry => entry.actorNumber == opActorNum)
                .Select(entry => entry.remainingCost)
                .DefaultIfEmpty(0)
                .Max();

            int diff = Mathf.Abs(maxRemainingCost - action.actionClock);
            myPlayerData.constraintStats.actionClockDiffer.Add(diff);
           myPlayerData.constraintStats.thisCycleActionClocks.Add(action.actionClock);

        }
        //이동은 발동 될 때가 아닌 낼 때 안되는 것  
        else
        {
            myPlayerData.canMove = false;
        }


        //apProp초기화
        foreach (apProp prop in Enum.GetValues(typeof(apProp)))
        {
            if (prop.ToString().Contains("temp") && players[actorNum].forActionPacket.ContainsKey(prop))
            {
                players[actorNum].forActionPacket[prop] = 0;
            }
        }



    }





    public void SubmintRenderingDone(int actorNumber)
    {
        photonView.RPC(nameof(RPC_SyncDone), RpcTarget.MasterClient, actorNumber);
    }

    
  

    [PunRPC]
    void RPC_FaceOff_ChooseAction_M2C(int actorNumber,  int nthFaceOff, string apjson)
    {
        //여기서 방어도 렌더링 
        //싸이클 리셋터
        LocalRenderingManager.Instance.Rendering_FaceOff_Start(nthFaceOff);
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LocalState.Instance?.Start_FaceDown_Phase(actorNumber, apjson);
        }
     

    }

    [PunRPC]
    void RPC_Norm_ChooseAction_M2C(int actorNumber, string apjson)
    {
        //여기서 방어도 렌더링
        //여기서 방어도 렌더링 
        //싸이클 리셋터
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LocalState.Instance?.Start_NormChoose_Phase(actorNumber, apjson);
        }
        //니아이디면 남의 선택 기다리는 애니메
        else
        {

            //기다리는 함수 렌도링
            //기다리는 함수 구현 아이디어
            //playaction RPC들어옴과 동시에 localplayer가 waiting 중이었다면
            //해당 코루틴을 끝내버리기 . 
        }




    }

    IEnumerator Norm_chooseTile(int actornum, ActionData action)
    {
        //lr패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);



        photonView.RPC(nameof(RPC_Rendering_Before_ChooseTile_M2C), RpcTarget.All, var1json, var2json, actornum);
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;
        


        if (action.actionId != 1)
        {
            yield break;
        }

     
        //

        //액션제이선
       string actionJson = JsonConvert.SerializeObject(action);
       photonView.RPC(nameof(RPC_Norm_ChooseTile_M2C), RpcTarget.All, actornum, actionJson);
         
        yield return new WaitUntil(() => pendingTiles.Count ==1);

        foreach (var (actorNum, tiles) in pendingTiles)
        {
            if (actornum == actorNum)
            {
                action.effectTiles = new List<int>(tiles);
            }
            
        }

        if(pendingTiles.Count > 1)
          Debug.Log("격돌이 아닌데 타일을 두명이 보냇다 긴급상황 발생");




        pendingTiles.Clear();
    }

    IEnumerator Showdown_chooseTile ((int actornum, ActionData action)p1, (int actornum, ActionData action)p2)
    {

        var templist = new List<(int actornum, ActionData action)>();
        if (p1.action != null) templist.Add(p1);
        if (p2.action != null) templist.Add(p2);

        int howmany = 0;
        List<string> actionJsonList = new List<string>();

        foreach (var (actNum, action) in templist)
        {//여기 고치면 됨
            if (action.actionId == 1)
                howmany++;

            var packet = new Dictionary<string, object>
                 {
                  { "actorNumber", actNum },
                    { "action", action }
                };
                 actionJsonList.Add(JsonConvert.SerializeObject(packet));
            
        }

        // 렌더링 정보
        //lr패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);
        //

        // RPC 호출 (순서 상관없이 정보 포함)
        photonView.RPC(nameof(RPC_ShowDown_ChooseTile_M2C),
            RpcTarget.All,
            actionJsonList.ToArray(), // string[]
            var1json,
            var2json
        );

        yield return new WaitUntil(() => pendingTiles.Count == howmany);

        foreach (var (actorNum, tiles) in pendingTiles)
        {
            if (p1.actornum == actorNum)
                p1.action.effectTiles = new List<int>(tiles);
            else if (p2.actornum == actorNum)
                p2.action.effectTiles = new List<int>(tiles);
        }

        pendingTiles.Clear();
    }
























    [PunRPC]
    void RPC_Norm_Play_Action_M2C(int actorNumber, string actionJson, string lrjson1, string lrjson2, HookType h, string actionJson2, bool isGA)
    {
        //actionJson2 는 가드시 상대 액션 애니메용
        // GA_On은 가드 애니메이션 재생 여부 결정용
        var action = JsonConvert.DeserializeObject<ActionData>(actionJson);
        var action2 = JsonConvert.DeserializeObject<ActionData>(actionJson2);
        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);

        LocalRenderingManager.Instance.Rendering_Norm_Action(actorNumber, data1, data2, action, h, action2, isGA );

    }

    [PunRPC]
    void RPC_Rumble_Play_Single_Action_M2C(int actorNumber, string actionJson, string lrjson1, string lrjson2, HookType h, string actionJson2 )
    {
        //actionJson2 는 가드시 상대 액션 애니메용
        var action = JsonConvert.DeserializeObject<ActionData>(actionJson);
        var action2 = JsonConvert.DeserializeObject<ActionData>(actionJson2);
        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);

        LocalRenderingManager.Instance.Rendering_Rumble_Single_Action(actorNumber, data1, data2);

    }


    [PunRPC]
    void RPC_Rumble_Play_Multi_Action_M2C(int winner,  string lrjson1, string lrjson2 )
    {

        //winner 0 no one 1 2, actor ,m 3 both dam
        //actionJson2 는 가드시 상대 액션 애니메용
        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);

        LocalRenderingManager.Instance.Rendering_Rumble_Single_Action(winner, data1, data2);

    }

    [PunRPC]
    void RPC_Dot_Play_Action_M2C(int actorNumber, string actionJson, string lrjson1, string lrjson2, HookType h)
    {

        var action = JsonConvert.DeserializeObject<ActionData>(actionJson);
        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);

        LocalRenderingManager.Instance.Rendering_Dot_Action(actorNumber, data1, data2, action, h);

    }












    [PunRPC]

    void RPC_Rendering_Before_ChooseTile_M2C(string lrjson1, string lrjson2, int whoSelect)
    {
        //이동 때문에 이녀석이 불가피하게 되었다

        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);
        LocalRenderingManager.Instance.Rendering_Before_Tile_Choose( data1, data2, whoSelect);

    }

    [PunRPC]

    void RPC_Extra_Selection_M2C(ExtraSelection e, int actornum)
    {

        //else에서는 선택 아닌놈이 동기화 쏴주나..?


        if (actornum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //상대 생각중!

        }

        else
        {

            ExtraSelectionState.Instance.SetActive(true, e);
        }

    }

    [PunRPC]

    void RPC_Norm_ChooseTile_M2C(int actornum, string actioJson)
    {
        //일단 렌더링은 둘다 해주고 


        var action = JsonConvert.DeserializeObject<ActionData>(actioJson);

        // var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        //var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);
        //LocalRenderingManager.Instance.Rendering_Before_Tile_Choose(actornum, data1, data2, action);
        //
      //  LocalRenderingManager.Instance.Rendering_Tile_Choose(actornum, action);
        if (actornum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //상대 생각중!

           var  chooserOb = LocalState.Instance?.PlayerObDic[actornum];
            var tile_chooser_anim = chooserOb.GetComponentInChildren<Animator>();
            if (tile_chooser_anim == null)
            {
                Debug.LogError("난 처맞을 건데, 공격자 Animator 없음");
           
                 return;
            }
            tile_chooser_anim.SetTrigger("Trig_TileChoose");


        }

        else
        {

            CardChooseTile.Instance.SetActive(true, action);

        }

    }


    public void ExtraSelection_Caller(ExtraSelection e, int actorNum)
    {

        photonView.RPC(nameof(RPC_Extra_Selection_M2C), RpcTarget.All, e, actorNum);

    }



  




    [PunRPC]

    void RPC_ShowDown_ChooseTile_M2C(string[] actionJsonList, string renderJson1, string renderJson2)
    {
        List<(int actorNum, ActionData action)> actionList = new List<(int, ActionData)>();

        foreach (var actionJson in actionJsonList)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(actionJson);
            int actorNumber = Convert.ToInt32(dict["actorNumber"]);
            var action = JsonConvert.DeserializeObject<ActionData>(dict["action"].ToString());

            actionList.Add((actorNumber, action));
        }

        // 렌더링 데이터도 마찬가지로
        var r1 = JsonConvert.DeserializeObject<LocalRenderingData>(renderJson1);
        var r2 = JsonConvert.DeserializeObject<LocalRenderingData>(renderJson2);

        
       var myAction =  LocalRenderingManager.Instance.Rendering_Before_Tile_Choose_ShowDown(actionList, r1, r2);
        //상대 캐릭터 타일 추즈 애니메

        var opGo = LocalState.Instance.PlayerObDic[GetOtherPlayerNumber(PhotonNetwork.LocalPlayer.ActorNumber)];
        var opAnim = opGo.GetComponentInChildren<Animator>();
        opAnim.SetTrigger("Trig_TileChoose");


        //조건을 내 액션은 이동일 경우에는 암것도 안하는 걸로 해야한다리오리우스
        if (myAction.actionId !=1)
        {
            //상대 생각중!
            //여기서 거르기 때문에 ㄴshow_down_ChoooseTIle 수정 가능


        }
        else
        {
            
            CardChooseTile.Instance.SetActive(true, myAction);

        }
         
    }


    


    public void SendTileandDirec(int actorNum, List<int> tiles, string direc)
    {

        string tilejson = JsonConvert.SerializeObject(tiles);
        photonView.RPC(nameof(RPC_ReceiveTile_C2M),RpcTarget.MasterClient, actorNum,tilejson, direc  );



    }
    public void Send_Extra_Selection(string json)
    {
        photonView.RPC(nameof(RPC_Receive_Extra_Select_C2M), RpcTarget.MasterClient, json);
    }
    [PunRPC]
    void RPC_Receive_Extra_Select_C2M(string esJson)
    {
        extraSelectionJson = esJson;
         Debug.Log($"엑스트라 셀렉션이 잘 도착했다");

    }
    [PunRPC]
    void RPC_ReceiveTile_C2M(int actorNum, string tileListJson, string mouseSide)
    {

        Debug.Log($"[Master] RPC_ReceiveTile 호출됨! actorNum={actorNum}, tileList={tileListJson}");
        List<int> deser = JsonConvert.DeserializeObject<List<int>>(tileListJson);
        pendingTiles.Add((actorNum, deser));
        Debug.Log($"[Master] pendingTiles.Count={pendingTiles.Count}");

        if (mouseSide!= null)
        {

            players[actorNum].rightOrLeft = mouseSide;
        }

    }


    //해당 액션 추가 선택 까지 이 안에서 구현하면 됩니다 
    IEnumerator Norm_After_Hook((int actorNum, ActionData action) Nowhooker, HookType h, bool isGA, ActionData opAction)
    {
        if (!Nowhooker.action.effects.Any(effect => effect.hookType == h)) //이번 액션에 해당 훅없으면 스킵.
            yield break;

        //lr 패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);
        //lr 패킷




        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
        string actionJson = JsonConvert.SerializeObject(Nowhooker.action);
        string actionJson2 = JsonConvert.SerializeObject(opAction);
        Debug.Log($"{Nowhooker.action.cardname}의 렌더링하라고 마스터 클라이언트 요청 :: 렐렐");
        photonView.RPC(nameof(RPC_Norm_Play_Action_M2C), RpcTarget.All, Nowhooker.actorNum, actionJson, var1json, var2json, h, actionJson2, isGA);
        
         yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;

    }
    IEnumerator Rumble_Single_After_Hook((int actorNum, ActionData action) Nowhooker, HookType h,  ActionData opAction)
    {
        if (!Nowhooker.action.effects.Any(effect => effect.hookType == h)) //이번 액션에 해당 훅없으면 스킵.
            yield break;

        //lr 패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);
        //lr 패킷




        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
        string actionJson = JsonConvert.SerializeObject(Nowhooker.action);
        string actionJson2 = JsonConvert.SerializeObject(opAction);
        Debug.Log($"{Nowhooker.action.cardname}의 렌더링하라고 마스터 클라이언트 요청 :: 렐렐");
        photonView.RPC(nameof(RPC_Rumble_Play_Single_Action_M2C), RpcTarget.All, Nowhooker.actorNum, actionJson, var1json, var2json, h, actionJson2);

        yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;

    }

    IEnumerator Dot_After_Hook((int actorNum, ActionData action) Nowhooker, HookType h)
    {
        if (!Nowhooker.action.effects.Any(effect => effect.hookType == h)) //이번 액션에 해당 훅없으면 스킵.
            yield break;

        //lr 패킷
        var keys = players.Keys.ToList();

        if (keys.Count < 2)
        {
            Debug.LogError("플레이어 수가 2명 미만입니다.");
            yield break;
        }

        int var1ActorNum = keys[0];
        int var2ActorNum = keys[1];

        LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
        LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

        string var1json = JsonConvert.SerializeObject(var1);
        string var2json = JsonConvert.SerializeObject(var2);
        //lr 패킷


        Debug.Log($"{Nowhooker.action.cardname}의 렌더링하라고 마스터 클라이언트 요청 :: 렐렐");


        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
        string actionJson = JsonConvert.SerializeObject(Nowhooker.action);
        photonView.RPC(nameof(RPC_Dot_Play_Action_M2C), RpcTarget.All, Nowhooker.actorNum, actionJson, var1json, var2json, h);

        yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;

    }







    public void  SyncDone()
    {
        syncCount++;

    }

    [PunRPC]
    void RPC_SyncDone(int actorNumber)
    {
        SyncDone();

    }


    //액션결과 렌더링하는 함수.. 큭큭..


    private void TimeGoesOn()
    {
        //actionqueue에서 발동되는 액션만큼의 remainingcost를 소모하는 함수
        // 정렬되있응께 맨 앞놈이 Min코스트
        int minCost = actionQueue[0].remainingCost;
        if (minCost == 0)
            return;
        for (int i = 0; i < actionQueue.Count; i++)
            actionQueue[i] = (actionQueue[i].actorNumber,
                              actionQueue[i].action,
                              actionQueue[i].remainingCost - minCost);

    }
   
    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public int GetOtherPlayerNumber(int actorNumber) //이거 일단 플레이어가 둘 뿐이라 가능
    {
        // actorNumber가 1이면 2, 2면 1을 반환
        return actorNumber == 1 ? 2 : 1;
    }

    public int GetMinOpponentRemainingCost(int actorNum)
    {
        // actorNum과 다르고, 남은 코스트를 뽑아서
        var opponentCosts = actionQueue
            .Where(entry => entry.actorNumber != actorNum)
            .Select(entry => entry.remainingCost);

        // 만약 상대 액션이 없다면 0을 반환 (필요하다면 다른 기본값으로 바꿔도 됨)
        if (!opponentCosts.Any())
            return 0;

        // 그중 최솟값 반환
        return opponentCosts.Min();
    }
    /// <summary>
    /// 두 플레이어가 동시에 남은 Cost == 0이 되어 충돌할 때 호출
    /// </summary>
    private IEnumerator ShowDown()
    {

        photonView.RPC(nameof(RPC_CycleSync_M2C), RpcTarget.All, -1, faceOffCount);
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;


        int showdownCircFlag = 0;

        // 1, 2 승자 존재: 승자 번호 3: 동시발동 4: 00후 처맞기 5: 처맞고 00하기하기
        //6 : 아슬아슬 피하기 7: 비김
        //3 : 원래 안맞는 놈 , 두 플레이어 모두 이동 , 둘다 헛발질
        //애니메이션 재생 순서가 중요한 경우
        //00후 처맞기, 처맞고 00하기, 아슬아슬 피하기
        //그 외에는 두 액션 모두 동시에 출력되도 됨, 

        //가드 훅
        // 1) 맞붙는 두 액션 빼내기
        var first = actionQueue[0];
        var second = actionQueue[1];

        var a1 = (actor: first.actorNumber, action: first.action);
        var a2 = (actor: second.actorNumber, action: second.action);

        // 2) BeforeRumble 훅 처리 (럼블 포인트 계산용)

        // 3) 이동인지 액션인지
        bool isAction1 = a1.action.actionId == 1;
        bool isAction2 = a2.action.actionId == 1;


        ActionData a1ttAction = a1.action;
        ActionData a2ttAction = a2.action;
        ActionData a1MainAction = null;
        ActionData a2MainAction = null;

        // 1. 큐 내 모든 액션의 hasOtherExecutedSinceInsertion = true
        foreach (var tuple in actionQueue)
        {
            tuple.action.hasOtherExecutedSinceInsertion = true;
        }

        // 2. 메인 액션 판별 (asCombo == false)
        foreach (var tuple in actionQueue)
        {
            if (!tuple.action.asCombo)
            {
                if (tuple.actorNumber == a1.actor)
                {
                    a1MainAction = tuple.action;
                }
                else if (tuple.actorNumber == a2.actor)
                {
                    a2MainAction = tuple.action;
                }
            }
        }


        
        yield return Showdown_chooseTile((a1.actor, a1.action), (a2.actor,a2.action));

               //추가사항//

        //여기에 두 캐릭터 컷신 뙁 하며 나오면서 맞붙는 연출 쏴주는 RPC 작성하기 ㅎ ㅎ//

        //추가사항//

        // 두 액션 모두 액션인 경우만 진짜 격돌
        if (isAction1 && isAction2)
        {


            //yield return MasterBeforeRumbleCalc(a1.actor, a1.action, a2.action);
            //yield return MasterBeforeRumbleCalc(a2.actor, a2.action, a1.action);




            //저 널 자리는 이번 턴에 발동되는 액션 자리인데, 격돌에는 2개이므로, 일단 null로
            //왜냐면 살펴본 바로는 비포 럼블에서는 딱히 저길 건드리는 코드가 없음 .. .
            yield return a1ttAction.Norm_ProcessHook(HookType.BeforeRumble, a1.actor, a2.actor, null, a2MainAction, a1MainAction);
            yield return Norm_After_Hook((a1.actor, a1ttAction), HookType.BeforeRumble, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌

            yield return a2ttAction.Norm_ProcessHook(HookType.BeforeRumble, a2.actor, a1.actor, null, a1MainAction, a2MainAction);
            yield return Norm_After_Hook((a2.actor, a2ttAction), HookType.BeforeRumble, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌













            //각각의 럼블포인트 계산
            int rumble1 = isAction1 ? a1.action.rumblePoint : int.MinValue;
            int rumble2 = isAction2 ? a2.action.rumblePoint : int.MinValue;

            // 3.1 진짜 격돌: 서로의 범위 안에 있는지 확인
            bool inRange1 = IsInEffectRange(a1.actor, a1.action, a2.actor);
            bool inRange2 = IsInEffectRange(a2.actor, a2.action, a1.actor);

            if (inRange1 && inRange2)
            {
                // 3.1.1 승패 결정
                if (rumble1 > rumble2)
                {

                    showdownCircFlag = a1.actor;
                    yield return Rumble_Single_Action_Calc(a1.actor, a1.action,a2.action, a2MainAction, a1MainAction,  showdownCircFlag);
                    yield return a1ttAction.Norm_ProcessHook(HookType.RumbleWin, a1.actor, a2.actor, null, a2MainAction, a1MainAction);
                    yield return Norm_After_Hook((a1.actor, a1ttAction), HookType.RumbleWin, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌

                }
                else if (rumble2 > rumble1)
                {
                    showdownCircFlag = a2.actor;
                    yield return Rumble_Single_Action_Calc(a2.actor, a2.action, a1.action, a1MainAction, a2MainAction, showdownCircFlag);

                    yield return a2ttAction.Norm_ProcessHook(HookType.RumbleWin, a2.actor, a1.actor, null, a1MainAction, a2MainAction);
                    yield return Norm_After_Hook((a2.actor, a2ttAction), HookType.RumbleWin, false, null); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌


                }
                else
                {

                    showdownCircFlag = 7;
                    //비기면 그냥 튕겨나가자 .. 아무 일도 일어나지 않고
                    //yield return Rumble_Action_Calc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);
                    //여기엔 걍 멀티 액션 박는게 맞을지도?
                    yield return Rumble_Multi_Action_Calc(a1.actor, a2.actor, a1.action, a2.action, a1MainAction, a2MainAction, 3);

                }

            }


            //applyunilateralhit 이랑 어차피 동작이 똑같음
            else if (inRange1)
            {
                //생각해보면 안맞추면 발동된다는게 너무 개사기 같아서 
                showdownCircFlag = 5;
                yield return Rumble_Single_Action_Calc(a1.actor, a1.action, a2.action, a2MainAction, a1MainAction, a1.actor);

            }

            else if (inRange2)
            {

                //생각해보면 안맞추면 발동된다는게 너무 개사기 같아서 
                showdownCircFlag = 4;
                yield return Rumble_Single_Action_Calc(a2.actor, a2.action, a1.action, a1MainAction, a2MainAction, a2.actor);

            }
            else {
                //둘다 빗나감
                showdownCircFlag = 0;
                //뭔가 젖병신같음 ㅋ
                //yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);
                //여기엔 걍 멀티 액션 박는게 맞을지도?
                yield return Rumble_Multi_Action_Calc(a1.actor, a2.actor, a1.action, a2.action, a1MainAction, a2MainAction, showdownCircFlag);

            }
        }
        else
        {
            if (isAction1 ^ isAction2) // 한명은 이동 한명은 액션인 경우이다
            {
                int prevPos = players[a1.actor].curpos;
                if (a1.action.actionId == 0)//이동 , 액션 순서
                {

                    if (IsInEffectRange(a2.actor, a2.action, a1.actor))
                    {
                        //이동 후 처 맞기 플래그 on
                        showdownCircFlag = 2;

                    }
                    else
                    {
                        if (a2.action.effectTiles.Contains(prevPos))
                        {
                            //that was close 플래그 온
                            showdownCircFlag = 0;
                        }
                        else   //원래 안맞는놈
                        {       
                            showdownCircFlag = 0;
                        }

                    }
                }

                else //액션 , 이동 순서 a1이 액션, a2가 무빙
                {
                    if (IsInEffectRange(a1.actor, a1.action, a2.actor))
                    {
                        //개같이 처맞음 플래그 온
                        //처맞고 이동On
                        showdownCircFlag = 1;

                    }
                    else
                    {
                        //헛손질 후 이동 on
                        showdownCircFlag = 0;
                    }
                }

                yield return Rumble_Multi_Action_Calc(a1.actor, a2.actor, a1.action, a2.action, a1MainAction, a2MainAction, showdownCircFlag);


            }
            else //둘다 평화로운 이동이다 
            {
                showdownCircFlag = 3;
                yield return Rumble_Multi_Action_Calc(a1.actor, a2.actor, a1.action, a2.action, a1MainAction, a2MainAction, 0);


            }
        }

        Debug.Log("자여기까지 몇마릴까");
        actionQueue.RemoveRange(0, 2);


    }

    /// <summary>두 액션이 서로의 효과 범위에 들어왔는지 체크</summary>
    private bool IsInEffectRange(int sourceActor, ActionData sourceAction, int targetActor)
    {
        var srcTiles = sourceAction.effectTiles;
        int pos = players[targetActor].curpos;
        return srcTiles.Contains(pos);
    }

    IEnumerator UpdateCycleState(int newState) {
        //ttMainAction이 asCombo면 이라는 경우를 생각하셈
        if (cycleState == -1)
        {
            cycleState = newState;


            photonView.RPC(nameof(RPC_CycleSync_M2C), RpcTarget.All, newState, faceOffCount);
            yield return new WaitUntil(() => syncCount == 2);
            syncCount = 0;


            yield break;
        }

        if (cycleState != newState)
        {
            if (newState == 0)
            {
                players[1].canMove = true;
                players[2].canMove = true;
            }
            else if (newState == 1)
            {
              //  players[newState].isStealthed = false;
                players[newState].canMove = true;
            }
            else if (newState == 2)
            {

              //  players[newState].isStealthed = false;
                players[newState].canMove = true;
            }



            //여기서 cycleState에 대해서 boundchecker호출!@!

           if (cycleState != 0)
            {
             var BC_ret =    BoundChecker.BoundCheck(cycleState);
                //    yield return new WaitUntil(() => syncCount == 2);
                //   syncCount = 0;

                CallRPCJuju(cycleState, BC_ret.ret_bound, BC_ret.ret_check);
                yield return new WaitUntil(() => syncCount == 2);
                Debug.Log("여기 못온거잖아 그치?");
                syncCount = 0;
                BoundChecker.After_CallRPCJuju(cycleState);
                //여기서 불러야함 콜주주랑
                //웨이트 여기서 걸어주고 ㅋ.ㅋ.ㅋ.ㅋ .ㅋ .ㅋ
                //애프터 


                // 바운드 & 주술 결과 동기화
                var keys = players.Keys.ToList();

                if (keys.Count < 2)
                {
                    Debug.LogError("플레이어 수가 2명 미만입니다.");
                    yield break;
                }

                int var1ActorNum = keys[0];
                int var2ActorNum = keys[1];

                LocalRenderingData var1 = RenderingConverter.FromPlayer(players[var1ActorNum]);
                LocalRenderingData var2 = RenderingConverter.FromPlayer(players[var2ActorNum]);

                string var1json = JsonConvert.SerializeObject(var1);
                string var2json = JsonConvert.SerializeObject(var2);

                photonView.RPC(nameof(RPC_JujuSync_M2C), RpcTarget.All, var1json, var2json);
                            yield return new WaitUntil(() => syncCount == 2);
                            syncCount = 0;
            }

            //  BoundChecker.BoundParamReset();
            photonView.RPC(nameof(RPC_CycleSync_M2C), RpcTarget.All, newState, faceOffCount);
            yield return new WaitUntil(() => syncCount == 2);
            syncCount = 0;


        }
        else
        {
            if (newState == 0) //페이스오프 이후 즉시 격돌한 경우  
            {
                players[1].energy++;
                players[1].canMove = true;
                players[2].energy++;
                players[2].canMove = true;


                photonView.RPC(nameof(RPC_CycleSync_M2C), RpcTarget.All, newState, faceOffCount);
                yield return new WaitUntil(() => syncCount == 2);
                syncCount = 0;

            }


        }









        cycleState = newState;






        





    }


    
    //summary//
    //오버마인드에서 제약 코드에 맞게 생성할 주술 선택지를 클라이언트에게 넘겨주기 위한 연산//
     public void  CallRPCJuju(int actorNum, string boundCode, bool missionclear)
    {

        int boundPointThreshold = BoundLoader.boundDataBase[boundCode].boundPoint;

        // 필터링// 
        List<string> filteredJujuCodes = new List<string>();
        foreach (var jujuCode in players[actorNum].JujuCode)
        {
            if (playerJujuData[actorNum].TryGetValue(jujuCode, out var juju))
            {
                // 1. boundPoint 기준 필터
                if (juju.boundPoint > boundPointThreshold)
                    continue;

                // 2. require 조건 검사
                if (juju.require != "none")
                {
                    // 요구되는 주술코드가 존재하는지 검사
                    if (playerJujuData[actorNum].TryGetValue(juju.require, out var requiredJuju))
                    {
                        if (requiredJuju.isUsed != 1)
                            continue; // 아직 사용되지 않았으면 필터링 탈락
                    }
                    else
                    {
                        // 요구하는 주술 자체가 없으면 탈락
                        continue;
                    }
                }

                // 3. onlyOnce 제한 검사
                if (juju.onlyOnce == 1 && juju.isUsed == 1)
                    continue;

                // 통과한 경우에만 추가
                filteredJujuCodes.Add(jujuCode);
            }
        }
        Debug.Log($"총 {filteredJujuCodes.Count}개가 필터링 됫다 뭐가 나올지 궁금하군 후후");


        ShuffleList(filteredJujuCodes);

        // 최대 3개만 선택
        int count = Mathf.Min(3, filteredJujuCodes.Count);

        List<string> tempJujuCodes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            string selectedCode = filteredJujuCodes[i];
            tempJujuCodes.Add($"{selectedCode}");
        }


       string jujuJson = JsonConvert.SerializeObject(tempJujuCodes);



        photonView.RPC(nameof(RPC_SelectJuju_M2C), RpcTarget.All, actorNum, boundCode, jujuJson, missionclear, players[actorNum].boundIndex);
        // yield return new WaitUntil(() => syncCount == 2);
       // syncCount = 0;
        //여기서 이 함수 코루틘으로 맹글어서 그냥 기달려부려~

        //BoundChecker.After_CallRPCJuju(actorNum);


    }


    /*private void RefillDeckFromTrash(PlayerData player)
    {
        if (player.trash.Count == 0)
        {
            Debug.Log("[RefillDeckFromTrash] Trash is empty, cannot refill deck.");
            return;
        }

        // Trash → DeckCodes로 이동
        player.DeckCodes.AddRange(player.trash);
        player.trash.Clear();

        // Shuffle
        ShuffleList(player.DeckCodes);

        Debug.Log($"[RefillDeckFromTrash] Deck refilled with {player.DeckCodes.Count} cards.");


    }*/
    private void RefillDeckFromTrash(PlayerData player)
    {
        if (player == null)
        {
            Debug.LogError("[RefillDeckFromTrash] player == null");
            return;
        }
        if (player.trash == null || player.trash.Count == 0)
        {
            Debug.Log("[RefillDeckFromTrash] Trash is empty, cannot refill deck.");
            return;
        }

        var toMove = new List<string>();
        foreach (var code in player.trash)
        {
            var card = CardCSVLoader.Instance?.GetCardByCode(code);
            if (card == null)
            {
                Debug.LogWarning($"[RefillDeckFromTrash] Unknown code '{code}', skipped.");
                continue;
            }
            if (card.cardType == 0)
                toMove.Add(code);
        }

        if (toMove.Count == 0)
        {
            Debug.Log("[RefillDeckFromTrash] No eligible (cardType==0) cards in trash.");
            // 상태 출력
            Debug.Log($"[RefillDeckFromTrash] Deck ({player.DeckCodes.Count}): [{string.Join(", ", player.DeckCodes)}]");
            Debug.Log($"[RefillDeckFromTrash] Trash ({player.trash.Count}): [{string.Join(", ", player.trash)}]");
            return;
        }

        // 덱으로 이동
        player.DeckCodes.AddRange(toMove);

        // 트래시에서 옮긴 것만 제거
        var movedSet = new HashSet<string>(toMove);
        player.trash.RemoveAll(code => movedSet.Contains(code));

        // 셔플
        ShuffleList(player.DeckCodes);

        // 결과 로그
        //Debug.Log($"[RefillDeckFromTrash] Moved {toMove.Count} card(s) from trash to deck (type==0).");
       // Debug.Log($"[RefillDeckFromTrash] Deck now has {player.DeckCodes.Count} cards; Trash left {player.trash.Count}.");

//        Debug.Log($"[RefillDeckFromTrash] Deck: [{string.Join(", ", player.DeckCodes)}]");
  //      Debug.Log($"[RefillDeckFromTrash] Trash: [{string.Join(", ", player.trash)}]");
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [PunRPC]
    void RPC_JujuSync_M2C(string json1, string json2)
    {

        LocalRenderingData packet1 = JsonConvert.DeserializeObject<LocalRenderingData>(json1);
        LocalRenderingData packet2 = JsonConvert.DeserializeObject<LocalRenderingData>(json2);

        LocalRenderingManager.Instance.Rendering_JujuSync(packet1, packet2);
            
    }

    [PunRPC]
    void RPC_CycleSync_M2C(int newcycle, int faceoffCount)
    {
        LocalCycleManager.Instance.PlayCycleBanner(newcycle, faceoffCount);


        //LocalRenderingManager.Instance.Rendering_JujuSync(packet1, packet2);

    }

    [PunRPC]
   public void RPC_After_Action_Selection_Sync_M2C(string json1, string json2, int who_Select)
    {

        LocalRenderingData packet1 = JsonConvert.DeserializeObject<LocalRenderingData>(json1);
        LocalRenderingData packet2 = JsonConvert.DeserializeObject<LocalRenderingData>(json2);


        LocalRenderingManager.Instance.Rendering_AfterActionSelect(packet1, packet2, who_Select);


    }


    [PunRPC]
    void RPC_SelectJuju_M2C(int actorNum, string boundCode, string jujuJson, bool missionClear, int index )
    {
        //여기에 불값 넣어서 
        
        List <string > jujucodes = JsonConvert.DeserializeObject < List<string>>(jujuJson);


        if (actorNum == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (missionClear)
                LocalState.Instance.SelectJuju(actorNum, boundCode, jujucodes);
            else
            {
                Submit_Juju(actorNum, null);

            }
        }



        else
        {
            //여기서 상대 제약이 뭐였는지 판단하는 함수 호출 
            // LocalState.Instance.GuessJuju(index);

            Submit_Juju(actorNum, null);
        }
    }


    [PunRPC]
    void RPC_ReceiveJuju_C2M(string jujuCode, int actorNum)
    {

         if (jujuCode != null)
        {


            playerJujuData[actorNum][jujuCode].Apply(actorNum);
            playerJujuData[actorNum][jujuCode].isUsed = 1;
           

        }
        syncCount++;
        Debug.Log($"{actorNum}한테 일단 하나 받음 현재 싱크 카운트 : {syncCount}");


    }

    public void Submit_Juju(int actorNum, string jujucode)

    {

        photonView.RPC(nameof(RPC_ReceiveJuju_C2M), RpcTarget.MasterClient, jujucode, actorNum );

    }
    
    public void Submit_RenderingDone(int actorNum)
    {
        photonView.RPC(nameof(RPC_JustSync_C2M), RpcTarget.MasterClient,actorNum);

    }
   

    public void Submit_GameStart_SyncDone()
    {
        //시작연출로 인해 ㅎㅎ.
        photonView.RPC(nameof(RPC_InitGameSyncDone_C2M), RpcTarget.MasterClient,
         PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_JustSync_C2M(int actorNum)
    {
        Debug.Log($"{actorNum}의 렌더링 완료");

        syncCount++;
    }
}
