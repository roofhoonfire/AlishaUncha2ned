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




public enum apProp
{

    defaultMove,defaultMoveCast,tempCast,permCast,tempDef,permDef,tempDam,permDam, CastingMinimum
}
public class ConstraintStats
{
    public int damageDealtThisCycle = 0;
    public int actionUsedHowmany = 0;
    public List<int> actionClockDiffer = new();
    public bool moved = false;
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

    public bool isStunned = false;

    public int actionClockManuplate = 0;
    public int defenseManuplate = 0;
    public int timeBombSetinTomMotion = 0;

    public List<string> Bounds ; //바운드 아이디의 리스투이돵돠라돵돵
    public int boundIndex = 0;



    //수치관련//

    public int defaultMove = 3;
    public int defaultMoveCast = 99;






    //액션 패킷 강화용도

    public Dictionary<apProp, int> forActionPacket;





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
        ActorNumber = actorNumber;
        DeckCodes = deckCodes;
        HP = initialHP;
        deckIndexStart = 0;
        prevHP = initialHP;
        energy = 3;

        //기찮으므로로로로루뢰뢰
        Bounds = new List<string> {"b1", "b3", "b4","b5","b6","b7","b8" };
        JujuCode = new List<string> { "j1", "j2", "j3", "j4" };




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
    
    
    
    
    
    
    }
}

public class ActionData //여기 뭐 추가할 거면 carddragHandler로 수정해야함
{

    public int actionId; //0 이면 이동 1이면 카드.. 등
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

    public List<int> effectTiles = new();


    [NonSerialized]
    public bool hasOtherExecutedSinceInsertion = false; //선공, 대처 결정용 변수

