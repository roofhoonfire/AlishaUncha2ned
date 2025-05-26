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

public class PlayerData //여기 변수 추가할 때마다 local의 SyncAll과 SyncPartial 업뎃 해야함
{
    public int ActorNumber;
    public int prevHP;
    public int HP;//currentHp
    public List<string> DeckCodes;
    public int curpos;
    public int deckIndexStart; //몇번째 카드 부터 
    public int deckIndexEnd; //이 덱 정보는 지금 로컬에서만 관리되고 마스터로 보내주진않음 
    public int defense; // localstateUIrendering에서는 playerdata의 값을 일괄적으로 보기 때문에 action이랑 중복해서도 저장해서 쓴다.. 
    //즉 방어도 렌더링은 카드 내릴때, 한번 처음 해주고 그 뒤엔 localUIRendering에서 맘껏 건들면될듯 ;;
    public int energy;
    public bool canMove = true; 

    public bool isStunned = false;


    public PlayerData(int actorNumber, List<string> deckCodes, int initialHP = 100)
    {
        ActorNumber = actorNumber;
        DeckCodes = deckCodes;
        HP = initialHP;
        deckIndexStart = 0;
        prevHP = initialHP;
        defense = 0;
        energy = 3;

    }
}

public class ActionData //여기 뭐 추가할 거면 carddragHandler로 수정해야함
{

    public int actionId; //0 이면 이동 1이면 카드.. 등
    public int destindex; // 이동 일 경우 목적지 인덱스를 저장
    public int defense;
    public int rumblePoint;
    public int actionClock;
    public string cardcode;
    public int plusAlpha; //플러스 알파는 액션 단위로 관리해야함
    public List <int> flags = new List<int> () ;//
    public bool isfirstStrikeSuccess = false; //이것도 사실상 의미가 없어졌지만, 일단 냅두자
    public string cardname;
    public int tileType;
    public int zoneIndex;
    public List<AnimationClip> animations;//사실상 필요가 없어졌다
  
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
            if (e.hookType != hook && e.hookType != HookType.Support)
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

