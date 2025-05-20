using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;


public class LobbyManager : MonoBehaviourPunCallbacks
{

    private string gameVersion = "Test"; //같은 버전 끼리만 매칭된다 놀라운 사실

    public Text connectionInfoText; //네트워크 정보 표시 예정
    public Button joinButton; // 입장
    public int MP = 2; // 방 별 최대 입장 가능한 클라이언트 
    public Overmind ovm;






    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings(); //이새끼가 접속 하게 해주는 녀석

        joinButton.interactable = false;
        connectionInfoText.text = "마스터 서버에 접속 중...";

    }

    public override void OnConnectedToMaster() //마스터 서버에 접속 성공시 알아서 호출됨 마스터 클라이언트가 아님 
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

    public void Connect() //이게 이제 룸에 입장하는 코드 버튼 누르면 호출되게끔

    {
        joinButton.interactable = false; //중복 접속 막음

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



    //마찬가지로 room 조인 실패시 자동 호출 ㅗ딘다 
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        connectionInfoText.text = "빈 방 없음, 이제 부턴 내가 마스터다";

        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = MP });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "매칭 성공 덤벼라 ";
        ovm.SubmitMyDeckCodes();
      
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2) //방문닫으며 호출
        {
            ovm.RequestLoadBattleScene();
        }
    }
}

//경우는 2가지일 것이다, 