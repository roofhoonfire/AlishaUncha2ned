using System.Collections; // ★ 코루틴용
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;
using UnityEngine.Playables; // 타임라인 제어

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "dodo"; // 같은 버전끼리만 매칭

    [Header("UI")]
    public Text connectionInfoText;
    public Button joinButton;
    public int MP = 2;
    public Overmind ovm;

    // ---------- 오프닝 타이틀 시네마틱 ----------
    [Header("오프닝 타이틀 시네마틱")]
    [Tooltip("개발자 모드(아무 키/마우스로 스킵 허용)")]
    public bool isDeveloper = false;

    [Tooltip("PlayableDirector 컴포넌트가 붙은 오브젝트(타이틀 타임라인)")]
    public PlayableDirector titleDirector; // 드래그&드랍

    [Tooltip("타임라인 종료 시 활성화할 오브젝트들")]
    public GameObject[] activateOnEnd;     // 드래그&드랍

    private bool _titleTimelinePlaying = false;

    // ---------- 매칭 시작 시네마틱 ----------
    [Header("매칭 시작 시네마틱")]
    [Tooltip("Connect() 호출 전에 재생할 매칭 시작 시네마틱")]
    public PlayableDirector matchStartDirector; // 드래그&드랍

    private bool _connectFlowRunning = false;
    // -----------------------------------------

    // ★ Awake에서 타이틀 타임라인 재생 시작
    private void Awake()
    {
        if (titleDirector != null && titleDirector.playableAsset != null)
        {
            titleDirector.stopped += OnTitleStopped;
            _titleTimelinePlaying = true;
            titleDirector.Play();
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion; // 또는 Application.version
                                                 // (여기서 아직 ConnectUsingSettings() 호출하지 마)
    }

    private void OnDestroy()
    {
        if (titleDirector != null) titleDirector.stopped -= OnTitleStopped;
    }

    private void Update()
    {
        // 개발자 모드에서만 타이틀 시네마틱 스킵 허용
        if (_titleTimelinePlaying && isDeveloper)
        {
            if (Input.anyKeyDown ||
                Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2))
            {
                titleDirector.Stop(); // stopped 이벤트로 마무리
            }
        }
    }

    private void OnTitleStopped(PlayableDirector _)
    {
        if (!_titleTimelinePlaying) return;
        _titleTimelinePlaying = false;

        // 종료와 동시에 등록된 오브젝트 활성화
        if (activateOnEnd != null)
        {
            foreach (var go in activateOnEnd)
            {
                if (go != null) go.SetActive(true);
            }
        }
    }

    // ------------------- 기존 로비 로직 -------------------
    void Start()
    {
       // PhotonNetwork.AutomaticallySyncScene = true;
       // PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        if (joinButton) joinButton.interactable = false;
        if (connectionInfoText) connectionInfoText.text = "마스터 서버에 접속 중...";
    }

    public override void OnConnectedToMaster()
    {
        if (joinButton) joinButton.interactable = true;
        if (connectionInfoText) connectionInfoText.text = "온라인 : 마스터 서버와 연결됨";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (joinButton) joinButton.interactable = false;
        if (connectionInfoText) connectionInfoText.text = "서버와 연결 끊김 접속 재시도 중..";
        PhotonNetwork.ConnectUsingSettings();
    }

    // ★ 버튼으로 호출: 매칭 시작 시네마틱 → 끝날 때까지 대기 → 네트워크 처리
    public void Connect()
    {
        if (_connectFlowRunning) return; // 중복 호출 방지
        StartCoroutine(ConnectFlow());
    }

    private IEnumerator ConnectFlow()
    {
        _connectFlowRunning = true;

        // UI 잠금
        if (joinButton) joinButton.interactable = false;

        // 1) 매칭 시작 시네마틱 재생 & 종료까지 대기
        yield return PlayAndWait(matchStartDirector);

        // 2) 기존 네트워크 로직
        if (PhotonNetwork.IsConnected)
        {
            if (connectionInfoText) connectionInfoText.text = "룸에 접속 ";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            if (connectionInfoText) connectionInfoText.text = "서버와 연결 끊김 접속 재시도 중..";
            PhotonNetwork.ConnectUsingSettings();
        }

        _connectFlowRunning = false;
    }

    // PlayableDirector 재생 후 종료까지 프레임 단위로 대기
    private IEnumerator PlayAndWait(PlayableDirector dir)
    {
        if (dir == null || dir.playableAsset == null)
            yield break;

        dir.time = 0;
        dir.Play();

        // 재생 중 루프
        while (dir.state == PlayState.Playing)
        {
            // 개발자 모드면 스킵 허용
            if (isDeveloper &&
                (Input.anyKeyDown ||
                 Input.GetMouseButtonDown(0) ||
                 Input.GetMouseButtonDown(1) ||
                 Input.GetMouseButtonDown(2)))
            {
                dir.Stop();
                break;
            }
            yield return null;
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (connectionInfoText) connectionInfoText.text = "빈 방 없음, 이제 부턴 내가 마스터다";
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = MP });
    }

    public override void OnJoinedRoom()
    {
        if (connectionInfoText) connectionInfoText.text = "매칭 상대를 찾는 중 ... ";
        ovm.SubmitMyDeckCodes();

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            //여기서 매칭 성공 시네마틱 하나 넣으면 ㅇㅋ
            ovm.RequestLoadBattleScene();
        }
    }
}
