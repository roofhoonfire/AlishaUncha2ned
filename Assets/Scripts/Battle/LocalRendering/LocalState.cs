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
                rightOrLeft = kvp.Value.rightOrLeft,
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