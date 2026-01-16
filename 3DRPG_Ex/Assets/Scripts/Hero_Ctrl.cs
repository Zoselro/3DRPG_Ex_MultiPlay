using UnityEngine;

public class Hero_Ctrl : MonoBehaviour
{
    //--- 키보드 이동 관련 변수 선언
    float h = 0, v = 0;
    Vector3 m_KeyMvDir = Vector3.zero;
    float rotVelocity = 0.0f;    // SmoothDampAngle에서 속도 누적 저장
    float rotSmoothTime = 0.13f; // Slerp(0.13f)와 비슷한 반응 속도
    //--- 키보드 이동 관련 변수 선언

    //--- 캐릭터 이동 속도 변수
    float m_MoveVelocity = 5.0f;  //평면 초당 이동 속도...
    //--- 캐릭터 이동 속도 변수

    //--- Animator 관련 변수
    Animator m_Animator = null;
    AnimState m_PreState = AnimState.idle;
    AnimState m_CurState = AnimState.idle;
    //--- Animator 관련 변수

    //--- JoyStick 이동 처리 변수
    float m_JoyMvLen = 0.0f;
    Vector3 m_JoyMvDir = Vector3.zero;
    //--- JoyStick 이동 처리 변수

    //--- 이동 관련 공통 변수
    Vector3 m_MoveDir = Vector3.zero; // x, z 평면 진행 방향
    float m_RotSpeed = 7.0f;          // 초당 7도 회전하려는 속도
    Quaternion m_TargetRot = Quaternion.identity; // 회전 계산용 변수
    //--- 이동 관련 공통 변수

    //--- Picking 관련 변수
    Ray m_MouseRay;
    RaycastHit hitInfo;
    public LayerMask LayerMask = -1;

    bool m_IsPickMoveOnOff = false;     // 피킹 이동 OnOff
    Vector3 m_TargetPos = Vector3.zero; // 최종 목표 위치
    double m_MoveDurTime = 0;           // 목표점까지 도착하는데 걸리는 시간
    double m_AddTimeCnt = 0;            // 누적 시간 카운트
    Vector3 m_CacLenVec = Vector3.zero; // 이동 계산용 변수
    //--- Picking 관련 변수

    void Awake()
    {
        Camera_Ctrl a_CamCtrl = Camera.main.GetComponent<Camera_Ctrl>();
        if (a_CamCtrl != null)
            a_CamCtrl.InitCamera(this.gameObject);
    }

    void Start()
    {
        m_Animator = this.GetComponent<Animator>(); 
    }

    void Update()
    {
        KeyBDMove();
        JoyStickMoveUpdate();
        UpdateAnimState(); //<-- idle 애니메이션으로 돌아가야 하는지 감시하는 함수

    }//void Update()

    void KeyBDMove()
    {
        h = Input.GetAxisRaw("Horizontal"); //화살표키 좌우키를 누르면 -1.0f ~ 1.0f
        v = Input.GetAxisRaw("Vertical");

        if(0.0f != h || 0.0f != v)
        {
            m_KeyMvDir = new Vector3(h, 0.0f, v);
            //--- 카메라 좌표계를 기준으로 방향벡터를 계산해 주는 함수
            m_KeyMvDir = Camera.main.transform.TransformDirection(m_KeyMvDir);
            m_KeyMvDir.y = 0;
            //--- 카메라 좌표계를 기준으로 방향벡터를 계산해 주는 함수

            m_KeyMvDir.Normalize();

            //--- SmoothDampAngle() 함수 방식
            float targetAngle = Mathf.Atan2(m_KeyMvDir.x, m_KeyMvDir.z) * Mathf.Rad2Deg;
            float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                                            targetAngle, ref rotVelocity, rotSmoothTime);
            transform.rotation = Quaternion.Euler(0, smoothedAngle, 0);
            //--- SmoothDampAngle() 함수 방식

            transform.position += m_KeyMvDir * m_MoveVelocity * Time.deltaTime;

            ChangeAnimState(AnimState.move);

        }//if(0.0f != h || 0.0f != v)

    }//void KeyBDMove()

    public void SetJoyStickMv(Vector2 joyMvDir)
    {
        m_JoyMvLen = joyMvDir.magnitude;
        if(0.0f < m_JoyMvLen)
        {
            //--- 카메라가 바라보고 있는 전면을 기준으로 회전 시켜주는 코드
            //new Vector3(joyMvDir.x, 0.0f, joyMvDir.y) 카메라의 로컬좌표 방향으로 변경하는 함수
            m_JoyMvDir = Camera.main.transform.TransformDirection(new Vector3(joyMvDir.x, 0.0f, joyMvDir.y));

            m_JoyMvDir.y = 0.0f;
            m_JoyMvDir.Normalize();
            //--- 카메라가 바라보고 있는 전면을 기준으로 회전 시켜주는 코드
        }
    }

    void JoyStickMoveUpdate()
    {
        //키보드 움직임이 있으면 조이스틱 움직임 취소
        if (0.0f != h || 0.0f != v)
            return;

        //--- 조이스틱 이동 처리
        if(0.0f < m_JoyMvLen)
        {
            m_MoveDir = m_JoyMvDir;

            //--- 캐릭터 회전
            if(0.0001f < m_JoyMvDir.magnitude)
            {
                m_TargetRot = Quaternion.LookRotation(m_JoyMvDir);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    m_TargetRot, Time.deltaTime * m_RotSpeed);
            }//--- 캐릭터 회전

            transform.position += m_JoyMvDir * (m_MoveVelocity * Time.deltaTime);
            ChangeAnimState(AnimState.move);
        }
    }

    void MousePickCheck()  // 마우스 클릭 감지를 위한 함수
    {
        if (Input.GetMouseButtonDown(0)) // 왼쪽 마우스 버튼 클릭시
        {

        }
    }

    void MousePicking(Vector3 pickVec, GameObject pickMon = null) // 마우스 클릭 처리 함수
    {

    }

    void MousePickUpdate() // 마우스 클릭으로 캐릭터 이동을 계산하는 함수
    {

    }

    //--- 애니메이션 상태 변경 메서드
    public void ChangeAnimState(AnimState newState,
                                float crossTime = 0.1f, string animName = "")
    {
        if (m_Animator == null)
            return;

        if(m_PreState == newState)
            return;

        m_Animator.ResetTrigger(m_PreState.ToString());
        //기존에 적용되어 있던 Trigger 변수를 제거

        if(0.0f < crossTime)
        {
            m_Animator.SetTrigger(newState.ToString());
        }
        else
        {
            m_Animator.Play(animName, -1, 0);
            //가운데 -1은 Layer Index, 뒤에 0은 처음부터 다시 시작 플레이 시키겠다는 의미
        }

        m_PreState = newState;
        m_CurState = newState;

    }//public void ChangeAnimState(AnimState newState,

    //애니메이션 상태 업데이트 메서드
    void UpdateAnimState()
    {
        //키보드, 조이스틱, 마우스 피킹 이동중이 아닐 때는 아이들 동작으로 돌아가게 한다.
        if( (0 == h && 0 == v) && m_JoyMvLen <= 0.0f )
        {
            ChangeAnimState(AnimState.idle);
        }
    }//void UpdateAnimState()
}
