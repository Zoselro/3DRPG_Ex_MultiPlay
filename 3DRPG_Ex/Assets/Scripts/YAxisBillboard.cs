using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YAxisBillboard : MonoBehaviour
{
    //// Start is called before the first frame update
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 카메라와 같은 방향을 바라보되, Y축만 회전
        Vector3 targetPos = transform.position + cam.transform.forward;
        targetPos.y = transform.position.y;

        transform.LookAt(targetPos);
    }
}
