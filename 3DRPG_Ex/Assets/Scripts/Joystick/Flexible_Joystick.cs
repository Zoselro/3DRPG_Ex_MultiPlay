using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Flexible_Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform m_Js_Background;
    public RectTransform m_Js_Handle;

    Vector2 inputDirection;
    float js_Radius;
    Vector3 m_OriginPos = Vector3.zero;

    Hero_Ctrl m_RefHero = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_RefHero = FindFirstObjectByType<Hero_Ctrl>();
        m_OriginPos = m_Js_Background.transform.position;
        js_Radius = m_Js_Background.sizeDelta.x * 0.34f;
        if(Joystick_Mgr.Inst.m_JoystickType == JoystickType.FlexibleOnOff)
        {
            m_Js_Background.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnPointerDown(PointerEventData eventData)
    {//JoystickPickPanel 이미지를 드래그하는 동안 발생되는 이벤트 함수
     
        if(eventData.button != PointerEventData.InputButton.Left)// 마우스 왼쪽 버튼만
        {
            return;
        } 

        m_Js_Background.gameObject.SetActive(true);
        m_Js_Background.position = eventData.position;
        m_Js_Handle.anchoredPosition = Vector2.zero;
        //부모 RectTransform의 pivot + Anchors 기준 위치

        if (m_Js_Background != null)
            m_Js_Background.gameObject.GetComponent<Image>().color =
                new Color32(255, 255, 255, 255);

        if (m_Js_Handle != null)
            m_Js_Handle.gameObject.GetComponent<Image>().color =
                new Color32(255, 255, 255, 255);
    }
    public void OnDrag(PointerEventData eventData)
    {//JoystickPickPanel 이미지를 마우스로 클릭하는 순간 발생되는 이벤트 함수
        if (eventData.button != PointerEventData.InputButton.Left) // 마우스 왼쪽 버튼만
            return;

        Vector2 touchPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_Js_Background, eventData.position, eventData.pressEventCamera, out touchPos);

        Vector2 clmapedPos = Vector2.ClampMagnitude(touchPos, js_Radius);
        m_Js_Handle.anchoredPosition = clmapedPos;
        inputDirection = clmapedPos / js_Radius; // 벡터의 최대 크기는 1.0f 가 되도록 계산

        //캐릭터 이동 처리
        if (m_RefHero != null)
            m_RefHero.SetJoyStickMv(inputDirection);
    }


    public void OnPointerUp(PointerEventData eventData)
    {//JoystickPickPanel 이미지에서 마우스를 드래그 하다가 손가락을 놓는 순간 발생되는 이벤트 함수
        //핸들 이미지를 로컬의 중심점으로 이동 시키겠다는 의미
        inputDirection = Vector2.zero;
        m_Js_Background.transform.position = m_OriginPos;
        m_Js_Handle.anchoredPosition = Vector2.zero; // 핸들 초기화 (원래 위치로)

        if (m_Js_Handle != null)
            m_Js_Handle.gameObject.GetComponent<Image>().color =
                new Color32(255, 255, 255, 120);

        if (m_Js_Background != null)
            m_Js_Background.gameObject.GetComponent<Image>().color =
                new Color32(255, 255, 255, 120);

        if(Joystick_Mgr.Inst.m_JoystickType == JoystickType.FlexibleOnOff)
        {
            m_Js_Background.gameObject.SetActive(false);
        }

        //캐릭터 이동 멈춤 처리
        if (m_RefHero != null)
            m_RefHero.SetJoyStickMv(inputDirection);

    }
}
