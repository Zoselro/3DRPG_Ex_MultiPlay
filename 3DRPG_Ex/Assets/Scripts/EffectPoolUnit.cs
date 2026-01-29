using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectPoolUnit : MonoBehaviour
{
    public float m_Delay = 1.0f;    //풀에 환원되고 적어도 1초 지난 것들 사용해야 됨
    DateTime m_InactiveTime; //Active 껐을 때의 시간 : 꺼졌을 때부터 1초 지난 거 체크 위해 사용
    EffectPool m_ObjectPool;
    string m_EffectName;

    //--- PaticleAutoDestroy 를 위해 필요한 타입
    public enum DESTROY_TYPE
    {
        Destroy,
        Inactive
    }

    DESTROY_TYPE m_DelType = DESTROY_TYPE.Inactive;
    //풀에 환원하는게 원칙이라 기본값 Inactive
    float m_LifeTime = 0.0f;
    //안꺼지고 Loop 도는 파티클들 때문에 안꺼지는 파티클을 제어하기 위해
    //LifeTime 설정 버프(지속시간) 사용
    //LifeTime에 값이 있다면 이 변수로 제어... 없다면 그냥 isPlaying으로 제어
    float m_curLifeTime;
    ParticleSystem[] m_Particles;

    // Start is called before the first frame update
    void Start()
    {
        m_Particles = GetComponentsInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_LifeTime > 0)
        {
            m_curLifeTime += Time.deltaTime;
            if (m_LifeTime <= m_curLifeTime)
            {
                DestroyParticles();
                m_curLifeTime = 0;
            }
        }
        else
        {
            bool isPlay = false;
            for (int i = 0; i < m_Particles.Length; i++)
            {
                if (m_Particles[i].isPlaying == true) //파티클 재생중인지 체크가능
                {
                    isPlay = true;
                    break;
                }
            }
            if (isPlay == false)
            {
                DestroyParticles();
            }
        }//else

    }//void Update()

    void DestroyParticles()
    {
        switch (m_DelType)
        {
            case DESTROY_TYPE.Destroy:
                Destroy(gameObject);
                break;

            case DESTROY_TYPE.Inactive:
                gameObject.SetActive(false);
                break;
        }
    }//void DestroyParticles()

    public void SetObjectPool(string effectName, EffectPool objectPool)
    {
        m_EffectName = effectName;  //어떤 이펙트인지?
        m_ObjectPool = objectPool;  //어떤 풀에서 관리하는 이펙트인지?
        ResetParent();
    }

    public void ResetParent()
    {
        transform.SetParent(m_ObjectPool.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public bool IsReady()
    {
        if (gameObject.activeSelf == false) //꺼져 있으면 풀에 들어가 있음을 의미
        {
            TimeSpan timeSpan = DateTime.Now - m_InactiveTime;
            //현재시간 - 액티브 : 껐을 때 시간 //timeSpan으로 값이 나옴
            if (timeSpan.TotalSeconds > m_Delay)
            {
                //시간을 전체 시 / 분 / 초 로 나눠서 받을 수 있다.
                //timeSpan.TotalSeconds 초로만 반환을 했을 때 1초보다 크면

                //액티브가 꺼진지 1초이상 지나면 이펙트
                //여러개 터트려도 문제가 발생안되서 1초 조건 걸었음
                return true;
            }
        }//if (gameObject.activeSelf == false) //꺼져 있으면 풀에 들어가 있음을 의미

        return false;
    }//public bool IsReady()

    private void OnDisable() //비활성화 될 때마다 호출되는 이벤트 함수(스크립트든, 오브젝트든)
    {
        m_InactiveTime = DateTime.Now;
        m_ObjectPool.AddPoolUnit(m_EffectName, this);
        //액티브가 꺼질 때 메모리풀에 다시 넣어줌
    }

}
