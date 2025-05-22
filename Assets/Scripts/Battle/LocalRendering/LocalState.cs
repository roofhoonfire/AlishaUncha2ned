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

public class LocalState : MonoBehaviour
{
    public static LocalState Instance;
    [SerializeField] private GameObject charaprefab;

    public Dictionary<int , GameObject > PlayerObDic;
    // 각 ActorNumber에 대응하는 플레이어 상태
    public Dictionary<int, PlayerData> localPlayers = new Dictionary<int, PlayerData>();

    public List<(int actorNumber,  int remainingCost)> localActionQueue =new List<(int actorNumber, int remainingCost)>();

    private Coroutine _chooseMoveRoutine;
 
    private Coroutine _tempActionAlert;

    private int previousHPMe = 100; //플레이어 데이터에 붙은 prev는 싸이클 체커용이라 별개임

    private int previousHPOp = 100;

    public TextMeshPro myHP;
    public TextMeshPro opHP;
    public TextMeshPro mydefense;
    public TextMeshPro opdefense;

    public TextMeshProUGUI opponencostRemainTxt;


    public GameObject alim; // 나중엔 걍 애니메이션으로 퉁쳐잇~
    public GameObject dialoguePanel; //마찬가지로 나중엔 죽일거다
    public  GameObject dialogueText;
    //s나중에 렌더링 따로 싹다 새 클래스로 빼서 쓰자..

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
                localData.defense = masterData.defense;
                localData.prevHP = masterData.prevHP;
                localData.energy = masterData.energy;
                localData.isStunned = masterData.isStunned;
                // 덱 정보도 완전히 교체
               // localData.DeckCodes = new List<string>(masterData.DeckCodes);
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


        Debug.Log("LocalState: 부분 동기화 완료");
    }


    /// <summary>
    /// 전체 로컬 상태를 초기화합니다.
    /// (예: Battle 씬 진입 시 초기 동기화용)
    /// </summary>
    public void SyncAll(Dictionary<int, PlayerData> copiedData)
    {
        localPlayers.Clear();

        foreach (var kvp in copiedData)
        {
            var player = kvp.Value;
            localPlayers[player.ActorNumber] = new PlayerData(
                player.ActorNumber,
                new List<string>(player.DeckCodes),
                player.HP
            )
            {
                curpos = player.curpos
            };
        }

        Debug.Log("LocalState: 전체 플레이어 상태 초기화됨");
        //d아래는 디버깅 용이다 나중에 지우기
        foreach (var kvp in localPlayers)
        {
            var player = kvp.Value;
            string deckStr = string.Join(", ", player.DeckCodes);
            Debug.Log($"ActorNumber: {player.ActorNumber}, HP: {player.HP}, Pos: {player.curpos}, Deck: [{deckStr}]");
        }

        CharaObInit();
        PlayerStateUIRendering();
    }



    public void CharaObInit()
    {
        //여기서 렌더링 정보 다하기 . 스킨 보는 방향 등.
        foreach (var pinfo in localPlayers)


        {
            Transform summonpoint = tileIndextoPosition(pinfo.Value.curpos);
            GameObject temp = Instantiate(charaprefab);
            GameObject mcChecker = temp.transform.Find("MyChara")?.gameObject;

            CharaInfo charinfo  = temp.GetComponent<CharaInfo>();
            temp.transform.position = summonpoint.transform.position;
            charinfo.actorNum = pinfo.Value.ActorNumber;

            if (PhotonNetwork.LocalPlayer.ActorNumber == pinfo.Value.ActorNumber)
            {
                myHP = charinfo.Hp;
                mydefense = charinfo.Def;
                mcChecker.SetActive(true);
            }
            else
            {
                opHP = charinfo.Hp;
                opdefense = charinfo.Def;
                mcChecker.SetActive(false);
            }
            PlayerObDic.Add(pinfo.Value.ActorNumber, temp);
            
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
        // 이미 대기 중이면 중단
        if (_chooseMoveRoutine != null)
            StopCoroutine(_chooseMoveRoutine);

        // 키 입력 대기 코루틴 시작
        _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop());

    }

    private IEnumerator ChooseMoveInputLoop()
    {
        // 대기 상태
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                // M 키 눌리면 MoveModeState의 반복 로직 시작
                MoveModeState.Instance.SetActive(true);
                break;
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
           
              CardModeState.Instance.SetActive(true);
              break;
           }

            yield return null;
        }

        // 코루틴 참조 해제
        _chooseMoveRoutine = null;
    }

 
    IEnumerator tempActionAlert(ActionData action, int actorNum, HookType h)
    {
        string message = $"플레이어 {actorNum}의 {action.cardname}의 {h} 크하하하!";
        float startDelay = 0.5f;
        float typingSpeed = 0.1f;
        dialoguePanel.SetActive(true);
        dialogueText.GetComponent<TextMeshProUGUI>().text = "";
        
        yield return new WaitForSeconds(startDelay); // 타이핑 시작 전 0.5초 지연

        foreach (char letter in message.ToCharArray())
        {
            dialogueText.GetComponent<TextMeshProUGUI>().text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(2f); // 타이핑 완료 후 2초 대기
        dialoguePanel.SetActive(false);
        _tempActionAlert = null;
    }
    IEnumerator StartTempActionAlertWhenFree(ActionData action, int actorNum, HookType h)
    {
        // 이미 실행 중이라면, 끝날 때까지 매 프레임 대기
        while (_tempActionAlert != null)
        {
            yield return null;
        }

        // null이 된 순간, 새 코루틴을 시작하고 _tempActionAlert에 할당
        _tempActionAlert = StartCoroutine(tempActionAlert(action, actorNum, h));
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

            Debug.Log($"플레이어 {actorNum}의 {action.cardname}의 {h} 크후후!");
            StartCoroutine(StartTempActionAlertWhenFree(action, actorNum, h));
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

    private void OpponentCostRendering(HookType h)
    {
        int myactorNum = PhotonNetwork.LocalPlayer.ActorNumber;

        int realcost = GetMinOpLocalRemainingCost(myactorNum);

        if (h == HookType.Activate)
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
        mydefense.text = localPlayers[myactorNum].defense.ToString();
        opdefense.text = localPlayers[actorNumOp].defense.ToString();
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
   
}