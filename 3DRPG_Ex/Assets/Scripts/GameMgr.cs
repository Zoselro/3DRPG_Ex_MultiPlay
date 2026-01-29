using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{
    [HideInInspector] public Hero_Ctrl m_RefHero = null;

    //--- MouseClickMark 처리변수
    public GameObject m_MsClickMark = null;
    Vector3 m_CacVLen = Vector3.zero;
    //--- MouseClickMark 처리변수

    [Header("--- Button Handle ---")]
    public Button m_BackBtn = null;
    public Button m_Attack_Btn = null;
    public Button m_Skill_Btn  = null;

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
    }
    //--- 싱글턴 패턴을 위한 인스턴스 변수 선언

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //--- Attack Button 처리 코드
        if (m_Attack_Btn != null)
            m_Attack_Btn.onClick.AddListener(() =>
            {
                if (m_RefHero != null)
                    m_RefHero.AttackOrder();
            });

        //--- Skill Button 처리 코드

    }//void Start()

    // Update is called once per frame
    void Update()
    {
        MsOffObserver();
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

}
