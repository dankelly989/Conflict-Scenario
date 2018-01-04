using System;
using System.Collections.Generic;
using UnityEngine;
using MH.GUIData;

namespace MH
{
    /// <summary>
    /// a dialog used to display several options, with UnityGUI
    /// when selected an option, Send Message
    /// </summary>
    public class CC_Unity_Select_SendMsg : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public MsgOptions m_AllOptions;
        public float m_SetAnimSpeed = 0f;
        public float m_LimitTime = -1f; //negative means no limit, this is the time limit for selection
        public int m_DefaultSelection = 0;

        public GUISkin m_Skin;

        #endregion "configurable data"

        #region "data"
        // data

        private CC_GUI m_Dialog;
        private float m_TimeSinceDialogStart = 0f;
        private float m_prevAnimSpeed = 0f;

        private Vector2 m_scrollPos;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void StartDialog(CC_GUI ccgui)
        {
            m_Dialog = ccgui;

            enabled = true;

            AnimationState animState = CutsceneController.GetAnimState(m_Dialog.CC);
            m_prevAnimSpeed = animState.speed;
            animState.speed = m_SetAnimSpeed; //stop the anim

            m_TimeSinceDialogStart = 0f;

            m_scrollPos = Vector2.zero;
        }

        void OnGUI()
        {
            GUIUtil.PushSkin(m_Skin);

            Rect wndRect = new Rect(0, Screen.height - WND_HEIGHT, Screen.width, WND_HEIGHT);

            GUI.Box(wndRect, "");
            GUILayout.BeginArea(wndRect);
            {
                GUILayout.Label(m_AllOptions.m_Prompt);

                m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);
                {
                    List<OneMsgOption> opts = m_AllOptions.m_Options;
                    for (int idx = 0; idx < opts.Count; ++idx)
                    {
                        OneMsgOption oneOpt = opts[idx];
                        if (GUILayout.Button(oneOpt.m_Text))
                        {
                            SelectOption(idx);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            GUIUtil.PopSkin();
        }

        void Update()
        {
            m_TimeSinceDialogStart += Time.deltaTime;

            if (m_LimitTime > 0 && m_TimeSinceDialogStart >= m_LimitTime)
            {
                SelectOption(m_DefaultSelection);
            }
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        public void SelectOption(int idx)
        {
            AnimationState astate = CutsceneController.GetAnimState(m_Dialog.CC);

            astate.speed = m_prevAnimSpeed;//resume the anim

            OneMsgOption opt = m_AllOptions.m_Options[idx];
            string info = opt.m_ExtraInfo;

            m_Dialog.CC.SendMessage(m_AllOptions.m_Function, info);

            enabled = false;
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        public const float WND_HEIGHT = 150f;

        #endregion "constant data"

    }
}