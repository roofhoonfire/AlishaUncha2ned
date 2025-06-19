using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using DG.Tweening;
using System.Collections;
using UnityEditor;
using Photon.Realtime;
using TMPro;
using System.Reflection.Emit;
using System.Linq;
using UnityEditor.Rendering;
using Newtonsoft.Json;

public class LocalState : MonoBehaviour
{
    public static LocalState Instance;
    [SerializeField] private GameObject charaprefab;

    public Dictionary<int , GameObject > PlayerObDic;
    // 각 ActorNumber에 대응하는 플레이어 상태
    public Dictionary<int, PlayerData> localPlayers = new Dictionary<int, PlayerData>();

    public List<(int actorNumber,  int remainingCost)> localActionQueue =new List<(int actorNumber, int remainingCost)>();

    private Coroutine _chooseMoveRoutine;
    public BacktoMaster btmPacket;


    private int previousHPMe = 100; //플레이어 데이터에 붙은 prev는 싸이클 체커용이라 별개임

    private int previousHPOp = 100;

    public TextMeshPro myHP;
    public TextMeshPro opHP;
    public TextMeshPro mydefense;
    public TextMeshPro opdefense;

    public TextMeshProUGUI opponencostRemainTxt;
    public TextMeshProUGUI mycostRemainTxt;

