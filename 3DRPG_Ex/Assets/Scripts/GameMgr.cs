using Photon.Pun;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public Button m_Skill_Btn  = null;

    //--- 스킬 쿨 타임 적용
    Text m_Skill_Cool_Label = null;
    Image  m_Skill_Cool_Mask = null;
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
    async Task Start()
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

        if(m_Skill_Btn != null)
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

    }//void Start()

    // Update is called once per frame
    void Update()
    {
        MsOffObserver();

        SkillCool_Update();
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

        if(colorInx == 1) //주인공인 경우
        {
            if(m_RefHero != null)
            {
                Canvas fCanvas = m_RefHero.GetComponentInChildren<Canvas>();
                if(fCanvas != null)
                    dmgObj.transform.SetParent(fCanvas.transform);
            }//if(m_RefHero != null)
        }//if(colorInx == 1) //주인공인 경우
        else //몬스터인 경우
        {
            dmgObj.transform.SetParent(m_DText_Canvas_W);
        }

        DamageText_W damageTx = dmgObj.GetComponentInChildren<DamageText_W>();
        if(damageTx != null)
        {
            if (colorInx == 1) //주인공인 경우
                damageTx.InitState(-dmg, spPos, new Color32(255, 255, 230, 255), false);
            else
                damageTx.InitState(-dmg, spPos, new Color32(255, 255, 255, 255));
        }//if(damageTx != null)

    }//public void SpawnDText_W(int dmg, Vector3 a_SpPos, int a_ColorIdx = 0)

    void SkillCool_Update()
    {
        if(0.0f < m_Skill_CurCool)
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
        if(hPosObj != null)
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
        // 마지막 사람이 방을 떠날 때 룸의 CustomProerties를 초기화 해줘야 한다.
        if(PhotonNetwork.PlayerList != null && PhotonNetwork.PlayerList.Length <= 1)
        {
            if(PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.CustomProperties.Clear();
            }
        }

        // 지금 나가려는 유저를 찾아서 그 유저의
        // 모든 CustomProperties를 초기화 해 주고 나가는 것이 좋다.
        // 그렇지 않으면 나갔다 즉시 방 입장시 오류가 발생한다.
        if(PhotonNetwork.LocalPlayer != null)
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
}
