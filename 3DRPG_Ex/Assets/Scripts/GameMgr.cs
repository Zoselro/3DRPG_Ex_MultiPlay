using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMgr : MonoBehaviourPunCallbacks
{
    PhotonView pv;

    [HideInInspector] public Hero_Ctrl m_RefHero = null;

    //--- MouseClickMark 처리변수
    public GameObject m_MsClickMark = null;
    Vector3 m_CacVLen = Vector3.zero;
    //--- MouseClickMark 처리변수

    [Header("--- Button Handle ---")]
    public Button m_BackBtn = null;
    public Button m_Attack_Btn = null;
    public Button m_Skill_Btn = null;

    //--- 스킬 쿨 타임 적용
    Text m_Skill_Cool_Label = null;
    Image m_Skill_Cool_Mask = null;
    Button m_Sk_UI_Btn = null;
    [HideInInspector] public float m_Skill_CurCool = 0.0f;
    float m_Skill_CoolDur = 7.0f;
    //--- 스킬 쿨 타임 적용

    [Header("--- Damage Text ---")]
    public Transform m_DText_Canvas_W = null;
    public GameObject m_DTextPrefab_W = null;

    [Header("--- Shader ---")]
    public Shader g_AddTexShader = null;    //주인공 데미지 연출용(빨간색으로 변했다 돌아올 때)
    public Shader g_VertexLitShader = null; //몬스터 사망시 투명하게 사라지게 하기 용

    // 접속 로그를 표시할 Text UI 항목 변수
    [Header("--- Chatting ---")]
    public Text txtLogMsg;
    public InputField InputFdChat;
    [HideInInspector] public bool bEnter = false;
    List<string> m_MsgList = new List<string>();

    //--- 싱글턴 패턴을 위한 인스턴스 변수 선언
    public static GameMgr Inst = null;

    void Awake()
    {
        Inst = this;

        // Photon View 컴포넌트 할당
        pv = GetComponent<PhotonView>();

        // 주인공 생성하는 함수 호출
        CreateHero();
    }
    //--- 싱글턴 패턴을 위한 인스턴스 변수 선언

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        Time.timeScale = 1.0f; // 일시정지 풀어주기
        PhotonNetwork.IsMessageQueueRunning = true; // 통신을 주고받는 처리를 다시 동기화

        //--- Attack Button 처리 코드
        if (m_Attack_Btn != null)
            m_Attack_Btn.onClick.AddListener(() =>
            {
                if (m_RefHero != null)
                    m_RefHero.AttackOrder();
            });

        //--- Skill Button 처리 코드
        m_Skill_CurCool = 0.0f;

        if (m_Skill_Btn != null)
        {
            m_Skill_Btn.onClick.AddListener(() =>
            {
                if (m_RefHero != null)
                    m_RefHero.SkillOrder("RainArrow",
                            ref m_Skill_CoolDur, ref m_Skill_CurCool);
            });

            m_Skill_Cool_Label = m_Skill_Btn.transform.GetComponentInChildren<Text>();
            m_Skill_Cool_Mask = m_Skill_Btn.transform.Find("SkillCoolMask").GetComponent<Image>();

            m_Sk_UI_Btn = m_Skill_Btn.GetComponent<Button>();
        }
        //--- Skill Button 처리 코드

        if (m_BackBtn != null)
            m_BackBtn.onClick.AddListener(OnClickBackBtn);

        // 로그 메시지에 출력할 문자열 생성
        string msg = "\n<color=#33ff33>[" +
                        PhotonNetwork.LocalPlayer.NickName +
                        "] Connected</color>";

        // RPC 함수 호출
        // All : 현재 플레이어 모두 실행
        // Others : 나 제외하고 실행
        // MasterClient : 마스터(방장)만 실행
        // AllBuffered : 모두 실행 + 기록 저장 -> 맵에 있는 나무통을 부쉈을 때 난입한 플레이어의 화면에서도 그 나무통은 부서져 있어야함
        // AllViaServer : 나도 서버를 거쳐서 늦게 받음 -> 다른 사람들과 비슷하게 받음.
        // AllBufferedViaServer : 서버를 거쳐서 모두에게 실행하고, 나중에 온 사람을 위해 기록도 남김.
        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg, false);

    }//void Start()

    // Update is called once per frame
    void Update()
    {
        MsOffObserver();

        SkillCool_Update();

        //--- 채팅 구현 텍스트
        if (Input.GetKeyUp(KeyCode.Return))
        {// 엔터키를 누르면 인풋 필드 활성화
            bEnter = !bEnter;

            if (bEnter)
            {
                InputFdChat.gameObject.SetActive(true);
                InputFdChat.ActivateInputField(); // <--- 키보드 커서 입력 상자 쪽으로 가게 만들어 줌
            }
            else
            {
                InputFdChat.gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(InputFdChat.text.Trim()))
                {
                    BroadcastingChat();
                }
            }
        }
    }

    public void MsClickMarkOn(Vector3 pickPos)
    {
        if (m_MsClickMark == null)
            return;

        m_MsClickMark.transform.position =
                new Vector3(pickPos.x, pickPos.y + 0.1f, pickPos.z);

        m_MsClickMark.SetActive(true);
    }

    void MsOffObserver() //<-- 클릭마크 끄기 감시자 함수
    {
        if (m_MsClickMark == null)
            return;

        if (m_MsClickMark.activeSelf == false)
            return;

        if (m_RefHero == null)
            return;

        m_CacVLen = m_RefHero.transform.position -
                        m_MsClickMark.transform.position;
        m_CacVLen.y = 0.0f;

        if (m_CacVLen.magnitude < 1.0f)
            m_MsClickMark.SetActive(false);

    }//void MsOffObserver()

    public void SpawnDText_W(int dmg, Vector3 spPos, int colorInx = 0)
    {
        if (m_DTextPrefab_W == null || m_DText_Canvas_W == null)
            return;

        GameObject dmgObj = Instantiate(m_DTextPrefab_W);

        if (colorInx == 1) //주인공인 경우
        {
            if (m_RefHero != null)
            {
                Canvas fCanvas = m_RefHero.GetComponentInChildren<Canvas>();
                if (fCanvas != null)
                    dmgObj.transform.SetParent(fCanvas.transform);
            }//if(m_RefHero != null)
        }//if(colorInx == 1) //주인공인 경우
        else //몬스터인 경우
        {
            dmgObj.transform.SetParent(m_DText_Canvas_W);
        }

        DamageText_W damageTx = dmgObj.GetComponentInChildren<DamageText_W>();
        if (damageTx != null)
        {
            if (colorInx == 1) //주인공인 경우
                damageTx.InitState(-dmg, spPos, new Color32(255, 255, 230, 255), false);
            else
                damageTx.InitState(-dmg, spPos, new Color32(255, 255, 255, 255));
        }//if(damageTx != null)

    }//public void SpawnDText_W(int dmg, Vector3 a_SpPos, int a_ColorIdx = 0)

    void SkillCool_Update()
    {
        if (0.0f < m_Skill_CurCool)
        {
            m_Skill_CurCool -= Time.deltaTime;
            m_Skill_Cool_Label.text = ((int)m_Skill_CurCool).ToString();
            m_Skill_Cool_Mask.fillAmount = m_Skill_CurCool / m_Skill_CoolDur;

            if (m_Sk_UI_Btn != null)
                m_Skill_Btn.enabled = false;    //버튼 눌려지지 않게 하기...
        }
        else
        {
            m_Skill_CurCool = 0.0f;
            m_Skill_Cool_Label.text = "";
            m_Skill_Cool_Mask.fillAmount = 0.0f;

            if (m_Sk_UI_Btn != null)
                m_Skill_Btn.enabled = true;   //버튼 눌려지지 않게 하기...
        }
    }//void Skill_Cool_Updade()

    public bool IsPointerOverUIObject()
    {   //마우스가 UI를 위에 있는지? 아닌지? 를 확인 하는 함수
        PointerEventData a_EDCurPos = new PointerEventData(EventSystem.current);

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)

			List<RaycastResult> results = new List<RaycastResult>();
			for (int i = 0; i < Input.touchCount; ++i)
			{
				a_EDCurPos.position = Input.GetTouch(i).position;  
				results.Clear();
				EventSystem.current.RaycastAll(a_EDCurPos, results);
                if (0 < results.Count)
                    return true;
			}

			return false;
