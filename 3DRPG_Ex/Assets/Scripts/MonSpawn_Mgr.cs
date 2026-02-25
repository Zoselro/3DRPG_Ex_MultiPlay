using Photon.Pun;
using HashTable = ExitGames.Client.Photon.Hashtable; // 포톤의 해시테이블을 사용하기 위한 별칭
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonSpawn_Mgr : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class SpawnPos
    {
        public Transform transform;
        public string key; // "SP_0", "SP_1"
    }

    public List<SpawnPos> m_SpawnPos = new List<SpawnPos>();

    // [상태 정의] 직관적인 관리를 위한 변수
    const double IDLE_STATE = -1.0f; // 진짜 비어있음(초기 상태)
    const double STATE_ACTIVE = -2.0f; // 몬스터가 살아서 활동 중
    bool IsAllSpawns = false;

    float g_NetDelay = 0.0f; // 방장이 바뀌었을 때 네트워크 지연 시간

    public static MonSpawn_Mgr Inst = null;

    private void Awake()
    {
        Inst = this;

        Transform[] spawnPointList = gameObject.GetComponentsInChildren<Transform>();
        int index = 0;

        foreach (Transform child in spawnPointList)
        {
            if(child == this.transform) continue; // 자신은 제외

            SpawnPos data = new SpawnPos();
            data.transform = child;
            data.key = $"SP_{index}";
            m_SpawnPos.Add(data);
            index++;
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) // 포톤이 인게임 방안에 있을 때만..
            return;

        if(PhotonNetwork.IsMasterClient) // 방장만 몬스터 스폰 관리
        {
            // 전체 스폰 위치에 한 번 스폰 예약을 해야되는데 
            // 아직 전체 스폰이 되지 않은 상태라면?
            if (!IsAllSpawns)
            {
                // 무조건 덮어 쓰지 않고, 방에 정보가 없는지 확인
                if (!CheckifSpawnAlreadyExist())
                {
                    ScheduleAllSpawns(1.5f, 5.0f);
                }

                // 마스터는 이제 스폰 관리를 시작 했음을 표시
                IsAllSpawns = true;
            }
        }

        // 1. 마스터 변경 시 딜레이 처리
        if(0.0f < g_NetDelay)
        {
            g_NetDelay -= Time.deltaTime;
            return; // 마스터 변경 시 스폰 잠시 딜레이 주기 마스터 정보 동기화를 위해
        }

        CheckSpawn();
    }

    private bool CheckifSpawnAlreadyExist()
    {
        HashTable cp = PhotonNetwork.CurrentRoom.CustomProperties;

        // 첫 번째 스폰 포인트의 키가 존재하는지 확인
        if(m_SpawnPos.Count > 0 && cp.ContainsKey(m_SpawnPos[0].key))
        {
            return true; // 이미 스폰 위치가 존재함
        }

        return false; // 스폰 위치가 존재하지 않음
    }


    // 네 개의 스폰 포인트에 각각 랜덤한 시간으로 스폰 예약을 하는 함수
    public void ScheduleAllSpawns(float minDelay, float maxDelay)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        HashTable props = new HashTable();
        double currentTime = PhotonNetwork.Time;

        foreach (var sp in m_SpawnPos)
        {
            double randomDelay = Random.Range(minDelay, maxDelay);
            props.Add(sp.key, currentTime + randomDelay);
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 각 스폰 자리별로 스폰 상태 체크 하는 함수
    private void CheckSpawn()
    {
        double currentTime = PhotonNetwork.Time; // 네트워크 시간을 바로 받아오기 위한 프로퍼티
        HashTable cp = PhotonNetwork.CurrentRoom.CustomProperties;
        Hashtable newProbs = new Hashtable();

        bool needsUpdate = false; // 업데이트가 필요한지 여부

        SpawnPos sp = null;
        var monList = FindObjectsByType<Monster_Ctrl>(FindObjectsSortMode.None);

        for(int i = 0; i < m_SpawnPos.Count; i++)
        {
            sp = m_SpawnPos[i];

            // 방 속성에서 시간 가져오기(없으면 대기 상태로 간주)

            double spawnTime = IDLE_STATE; // 기본값은 대기 상태
            if (cp.ContainsKey(sp.key))
            {
                spawnTime = (double)cp[sp.key];
            }

            // double spawnTime = IDLE_STATE or spawnTime == STATE_ACTIVE
            // 몬스터가 살아있다는 뜻이니 아무것도 하지말고 패스! (가장 빠른 탈출)
            if (spawnTime > 0.0f)
                continue;
        }
    }
}
