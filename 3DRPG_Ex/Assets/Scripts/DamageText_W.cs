using UnityEngine;
using UnityEngine.UI;

public class DamageText_W : MonoBehaviour
{
    Transform m_CameraTr = null;
    Text m_RefText = null;
    float m_DamageVal = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_CameraTr = Camera.main.transform;
        Destroy(gameObject, 1.2f);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.forward = m_CameraTr.forward; //ºôº¸µå
    }

    public void InitState(int dmg, Vector3 wSpawnPos, Color tColor, 
                            bool IsOutline = true)
    {
        transform.position = wSpawnPos;

        m_DamageVal = dmg;
        m_RefText = gameObject.GetComponentInChildren<Text>();

        if(m_RefText != null)
        {
            if (m_DamageVal <= 0)
                m_RefText.text = m_DamageVal.ToString();
            else
                m_RefText.text = "+" + m_DamageVal + " Heal";

            m_RefText.color = tColor;
        }//if(m_RefText != null)

        if(IsOutline == false)
        {
            Outline outLine = gameObject.GetComponentInChildren<Outline>();
            if (outLine != null)
            {
                //outLine.effectColor = new Color32(255, 255, 255, 0);
                outLine.enabled = false;
            }
        }//if(IsOutline == false)

    }//public void InitState(int dmg, Vector3 wSpawnPos, Color tColor, 
}