    public GameObject alim; // 나중엔 걍 애니메이션으로 퉁쳐잇~
  
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
        PlayerObDic = new Dictionary<int, GameObject>();
    }

    /// <summary>
    /// 마스터 클라이언트에서 받은 data로 로컬 플레이어 상태만 부분 동기화합니다.
    /// 이미 localPlayers에 키가 존재한다고 가정하고, 값만 갱신합니다.
    /// </summary>
    public void SyncPartial(Dictionary<int, PlayerData> data, List<Dictionary<string, int>>simpleq)
    {
        foreach (var kvp in data)
        {
            int actorNumber = kvp.Key;
            PlayerData masterData = kvp.Value;

            if (localPlayers.ContainsKey(actorNumber))
            {
                PlayerData localData = localPlayers[actorNumber];
                localData.HP = masterData.HP;
                localData.curpos = masterData.curpos;
                localData.prevHP = masterData.prevHP;
                localData.energy = masterData.energy;
                localData.canMove = masterData.canMove;
                localData.isStunned = masterData.isStunned;
                localData.actionClockManuplate = masterData.actionClockManuplate;
                localData.defenseManuplate  = masterData.defenseManuplate;
                localData.timeBombSetinTomMotion    =       masterData  .timeBombSetinTomMotion;
                localData.boundIndex = masterData.boundIndex;


                //수치 업뎃//
                localData.defaultMove = masterData.defaultMove; 
                localData.defaultMoveCast   = masterData.defaultMoveCast;   



                //손패 업뎃
                localData.DeckCodes = new List<string>(masterData.DeckCodes);

                localData.hands = new List<string>(masterData.hands);

                localData.trash = new List<string>(masterData.trash);

            }
            else
            {
                Debug.LogWarning($"LocalState: actorNumber {actorNumber}가 존재하지 않음, 동기화 건너뜀.");
            }
        }

        //큐 복사 ㅎ

        localActionQueue.Clear();
        foreach (var dict in simpleq)
        {
            int actorNum = dict["actorNumber"];
            int remainingCost = dict["remainingCost"];
            localActionQueue.Add((actorNum, remainingCost));
        }

        PlayerStateUIRendering();

        Debug.Log("LocalState: 부분 동기화 완료");
    }


    /// <summary>
    /// 전체 로컬 상태를 초기화합니다.
    /// (예: Battle 씬 진입 시 초기 동기화용)
    /// </summary>
    public void GameStart(Dictionary<int, LocalRenderingData> copiedData)
    {
        LocalRenderingStatic.localRenderingDatas.Clear();
        List<LocalRenderingData> copies = new List<LocalRenderingData>();
        int i = 0;
        foreach (var kvp in copiedData)
        {
            LocalRenderingData copied = new LocalRenderingData()
            {
                actorNum = kvp.Key,
                curpos = kvp.Value.curpos,
                hp = kvp.Value.hp,
                defense = kvp.Value.defense,
                bounds = new List<string>(kvp.Value.bounds), // 참조형 필드는 복제
                remainingCost = kvp.Value.remainingCost,
                boundIndex = kvp.Value.boundIndex,
            };

            LocalRenderingStatic.localRenderingDatas[kvp.Key] = copied;
            copies.Add( LocalRenderingStatic.localRenderingDatas[kvp.Key]);

            i++;
        }

        Debug.Log("LocalState: 전체 플레이어 상태 초기화됨");
        //d아래는 디버깅 용이다 나중에 지우기
        
        //게임 시작 연출 하나 집어넣기 ;


        CharaObInit();
        LocalRenderingManager.Instance.Rendering_GameStart(copies[0], copies[1]);

       //PlayerStateUIRendering();
    }



    public void CharaObInit()
    {
        //여기서 렌더링 정보 다하기 . 스킨 보는 방향 등.
        //셀렉션 바 연결
        foreach (var pinfo in LocalRenderingStatic.localRenderingDatas)


        {
            Transform summonpoint = tileIndextoPosition(pinfo.Value.curpos);
            GameObject temp = Instantiate(charaprefab);
            GameObject mcChecker = temp.transform.Find("MyChara")?.gameObject;

            
            //렌더링 용 커넥션 변수
            CharaInfo charinfo  = temp.GetComponent<CharaInfo>();
          
            
            temp.transform.position = summonpoint.transform.position;
            charinfo.actorNum = pinfo.Value.actorNum;

            if (PhotonNetwork.LocalPlayer.ActorNumber == pinfo.Value.actorNum)
            {
                LocalRenderingManager.Instance.myHP = charinfo.Hp;
                LocalRenderingManager.Instance.mydefense = charinfo.Def;
                mcChecker.SetActive(true);
               SelectionBarManager.Instance.CM =  temp.GetComponent<Transform>().Find("CM");
               SelectionBarManager.Instance.MM = temp.GetComponent<Transform>().Find("MM");
               SelectionBarManager.Instance.JM = temp.GetComponent<Transform>().Find("JM");
                SelectionBarManager.Instance.CM.gameObject.SetActive(false);
                SelectionBarManager.Instance.MM.gameObject.SetActive(false);
                SelectionBarManager.Instance.JM.gameObject.SetActive(false);
                CameraLovesAlisha.Instance.target = temp.GetComponent<Transform>();

            }
            else
            {
                LocalRenderingManager.Instance.opHP = charinfo.Hp;
                LocalRenderingManager.Instance.opdefense = charinfo.Def;
                mcChecker.SetActive(false);
            }
            PlayerObDic.Add(pinfo.Value.actorNum, temp);
            
        }


    }

    private Transform tileIndextoPosition(int tileindex)
    {

        //이거 로컬에서도 그리드 초기화 해야함 
        return GridManagement.Instance?.tileObjects[tileindex].transform.Find("charpoint");
        //리턴 된 놈은 Transform으로 받고 .transform.position으로 써야 작동

    }


   

    //해줄일 액터 넘버도 같이 변수로 넘겨줘서 어느 액터인지

    public void StartChooseMovePhase(int actorNumber)
    {

        Debug.Log($"이번턴의 제약은 {localPlayers[actorNumber].Bounds[localPlayers[actorNumber].boundIndex]}");
        // 이미 대기 중이면 중단
        if (_chooseMoveRoutine != null)
            StopCoroutine(_chooseMoveRoutine);

        // 키 입력 대기 코루틴 시작
     //   _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop());

    }

    public void Start_NormChoose_Phase(int actorNumber, string apjson)
    {
        ActionPacketData apData = JsonConvert.DeserializeObject<ActionPacketData>(apjson);
        var playerData = LocalRenderingStatic.localRenderingDatas[actorNumber];
        Debug.Log($"이번턴의 제약은 {playerData.bounds[playerData.boundIndex]}");
        // 이미 대기 중이면 중단
        if (_chooseMoveRoutine != null)
            StopCoroutine(_chooseMoveRoutine);

        // 키 입력 대기 코루틴 시작
          _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop(apData));

    }

    public void Start_FaceDown_Phase(int actorNumber, string apjson)
    {
        ActionPacketData apData = JsonConvert.DeserializeObject<ActionPacketData>(apjson);  
        if (_chooseMoveRoutine != null)
            StopCoroutine(_chooseMoveRoutine);

        // 키 입력 대기 코루틴 시작
        _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop(apData));

    }
    private IEnumerator ChooseMoveInputLoop(ActionPacketData apData)
    {
        InitBacktoMaster();

        SelectionBarManager.Instance.SetActive();
        // 대기 상태
       
        //여기서 n초 기다려야 셀렉션 바 삐꾸 안날듯 
       //페이스 다운 페이즈에서 호출시 m 은 불가하게 
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.M) && 
                apData.canMove
                )
            {
                // M 키 눌리면 MoveModeState의 반복 로직 시작
                MoveModeState.Instance.SetActive(true, apData);
                break;
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
           
              CardModeState.Instance.SetActive(true, apData);
              break;
           }

            yield return null;
        }

        // 코루틴 참조 해제
        _chooseMoveRoutine = null;
        SelectionBarManager.Instance.SetActive();

    }


    //마스터한테 돌려줄 것들을 담는 패키지를 초기화한다
    private void InitBacktoMaster()
    {
        btmPacket = new BacktoMaster();
        btmPacket.usedCard = new List<string>();


    }


    public void PlayActionRendering(int actorNum, ActionData action, int opCost, HookType h) //여기에 상대 액숀도받아 와서 남은 시간 체크
    {
        if (action.actionId == 0)//이동이면
        {
            LocalMoveRendering(actorNum, action.destindex);
            Debug.Log($"플레이어 {actorNum}의 훌륭한 이동이다!"); 

            //나중에 이것도 move 이펙트에 애니메이션 넣어서 일괄로 처리해도됨

        }
        else if (action.actionId == 1)
        { //처맞고 때리고 하는 애니메이숑 액션에 붙어있는 애니메이션 클립출력
            if (action.destindex != -10)
            {
                //이동 + 이동 아니메이션

                LocalMoveRendering(actorNum, action.destindex);

            }
            Debug.Log($"플레이어 {actorNum}의 {action.cardname}의 {h} ");
            AlertDialogue.Instance.StartDialogue(action, actorNum, h, 0,DialogueType.Activate);
        }

        OpponentCostRendering(h);
        PlayerStateUIRendering();
        Overmind.Instance?.SubmintRenderingDone(actorNum);

    }
    public void PlayShowDownRendering(int actorNum1, int actorNum2, ActionData action1, ActionData action2, int flag, int opofa1cost, int opofa2cost, HookType h)
    {
        int myactorNum = PhotonNetwork.LocalPlayer.ActorNumber;
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




        if(flag == 1)
        {
            if(myactorNum == actorNum1)
            {
                Debug.Log("격돌에서 승리했다 죽여버리자!"); 
            }
            else
            {
                
                Debug.Log("격돌에서 패배햇다ㅠ");
            }

        }

        if (flag == 2)
        {
            if (myactorNum == actorNum2)
            {
               

                Debug.Log("격돌에서 승리했다 죽여버리자!");
            }
            else
            {
               

                Debug.Log(" 격돌에서 패배했다");
            }


        }

        if (flag == 3)
        {

            Debug.Log("우우.. 맥빠지는군");
            
        }

        if (flag == 4)
        {

            Debug.Log("00후 처맞았대요~");
           
        }
        if (flag == 5)
        {

            Debug.Log("처맞고 00했대요~");
           
        }
        if (flag == 6)
        {

            Debug.Log("아슬아슬했는걸!");

        }
        if (flag == 7)
        {

            Debug.Log("비김");
        

         }

        //원래는 케이스 별로 재생 순서 및 연출을 달리 해야하지만
        //지금은 귀찮으니 아래와 같이 구현한다 

        
        PlayActionRendering(actorNum1, action1, opofa1cost, h);

        PlayActionRendering(actorNum2, action2, opofa2cost, h);


     //   Overmind.Instance?.SubmintRenderingDone(myactorNum);
    }

    private void LocalMoveRendering(int actorNumber, int destindex)
    {

        //이거 사실 중복 ㅋlocalPlayers[actorNumber].curpos = destindex;
        GameObject chara = PlayerObDic[actorNumber];
       
        chara.transform.position = tileIndextoPosition(destindex).position;


    }

    public void FaceOffRendering(int nthfaceoff)
    {

        alim.SetActive(true);
        alim.GetComponent<TextMeshProUGUI>().text = $"{nthfaceoff}번째 페이스 오프!!";



    }

    public void OpponentCostRendering(HookType h)
    {
        int myactorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        int realcost = GetMinOpLocalRemainingCost(myactorNum);

        //값이 바뀌었을 때만 애니 출력하게 바꾸자
        if (h == HookType.Activate)// 카드 중 발동 효과로 상대의 액션을 느리게 만드는 놈도 있기 땜에 넣어줘야함//일단은?
        {
            if (realcost != 0)
                opponencostRemainTxt.text = $"상대 행동까지 {realcost} 남았다";
            else if (realcost == 0)
            {

                opponencostRemainTxt.text = $"상대 행동 결정 중 ";

            }

        }

    }
   
    private void PlayerStateUIRendering()
    {
        //기본적으로 모든 값은 바뀌었을 때 체크해주는게 젤 좋을 거같음
        int myactorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        int actorNumOp = Overmind.Instance.GetOtherPlayerNumber(myactorNum);
        //일단 바뀌면 바꾸는건 주석 처리해놈
        
           // if (previousHPOp != localPlayers[actorNumOp].HP)
            //{

              opHP.text = localPlayers[actorNumOp].HP.ToString();
           //  previousHPOp = localPlayers[actorNumOp].HP;
            //}

      
            //if (previousHPMe != localPlayers[myactorNum].HP)
            //{

                myHP.text = localPlayers[myactorNum].HP.ToString();
              //  previousHPMe = localPlayers[myactorNum].HP;
            //}

        //연출 주고 싶으면 prevdefense 값 셋팅해서 바뀌면 애니메 뚜루룽~
        //체력 바뀐거 렌더링해줏3


        //에너지 렌더링 도 여기서 한다잉
    }

    public int GetMinOpLocalRemainingCost(int actorNum)
    {
        // actorNum과 다르고, 남은 코스트를 뽑아서
        var opponentCosts = localActionQueue
            .Where(entry => entry.actorNumber != actorNum)
            .Select(entry => entry.remainingCost);

        // 만약 상대 액션이 없다면 0을 반환 (필요하다면 다른 기본값으로 바꿔도 됨)
        if (!opponentCosts.Any())
            return 0;

        // 그중 최솟값 반환
        return opponentCosts.Min();
    }

    public void SelectJuju(int actorNum, string boundCode, List<string> jujucodes)
    {
        JujuSelectModeState.Instance.SetActive(true, actorNum, boundCode, jujucodes);

    }
   

 
}