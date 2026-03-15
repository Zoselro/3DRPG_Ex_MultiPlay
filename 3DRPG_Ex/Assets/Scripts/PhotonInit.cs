using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
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

    // ---- 방 목록을 표시할 UI 항목 연결 변수
    // RoomItem 차일드로 생성할 Parent 객체
    public GameObject scrollContents;
    // 룸 목록 만큼 생성될 RoomItem 프리팹
    public GameObject roomItem;
    RoomItem[] m_RoomItemList; // Contents 아래에 생성된 룸 목록을 찾기 위한 배열
    // ---- 방 목록을 표시할 UI 항목 연결 변수

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

        if(createRoomBtn != null)
        {
            Debug.Log("MakeRoom 버튼 클릭");
            createRoomBtn.onClick.AddListener(OnClickCreateRoom);
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
        AsyncOperation ao = SceneManager.LoadSceneAsync("VillageScene"); // 로딩연출 할 때 쓰는 씬 (게이지바가 올라가는 거라던지..)
        //AsyncOperation ao = SceneManager.LoadSceneAsync("GameScene");
        //AsyncOperation ao = SceneManager.LoadSceneAsync("SampleScene");
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

    // MakeRoom 버튼 클릭 시 호출되는 함수
    public void OnClickCreateRoom()
    {
        string roomName = this.roomName.text; // 방 이름 입력 UI에서 입력한 텍스트 가져오기
        // 룸 이름이 없거나 null일 경우 룸 이름 저장
        if(string.IsNullOrEmpty(this.roomName.text))
        {
            roomName = "Room_" + Random.Range(0, 999);
        }

        // 로컬 플레이어의 이름 설정
        PhotonNetwork.LocalPlayer.NickName = userID.text;
        // 플레이어 이름을 저장
        PlayerPrefs.SetString("USER_ID", userID.text);

        // 생성할 룸의 조건 설정
        RoomOptions roomOptions = new RoomOptions();

        roomOptions.IsOpen = true; // 룸 입장 허용 여부
        roomOptions.IsVisible = true; // 로비에서 룸의 노출 여부
        roomOptions.MaxPlayers = 8; // 룸에 입장 수 최대 접속자 수

        // 지정한 조건에 맞는 룸 생성 함수
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default); // 방 이름, 룸 옵션, 로비 타입(로비가 여러개 일 때 구분하기 위해서)
        // TypedLobby.Default : 포톤에서 제공하는 기본 로비 타입 (로비가 여러개 일 때 구분하기 위해서) -> 기본 로비에 방을 생성하겠다.
    }

    // PhotonNetwork.CreateRoom(); 함수가 실패 했을 경우, 호출 되는 오버라이드 함수
    // (같은 이름의 방이 존재할 때 실패함)
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 생성 실패");
        // 주로 같은 이름의 방이 존재할 때 룸 생성 에러가 발생한다. -> 방 이름을 랜덤으로 바꿔서 다시 생성 시도
        Debug.Log(returnCode.ToString()); // 에러 코드 번호
        Debug.Log(message); // 에러 메시지

    }

    // 생성된 룸 목록이 변경 되었을 때 호출되는 오버라이드 함수
    // 방 리스트 갱신은 포톤 클라우드 로비에서만 가능하다.
    // <이 함수가 호출되는 상황들 (방 정보 갱신이 필요한 상황들)>
    // 1. 내가 로비로 진입할 때 OnRoomListUpdate(List<RoomInfo> roomList) 함수가 호출되면서 방 목록을 보내 줌
    
    // 2.누군가 방을 만들거나, 방을 파괴될 때 OnRoomListUpdate(List<RoomInfo> roomList) 함수가 호출이 되면서 방 정보를 보내 줌
    // A가 로비에서 대기하고있고, B가 방을 만들고 들어가면
    // OnRoomListUpdate(List<RoomInfo> roomList) 함수가 A에게 호출이 된다. -> A는 방 목록이 업데이트 되었다는 것을 알 수 있다.
    // B가 방을 만들면서 들어갈 때는 roomList[i].RemovedFromList = false; -> A는 방 목록이 업데이트 되었다는 것을 알 수 있다.
    // B가 방을 떠나면서 방이 제거되야 할 때 roomList[i].RemovedFromList = true; -> A는 방 목록이 업데이트 되었다는 것을 알 수 있다.

    // 3. A가 로그아웃(포톤 서버에 접속 끊기) 했다가 다시 로직까지 들어올 때도
    //  OnRoomListUpdate() 함수를 받게 된다.
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("방 목록 업데이트");

        // Contents 아래에 생성된 룸 목록을 찾기 위한 배열
        // 혹시나 Active 상태가 false 인 RoomItem도 포함해서 가져오기 위해서 true
        m_RoomItemList = scrollContents.transform.GetComponentsInChildren<RoomItem>(true);
                                                                                           

        int roomCount = roomList.Count; // 방 목록의 개수
        int arrIdx = 0; // 배열 인덱스 초기화
        for(int i = 0; i < roomCount; i++)
        {
            arrIdx = MyFindIndex(m_RoomItemList, roomList[i]); // 방 목록에서 방 정보가 일치하는 RoomItem이 있는지 찾는 함수

            if (roomList[i].RemovedFromList == false)
            {// 누군가 방을 새로 생성했거나, 방정보를 갱신해 줘야 하는 상황
                if(arrIdx < 0)
                { // 방을 새로 생성하는 경우
                    // 스크롤 뷰에 붙여 줄 새로운 방 오브젝트를 새로 생성해 줘야 함
                    // --- 새로운 방 오브젝트 새로 생성
                    GameObject room = Instantiate(roomItem) as GameObject; // RoomItem 프리팹을 새로 생성
                    // 생성한 RoomItem 프리팹을 Contents의 자식으로 설정
                    room.transform.SetParent(scrollContents.transform, false); // Contents의 자식으로 설정하면서, 월드 좌표 유지 여부는 false로 설정 (false로 설정하면, 부모의 위치에 맞춰서 자식의 위치가 조정됨)
                    // 생성한 RoomItem에 표시하기 위한 텍스트 정보 전달
                    RoomItem roomData = room.GetComponent<RoomItem>(); // 생성한 RoomItem 프리팹에서 RoomItem 컴포넌트 가져오기
                    roomData.roomName = roomList[i].Name; // 방 이름 전달
                    roomData.connectPlayer = roomList[i].PlayerCount; // 방에 접속한 플레이어 수 전달
                    roomData.maxPlayers = roomList[i].MaxPlayers; // 방에 접속할 수 있는 최대 플레이어 수 전달

                    //텍스트 정보를 표시
                    roomData.DispRoomData(roomList[i].IsOpen); // 방이 열려있는지 여부 전달
                }
                else // 해당 방 목록이 존재하는 경우, 방 정보 갱신
                {
                    // 기존 방 정보만 갱신
                    m_RoomItemList[arrIdx].roomName = roomList[i].Name;
                    m_RoomItemList[arrIdx].connectPlayer = roomList[i].PlayerCount;
                    m_RoomItemList[arrIdx].maxPlayers = roomList[i].MaxPlayers;


                    //텍스트 정보를 표시
                    m_RoomItemList[arrIdx].DispRoomData(roomList[i].IsOpen);
                }
            }
            else // 방이 파괴가 되면서, 방 목록에서 제거되어야 하는 상황
            {
                if(0 <= arrIdx) // 방 목록에서 방 정보가 일치하는 RoomItem이 존재하는 경우, 해당 RoomItem 제거
                {
                    MyDestroy(m_RoomItemList, roomList[i]); // 이 방 정보를 갖고있는 리스트 뷰 목록을 모두 제거
                }
            }
        }
    }

    private int MyFindIndex(RoomItem[] rmItemList, RoomInfo roomInfo)
    {
        if(rmItemList == null || roomInfo == null) // 방 목록이 존재하지 않거나, 방 정보가 존재하지 않을 때, 방 정보가 일치하는 RoomItem이 있는지 찾기 위함
        {
            return -1;
        }

        if(rmItemList.Length <= 0) // 방 목록이 존재하지 않을 때, 방 정보가 일치하는 RoomItem이 있는지 찾기 위함
        {
            return -1;
        }

        for(int i = 0; i < rmItemList.Length; i++) // 방 목록에서 방 정보가 일치하는 RoomItem이 있는지 찾는 함수
        {
            if(rmItemList[i].roomName.Equals(roomInfo.Name)) // 방 이름이 일치하는 RoomItem이 있는지 찾는 조건문
            {
                return i;
            }
        }

        return -1; // 방 정보가 일치하는 RoomItem이 없는 경우, -1 반환
    }

    private void MyDestroy(RoomItem[] rmItemList, RoomInfo roomInfo)
    {
        if (rmItemList == null || roomInfo == null) // 방 목록이 존재하지 않거나, 방 정보가 존재하지 않을 때, 방 정보가 일치하는 RoomItem이 있는지 찾기 위함
        {
            return;
        }
        if (rmItemList.Length <= 0) // 방 목록이 존재하지 않을 때, 방 정보가 일치하는 RoomItem이 있는지 찾기 위함
        {
            return;
        }
        for (int i = 0; i < rmItemList.Length; i++) // 방 목록에서 방 정보가 일치하는 RoomItem이 있는지 찾는 함수
        {
            if (rmItemList[i].roomName.Equals(roomInfo.Name)) // 방 이름이 일치하는 RoomItem이 있는지 찾는 조건문
            {
                Destroy(rmItemList[i].gameObject); // 해당 RoomItem 오브젝트 제거
            }
        }
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

    // 방 목록에서 선택해서 버튼을 눌렀을 때 호출되는 함수
    public void OnClickRoomItem(string roomName)
    {
        Debug.Log(GetUserID() + "님이 " + roomName + " 방에 참가 시도");

        // 로컬 플레이어 이름 설정
        PhotonNetwork.LocalPlayer.NickName = userID.text;
        // 플레이어 이름을 저장
        PlayerPrefs.SetString("USER_ID", userID.text);

        // 인자로 전달 된 이름에 해당 방에 입장
        PhotonNetwork.JoinRoom(roomName);
    }
    private void OnGUI()
    {
        string str = PhotonNetwork.NetworkClientState.ToString();
        // 현재 포톤상태를 string으로 리턴 해 주는 함수
        GUI.Label(new Rect(10, 1, 1500, 60),
            "<color=#00ff00><size=35>" + str + "</size></color>");
    }
}