    public int nthaction =0;


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
    public void Norm_ProcessHook(
        HookType hook,
        int hActorNum,
        int hOpActorNum,
        int ttActorNum,
        ActionData ttAction,
        ActionData hOpMainAction,
        ActionData hMainAction)
        //hMainAction은 이 훅을 Process하는 놈이
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
            e.Norm_Apply(hActorNum, hOpActorNum,  ttActorNum, this,  hOpMainAction, hMainAction, ttAction);
            flags.Remove(e.flag);

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
            players[actorNumber] = new PlayerData(actorNumber, codeList, 100);
        }
        else
        {
            players[actorNumber].DeckCodes = codeList;
        }

        Debug.Log($"Overmind: Actor {actorNumber} 덱 코드 저장 - [{string.Join(", ", codes)}]");
    }

    /// <summary>
    /// 특정 플레이어의 PlayerData를 가져옵니다.
    /// </summary>
    public PlayerData GetPlayerData(int actorNumber)
    {
        players.TryGetValue(actorNumber, out var data);
        return data;
    }


    /// <summary>
    /// 이 오브젝트가 MasterClient에서 실행되고 있는지 여부를 반환합니다.
    /// </summary>
    public bool IsRunningOnMaster()
    {
        return PhotonNetwork.IsMasterClient;
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

            // 2) 덱 셔플 
            ShuffleList(player.DeckCodes);

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
                boundIndex = 0

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
        photonView.RPC(nameof(RPC_InitGameSyncDone_C2M), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber);
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

    [PunRPC]
    void RPC_SyncUpdateState(string json, string qjson) //로칼 딕셔너리를 업뎃해줄것임
    {
        var data = JsonConvert.DeserializeObject<Dictionary<int, PlayerData>>(json);


        var receivedq = JsonConvert
            .DeserializeObject<List<Dictionary<string, int>>>(qjson);
        LocalState.Instance?.SyncPartial(data, receivedq); //부분 업데이트 함수
        
        // 마스터 포함 모든 클라이언트가 완료 보고
        photonView.RPC(nameof(RPC_SyncDone), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber);
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
        actionQueue.Clear();
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

        photonView.RPC(nameof(RPC_After_Action_Selection_Sync_M2C), RpcTarget.All, var1json, var2json);
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
               while (readyCount < actionQueue.Count && actionQueue[readyCount].remainingCost == 0)
                    readyCount++;
               
                // Find ready actions

                //경우  1 : 격돌
                if (readyCount > 1)
                {
                    
                    yield return HandleRumblePhase();

                    if (actionQueue.Count <= 0) //연계카드가 없다면 무조건 이 경우일 것
                    {
                        break; //다시 페이스 오프

                    }
                    else //이건 연계의 케이스를 고려해서 짜두었다
                    {

                        int loneActor = actionQueue[0].actorNumber;
                        bool allSame = actionQueue.All(a => a.actorNumber == loneActor);
                        if (allSame)
                        {
                            // 2) 상대 플레이어 번호 구하기
                            int otherActor = GetOtherPlayerNumber(loneActor);

                            // 3) pendingSelections 초기화
                            pendingSelections.Clear();

                            // 4) 상대에게 선택 요청
                            photonView.RPC(nameof(RPC_BeginChooseMove), RpcTarget.All, otherActor, false, faceOffCount);

                            // 5) 상대 응답 대기
                            yield return new WaitUntil(() => pendingSelections.ContainsKey(otherActor));

                            // 6) 응답 처리
                            var selOther = pendingSelections[otherActor];
                            pendingSelections.Remove(otherActor);

                            // 7) 액션 초기화 및 큐에 추가
                            InitActionBeforeInsert(selOther.action, otherActor);
                            actionQueue.Add((otherActor, selOther.action, selOther.cost));
                            actionQueue.Sort((a, b) =>
                                a.remainingCost != b.remainingCost
                                    ? a.remainingCost.CompareTo(b.remainingCost)
                                    : a.action.nthaction.CompareTo(b.action.nthaction)
                            );
                        }
                        ///다시 선택후
                        ///
                        continue;

                    } //연계카드가 있다면 요 경우가 될 것이다
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

                actionQueue.RemoveAt(0);


                // Request next move if no pending action
                if (!actionQueue.Exists(a => a.actorNumber == ttActorNum))
                {
                    pendingSelections.Clear();






                    Hand_Gen(ttTuple.actorNumber);
                    ActionPacketData var = ActionPacketConverter.FromPlayer(players[ttTuple.actorNumber]);

                    Player targetP = PhotonNetwork.CurrentRoom.Players[ttTuple.actorNumber];  //쇼다운RPC는 따로 만들기 혹은 매개변수로 조절 (연출용)
                    string apjson = JsonConvert.SerializeObject(var);



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

    IEnumerator MasterRumbleCalc(int actorNum, int actor2Num , ActionData action1, ActionData action2, ActionData nextaction1, ActionData nextaction2, int RumbleFlag)
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

        if (RumbleFlag < 3)
        {

            (int actor, ActionData action, ActionData nextmove) winner = (RumbleFlag == actorNum) ? (actorNum, action1, nextaction1) : (actor2Num, action2, nextaction2);
            (int actor, ActionData action, ActionData nextmove) loser = (RumbleFlag == actorNum) ? (actor2Num, action2, nextaction2) : (actorNum, action1, nextaction1);


            
            winner.action.ProcessHook(HookType.Activate, winner.actor, loser.nextmove, loser.actor, loser.action, winner.nextmove);
            yield return showDownAfterHook(actorNum, actor2Num, action1, action2, winner.actor, HookType.Activate);
            yield return MasterForEveryIntheQueAfterCalc(winner.action, winner.actor, nextaction1, nextaction2);

        }
        else
        {
                action1.ProcessHook(HookType.Activate, actorNum, nextaction2, actor2Num, action2, nextaction1);
                action2.ProcessHook(HookType.Activate, actor2Num, nextaction1, actorNum, action1, nextaction2);
            
            yield return showDownAfterHook(actorNum, actor2Num, action1, action2, RumbleFlag, HookType.Activate);

            yield return MasterForEveryIntheQueAfterCalc(action1, actorNum, nextaction1, nextaction2);
            yield return MasterForEveryIntheQueAfterCalc(action2, actor2Num, nextaction2, nextaction1);


        }
     
        yield return null;
    }
    IEnumerator Norm_Action_Calc(int ttActorNum, ActionData ttAction,  ActionData ttOpMainAction,ActionData ttMainAction)
    {
        int ttOpActorNum = GetOtherPlayerNumber(ttActorNum);

        // GuardOrCounter는 현재 행동하는 액터의 상대 액터의 큐에 삽입되있는 바로 다음 액션임
        //action이 지금 발동되는 액션

        ttOpMainAction.Norm_ProcessHook(HookType.Guard, ttOpActorNum, ttActorNum, ttActorNum,  ttAction, ttMainAction, ttOpMainAction);
        yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Guard); //여기서 해당 액션이 추가 선택이 잇다 하면 그것까지 넘겨줌



        //action.ProcessHook(HookType.Activate, actorNum, )
        ttAction.Norm_ProcessHook(HookType.Priority, ttActorNum, ttOpActorNum, ttActorNum, ttAction, ttOpMainAction, ttMainAction);
        ttAction.Norm_ProcessHook(HookType.Activate, ttActorNum, ttOpActorNum, ttActorNum, ttAction, ttOpMainAction, ttMainAction); 
        yield return Norm_After_Hook((ttActorNum, ttAction), HookType.Activate);

        ttOpMainAction.Norm_ProcessHook(HookType.Counter, ttOpActorNum, ttActorNum, ttActorNum, ttAction, ttMainAction, ttOpMainAction);
        yield return Norm_After_Hook((ttOpActorNum, ttOpMainAction), HookType.Counter);

        //nth 액션이랑 비교해서 순서대로 실행하기 
        //여기선 action의 반대 플레이어가 카운터의 주체이므로 헷갈리지 말자 ㅎㅎ

      //  yield return MasterForEveryIntheQueAfterCalc(action, actorNum, myrightNextAction, GuardOrCounter);

        //애프터 액션 플래그 같은거 주면 될 듯 

       // Turn_End_Call();
    }



    //이것도 그냥 딴데로 옮길까..
    private void Turn_End_Call()
    {
        foreach (var p in players)
        {
            p.Value.prevHP = p.Value.HP;

        }


    }
    
    IEnumerator  MasterForEveryIntheQueAfterCalc(ActionData nowbaldong, int actorNum, ActionData countersopnextaction, ActionData mynextMove)
    {

       // if (nowbaldong.actionId == 0)
         //   return;
        for (int i = 0; i < actionQueue.Count; i++) //방어도 방어력. 고민하기
        {
            if ((actionQueue[i].actorNumber != actorNum))
            {

                foreach (var e in actionQueue[i].action.effects)
                {
                    if (e.hookType == HookType.IQA)
                    {

                       yield return  MasterIntheQueAfterCalc(actionQueue[i].actorNumber, actionQueue[i].action, countersopnextaction, mynextMove);//대처발동;
                    }


                }
            }
        }

    }
    IEnumerator MasterIntheQueAfterCalc(int actorNum, ActionData action, ActionData countersopnextaction, ActionData myrightnextmove)
    {
        //카드의 경우
        if (action.actionId == 1)
        {


            //여기서 넘겨주는 훅타입은 호출에 필요한 훅타입이여! 여기는 발동 부의 체커니까
            //action.ProcessHook(HookType.Activate, actorNum, )

            action.ProcessHook(HookType.IQA, actorNum, countersopnextaction, GetOtherPlayerNumber(actorNum),null,myrightnextmove);//다음 플레이어의 액션을 넘겨주는건 추가 함수 작성하자
            
            if (action.effects.Any(effect => effect.hookType == HookType.IQA))
            
                yield return Norm_After_Hook((actorNum, action), HookType.IQA);


        }

    }

    public void MasterBeforeRumbleCalc(int actorNum, ActionData myaction, ActionData opaction)
    {
        //카드의 경우
        if (myaction.actionId == 1)
        {


            //여기서 넘겨주는 훅타입은 호출에 필요한 훅타입이여! 여기는 발동 부의 체커니까
            //action.ProcessHook(HookType.Activate, actorNum, )

            myaction.ProcessHook(HookType.BeforeRumble, actorNum, null, GetOtherPlayerNumber(actorNum),opaction, null);//다음 플레이어의 액션을 넘겨주는건 추가 함수 작성하자

        }

    }


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
                myPlayerData.trash.Add(code);

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
    void RPC_HandleRumble(int[] actors)
    {
        //액션끼리 격돌할 때만 처리한다

        //액션과 이동이 격돌할 때 아슬아슬하게 피했을경우
        //that was close!
    }

    [PunRPC]
    void RPC_ReceiveSelection(int actorNumber, string actionjson, int cost, string handjson, string trashjson, string deckjson)
    {

        players[actorNumber].actionClockManuplate = 0;
        var action = JsonConvert.DeserializeObject<ActionData>(actionjson);
        var hand = JsonConvert.DeserializeObject<List<string>>(handjson);

        var trash = JsonConvert.DeserializeObject<List<string>>(trashjson);
        var deck = JsonConvert.DeserializeObject<List<string>>(deckjson);

        //신경 쓰이면 뉴 리스트로 해도 된다 하지만 어차피  ㅋ 
        players[actorNumber].DeckCodes = deck;
        players[actorNumber].hands = hand;
        players[actorNumber].trash = trash;





        pendingSelections[actorNumber] = (action, cost);
        if(action.actionId != 0)
        {
            //summary//
            //제약 체크 용도//
            ////
            players[actorNumber].constraintStats.actionUsedHowmany++;

            int opActorNum = GetOtherPlayerNumber(actorNumber);

            int maxRemainingCost = actionQueue
                .Where(entry => entry.actorNumber == opActorNum)
                 .Select(entry => entry.remainingCost)
                .DefaultIfEmpty(0) // 없을 때 0 리턴
                .Max();

            int diff = Mathf.Abs(maxRemainingCost - action.actionClock);


            players[actorNumber].constraintStats.actionClockDiffer.Add(diff);

            Debug.Log($"제약 : 플레이어 {actorNumber}의 {players[actorNumber].constraintStats.actionUsedHowmany}와 {players[actorNumber].constraintStats.actionClockDiffer}");
        }
        
    }


    //
    [PunRPC]
    void RPC_BeginChooseMove(int actorNumber, bool faceoff, int nthshowdon)
    {

        if (faceoff)
        {
            //로컬 쇼다운 렌더링 함수 고고씽

            LocalState.Instance?.FaceOffRendering(nthshowdon);
        }
        //내 아이디면 LocalState.Instance?.StartChooseMovePhase(actorNumber);
        //어차피 쇼다운에서는 특정 액터에게만 뿌리므로 그렇다!
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LocalState.Instance?.StartChooseMovePhase(actorNumber);
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

    [PunRPC]
    void RPC_FaceOff_ChooseAction_M2C(int actorNumber,  int nthFaceOff, string apjson)
    {

        LocalRenderingManager.Instance.Rendering_FaceOff_Start(nthFaceOff);
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LocalState.Instance?.Start_FaceDown_Phase(actorNumber, apjson);
        }
     

    }

    [PunRPC]
    void RPC_Norm_ChooseAction_M2C(int actorNumber, string apjson)
    {

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

    IEnumerator showDownAfterHook(int actor1, int actor2, ActionData action1, ActionData action2, int showdownflag, HookType h)
    {

        int actor1opCost = GetMinOpponentRemainingCost(actor1);
        int actor2opCost = GetMinOpponentRemainingCost(actor2);

        

        //오버마인드 player 딕셔너리를 로컬 player 딕셔너리로 복사
        photonView.RPC(nameof(RPC_SyncUpdateState), RpcTarget.All, JsonConvert.SerializeObject(players), toSendActionQbutOnlyActorNumandCost());
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;
        Debug.Log("쇼다운 후 싱크완료");

        //발동된 애니메이션 결과 출력 . 코스트도 여기서 넘겨줌
        string actionJson1 = JsonConvert.SerializeObject(action1);
        string actionJson2 = JsonConvert.SerializeObject(action2);

        //2개의 애니메이션을 한번에 보여주는 코드 일뿐! 헷갈릴거 없다 장훈 쿤
        photonView.RPC(nameof(RPC_PlayShowDownAction), RpcTarget.All, actor1, actionJson1, actor2, actionJson2, showdownflag, actor1opCost, actor2opCost, h);
        

        yield return new WaitUntil(() => syncCount == 4); //나중에 수정하든가 
        syncCount = 0;



    }

    [PunRPC]
    void RPC_JustCostRendering(HookType h)
    {

        LocalState.Instance.OpponentCostRendering(h);

        photonView.RPC(nameof(RPC_SyncDone), RpcTarget.MasterClient,
              PhotonNetwork.LocalPlayer.ActorNumber);
    }
    IEnumerator Norm_chooseTile(int actornum, ActionData action)
    {

        if(action.actionId != 1)
        {
            yield break;
        }

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

        //액션제이선
       string actionJson = JsonConvert.SerializeObject(action);
       photonView.RPC(nameof(RPC_Norm_ChooseTile_M2C), RpcTarget.All, actornum, actionJson, var1json, var2json);
         
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

    IEnumerator chooseTile ((int actornum, ActionData action)p1, (int actornum, ActionData action)p2)
    {
       
        List<(int actornum, ActionData action)> templist = new List<(int actorNum, ActionData action)>();
        if (p1.action!=null) templist.Add(p1);
        if (p2.action!=null) templist.Add(p2);
        int howmany = 0;


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

        //lr패킷


        foreach (var p in templist)
        {
                if (p.action != null && p.action.actionId == 1) {
                    string actionJson  = JsonConvert.SerializeObject(p.action);
                    photonView.RPC(nameof(RPC_ChooseTile), RpcTarget.All, p.actornum, actionJson, var1json, var2json);
                    howmany++;
           }
       }
        yield return new WaitUntil(() => pendingTiles.Count == howmany);
       
        foreach (var (actorNum, tiles) in pendingTiles)
        {
            if (p1.actornum == actorNum)
            {
                p1.action.effectTiles = new List<int>(tiles);
            }
            else if (p2.actornum == actorNum)
            {
                p2.action.effectTiles = new List<int>(tiles);
            }
        }





        pendingTiles.Clear();
    }
    [PunRPC]
    void RPC_Norm_Play_Action_M2C(int actorNumber, string actionJson, string lrjson1, string lrjson2, HookType h)
    {
        var action = JsonConvert.DeserializeObject<ActionData>(actionJson);
        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);

        LocalRenderingManager.Instance.Rendering_Norm_Action(actorNumber, data1, data2, action, h);

    }

    [PunRPC]

    void RPC_Norm_ChooseTile_M2C(int actornum, string actioJson, string lrjson1, string lrjson2)
    {
        //일단 렌더링은 둘다 해주고 


        var action = JsonConvert.DeserializeObject<ActionData>(actioJson);

        var data1 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson1);

        var data2 = JsonConvert.DeserializeObject<LocalRenderingData>(lrjson2);
        LocalRenderingManager.Instance.Rendering_Before_Tile_Choose(actornum, data1, data2, action);
        //

        if (actornum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //상대 생각중!

        }

        else
        {

            CardChooseTile.Instance.SetActive(true, action);

        }

    }

    [PunRPC]
    void RPC_Only_Sync_C2M()
    {
        SyncDone();

    }


    [PunRPC]

    void RPC_ChooseTile(int actornum, string actioJson, string lrpacketjson)
    {

        
        if (actornum != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //액터넘이 내가 아닌데, 이미 다른 코루틴이 실행중이라면 을 생각해서 좀해보셈

            //이거 경우 잘 생각해서 기다리기 모션도 잘 만들어보쟈
        }

        else
        {
            var action = JsonConvert.DeserializeObject<ActionData>(actioJson);

            CardChooseTile.Instance.SetActive(true, action);

        }

    }


    public void SendTile(int actorNum, List<int> tiles)
    {

        string tilejson = JsonConvert.SerializeObject(tiles);
        photonView.RPC(nameof(RPC_ReceiveTile),RpcTarget.MasterClient, actorNum,tilejson  );



    }
    
    [PunRPC]
    void RPC_ReceiveTile(int actorNum, string tileListJson)
    {

        Debug.Log($"[Master] RPC_ReceiveTile 호출됨! actorNum={actorNum}, tileList={tileListJson}");
        List<int> deser = JsonConvert.DeserializeObject<List<int>>(tileListJson);
        pendingTiles.Add((actorNum, deser));
        Debug.Log($"[Master] pendingTiles.Count={pendingTiles.Count}");

    }


    //해당 액션 추가 선택 까지 이 안에서 구현하면 됩니다 
    IEnumerator Norm_After_Hook((int actorNum, ActionData action) Nowhooker, HookType h)
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


        int opCost = GetMinOpponentRemainingCost(Nowhooker.actorNum);

        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
        string actionJson = JsonConvert.SerializeObject(Nowhooker.action);
        photonView.RPC(nameof(RPC_Norm_Play_Action_M2C), RpcTarget.All, Nowhooker.actorNum, actionJson, var1json, var2json, h);
       
         yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;



    }


  /*  IEnumerator afterHook((int actorNum, ActionData action)Nowhooker, HookType h )
    {
        int opCost = GetMinOpponentRemainingCost(Nowhooker.actorNum);

        if (!Nowhooker.action.effects.Any(effect => effect.hookType == h)) //이거 왜 들어갔더라 시발.. 이제 슬 기억이 나지 않는다. . .   .
            yield break;
        //대처 처리 함수 길어서 함수로 묶음 , 액션 큐를 훑으면서 발동된 액션 외의 액터 넘버의 액션들에 대해 카운터 호출
        //   MasterCounterEveryResultCalc(next.action, next.actorNumber);



        //오버마인드 player 딕셔너리를 로컬 player 딕셔너리로 복사
        photonView.RPC(nameof(RPC_SyncUpdateState), RpcTarget.All, JsonConvert.SerializeObject(players), toSendActionQbutOnlyActorNumandCost());
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;

        //발동된 애니메이션 결과 출력 . 코스트도 여기서 넘겨줌
        //이제 코스트안넘겨도됨
        string actionJson = JsonConvert.SerializeObject(Nowhooker.action);
        photonView.RPC(nameof(RPC_PlayAction), RpcTarget.All, Nowhooker.actorNum, actionJson, opCost,h);
        //여기서 출력되어야하는 애니메이션을 한번에 보여주면 됨 그냥(순차적으로)
        //한번의 해프터 후커당 하나의 애니메이션이 출력된다고 생각해라 게이야 
        yield return new WaitUntil(() => syncCount == 2); //나중에 수정하든가 
        syncCount = 0;



    }

    */






    [PunRPC]

    void RPC_PlayShowDownAction(int actorNumber1, string actionJson1, int actorNumber2, string actionJson2, int showdownflag, int opofa1Cost, int opofa2Cost, HookType h)

    {  // 1, 2 승자 존재: 승자 번호
       // 3: 동시발동
       // 4: 00후 처맞기
       // 5: 처맞고 00하기하기
       //6 : 아슬아슬 피하기
       //7: 비김
       //3 : 원래 안맞는 놈 , 두 플레이어 모두 이동 , 둘다 헛발질
       //애니메이션 재생 순서가 중요한 경우
       //00후 처맞기, 처맞고 00하기, 아슬아슬 피하기
       //그 외에는 두 액션 모두 동시에 출력되도 됨, 

        //플레이 쇼다운 렌더링으로 따로 나눈 이유를 몰르겟다 ㅎ


        var action1 = JsonConvert.DeserializeObject<ActionData>(actionJson1);
        var action2 = JsonConvert.DeserializeObject<ActionData>(actionJson2);
        LocalState.Instance.PlayShowDownRendering(actorNumber1, actorNumber2, action1, action2, showdownflag, opofa1Cost, opofa2Cost, h);

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

  
    [PunRPC]
    void RPC_PlayAction(int actorNumber, string actionJson, int opCost, HookType h)
    {
        var action = JsonConvert.DeserializeObject<ActionData>(actionJson);

        LocalState.Instance?.PlayActionRendering(actorNumber, action, opCost,h);
    }


    private void TimeGoesOn()
    {
        //actionqueue에서 발동되는 액션만큼의 remainingcost를 소모하는 함수
        // 정렬되있응께 맨 앞놈이 Min코스트
        int minCost = actionQueue[0].remainingCost;
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
    private int GetMaxOpponentRemainingCost(int actorNum)
    {
        // actorNum과 다르고, 남은 코스트를 뽑아서
        var opponentCosts = actionQueue
            .Where(entry => entry.actorNumber != actorNum)
            .Select(entry => entry.remainingCost);

        // 만약 상대 액션이 없다면 0을 반환 (필요하다면 다른 기본값으로 바꿔도 됨)
        if (!opponentCosts.Any())
            return 0;

        // 그중 최댓값 반환
        return opponentCosts.Max();
    }

    /// <summary>
    /// 두 플레이어가 동시에 남은 Cost == 0이 되어 충돌할 때 호출
    /// </summary>
    private IEnumerator HandleRumblePhase()
    {
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
        actionQueue.RemoveRange(0, 2);

        var a1 = (actor: first.actorNumber, action: first.action);
        var a2 = (actor: second.actorNumber, action: second.action);

        // 2) BeforeRumble 훅 처리 (럼블 포인트 계산용)

        // 3) 실제 격돌 판정
        bool isMain1 = a1.action.actionId == 1;
        bool isMain2 = a2.action.actionId == 1;


        //선공 불가 선언 및 쇼다운 외 다음 액션 있나 체크
        ActionData a1soppmove = null;
        ActionData a1soprightnextmove = null;
        ActionData a2soppmove = null;
        ActionData a2soprightnextmove = null;

        int nextOpMoveChecker = 0;
        //  if (next.action.actionId == 1)
        //    {
        for (int i = 0; i < actionQueue.Count; i++)
        {
            if (actionQueue[i].actorNumber != a1.actor)
            {
                nextOpMoveChecker++;

                a1soppmove = actionQueue[i].action;

                if (nextOpMoveChecker == 1)
                {
                    a1soprightnextmove = a1soppmove;
                }
                if (a2.action.actionId == 1)
                    a1soppmove.hasOtherExecutedSinceInsertion = true; //즉 단순 이동으로는 선공을 끊을 수 없다 ㅎ
            }
        }

        nextOpMoveChecker = 0;

        for (int i = 0; i < actionQueue.Count; i++)
        {
            if (actionQueue[i].actorNumber != a2.actor)
            {
                nextOpMoveChecker++;

                a2soppmove = actionQueue[i].action;

                if (nextOpMoveChecker == 1)
                {
                    a2soprightnextmove = a2soppmove;
                }
                if (a1.action.actionId == 1)
                    a2soppmove.hasOtherExecutedSinceInsertion = true; //즉 단순 이동으로는 선공을 끊을 수 없다 ㅎ
            }
        }

        yield return chooseTile((a1.actor, a1.action), (a2.actor,a2.action));



        // 두 액션 모두 메인액션인 경우만 진짜 격돌
        if (isMain1 && isMain2)
        {


            MasterBeforeRumbleCalc(a1.actor, a1.action, a2.action);
            MasterBeforeRumbleCalc(a2.actor, a2.action, a1.action);
            //각각의 럼블포인트 계산
            int rumble1 = isMain1 ? a1.action.rumblePoint : int.MinValue;
            int rumble2 = isMain2 ? a2.action.rumblePoint : int.MinValue;

            // 3.1 진짜 격돌: 서로의 범위 안에 있는지 확인
            bool inRange1 = IsInEffectRange(a1.actor, a1.action, a2.actor);
            bool inRange2 = IsInEffectRange(a2.actor, a2.action, a1.actor);

            if (inRange1 && inRange2)
            {
                // 3.1.1 승패 결정
                if (rumble1 > rumble2)
                {

                    showdownCircFlag = a1.actor;
                    yield return MasterRumbleCalc(a1.actor, a2.actor,  a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);
                }
                else if (rumble2 > rumble1)
                {
                    showdownCircFlag = a2.actor;
                    yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);
                }
                else
                {

                    showdownCircFlag = 7;
                    yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);

                }

            }


            //applyunilateralhit 이랑 어차피 동작이 똑같음
            else if (inRange1)
            {

                showdownCircFlag = 5;
                yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);

            }

            else if (inRange2)
            {


                showdownCircFlag = 4;
                yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);

            }
            else {
                //둘다 빗나감
                showdownCircFlag = 3;
                yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);

            }
        }
        else
        {
            if (isMain1 ^ isMain2) // 한명은 이동 한명은 액션인 경우이다
            {
                int prevPos = players[a1.actor].curpos;
                if (a1.action.actionId == 0)//이동 , 액션 순서
                {

                    if (IsInEffectRange(a2.actor, a2.action, a1.actor))
                    {
                        //이동 후 처 맞기 플래그 on
                        showdownCircFlag = 4;

                    }
                    else
                    {
                        if (a2.action.effectTiles.Contains(prevPos))
                        {
                            //that was close 플래그 온
                            showdownCircFlag = 6;
                        }
                        else   //원래 안맞는놈
                        {       
                            showdownCircFlag = 3;
                        }

                    }
                }

                else //액션 , 이동 순서 a1이 액션, a2가 무빙
                {
                    if (IsInEffectRange(a1.actor, a1.action, a2.actor))
                    {
                        //개같이 처맞음 플래그 온
                        //처맞고 이동On
                        showdownCircFlag = 5;

                    }
                    else
                    {
                        //헛손질 후 이동 on
                        showdownCircFlag = 3;
                    }
                }

                yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);


            }
            else //둘다 평화로운 이동이다 
            {
                showdownCircFlag = 3;
                yield return MasterRumbleCalc(a1.actor, a2.actor, a1.action, a2.action, a2soprightnextmove, a1soprightnextmove, showdownCircFlag);


            }
        }

        // 4) 결과 동기화 & 애니메이션 RPC
       /* photonView.RPC(nameof(RPC_SyncUpdateState), RpcTarget.All, JsonConvert.SerializeObject(players));
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;

        string json1 = JsonConvert.SerializeObject(a1.action);
        string json2 = JsonConvert.SerializeObject(a2.action);
        photonView.RPC(nameof(RPC_PlayShowDownAction), RpcTarget.All,
            a1.actor, json1, a2.actor, json2,
            showdownCircFlag,          // 승자 ActorNumber 또는 -1
            GetMaxOpponentRemainingCost(a1.actor),   // 다음 Cost1
            GetMaxOpponentRemainingCost(a2.actor));  // 다음 Cost2

        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;
    */}

    /// <summary>두 액션이 서로의 효과 범위에 들어왔는지 체크</summary>
    private bool IsInEffectRange(int sourceActor, ActionData sourceAction, int targetActor)
    {
        var srcTiles = sourceAction.effectTiles;
        int pos = players[targetActor].curpos;
        return srcTiles.Contains(pos);
    }

  private string toSendActionQbutOnlyActorNumandCost()
    {
        var simpleQueue = actionQueue
        .Select(e => new Dictionary<string, int>
        {
            ["actorNumber"] = e.actorNumber,
            ["remainingCost"] = e.remainingCost
        })
        .ToList();

        string queueJson = JsonConvert.SerializeObject(simpleQueue);
        return queueJson;
    }
    IEnumerator UpdateCycleState(int newState) {
        //ttMainAction이 asCombo면 이라는 경우를 생각하셈
        if (cycleState == -1)
        {
            cycleState = newState;
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
                players[newState].canMove = true;
            }
            else if (newState == 2)
            {
                players[newState].canMove = true;
            }



            //여기서 cycleState에 대해서 boundchecker호출!@!

           if (cycleState != 0)
            {
                BoundChecker.BoundCheck(cycleState);
                yield return new WaitUntil(() => syncCount == 2);
                syncCount = 0;

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

            BoundChecker.BoundParamReset();

        }
        else
        {
            if (newState == 0) //페이스오프 이후 즉시 격돌한 경우  
            {
                players[1].energy++;
                players[1].canMove = true;
                players[2].energy++;
                players[2].canMove = true;
            }
        }
            cycleState = newState;
    }



    //summary//
    //오버마인드에서 제약 코드에 맞게 생성할 주술 선택지를 클라이언트에게 넘겨주기 위한 연산//
    public void CallRPCJuju(int actorNum, string boundCode)
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



        photonView.RPC(nameof(RPC_SelectJuju_M2C), RpcTarget.All, actorNum, boundCode, jujuJson);


    }


    private void RefillDeckFromTrash(PlayerData player)
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
   public void RPC_After_Action_Selection_Sync_M2C(string json1, string json2)
    {

        LocalRenderingData packet1 = JsonConvert.DeserializeObject<LocalRenderingData>(json1);
        LocalRenderingData packet2 = JsonConvert.DeserializeObject<LocalRenderingData>(json2);


        LocalRenderingManager.Instance.Rendering_AfterActionSelect(packet1, packet2);


    }


    [PunRPC]
    void RPC_SelectJuju_M2C(int actorNum, string boundCode, string jujuJson )
    {
        List <string > jujucodes = JsonConvert.DeserializeObject < List<string>>(jujuJson);


        if (actorNum == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LocalState.Instance.SelectJuju(actorNum, boundCode, jujucodes);
        }



        else
        {
            Submit_Juju(actorNum,null);


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


    }

    public void Submit_Juju(int actorNum, string jujucode)

    {

        photonView.RPC(nameof(RPC_ReceiveJuju_C2M), RpcTarget.MasterClient, jujucode, actorNum );

    }
    
    public void Submit_RenderingDone(int actorNum)
    {
        photonView.RPC(nameof(RPC_JustSync_C2M), RpcTarget.MasterClient,actorNum);


    }
   


    [PunRPC]
    void RPC_JustSync_C2M(int actorNum)
    {

        syncCount++;
    }
}
