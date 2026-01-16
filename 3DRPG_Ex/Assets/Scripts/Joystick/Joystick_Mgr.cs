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

    //--- ╫л╠шео фпео
    public static Joystick_Mgr Inst = null;

    void Awake()
    {
        Inst = this;    
    }
    //--- ╫л╠шео фпео

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
            if(m_JoystickType == JoystickType.FlexibleOnOff)
            {
                m_Js_Background.SetActive(false);
            }

            var VJoystickSc = m_Js_Background.GetComponent<Fixed_Joystick>();
            if(VJoystickSc != null)
            {
                Destroy(VJoystickSc);
            }

            m_Js_Background.GetComponent<Image>().raycastTarget = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
