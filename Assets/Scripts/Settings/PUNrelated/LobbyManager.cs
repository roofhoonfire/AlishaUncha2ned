using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;
using UnityEngine.Playables; // ★ 추가: 타임라인 제어

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "Test"; // 같은 버전끼리만 매칭

    [Header("UI")]
    public Text connectionInfoText; // 네트워크 정보 표시
    public Button joinButton;       // 입장 버튼
    public int MP = 2;              // 방 최대 인원
    public Overmind ovm;

    // ---------- 오프닝타이틀시네마틱 ----------
    [Header("오프닝 타이틀 시네마틱")]
    [Tooltip("개발자 모드(아무 키/마우스로 스킵 허용)")]
    public bool isDeveloper = false;

    [Tooltip("PlayableDirector 컴포넌트가 붙은 오브젝트(타이틀 타임라인)")]
    public PlayableDirector titleDirector; // 드래그&드랍

    [Tooltip("타임라인 종료 시 활성화할 오브젝트들")]
    public GameObject[] activateOnEnd;     // 드래그&드랍

    private bool _timelinePlaying = false;
    // -----------------------------------------

    // ★ Awake에서 타임라인 재생 시작
    private void Awake()
    {
        if (titleDirector != null && titleDirector.playableAsset != null)
        {
            titleDirector.stopped += OnTitleStopped;
            _timelinePlaying = true;
            titleDirector.Play();
        }
    }

    private void OnDestroy()
    {
        if (titleDirector != null) titleDirector.stopped -= OnTitleStopped;
    }

    private void Update()
    {
        // 개발자 모드에서만 스킵 허용
        if (_timelinePlaying && isDeveloper)
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
        if (!_timelinePlaying) return;
        _timelinePlaying = false;

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
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        joinButton.interactable = false;
        connectionInfoText.text = "마스터 서버에 접속 중...";
    }

    public override void OnConnectedToMaster()
    {
        joinButton.interactable = true;
        connectionInfoText.text = "온라인 : 마스터 서버와 연결됨";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        joinButton.interactable = false;
        connectionInfoText.text = "서버와 연결 끊김 접속 재시도 중..";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Connect() // 버튼으로 호출
    {
        joinButton.interactable = false; // 중복 접속 방지

        if (PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "룸에 접속 ";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            connectionInfoText.text = "서버와 연결 끊김 접속 재시도 중..";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        connectionInfoText.text = "빈 방 없음, 이제 부턴 내가 마스터다";
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = MP });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "매칭 상대를 찾는 중 ... ";
        ovm.SubmitMyDeckCodes();

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            ovm.RequestLoadBattleScene();
        }
    }
}
