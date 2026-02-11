using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Hero_Ctrl : MonoBehaviour
{
    [HideInInspector] public PhotonView pv = null;

    //--- Hp 바 표시
    [HideInInspector] public float CurHp = 1000;
    [HideInInspector] public float MaxHp = 1000;
    float NetHp = 1000; //CurHp 중계용
    public Image ImgHpbar;
    //--- Hp 바 표시
    public Text NickNameText;

    //--- 키보드 이동 관련 변수 선언
    float h = 0, v = 0;
    Vector3 m_KeyMvDir = Vector3.zero;
    float rotVelocity = 0.0f;    // SmoothDampAngle에서 속도 누적 저장
    float rotSmoothTime = 0.13f; // Slerp(0.13f)와 비슷한 반응 속도
    //--- 키보드 이동 관련 변수 선언

    //--- 캐릭터 이동 속도 변수
    float m_MoveVelocity = 5.0f;  //평면 초당 이동 속도...
    //--- 캐릭터 이동 속도 변수

    //--- Joystick 이동 처리 변수
    float m_JoyMvLen = 0.0f;
    Vector3 m_JoyMvDir = Vector3.zero;
    //--- Joystick 이동 처리 변수

    //--- Picking 관련 변수
    Ray m_MouseRay;
    RaycastHit hitInfo;
    public LayerMask LayerMask = -1;

    bool m_IsPickMoveOnOff = false;     //피킹 이동 OnOff
    Vector3 m_TargetPos = Vector3.zero; //최종 목표 위치
    double m_MoveDurTime = 0;           //목표점까지 도착하는데 걸리는 시간
    double m_AddTimeCount = 0;          //누적 시간 카운트
    Vector3 m_CacLenVec = Vector3.zero; //이동 계산용 변수
    //--- Picking 관련 변수

    //--- 이동 관련 공통 변수
    Vector3 m_MoveDir = Vector3.zero;   //x, z 평면 진행 방향
    float m_RotSpeed = 7.0f;            //초당 7도 회전하려는 속도
    Quaternion m_TargetRot = Quaternion.identity; //회전 계산용 변수
    //--- 이동 관련 공통 변수

    //--- Animator 관련 변수
    Animator m_Animator = null;
    AnimState m_PreState = AnimState.idle;
    AnimState m_CurState = AnimState.idle;
    //--- Animator 관련 변수

    //--- 공격 관련 변수
    GameObject[] m_EnemyList = null;    //필드상의 몬스터들을 가져오기 위한 변수
    float m_AttackDist = 1.9f;          //주인공의 공격거리
    GameObject m_TargetEnemy = null;    //공격 대상 몬스터 객체 참조 변수
    Vector3 m_CacTgVec = Vector3.zero;  //타겟까지의 거리 계산용 변수
    Vector3 m_CacAtDir = Vector3.zero;  //공격시 방향 전환용 변수

    float m_CacRotSpeed = 7.0f;
    //--- 공격 관련 변수

    //--- 데미지 칼라 연출 관련 변수
    Shader m_DefTexShader = null;
    Shader m_WeaponTexShader = null;

    bool AttachColorChange = false;
    SkinnedMeshRenderer m_SMR = null;
    SkinnedMeshRenderer[] m_SMRList = null;
    MeshRenderer[] m_MeshList = null;   //장착 무기
    float AttachColorStartTime = 0.0f;
    float AttachColorTime = 0.1f;       //피격을 짧게 주기용
    float m_Ratio = 0.0f;
    float m_fCol  = 0.0f;
    float m_DamageColor = 0.63f;
    Color m_CacColor;
    //--- 데미지 칼라 연출 관련 변수

    void Awake()
    {

        
        Camera_Ctrl a_CamCtrl = Camera.main.GetComponent<Camera_Ctrl>();
        if (a_CamCtrl != null)
            a_CamCtrl.InitCamera(this.gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameMgr.Inst.m_RefHero = this;

        m_Animator = this.GetComponent<Animator>();

        //LayerMask = 1 << LayerMask.NameToLayer("MyTerrain");
        //LayerMask |= 1 << LayerMask.NameToLayer("Enemy");  //Enemy 레이어도 피킹이 되도록 설정

        FindDefShader();

    }//void Start()

    // Update is called once per frame
    void Update()
    {
        MousePickCheck(); //<-- 마우스 클릭 바닥지형을 클릭했는지 확인하는 함수

        FindEnemyTarget();

        KeyBDMove();
        JoyStickMvUpdate();
        MousePickUpdate();

        AttackRotUpdate();

        UpdateAnimState(); //<-- idle 애니메이션으로 돌아가야 하는지 감시하는 함수

        AttachColorUpdate();

    }//void Update()

    void KeyBDMove()
    {
        h = Input.GetAxisRaw("Horizontal"); //화살표키 좌우키를 누르면 -1.0f ~ 1.0f
        v = Input.GetAxisRaw("Vertical");

        if (0.0f != h || 0.0f != v)
        {
            if (IsSkill() == true)
                return;

            ClearMsPickMove();

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
        if (0.0f < m_JoyMvLen)
        {
            //마우스 피킹 이동 취소
            ClearMsPickMove();

            //--- 카메라가 바라보고 있는 전면을 기준으로 회전 시켜주는 코드
            //new Vector3(joyMvDir.x, 0.0f, joyMvDir.y) 카메라의 로컬 기준 방향으로 변경하는 함수
            m_JoyMvDir = Camera.main.transform.TransformDirection(
                                new Vector3(joyMvDir.x, 0.0f, joyMvDir.y));
            m_JoyMvDir.y = 0.0f;
            m_JoyMvDir.Normalize();
            //--- 카메라가 바라보고 있는 전면을 기준으로 회전 시켜주는 코드

        }//if(0.0f < m_JoyMvLen)
    }//public void SetJoyStickMv(Vector2 joyMvDir)

    void JoyStickMvUpdate()
    {
        if (0.0f != h || 0.0f != v)
            return;

        //--- 조이스틱 이동 처리
        if (0.0f < m_JoyMvLen)
        {
            if (IsSkill() == true)
                return;

            m_MoveDir = m_JoyMvDir;

            //--- 캐릭터 회전
            if (0.0001f < m_JoyMvDir.magnitude)
            {
                m_TargetRot = Quaternion.LookRotation(m_JoyMvDir);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                    m_TargetRot, Time.deltaTime * m_RotSpeed);
            }
            //--- 캐릭터 회전

            transform.position += m_JoyMvDir * (m_MoveVelocity * Time.deltaTime);
            ChangeAnimState(AnimState.move);
        }
        //--- 조이스틱 이동 처리
    }//void JoyStickMvUpdate()

    void MousePickCheck()  //마우스 클릭 감지를 위한 함수
    {
        if (Input.GetMouseButtonDown(0) == true) //왼쪽 마우스 버튼 클릭시
        if (GameMgr.Inst.IsPointerOverUIObject() == false) //UI가 아닌 곳을 클릭했을 때만 피킹
        {
            if(IsSkill() == true)
               return;

            m_MouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(m_MouseRay, out hitInfo, Mathf.Infinity, LayerMask.value))
            {
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {  //마루스로 몬스터를 피킹 했다면...
                    MousePicking(hitInfo.point, hitInfo.collider.gameObject);

                    if (GameMgr.Inst.m_MsClickMark != null)
                        GameMgr.Inst.m_MsClickMark.SetActive(false);
                }
                else //지형 바닥 피킹일 때
                {
                    //지형 바닥 피킹일 때
                    MousePicking(hitInfo.point);
                    GameMgr.Inst.MsClickMarkOn(hitInfo.point);
                }//else //지형 바닥 피킹일 때

            }//if (Physics.Raycast(m_MouseRay, out hitInfo, Mathf.Infinity, LayerMask.value))
        }//if(Input.GetMouseButtonDown(0) == true) //왼쪽 마우스 버튼 클릭시
    }//void MousePickCheck()  //마우스 클릭 감지를 위한 함수

    void MousePicking(Vector3 pickVec, GameObject pickMon = null)
    {  //마우스 클릭 처리 함수
        pickVec.y = transform.position.y;   //목표 위치

        m_CacLenVec = pickVec - transform.position;
        m_CacLenVec.y = 0.0f;

        //--- Picking Enemy 공격 처리 부분
        if(pickMon != null)
        {
            //지금 공격하려고 하는 몬스터의 어그로 타겟이 나라면...
            //공격 가시거리... 타겟이 있고, +1.0이면 어차피 몬스터도 다가올거고,
            //좀 일찍 공격 애니메이션에 들어가야 잠시라도 move 애니가 끼어 들지 못한다.
            float attDist = m_AttackDist;
            if(pickMon.GetComponent<Monster_Ctrl>().m_AggroTarget == this.gameObject)
            {
                attDist = m_AttackDist + 0.1f;
            }

            m_CacTgVec = pickMon.transform.position - transform.position;
            if(m_CacTgVec.magnitude <= attDist)
            {
                m_TargetEnemy = pickMon;
                AttackOrder();  //즉시공격

                return;
            }
        }//if(a_PickMon != null)
        //--- Picking Enemy 공격 처리 부분

        if (m_CacLenVec.magnitude < 0.5f) //너무 근거리 피킬은 스킵해 준다.
            return;

        m_TargetPos = pickVec;  //최종 목표 위치
        m_IsPickMoveOnOff = true;   //피킬 이동 OnOff

        m_MoveDir = m_CacLenVec.normalized;
        m_MoveDurTime = m_CacLenVec.magnitude / m_MoveVelocity; //도착하는데까지 걸리는 시간
        m_AddTimeCount = 0.0f;

        m_TargetEnemy = pickMon;

    }//void MousePicking(Vector3 pickVec, GameObject pickMon = null)

    void MousePickUpdate()  //마우스 클릭으로 캐릭터 이동을 계산하는 함수
    {
        if (m_IsPickMoveOnOff == true)
        {
            m_CacLenVec = m_TargetPos - transform.position;
            m_CacLenVec.y = 0.0f;

            m_MoveDir = m_CacLenVec.normalized;

            //캐릭터를 이동방향으로 회전시키는 코드
            if (0.0001f < m_CacLenVec.magnitude)
            {
                m_TargetRot = Quaternion.LookRotation(m_MoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                    m_TargetRot, Time.deltaTime * m_RotSpeed);
            }
            //캐릭터를 이동방향으로 회전시키는 코드

            m_AddTimeCount += Time.deltaTime;
            if (m_MoveDurTime <= m_AddTimeCount) //목표점에 도착한 것으로 판정한다.
            {
                //m_IsPickMoveOnOff = false; //마우스 클릭 이동 취소
                ClearMsPickMove();
            }
            else
            {
                transform.position += m_MoveDir * Time.deltaTime * m_MoveVelocity;
                ChangeAnimState(AnimState.move);
            }

            //--- 타겟을 향해 피킹 이동 공격
            if (m_TargetEnemy != null)
            {
                m_CacTgVec = m_TargetEnemy.transform.position - transform.position;
                if (m_CacTgVec.magnitude <= m_AttackDist) //공격거리 안쪽에 들어왔다면...
                    AttackOrder();
            }
            //--- 타겟을 향해 피킹 이동 공격

        }//if(m_IsPickMoveOnOff == true)
    }//void MousePickUpdate()

    void ClearMsPickMove()
    {
        m_IsPickMoveOnOff = false; //마우스 클릭 이동 취소

        //마우스 클릭 마크 취소
        if (GameMgr.Inst.m_MsClickMark != null)
            GameMgr.Inst.m_MsClickMark.SetActive(false);
    }

    //--- 애니메이션 상태 변경 메서드
    public void ChangeAnimState(AnimState newState,
                                float crossTime = 0.1f, string animName = "")
    {
        if (m_Animator == null)
            return;

        if (m_PreState == newState)
            return;

        m_Animator.ResetTrigger(m_PreState.ToString());
        //기존에 적용되어 있던 Trigger 변수를 제거

        if (0.0f < crossTime)
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
        if ((0 == h && 0 == v) && m_JoyMvLen <= 0.0f &&
            m_IsPickMoveOnOff == false && IsAttack() == false)
        {
            ChangeAnimState(AnimState.idle);
        }
    }//void UpdateAnimState()

 

    //현재 공격 중인지 확인하는 메서드
    public bool IsAttack()
    {
        return m_CurState == AnimState.attack || m_CurState == AnimState.skill;
    }

    //현재 스킬 애니메이션 중인지 확인하는 메서드
    public bool IsSkill()
    {
        return m_CurState == AnimState.skill;
    }

    public void AttackOrder()
    {
        if (IsAttack() == false)  //공격중이거나 스킬 사용중이 아닐 때만... 
        {
            //키보드 컨트롤이나 조이스킬 컨트롤로 이동 중이고
            //공격키를 연타해서 누르면 달리는 애니메이션에 잠깐동안
            //애니메이션 보간 때문에 공격 애니가 끼어드는 문제가 발생한다.
            //<-- 이런 현상에 대한 예외처리
            if ((0.0f != h || 0.0f != v) || 0.0f < m_JoyMvLen)
                return;

            ChangeAnimState(AnimState.attack);
            ClearMsPickMove();

        }//if(IsAttack() == false)  //공격중이거나 스킬 사용중이 아닐 때만... 
    }//public void AttackOrder()

    public void SkillOrder(string Type, ref float CoolDur, ref float CurCool)
    {
        if (0.0f < CurCool)
            return;

        if(IsSkill() == false) //스킬 사용중이 아닐 때만...
        {
            ChangeAnimState(AnimState.skill);
            ClearMsPickMove();

            CoolDur = 7.0f;
            CurCool = CoolDur;
        }
    } //public void SkillOrder(string Type, ref float CoolDur, ref float CurCool)

    #region --- 이벤트 함수

    //공격 타겟을 찾아주는 함수
    void FindEnemyTarget()
    {
        //마우스 피킬을 시도했고 이동 중이면 타겟을 다시 잡지 않는다.
        if (m_IsPickMoveOnOff == true)
            return;

        //애니메이션 보간 때문에 정확히 정밀한 공격 애니메이션만 하고 있을 때만...
        if (IsAttack() == false) //공격 애니메이션이 아니면...
            return;

        //타겟의 교체는 공격거리보다는 조금 더 여유(0.5f)를 두고 바뀌게 한다.
        if (IsTargetEnemyActive(0.5f) == true)
            return; //(공격거리 + 0.5) >= 주인공에서 타겟몬스터까지의 거리 (아직 타겟이 유효하면...)

        //공격 애니메이션 중이고 타겟이 무효화 되었으면... 타겟을 새로 잡아준다.

        m_EnemyList = GameObject.FindGameObjectsWithTag("Enemy");

        float minLen = float.MaxValue;
        float eCount = m_EnemyList.Length;
        m_TargetEnemy = null;   //우선 타겟 무효화
        for(int i = 0; i < eCount; i++) 
        {
            m_CacTgVec = m_EnemyList[i].transform.position - transform.position;
            m_CacTgVec.y = 0.0f;
            if(m_CacTgVec.magnitude <= m_AttackDist)
            { //공격거리 안쪽에 있을 경우만 타겟으로 잡는다.
                if(m_CacTgVec.magnitude < minLen)
                {
                    minLen = m_CacTgVec.magnitude;
                    m_TargetEnemy = m_EnemyList[i];
                }//if(m_CacTgVec.magnitude < minLen)
            }//if(m_CacTgVec.magnitude <= m_AttackDist)
        }//for(int i = 0; i < eCount; i++) 

    }//void FindEnemyTarget()

    //주인공 입장에서 주변 공격거리안쪽에 몬스터가 존재하는지 확인하는 함수
    bool IsTargetEnemyActive(float ExtLen = 0.0f) //ExtLen : 확장 거리 값
    {
        if (m_TargetEnemy == null)
            return false;

        //타겟이 활성화되어 있지 않으면 타겟 해제
        if(m_TargetEnemy.activeSelf == false)
        {
            m_TargetEnemy = null;
            return false;
        }

        //isDie 죽어 있어도
        Monster_Ctrl tMon = m_TargetEnemy.GetComponent<Monster_Ctrl>();
        if(tMon.m_CurState == AnimState.die) //죽었으면...
        {
            m_TargetEnemy = null;
            return false;
        }

        m_CacTgVec = m_TargetEnemy.transform.position - transform.position;
        m_CacTgVec.y = 0.0f;
        if(m_AttackDist + ExtLen < m_CacTgVec.magnitude)
        {  //(공격거리 + 검색 확장거리) 바깥쪽에 있을 경우도 타겟을 무효화 해 버린다.
            //m_TargetEnemy = null; //원거리인 경우 타겟을 공격할 수 있으니까...
            return false;
        }

        return true; //타겟이 아직 유효 하다는 의미

    }//bool IsTargetEnemyActive(float ExtLen = 0.0f)  //ExtLen : 확장 거리 값

    public void AttackRotUpdate()
    { //공격애니메이션 중일 때 타겟을 향해 회전하게 하는 함수

        //키보드 이동, 조이스틱 이동을 발동시켰다면 타겟은 즉시 무효화 처리
        if ((0.0f != h || 0.0f != v) || 0.0f < m_JoyMvLen)
            m_TargetEnemy = null;   //타겟 무효화

        if (m_TargetEnemy == null)  //타겟이 존재하지 않으면...
            return;

        m_CacTgVec = m_TargetEnemy.transform.position - transform.position;
        m_CacTgVec.y = 0.0f;

        if(m_CacTgVec.magnitude <= (m_AttackDist + 0.3f)) //공격거리 안쪽이면..
        {
            m_CacAtDir = m_CacTgVec.normalized;
            if(0.0001f < m_CacAtDir.magnitude)
            {
                m_CacRotSpeed = m_RotSpeed * 3.0f;  //초당 회전 속도
                Quaternion a_TargetRot = Quaternion.LookRotation(m_CacAtDir);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                    a_TargetRot, Time.deltaTime * m_CacRotSpeed);
            }//if(0.0001f < m_CacAtDir.magnitude)
        }//if(m_CacTgVec.magnitude <= (m_AttackDist + 0.3f)) //공격거리 안쪽이면..

    }//public void AttackRotUpdate()

    public void Event_AttHit()
    {
        m_EnemyList = GameObject.FindGameObjectsWithTag("Enemy");
        int iCount = m_EnemyList.Length;
        float fCacLen = 0.0f;
        GameObject effObj = null;
        Vector3 effPos = Vector3.zero;

        //--- 주변 모든 몬스터를 찾아서 데미지를 준다.(범위 공격)
        for(int i = 0; i < iCount; i++)
        {
            m_CacTgVec = m_EnemyList[i].transform.position - transform.position;
            fCacLen = m_CacTgVec.magnitude;
            m_CacTgVec.y = 0.0f;

            //공격각도 안에 있는 경우
            //45도 정도 범위 밖에 있다면 뜻
            if(Vector3.Dot(transform.forward, m_CacTgVec.normalized) < 0.45f)
                continue;

            //공격 거리 밖에 있는 경우
            if (m_AttackDist + 0.1f < fCacLen)
                continue;

            effObj = EffectPool.Inst.GetEffectObj("FX_Hit_01", Vector3.zero, Quaternion.identity);
            effPos = m_EnemyList[i].transform.position;
            effPos.y += 1.1f;
            effObj.transform.position = effPos + (-m_CacTgVec.normalized * 1.13f);
            effObj.transform.LookAt(effPos + (m_CacTgVec.normalized * 2.0f));

            m_EnemyList[i].GetComponent<Monster_Ctrl>().TakeDamage(this.gameObject);
        }
        //--- 주변 모든 몬스터를 찾아서 데미지를 준다.(범위 공격)

    }//public void Event_AttHit()

    void Event_AttFinish()
    {
        //Attack 상태일 때는 Attack상태로 끝나야 한다.
        if (m_CurState != AnimState.attack)
            return;

        if(IsTargetEnemyActive(0.2f) == true)
        {
            ChangeAnimState(AnimState.attack);
            ClearMsPickMove();
        }
        else
        {
            ChangeAnimState(AnimState.idle);
        }
    }//void Event_AttFinish()

    void Event_SkillHit()
    {
        m_EnemyList = GameObject.FindGameObjectsWithTag("Enemy");
        int iCount = m_EnemyList.Length;
        float fCacLen = 0.0f;
        GameObject effObj = null;
        Vector3 effPos = Vector3.zero;

        effObj = EffectPool.Inst.GetEffectObj("FX_AttackCritical_01",
                                        Vector3.zero, Quaternion.identity);
        effPos = transform.position;
        effPos.y += 1.0f;
        effObj.transform.position = effPos + (transform.forward * 2.3f);
        effObj.transform.LookAt(effPos + (-transform.forward * 2.0f));

        for(int i = 0; i < iCount; i++)
        {
            if (m_EnemyList[i] == null)
                continue;

            m_CacTgVec = m_EnemyList[i].transform.position - transform.position;
            fCacLen = m_CacTgVec.magnitude;
            m_CacTgVec.y = 0.0f;

            //공격 각도 제한 없음 360도 모두 데미지 주기

            //공격 볌위 밖에 있는 경우
            if (m_AttackDist + 0.1f < fCacLen)
                continue;

            effObj = EffectPool.Inst.GetEffectObj("FX_Hit_01", Vector3.zero, Quaternion.identity);
            effPos = m_EnemyList[i].transform.position;
            effPos.y += 1.1f;
            effObj.transform.position = effPos + (-m_CacTgVec.normalized * 1.13f);
            effObj.transform.LookAt(effPos + (m_CacTgVec.normalized * 2.0f));

            m_EnemyList[i].GetComponent<Monster_Ctrl>().TakeDamage(this.gameObject, 50);
        }//for (int i = 0; i < iCount; i++)
    }//void Event_SkillHit()

    void Event_SkillFinish()
    {
        //Skill 상태인데 Attack 애니메이션 끝이 들어온 경우라면 제외시켜 버린다.
        //공격 애니 중에 스킬 발동시 공격 끝나는 이벤트 함수가 들어와서 스킬이
        //취소되는 현상이 있을 수 있어서 예외 처리함
        //Skill 상태일 때는 Skill상태로 끝나야 한다.
        if (m_CurState != AnimState.skill)
            return;

        if(IsTargetEnemyActive(0.2f) == true)
        {
            ChangeAnimState(AnimState.attack);
            ClearMsPickMove();
        }
        else
        {
            ChangeAnimState(AnimState.idle);
        }
    }//void Event_SkillFinish()

    #endregion

    public void TakeDamage(float Damage = 10.0f)
    {
        if (CurHp <= 0.0f)
            return;

        CurHp -= Damage;
        if(CurHp < 0.0f)
           CurHp = 0.0f;

        ImgHpbar.fillAmount = CurHp / MaxHp;

        SetAttachColor();

        Vector3 cacPos = this.transform.position;
        cacPos.y += 2.65f;
        GameMgr.Inst.SpawnDText_W((int)Damage, cacPos, 1);

        if (CurHp <= 0.0f)
        {
            Die();   //사망처리
        }

    }//public void TakeDamage(float Damage = 10.0f)

    void Die()
    {
        //나중 처리
    }

    void FindDefShader()
    {
        if(m_SMR == null)
        {
            m_SMRList = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            m_MeshList = gameObject.GetComponentsInChildren<MeshRenderer>();
            m_SMR = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();

            if (m_SMR != null)
                m_DefTexShader = m_SMR.material.shader;

            if(0 < m_MeshList.Length)
                m_WeaponTexShader = m_MeshList[0].material.shader;
        }//if(m_SMR == null)

    }//void FindDefShader()

    void SetAttachColor()
    {
        AttachColorChange = true;
        AttachColorStartTime = Time.time;
    }

    void AttachColorUpdate()
    {
        if (this.gameObject.activeSelf == false)
            return;

        if (AttachColorChange == false)
            return;

        FindDefShader();

        m_Ratio = (Time.time - AttachColorStartTime) / AttachColorTime;
        m_Ratio = Mathf.Min(m_Ratio, 1.0f);
        m_fCol = m_DamageColor;
        m_CacColor = new Color(m_fCol, m_fCol, m_fCol);

        if(1.0f <= m_Ratio)
        {
            for(int i = 0; i < m_SMRList.Length; i++)
            {
                if(m_DefTexShader != null)
                   m_SMRList[i].material.shader = m_DefTexShader;
            }

            //--- 무기
            if(m_MeshList != null)
            {
                for(int i = 0; i < m_MeshList.Length; i++)
                {
                    if (m_WeaponTexShader != null)
                        m_MeshList[i].material.shader = m_WeaponTexShader;
                }
            }
            //--- 무기

            AttachColorChange = false;
        }//if(1.0f <= m_Ratio)
        else //if(m_Ratio < 1.0f)
        {
            for(int i = 0; i < m_SMRList.Length; i++)
            {
                if (GameMgr.Inst.g_AddTexShader != null &&
                    m_SMRList[i].material.shader != GameMgr.Inst.g_AddTexShader)
                    m_SMRList[i].material.shader = GameMgr.Inst.g_AddTexShader;

                m_SMRList[i].material.SetColor("_AddColor", m_CacColor);
            }

            //--- 무기
            if(m_MeshList != null)
            {
                for(int i = 0; i < m_MeshList.Length; i++)
                {
                    if(GameMgr.Inst.g_AddTexShader != null &&
                        m_MeshList[i].material.shader != GameMgr.Inst.g_AddTexShader)
                        m_MeshList[i].material.shader = GameMgr.Inst.g_AddTexShader;

                    m_MeshList[i].material.SetColor("_AddColor", m_CacColor);
                }
            }
            //--- 무기

        }//else //if(m_Ratio < 1.0f)

    }//void AttachColorUpdate()

}//public class Hero_Ctrl : MonoBehaviour
