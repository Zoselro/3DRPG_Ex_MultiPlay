using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    Dictionary<string, List<EffectPoolUnit>> m_DcEffectPool =
                        new Dictionary<string, List<EffectPoolUnit>>();

    int m_PresetSize = 3;   //몇개 필요할지 모르지만 기본적으로 3개씩 만들어 놓음

    //--- 싱글턴 패턴
    public static EffectPool Inst = null;

    void Awake()
    {
        Inst = this;
    }
    //--- 싱글턴 패턴

    // Start is called before the first frame update
    void Start()
    {
        StartCreate("FX_Hit_01");
        StartCreate("FX_Attack01_01");
        StartCreate("FX_AttackCritical_01");
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public void StartCreate(string effectName)
    {
        List<EffectPoolUnit> ListObjectPool = null;
        if (m_DcEffectPool.ContainsKey(effectName) == true)
        {
            ListObjectPool = m_DcEffectPool[effectName];
        }
        else
        {
            m_DcEffectPool.Add(effectName, new List<EffectPoolUnit>());
            ListObjectPool = m_DcEffectPool[effectName];
        }

        GameObject a_Prefab = Resources.Load<GameObject>("Effect/" + effectName);
        if (a_Prefab != null)
        {
            var results = a_Prefab.GetComponentsInChildren<Transform>();
            for (int k = 0; k < results.Length; k++)
                results[k].gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            //TransparentFX : flares와 투명값을 가지는 물체를 위한 레이어로,
            //lens flare의 장애물이 되지 않는다.
            //예를 들어 만약 flare가 Default layermask의 투명한 유리조작 뒤에 있다면,
            //flare는 표시되지 않을 것이다. 이때 Layer를
            //TransparentFX 로 설정하면 잘 보이게 된다.

            for (int j = 0; j < m_PresetSize; j++)  //미리 3개 정도 만들어 둠
            {
                GameObject obj = Instantiate(a_Prefab);

                EffectPoolUnit ObjectPoolUnit = obj.GetComponent<EffectPoolUnit>();
                if (ObjectPoolUnit == null)
                    ObjectPoolUnit = obj.AddComponent<EffectPoolUnit>();

                obj.transform.SetParent(transform);
                obj.GetComponent<EffectPoolUnit>().SetObjectPool(effectName, this);
                if (obj.activeSelf == true)
                {
                    //아직 이 이펙트는 풀에 들어가 있는 상태가 아님
                    //액티브 끄면 OnDisable 이벤트가 동작됨
                    obj.SetActive(false);
                }
                else
                {
                    AddPoolUnit(effectName, obj.GetComponent<EffectPoolUnit>());
                }

            }//for(int j = 0; j < m_PresetSize; j++)  //미리 3개 정도 만들어 둠

        }//if (a_Prefab != null)

    }//public void StartCreate(string effectName)

    public void AddPoolUnit(string effectName, EffectPoolUnit unit)
    {
        List<EffectPoolUnit> ListObjectPool = m_DcEffectPool[effectName];
        if (ListObjectPool != null)
        {
            ListObjectPool.Add(unit);
        }
    }

    public GameObject GetEffectObj(string effectName,
                                Vector3 postion, Quaternion rotation)
    {
        List<EffectPoolUnit> ListObjectPool = null;
        if (m_DcEffectPool.ContainsKey(effectName) == true)
        {
            ListObjectPool = m_DcEffectPool[effectName];
        }
        else
        {
            m_DcEffectPool.Add(effectName, new List<EffectPoolUnit>());
            ListObjectPool = m_DcEffectPool[effectName];
        }

        if (ListObjectPool == null)
            return null;

        if (0 < ListObjectPool.Count)
        {
            if (ListObjectPool[0] != null && ListObjectPool[0].IsReady())
            {  // 0번도 준비가 안되면 나머지는 무조건 안 돼 있기 때문에 0번 검사

                EffectPoolUnit unit = ListObjectPool[0];
                ListObjectPool.Remove(ListObjectPool[0]);
                unit.transform.position = postion;
                unit.transform.rotation = rotation;
                StartCoroutine(MySetActiveCo(unit.gameObject));
                return unit.gameObject;
            }
        }//if(0 < ListObjectPool.Count)

        GameObject a_Prefab = Resources.Load<GameObject>("Effect/" + effectName);
        GameObject obj = Instantiate(a_Prefab);

        EffectPoolUnit objectPoolUnit = obj.GetComponent<EffectPoolUnit>();
        if (objectPoolUnit == null)
        {
            objectPoolUnit = obj.AddComponent<EffectPoolUnit>();
            //OnDisable()시 메모리 풀로 돌리기... 1초 딜레이 후 사용 가능하도록...
        }

        obj.GetComponent<EffectPoolUnit>().SetObjectPool(effectName, this);
        StartCoroutine(MySetActiveCo(obj));
        return obj;

    }//public GameObject GetEffectObj(string effectName

    IEnumerator MySetActiveCo(GameObject obj)
    {
        yield return new WaitForEndOfFrame();
        // 프레임이 완전히 처리된 후에 코루틴이 이어서 실행됩니다.
        // 보통 화면을 그리는 작업이 끝난 후에 처리하고 싶은 일이 있을 때 사용됩니다.
        obj.SetActive(true);
    }

}//public class EffectPool : MonoBehaviour
