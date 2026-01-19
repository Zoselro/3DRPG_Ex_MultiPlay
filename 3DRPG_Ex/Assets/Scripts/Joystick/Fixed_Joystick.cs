using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Fixed_Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform m_Js_Background;
    public RectTransform m_Js_Handle;

    Vector2 InputDirection;
    float Js_Radius;

    Hero_Ctrl m_RefHero = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_RefHero = FindFirstObjectByType<Hero_Ctrl>();

        Js_Radius = m_Js_Background.sizeDelta.x * 0.34f;
        m_Js_Handle.anchoredPosition = Vector2.zero; // 핸들 초기화
        //핸들 이미지를 로컬의 중심점으로 이동 시키겠다는 뜻         
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {  //Js_Background 이미지를 마우스로 클릭하는 순간 발생되는 이벤트 함수

        if (eventData.button != PointerEventData.InputButton.Left) //마우스 왼쪽 버튼만
            return;

        if (m_Js_Background != null)
            m_Js_Background.gameObject.GetComponent<Image>().color =
                                                new Color32(255, 255, 255, 255);

        if (m_Js_Handle != null)
            m_Js_Handle.gameObject.GetComponent<Image>().color =
                                                new Color32(255, 255, 255, 255);

    }//public void OnPointerDown(PointerEventData eventData)

    public void OnDrag(PointerEventData eventData)
    {  //Js_Background 이미지를 마우스로 드래그하는 동안 발생되는 이벤트 함수

        if (eventData.button != PointerEventData.InputButton.Left) //마우스 왼쪽 버튼만
            return;

        Vector2 touchPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_Js_Background, eventData.position, eventData.pressEventCamera, out touchPos);
        //Js_Background	조이스틱 배경 (RectTransform) -- 이 안에서 좌표를 재계산하고 싶을 때 사용하는 함수
        //1번 매개변수 m_Js_Background : 환산하고 싶은 로컬 좌표계 UI의 RectTransform 컴포넌트 객체 대입
        //2번 매개변수 eventData.position : 마우스 좌표
        //3번 매개변수 eventData.pressEventCamera : 터치를 감지할 때 쓰는 카메라 (UI는 보통 Canvas에 연결된 카메라)
        //4번 매개변수 touchPos : 변환된 결과. 조이스틱 배경 기준의 위치 (중심을 (0,0)으로 하는 로컬 좌표계로 변환된 값)

        Vector2 clampedPos = Vector2.ClampMagnitude(touchPos, Js_Radius);
        m_Js_Handle.anchoredPosition = clampedPos;
        InputDirection = clampedPos / Js_Radius;  //벡터의 최대 크기는 1.0f가 될 것임

        //캐릭터 이동 처리
        if (m_RefHero != null)
            m_RefHero.SetJoyStickMv(InputDirection);

    }//public void OnDrag(PointerEventData eventData)

    public void OnPointerUp(PointerEventData eventData)
    {  //Js_Background 이미지에서 마우스를 드래그 하다가 손가락을 놓는 순간 발생되는 이벤트 함수

        InputDirection = Vector2.zero;
        m_Js_Handle.anchoredPosition = Vector2.zero; //핸들 초기화 (원래 위치로...)
        //핸들 이미지를 로컬의 중심점으로 이동 시키겠다는 뜻

        if (m_Js_Background != null)
            m_Js_Background.gameObject.GetComponent<Image>().color =
                                        new Color32(255, 255, 255, 120);

        if(m_Js_Handle != null)
            m_Js_Handle.gameObject.GetComponent<Image>().color =
                                        new Color32(255, 255, 255, 120);

        //캐릭터 이동 멈춤 처리
        if (m_RefHero != null)
            m_RefHero.SetJoyStickMv(InputDirection);

    }//public void OnPointerUp(PointerEventData eventData)
}
