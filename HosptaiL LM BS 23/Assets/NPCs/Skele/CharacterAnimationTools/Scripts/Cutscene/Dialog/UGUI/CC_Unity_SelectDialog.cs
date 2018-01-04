using System;
using System.Collections.Generic;
using UnityEngine;
using MH.GUIData;

namespace MH
{
/// <summary>
/// a dialog used to display several options, with UnityGUI
/// </summary>
public class CC_Unity_SelectDialog : MonoBehaviour
{
    #region "configurable data"
    // configurable data

    public Options m_AllOptions;
    public float m_SetTimeScale = 0f;
    public float m_LimitTime = -1f; //negative means no limit, this is the time limit for selection
    public int m_DefaultSelection = 0;

    public GUISkin m_Skin;

    #endregion "configurable data"

    #region "data"
    // data

    private CC_GUI m_Dialog;
    private float m_TimeSinceDialogStart = 0f;
    private float m_prevTimeScale = 0f;

    private Vector2 m_scrollPos;

    #endregion "data"

    #region "unity event handlers"
    // unity event handlers

    void StartDialog(CC_GUI ccgui)
    {
        m_Dialog = ccgui;

        enabled = true;

        //AnimationState animState = CutsceneController.GetAnimState(m_Dialog.CC);
        //m_prevTimeScale = animState.speed;
        //animState.speed = m_SetTimeScale; //stop the anim

        m_prevTimeScale = Time.timeScale;
        Time.timeScale = m_SetTimeScale;

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
                List<OneOption> opts = m_AllOptions.m_Options;
                for (int idx = 0; idx < opts.Count; ++idx)
                {
                    OneOption oneOpt = opts[idx];
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

        if (m_LimitTime >= 0 && m_TimeSinceDialogStart >= m_LimitTime)
        {
            SelectOption(m_DefaultSelection);
        }
    }

    #endregion "unity event handlers"

    #region "public method"
    // public method

    public void SelectOption(int idx)
    {
        //AnimationState astate = CutsceneController.GetAnimState(m_Dialog.CC);

        //astate.speed = m_prevTimeScale;//resume the anim
        Time.timeScale = m_prevTimeScale;

        OneOption opt = m_AllOptions.m_Options[idx];
        string ttag = opt.m_TimeTag;
        CutsceneController.JumpToTimeTag(m_Dialog.CC, ttag);

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