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
using Microlight.MicroBar;

public class LocalState : MonoBehaviour
{
    public static LocalState Instance;
    [SerializeField] private GameObject charaprefab;

    public Dictionary<int, GameObject> PlayerObDic;
    // 각 ActorNumber에 대응하는 플레이어 상태
    public Dictionary<int, PlayerData> localPlayers = new Dictionary<int, PlayerData>();

    public List<(int actorNumber, int remainingCost)> localActionQueue = new List<(int actorNumber, int remainingCost)>();

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



    //추가됨 낄렵
    private ActionPacketData _lastApData;
    private bool _isReturningToChoose;
    //

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
            copies.Add(LocalRenderingStatic.localRenderingDatas[kvp.Key]);

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
            CharaInfo charinfo = temp.GetComponent<CharaInfo>();


            temp.transform.position = summonpoint.transform.position;
            charinfo.actorNum = pinfo.Value.actorNum;

            if (PhotonNetwork.LocalPlayer.ActorNumber == pinfo.Value.actorNum)
            {
                LocalRenderingManager.Instance.myHP = charinfo.Hp;
                LocalRenderingManager.Instance.mydefense = charinfo.Def;
                mcChecker.SetActive(true);
                SelectionBarManager.Instance.CM = temp.GetComponent<Transform>().Find("CM");
                SelectionBarManager.Instance.MM = temp.GetComponent<Transform>().Find("MM");
                SelectionBarManager.Instance.JM = temp.GetComponent<Transform>().Find("JM");
                SelectionBarManager.Instance.CM.gameObject.SetActive(false);
                SelectionBarManager.Instance.MM.gameObject.SetActive(false);
                SelectionBarManager.Instance.JM.gameObject.SetActive(false);
                CameraLovesAlisha.Instance.target = temp.GetComponent<Transform>();

                //HP 바 셋팅

                HPBarManager.Instance.mycharBarHolder = temp.GetComponent<Transform>().Find("HPHolder");

            }
            else
            {
                LocalRenderingManager.Instance.opHP = charinfo.Hp;
                LocalRenderingManager.Instance.opdefense = charinfo.Def;
                mcChecker.SetActive(false);
                HPBarManager.Instance.opcharBarHolder = temp.GetComponent<Transform>().Find("HPHolder");

            }
            PlayerObDic.Add(pinfo.Value.actorNum, temp);

        }

        HPBarManager.Instance.HPBar_Init(Overmind.Instance.initialHP);
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
        var apData = JsonConvert.DeserializeObject<ActionPacketData>(apjson);
        _lastApData = apData;                             // ★ 보관
        if (_chooseMoveRoutine != null) StopCoroutine(_chooseMoveRoutine);
        _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop(apData));
    }

    public void Start_FaceDown_Phase(int actorNumber, string apjson)
    {
        var apData = JsonConvert.DeserializeObject<ActionPacketData>(apjson);
        _lastApData = apData;                             // ★ 보관
        if (_chooseMoveRoutine != null) StopCoroutine(_chooseMoveRoutine);
        _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop(apData));
    }
    //추가됨 낄렵

    /*  public void Start_NormChoose_Phase(int actorNumber, string apjson)
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

      }*/
    //추가됨 낄렵
    public void ReturnToChooseLoop()
    {
        if (_isReturningToChoose) return;                 // 재진입 방지
        _isReturningToChoose = true;

        // 선택바는 ‘숨김 대기’ 상태로 초기화(애니 없이 깔끔)
        SelectionBarManager.Instance?.PrepareHiddenStandby();

        // 혹시 남아있을지 모를 코루틴 정리 후 재시작
        if (_chooseMoveRoutine != null) StopCoroutine(_chooseMoveRoutine);
        _chooseMoveRoutine = StartCoroutine(ChooseMoveInputLoop(_lastApData));

        _isReturningToChoose = false;
    }
    private IEnumerator ChooseMoveInputLoop(ActionPacketData apData)
    {
        InitBacktoMaster();

        // 1) 선택바 토글(나타나기 시작)
        SelectionBarManager.Instance.SetActive();

        // 2) 등장 애니 끝날 때까지 대기 → 입력 경합 차단
        //    (유틸 쓰거나, 한 줄 계산식으로도 가능)
        // yield return SelectionBarManager.Instance.WaitUntilIdle();
        yield return new WaitForSeconds(
            SelectionBarManager.Instance.rotationDuration
            + SelectionBarManager.Instance.overlapDelay * 2f
        );

        // 3) 이제부터 C/M 입력을 받음
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.M) && apData.canMove)
            {
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

        _chooseMoveRoutine = null;

        // 4) 모드 진입이 결정됐으니 선택바는 토글로 닫기
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

    public void GuessJuju(int index)
    {
        JujuGuessMode.Instance.SetActive(true, index);
    }

}