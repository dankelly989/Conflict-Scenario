using UnityEngine;
using System.Collections;

namespace MH
{
    public class CCDemo0_Start : MonoBehaviour
    {
        public GUIStyle m_LabelStyle;
        public bool m_Loading = false;
        public string m_NextLevel = "CutsceneDemo0";

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonUp(0) && !m_Loading)
            {
                //Application.LoadLevelAsync(m_NextLevel);
                Levels.LoadLevelAsync(m_NextLevel);
                m_Loading = true;
            }
        }

        void OnGUI()
        {
            Rect rc = new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 20f, 300, 40);
            if (!m_Loading)
            {
                GUI.Label(rc, "Click Mouse to Start", m_LabelStyle);
            }
            else
            {
                float t = Time.time;
                float fac = t - Mathf.Floor(t);
                string p = null;
                if (fac < 0.25f) p = "";
                else if (fac < 0.5f) p = ".";
                else if (fac < 0.75f) p = "..";
                else p = "...";

                GUI.Label(rc, "Loading Level" + p, m_LabelStyle);
            }

        }
    }
}