#else
        a_EDCurPos.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(a_EDCurPos, results);
        return (0 < results.Count);
#endif
    }//public bool IsPointerOverUIObject() 


    private void CreateHero()
    {
        Vector3 hPos = Vector3.zero;
        Vector3 addPos = Vector3.zero;

        GameObject hPosObj = GameObject.Find("HeroSpawnPos");
        if (hPosObj != null)
        {
            // 10m 이내 랜덤 스폰
            addPos.x = Random.Range(-5.0f, 5.0f);
            addPos.z = Random.Range(-5.0f, 5.0f);
            hPos = hPosObj.transform.position + addPos;

            //Resources에 빼놨던 "HeroPrefab" 프리팹
            PhotonNetwork.Instantiate("HeroPrefab", hPos, Quaternion.identity, 0);
        }
    }

    // --- Back Button 처리 함수 (룸 나가기 버턴)
    public void OnClickBackBtn()
    {
        // 로그 메시지에 출력할 문자열 생성
        string msg = "\n<color=#ff0000>]" +
                    PhotonNetwork.LocalPlayer.NickName +
                    "] 방 나감</color>";
        //RPC 함수 호출
        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg, false);

        // 마지막 사람이 방을 떠날 때 룸의 CustomProerties를 초기화 해줘야 한다.
        if (PhotonNetwork.PlayerList != null && PhotonNetwork.PlayerList.Length <= 1)
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.CustomProperties.Clear();
            }
        }

        // 지금 나가려는 유저를 찾아서 그 유저의
        // 모든 CustomProperties를 초기화 해 주고 나가는 것이 좋다.
        // 그렇지 않으면 나갔다 즉시 방 입장시 오류가 발생한다.
        if (PhotonNetwork.LocalPlayer != null)
        {
            PhotonNetwork.LocalPlayer.CustomProperties.Clear();
        }
        // 그래야 중계되던 것이 모두 초기화 될 것이다.

        Debug.Log("방 나가기 버튼 클릭!");

        // 현재 룸을 빠져나가며 생성한 모든 네트워크 객체를 삭제
        PhotonNetwork.LeaveRoom(); // 포톤 방을 빠져나간다.
    }

    // 룸에서 접속 종료 되었을 때 호출되는 콜백 함수
    // LeaveRoom을 호출 되었을 때 호출되는 콜백 함수
    public override void OnLeftRoom()
    {
        Debug.Log("방 나가기 완료! OnLoeftRoom 콜백함수 호출!");
        Time.timeScale = 1.0f; // 일시정지 풀어주기
        SceneManager.LoadScene("PhotonLobby"); // 로비씬으로 이동
    }

    //채팅 내용을 중계하는 함수
    private void BroadcastingChat()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        string msg = "\n<color=#ffffff>[" +
                    PhotonNetwork.LocalPlayer.NickName + "] " +
                    InputFdChat.text + "</color>";

        pv.RPC("LogMsg", RpcTarget.AllBuffered, msg, true);

        InputFdChat.text = "";
    }

    // 중계 하기 위함
    [PunRPC]
    private void LogMsg(string msg, bool isChatMsg, PhotonMessageInfo info)
    {
        //로컬에서 내가 보낸 메시지인 경우만
        //채팅 메시지인지?
        //info.Sender.IsLocal == true // 로컬에서 보낸 메시지
        //info.Sender.IsLocal == false // PhotonNetwork.LocalPlayer.ActorNumber(IsMine의 고유번호)
        if(info.Sender.IsLocal == true && isChatMsg == true)
        {
            // 방장이 말을 한 경우는 "#00ffff"로 들어 오니까 방장이 한 말은 자신도 그냥 하늘 색으로 보일 것
            msg = msg.Replace("#ffffff", "#ffff00"); // 문자열을 찾아서, 바꿔주는 역할
        }

        m_MsgList.Add(msg);
        if(20 < m_MsgList.Count)
        {
            m_MsgList.RemoveAt(0);
        }

        // 로그 메시지 Text UI에 텍스트를 누적시켜 표시
        txtLogMsg.text = "";
        for(int i = 0; i < m_MsgList.Count; i++)
        {
            txtLogMsg.text += m_MsgList[i];
        }
    }
}
