using System;
using System.Collections.Generic;
using UnityEngine;
using MH.GUIData;

namespace MH
{
    /// <summary>
    /// a dialog used to display text, with UnityGUI
    /// </summary>
    public class CC_Unity_Texture : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public Texture m_Tex;
        public int m_GUIDepth = 0;
        public float m_TotalDisplayTime = 3f;
        public float m_FadeInTime = 1f;
        public float m_FadeOutTime = 1f;

        public float m_xScale = 0f;
        public float m_xOffset = 0f;
        public float m_yScale = 0f;
        public float m_yOffset = 0;
        public float m_wScale = 1f;
        public float m_wOffset = 0f;
        public float m_hScale = 1f;
        public float m_hOffset = 0f;

        #endregion "configurable data"

        #region "data"
        // data

        //private CC_GUI m_Dialog;
        private float m_TimeSinceStart = 0f;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        { 
        }

        void StartDialog(CC_GUI ccgui)
        {
            if (m_Tex == null)
            {
                Dbg.LogWarn("CC_Unity_Texture: no texture specified, do nothing: {0}", name);
                enabled = false;
                return;
            }

            //m_Dialog = ccgui;

            enabled = true;

            m_TimeSinceStart = 0f;
        }

        void OnGUI()
        {
            float alpha = _GetAlpha();

            float sw = Screen.width;
            float sh = Screen.height;

            Rect rc = new Rect();
            rc.x = m_xScale * sw + m_xOffset;
            rc.y = m_yScale * sh + m_yOffset;
            rc.width = m_wScale * sw + m_wOffset;
            rc.height = m_hScale * sh + m_hOffset;

            Color c = new Color(1, 1, 1, alpha);
            GUIUtil.PushGUIColor(c);
            //GUIUtil.PushDepth(m_GUIDepth);
            GUI.depth = m_GUIDepth;
            GUI.DrawTexture(rc, m_Tex);
            //GUIUtil.PopDepth();
            GUIUtil.PopGUIColor();
        }

        void Update()
        {
            m_TimeSinceStart += Time.deltaTime;

            if ( m_TimeSinceStart >= m_TotalDisplayTime)
            {
                m_TimeSinceStart = 0f;
                enabled = false;
            }
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"
        // private method

        private float _GetAlpha()
        {
            if( m_TimeSinceStart < m_FadeInTime )
            {
                return Mathf.Lerp(0, 1, m_TimeSinceStart / (m_FadeInTime - m_TimeSinceStart));
            }
            else if( m_TimeSinceStart > m_TotalDisplayTime - m_FadeOutTime )
            {
                return Mathf.Lerp(1, 0, (m_TimeSinceStart - (m_TotalDisplayTime - m_FadeOutTime)) / m_FadeOutTime);
            }
            else
            {
                return 1f;
            }
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"

    }
}