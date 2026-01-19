using UnityEngine;
using UnityEngine.UI;

public enum JoystickType
{
    Fixed = 0,
    Flexible = 1,
    FlexibleOnOff = 2
}

public class Joystick_Mgr : MonoBehaviour
{
    public JoystickType m_JoystickType = JoystickType.Fixed;

    public GameObject m_JoystickPickPanel = null;
    public GameObject m_Js_Background = null;
    public GameObject m_Js_Handle = null;

    //--- 싱글턴 패턴
    public static Joystick_Mgr Inst = null;

    void Awake()
    {
        Inst = this;    
    }
    //--- 싱글턴 패턴

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_Js_Background == null || m_Js_Handle == null ||
            m_JoystickPickPanel == null)
            return;

        if(m_JoystickType == JoystickType.Fixed)
        {
            m_JoystickPickPanel.SetActive(false);
            m_Js_Background.SetActive(true);
        }
        else if(m_JoystickType == JoystickType.Flexible ||
                m_JoystickType == JoystickType.FlexibleOnOff)
        {
            m_JoystickPickPanel.SetActive(true);
            if (m_JoystickType == JoystickType.FlexibleOnOff)
                m_Js_Background.SetActive(false);

            var VJoystickSc = m_Js_Background.GetComponent<Fixed_Joystick>();
            if(VJoystickSc != null)
                Destroy(VJoystickSc);   //스크립트 자체를 제거함

            m_Js_Background.GetComponent<Image>().raycastTarget = false;
        }

    }//void Start()

    // Update is called once per frame
    void Update()
    {
        
    }
}