            // 모든 조건 통과 시 효과 적용
            e.Apply(actorNum, this, oppActorNum, rightNextOpponent, rightNowOp, myRightNextMove);

        }

        if (hook == HookType.Activate) {
            if (myRightNextMove == null)
            Overmind.Instance.players[actorNum].defense = 0;
            else
            {
                Overmind.Instance.players[actorNum].defense = myRightNextMove.defense;
            }
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
            InitSyncPlayersToClients();  // 모든 클라이언트에 동기화 요청
            // 코루틴 시작은 동기화 완료 후 NotifyRPC에서 실행
        }
    }

    void InitGame() //players의 필요한 데이터를 초기화
                    //지금 코드에서는 위치 정보만
    {
        var rng = new System.Random();
        foreach (var kvp in players)
        {
            var player = kvp.Value;

            // 1) 위치 초기화
            player.curpos = (kvp.Key == PhotonNetwork.MasterClient.ActorNumber) ? 17 : 19;
            //에너지 , 체력도 여기서 새로 조정 해주고 싶으면 조정 해줘도 됨 


            // 2) 덱 셔플 (Fisher–Yates)
            var deck = player.DeckCodes; //같은 객체를 참조하기 때문에 deck의 순서만 섞어줘도 ㅇㅋ
            int n = deck.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);      // 0 ≤ j ≤ i
                // swap deck[i] and deck[j]
                var tmp = deck[i];
                deck[i] = deck[j];
                deck[j] = tmp;
            }
        }
    }

    void InitSyncPlayersToClients()
    {
        syncCount = 0;//syncCompleteCount = 0;
        string json = JsonConvert.SerializeObject(players);
        photonView.RPC(nameof(RPC_SyncInitialState), RpcTarget.All, json);
    }

    [PunRPC]
    void RPC_SyncInitialState(string json) //얜처음 한번만
    {
        var data = JsonConvert.DeserializeObject<Dictionary<int, PlayerData>>(json);
        LocalState.Instance?.SyncAll(data);

        // 마스터 포함 모든 클라이언트가 완료 보고
        photonView.RPC(nameof(RPC_NotifySyncComplete), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber);
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

    [PunRPC]
    public void RPC_NotifySyncComplete(int actorNumber)
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
    IEnumerator FaceOff()
    {
        faceOffCount++;
        UpdateCycleState(0);
        // Begin selection for all players
        pendingSelections.Clear();
        foreach (int actor in players.Keys)
        {
            Player targetP = PhotonNetwork.CurrentRoom.Players[actor];  //쇼다운RPC는 따로 만들기 혹은 매개변수로 조절 (연출용)
            photonView.RPC(nameof(RPC_BeginChooseMove), targetP, actor, true, faceOffCount);
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
                var next = actionQueue[0];
                UpdateCycleState(next.actorNumber);
                actionQueue.RemoveAt(0);

                //선공 대처 결정
                ActionData tempOp = null;
                ActionData GuardOrCounter = null;
                ActionData myMoveAfterthisTurn = null;

                int nextOpMoveChecker = 0;
                int mynextMoveChecker = 0;
              
                for (int i = 0; i < actionQueue.Count; i++)
                {
                    if (actionQueue[i].actorNumber != next.actorNumber)
                    {
                        nextOpMoveChecker++;

                        tempOp = actionQueue[i].action;

                        if (nextOpMoveChecker == 1)
                        {
                            GuardOrCounter = tempOp;
                        }
                        if (next.action.actionId == 1)
                            tempOp.hasOtherExecutedSinceInsertion = true; //즉 단순 이동으로는 선공을 끊을 수 없다 ㅎ
                    }
                    else
                    {
                        mynextMoveChecker++;
                        if(mynextMoveChecker == 1) myMoveAfterthisTurn = actionQueue[i].action;
                    }
                } //MasterActionCalc를 위한 next제외 액션들 할당 해주는 코드


                yield return chooseTile((next.actorNumber, next.action), (900, null));
                //액션 결과 마스터 내부에서 연산해서 player데이터 바꾸는 함수
                yield return MasterActionCalc(next.actorNumber, GuardOrCounter, next.action, myMoveAfterthisTurn);//경계와 발동 처리는 여기서 진행한다


                // Request next move if no pending action
                if (!actionQueue.Exists(a => a.actorNumber == next.actorNumber))
                {
                    pendingSelections.Clear();
                    //여기서 prevturn을 매개 변수로 넘겨줘서 연속행동 여부 확인 추가하기 
                    photonView.RPC(nameof(RPC_BeginChooseMove), RpcTarget.All, next.actorNumber, false, faceOffCount);
                    yield return new WaitUntil(() => pendingSelections.ContainsKey(next.actorNumber));


                    var selNew = pendingSelections[next.actorNumber];
                    pendingSelections.Remove(next.actorNumber);

                    InitActionBeforeInsert(selNew.action, next.actorNumber);

                    actionQueue.Add((next.actorNumber, selNew.action, selNew.cost));
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
        action.InitializeEffects();
        players[actorNum].defense = action.defense;

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
    IEnumerator MasterActionCalc(int actorNum, ActionData GuardOrCounter, ActionData action, ActionData myrightNextAction)
    {

        // GuardOrCounter는 현재 행동하는 액터의 상대 액터의 큐에 삽입되있는 바로 다음 액션임
        //action이 지금 발동되는 액션
        
        GuardOrCounter.ProcessHook(HookType.Guard, GetOtherPlayerNumber(actorNum), myrightNextAction, actorNum, action, null);
        yield return afterHook((GetOtherPlayerNumber(actorNum), GuardOrCounter), HookType.Guard);



            //여기서 넘겨주는 훅타입은 호출에 필요한 훅타입이여! 여기는 발동 부의 체커니까
            //action.ProcessHook(HookType.Activate, actorNum, )
            action.ProcessHook(HookType.Priority, actorNum, GuardOrCounter, GetOtherPlayerNumber(actorNum), null, myrightNextAction);

            action.ProcessHook(HookType.Activate, actorNum, GuardOrCounter, GetOtherPlayerNumber(actorNum), null, myrightNextAction);//다음 플레이어의 액션을 넘겨주는건 추가 함수 작성하자



            //
            

        yield return afterHook((actorNum, action), HookType.Activate);

        GuardOrCounter.ProcessHook(HookType.Counter, GetOtherPlayerNumber(actorNum), myrightNextAction, actorNum, null, null);
        yield return afterHook((GetOtherPlayerNumber(actorNum), GuardOrCounter), HookType.Counter);

        //nth 액션이랑 비교해서 순서대로 실행하기 
        //여기선 action의 반대 플레이어가 카운터의 주체이므로 헷갈리지 말자 ㅎㅎ

        yield return MasterForEveryIntheQueAfterCalc(action, actorNum, myrightNextAction, GuardOrCounter); 
      
        //애프터 액션 플래그 같은거 주면 될 듯 
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
            
                yield return afterHook((actorNum, action), HookType.IQA);


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
    public void SubmitSelection(ActionData action, int cost)
    {
        string json = JsonConvert.SerializeObject(action);

        photonView.RPC(nameof(RPC_ReceiveSelection), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber, json, cost);
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
    void RPC_ReceiveSelection(int actorNumber, string actionjson, int cost)
    {
        var action = JsonConvert.DeserializeObject<ActionData>(actionjson);
        pendingSelections[actorNumber] = (action, cost);
    }

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

    IEnumerator chooseTile ((int actornum, ActionData action)p1, (int actornum, ActionData action)p2)
    {
        photonView.RPC(nameof(RPC_SyncUpdateState), RpcTarget.All, JsonConvert.SerializeObject(players), toSendActionQbutOnlyActorNumandCost());
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;
        Debug.Log("타일 결정 전 싱크 완료우");

        photonView.RPC(nameof(RPC_JustCostRendering), RpcTarget.All, HookType.Activate);
        yield return new WaitUntil(() => syncCount == 2);
        syncCount = 0;
        Debug.Log("코스트 렌더링 싱크 완료");

        //여기서 로컬이랑 싱크한번해도 낫배드 일듯

        List<(int actornum, ActionData action)> templist = new List<(int actorNum, ActionData action)>();
        if (p1.action!=null) templist.Add(p1);
        if (p2.action!=null) templist.Add(p2);
        int howmany = 0;
        foreach (var p in templist)
        {
                if (p.action != null && p.action.actionId == 1) {
                    string actionJson  = JsonConvert.SerializeObject(p.action);
                    photonView.RPC(nameof(RPC_ChooseTile), RpcTarget.All, p.actornum, actionJson);
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

    void RPC_ChooseTile(int actornum, string actioJson)
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
        List<int> deser =  JsonConvert.DeserializeObject<List<int>>(tileListJson);
        pendingTiles.Add((actorNum, deser));


    }



    IEnumerator afterHook((int actorNum, ActionData action)Nowhooker, HookType h )
    {
        int opCost = GetMinOpponentRemainingCost(Nowhooker.actorNum);

        if (!Nowhooker.action.effects.Any(effect => effect.hookType == h))
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

    [PunRPC]
    void RPC_SyncDone(int actorNumber)
    {
        syncCount++;

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

    /// <summary>격돌 승자에게 우위 처리</summary>
/*    private int ApplyRumbleWinner((int actor, ActionData action) winner,
                                   (int actor, ActionData action) loser,
                                    ActionData wornm,
                                    ActionData wrnm)
    {
        //wornm은 격돌 승자의 상대편의 바로 다음 무브
        //wornm은 격돌 승자의 바로 다음 무브이다 
        MasterMoveActPriorResultCalc(winner.actor, wornm, winner.action, loser.action, wrnm);
        return winner.actor;
    }

    /// <summary>격돌 무승부 처리</summary>
    private int ApplyRumbleDraw((int actor, ActionData action) p1,
                                 (int actor, ActionData action) p2, ActionData p1sornm, ActionData p2sornm)
    {

        //마찬가지로 각 격돌 이후의 액션을 넘겨줌
        MasterMoveActPriorResultCalc(p1.actor, p1sornm, p1.action, p2.action, p2sornm);
        MasterMoveActPriorResultCalc(p2.actor, p2sornm, p2.action, p2.action, p1sornm);
        return 7;
    }
*/
    /// <summary>일방 타격 처리 (한 쪽만 범위內)</summary>
    //private void ApplyUnilateralHit((int actor, ActionData action) hitter,
    //                                (int actor, ActionData action) victim)
    //{
      //  MasterMoveActPriorResultCalc(hitter.actor, victim.action, hitter.action, );

//    }

    /// <summary>양쪽 다 빗나감 처리</summary>
 /*   private void ApplyBothMiss((int actor, ActionData action) p1,
                               (int actor, ActionData action) p2)
    {
        // 아무 액션도 실행되지 않음. 필요하다면 애니메이션 훅만 호출
    }

    /// <summary>누가 승자인지 반환(없으면 -1)</summary>
    private int GetRumbleWinnerActor((int actor, ActionData action) a1,
                                     (int actor, ActionData action) a2)
    {
        if (a1.action.rumblePoint > a2.action.rumblePoint) return a1.actor;
        if (a2.action.rumblePoint > a1.action.rumblePoint) return a2.actor;
        return -1;
    }
 */

   

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
    private void UpdateCycleState(int newState) {

        if (cycleState == -1)
        {
            cycleState = newState;
            return;
        }

        if (cycleState != newState)
        {
            if (newState == 0)
            {
                players[1].energy ++;
                players[1].canMove = true;
                players[2].energy ++;
                players[2].canMove = true;
            }
            else if (newState == 1)
            {
                players[newState].energy++;
                players[newState].canMove = true;
            }
            else if (newState == 2)
            {
                players[newState].energy++;
                players[newState].canMove = true;
            }
        }
        else
        {
            if (newState == 0)
            {
                players[1].energy++;
                players[1].canMove = true;
                players[2].energy++;
                players[2].canMove = true;
            }
        }
            cycleState = newState;
    }
}
