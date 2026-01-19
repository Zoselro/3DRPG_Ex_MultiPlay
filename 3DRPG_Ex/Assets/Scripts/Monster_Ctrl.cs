using Unity.VisualScripting;
using UnityEngine;

public enum MonType
{
    Skeleton = 0,
    Alien,
    Count
}

public class Monster_Ctrl : MonoBehaviour
{
    public MonType monType;

    //--- Hp 바 표시
    float CurHp = 100;
    float MaxHp = 100;
    float NetHp = 100;  //CurHp 중계용
    //--- Hp 바 표시

    AnimState m_PreState = AnimState.idle; //애니메이션 변경을 위한 함수 
    [HideInInspector] public AnimState m_CurState = AnimState.idle; //애니메이션 변경을 위한 변수
    AnimState AI_State = AnimState.idle; //몬스터 AI 상태를 계산하기 위한 Enum 변수

    //애니메이션 클래스 변수(인스펙터뷰에 표시용)
    public Anim anim;       //AnimSupporter.cs 쪽에 정의되어 있음
    Animation m_RefAnimation = null;        //Skeleton

    //--- Monster AI
    [HideInInspector] public GameObject m_AggroTarget = null;   //공격할 대상
    int m_AggroTgId = -1;  //이 몬스터가 공격해야 할 캐릭터의 고유번호
    Vector3 m_MoveDir = Vector3.zero;   //수평 진행 노멀 발향 벡터
    Vector3 m_CacVLen = Vector3.zero;   //주인공을 향하는 벡터
    float m_CacDist = 0.0f;             //거리 계산용 변수
    float m_TraceDist  = 7.0f;          //추적 거리
    float m_AttackDist = 1.8f;          //공격 거리
    Quaternion m_TargetRot;             //회전 계산용 변수
    float m_RotSpeed = 7.0f;            //초당 회전 속도
    Vector3 m_MoveNextStep = Vector3.zero; //이동 계산용 변수
    float m_MoveVelocity = 2.0f;        //평면 초당 이동 속도
    //--- Monster AI

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_RefAnimation = GetComponentInChildren<Animation>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_CurState == AnimState.die) //죽겄으면...
            return;

        MonStateUpdate();
        MonAnctionUpdate();
    }

    private void MonStateUpdate()
    {
        if(m_AggroTarget == null) //공격할 대상 타겟이 존재 하지 않을 경우
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
            {
                m_CacVLen = players[i].transform.position - this.transform.position;
                m_CacVLen.y = 0.0f;
                m_MoveDir = m_CacVLen.normalized; //주인공을 향해 바라 보도록...
                m_CacDist = m_CacVLen.magnitude;

                if(m_CacDist <= m_AttackDist) //공격거리 범위 이내로 들어왔는지 확인
                {
                    AI_State = AnimState.attack;
                    m_AggroTarget = players[i].gameObject; //타겟 설정
                    break;
                }
                else if(m_CacDist <= m_TraceDist) //추적거리 범위 이내로 들어왔는지 확인
                {
                    AI_State = AnimState.trace; //몬스터의 상태를 추적으로 설정
                    m_AggroTarget = players[i].gameObject;  //타겟 설정
                    break;
                }
            }//for (int i = 0; i < players.Length; i++)

            if(m_AggroTarget == null)
            {
                AI_State = AnimState.idle;  //몬스터의 상태를 idle 모드로 설정
            }

        }//if(m_AggroTarget == null) //공격할 대상 타겟이 존재 하지 않을 경우
        else //if(m_AggroTarget != null) //공격할 대상 타겟이 존재 하는 경우
        {
            m_CacVLen = m_AggroTarget.transform.position - this.transform.position;
            m_CacVLen.y = 0.0f;
            m_MoveDir = m_CacVLen.normalized; //주인공으 향해 바라 보도록...
            m_CacDist = m_CacVLen.magnitude;

            if(m_CacDist <= m_AttackDist) //주인공을 향해 바라 보도록...
            {
                AI_State = AnimState.attack;
            }
            else if(m_CacDist <= m_TraceDist) //추적거리 범위 이내로 들어왔는지 확인
            {
                AI_State = AnimState.trace;
            }
            else
            {
                AI_State = AnimState.idle;  //몬스터의 상태를 idle 상태로 설정
                m_AggroTarget = null;
                m_AggroTgId = -1;
            }
        }//else if(m_AggroTarget != null) //공격할 대상 타겟이 존재 하는 경우

    }//private void MonStateUpdate()

    private void MonAnctionUpdate()
    {
        if(m_AggroTarget == null) //공격할 대상 타겟이 존재 하지 않을 경우
        {
            MyChnageAnim(AnimState.idle, 0.12f);
        }
        else //if(m_AggroTarget != null) //공격할 대상 타겟이 존재 하는 경우
        {
            if(AI_State == AnimState.attack) //공격 상태 일때
            {
                if(0.0001f < m_CacDist) //m_MoveDir.magnitude
                {
                    m_TargetRot = Quaternion.LookRotation(m_MoveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                                    m_TargetRot, Time.deltaTime * m_RotSpeed);
                }//if(0.0001f < m_CacDist) //m_MoveDir.magnitude

                //공격 애니메이션 적용
                MyChnageAnim(AnimState.attack, 0.12f);

            }//if(AI_State == AnimState.attack) //공격 상태 일때
            else if(AI_State == AnimState.trace) //추적 상태 일때
            {
                //--- 몬스터 이동시 이동방향쪽을 회전 시켜주는 코드
                if (0.0001f < m_CacDist) //m_MoveDir.magnitude
                {
                    m_TargetRot = Quaternion.LookRotation(m_MoveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                                    m_TargetRot, Time.deltaTime * m_RotSpeed);
                }//if(0.0001f < m_CacDist) //m_MoveDir.magnitude
                 //--- 몬스터 이동시 이동방향쪽을 회전 시켜주는 코드

                //--- 몬스터 이동 코드
                m_MoveNextStep = m_MoveDir * (m_MoveVelocity * Time.deltaTime);
                m_MoveNextStep.y = 0.0f;
                transform.position += m_MoveNextStep;

                //추적 애니메이션 적용
                MyChnageAnim(AnimState.trace, 0.12f);

            }//else if(AI_State == AnimState.trace) //추적 상태 일때
            else if(AI_State == AnimState.idle)
            {
                MyChnageAnim(AnimState.idle, 0.12f);
            }
        } //else if(m_AggroTarget != null) //공격할 대상 타겟이 존재 하는 경우
    }//private void MonAnctionUpdate()

    void MyChnageAnim(AnimState newState, float CrossTime = 0.0f)
    {
        if (m_PreState == newState)
            return;

        if (m_RefAnimation != null)
        {
            string strAnim = anim.Idle.name;
            if (newState == AnimState.idle)
                strAnim = anim.Idle.name;
            else if (newState == AnimState.trace)
                strAnim = anim.Move.name;
            else if (newState == AnimState.attack)
                strAnim = anim.Attack1.name;
            else if(newState == AnimState.die)
                strAnim = anim.Die.name;

            if (0.0f < CrossTime)
                m_RefAnimation.CrossFade(strAnim, CrossTime);
            else
                m_RefAnimation.Play(strAnim);
        }//if (m_RefAnimation != null)

        m_PreState = newState;
        m_CurState = newState;

    }//void MyChnageAnim(AnimState newState, float CrossTime = 0.0f)

}//public class Monster_Ctrl 
