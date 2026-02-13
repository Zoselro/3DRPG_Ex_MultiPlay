using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class PhotonInit : MonoBehaviourPunCallbacks // 포톤에서 제공해주는 MonoBehaviour를 상속받은 클래스
{
    // 플레이어 이름을 입력하는 UI 항목 연결
    public InputField userID;
    public Button JoinRandomRoomBtn;

    // 룸 이름을 입력 받을 UI 항목 연결 변수
    public InputField roomName;
    public Button createRoomBtn;

    private void Awake()
    {
        // 포톤 서버 접속 확인
        // (인게임에서 빠져나온 경우가 있기 때문)
        if(!PhotonNetwork.IsConnected) // 포톤서버에 접속이 되어있지 않으면
        {
            // 1번 포톤 클라우드에 접속 시도
            PhotonNetwork.ConnectUsingSettings(); // 포톤 본사에 검증
            // 포톤 서버에 접속시도 (지역서버 접속) ---> AppID 사용자 인증
            Debug.Log("test");
        }
        userID.text = GetUserID();
        roomName.text = "Room_" + Random.Range(0, 999).ToString("000");
        // 사용자 이름 설정
    }

    private void Start()
    {
        if(JoinRandomRoomBtn  != null)
        {
            JoinRandomRoomBtn.onClick.AddListener(OnClickJoinRandomRoom);
        }
    }

    #region 포톤 서버에서 제공해주는 콜백함수들
    // PhotonNetwork.ConnectUsingSettings() 이 정상적으로 호출이 되어 성공을 하게되면 실행되는 콜백 함수
    // PhotonNetwork.LeaveRoom(); 으로 방을 떠날 때도 로비로 접속 되며 이 메서드가 자동으로 호출.
    public override void OnConnectedToMaster()
    {
        Debug.Log("포톤 서버 접속 완료");
        // 단순 포톤서버 접속만 된 상태(ConnectToMaster)

        // 규모가 작은 게임이면, 서버 로비가 보통 하나이고, 
        // 대형 게임이면 상급자로비, 중급자로비, 초급자로비처럼
        // 로비가 여러개 일 수 있다.
        PhotonNetwork.JoinLobby(); // 포톤에서 제공해주는 가상의 로비에 접속 시도
    }


    //PhotonNetwork.JoinLobby() 성공시 호출되는 로비 접속 콜백 함수
    public override void OnJoinedLobby()
    {
        Debug.Log("로비 접속 완료!");
        userID.text = GetUserID();
    }

    //PhotonNetwork.JoinRandomRoom(); 함수가 실패 했을 경우, 호출 되는 오버라이드 함수
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 참가 실패 (참가 할 방이 존재 하지 않습니다.)");

        // 방을 생성하면서 들어감
        // 생성할 룸의 조건 부터 설정

        RoomOptions roomOptions = new RoomOptions(); // using Photon.RealTime;
        roomOptions.IsVisible = true; // 로비에서 룸의 노출 여부
        roomOptions.MaxPlayers = 8;     // 룸에 입장 수 최대 접속자 수

        // 지정한 조건에 맞는 룸 생성 함수
        PhotonNetwork.CreateRoom("MyRoom", roomOptions);
    }

    // PhotonNetwork.CreateRoom(); 함수가 성공하면, 자동으로 호출되는 메서드
    // PhotonNetwork.JoinRoom(); 방을 지정(클릭) 해서 입장시 함수가 성공하면 자동으로 호출 되는 함수
    // PhotonNetwork.JoinRandomRoom(); 함수가 성공했을 때 자동으로 호출되는 함수
    
    public override void OnJoinedRoom()
    {   // 서버 역할인 경우 : 방입장
        // 클라이언트 역할인 경우 : JoinRoom, JoinRandomRoom --> 방 입장
        Debug.Log("방 참가 완료");

        StartCoroutine(this.LoadBattleField());
        // 룸씬으로 이동하는 코루틴 실행
    }

    private IEnumerator LoadBattleField()
    {
        // 씬을 이동하는 동안 포톤 클라이드 서버로부터 네크워크 메시지 수신 중단
        PhotonNetwork.IsMessageQueueRunning = false;
        // 백그라운드로 씬 로딩
        AsyncOperation ao = SceneManager.LoadSceneAsync("SampleScene"); // 로딩연출 할 때 쓰는 씬 (게이지바가 올라가는 거라던지..)
        yield return ao;
    }

    #endregion
    private void OnClickJoinRandomRoom()
    {
        // 로컬 플레이어 이름 설정
        PhotonNetwork.LocalPlayer.NickName = userID.text;

        // 플레이어 이름을 저장
        PlayerPrefs.SetString("USER_ID", userID.text);

        // 무작위 방 입장
        PhotonNetwork.JoinRandomRoom();
    }



    //로컬에 저장된 플레이어 이름을 반환하거나 생성하는 함수

    private string GetUserID()
    {
        string userID = PlayerPrefs.GetString("USER_ID");
        if(string.IsNullOrEmpty(userID))
        {
            userID = "USER " + Random.Range(0, 999).ToString("000");
        }
        return userID;
    }

    private void OnGUI()
    {
        string str = PhotonNetwork.NetworkClientState.ToString();
        // 현재 포톤상태를 string으로 리턴 해 주는 함수
        GUI.Label(new Rect(10, 1, 1500, 60),
            "<color=#00ff00><size=35>" + str + "</size></color>");
    }
}
