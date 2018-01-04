using System;
using System.Collections.Generic;
using UnityEngine;
using MH.GUIData;

namespace MH
{
/// <summary>
/// a dialog used to display text, with UnityGUI
/// </summary>
public class CC_Unity_NormalDialog : MonoBehaviour
{
    #region "configurable data"
    // configurable data

    public List<NormalParagraph> m_Paragraphs;
    public float m_SetTimeScale = 0f;
    public float m_ParagraphLimitTime = -1f; //negative means no limit, this is the time limit for each paragraph
    public bool m_AllowSkip = true;
    public GUISkin m_Skin = null;

    #endregion "configurable data"

    #region "data"
    // data

    //private CC_GUI m_Dialog;
    private float m_TimeSinceParagraphStart = 0f;
    private float m_prevTimeScale = 0f;

    private int m_ParagraphIdx = 0;

    #endregion "data"

    #region "unity event handlers"
    // unity event handlers

    void Start()
    { }

    void StartDialog(CC_GUI ccgui)
    {
        //m_Dialog = ccgui;
        //Dbg.Log("Start NormalDialog: {0}", m_Dialog.name);

        enabled = true;

        //AnimationState animState = CutsceneController.GetAnimState(m_Dialog.CC);
        //m_prevTimeScale = animState.speed;
        //animState.speed = m_SetTimeScale; //stop the anim

        m_prevTimeScale = Time.timeScale;
        Time.timeScale = m_SetTimeScale;

        m_ParagraphIdx = 0;
        m_TimeSinceParagraphStart = 0f;
    }

    void OnGUI()
    {
        GUIUtil.PushSkin(m_Skin);

        Rect wndRect = new Rect(0, Screen.height - WND_HEIGHT, Screen.width, WND_HEIGHT);
        Rect imgRect = new Rect();
        NormalParagraph p = m_Paragraphs[m_ParagraphIdx];

        if( p.m_SpeakerAvatarImg != null )
        {
            imgRect.xMin = 0;
            imgRect.yMin = wndRect.yMin;
            imgRect.width = p.m_SpeakerAvatarImg.width;
            imgRect.height = p.m_SpeakerAvatarImg.height;
            if (imgRect.height > WND_HEIGHT)
            {
                imgRect.y = Screen.height - imgRect.height;
            }
        }

        GUI.Box(wndRect, "");
        if( p.m_SpeakerAvatarImg != null )
        {
            GUIUtil.PushGUIColor(Color.white);
            GUI.DrawTexture(imgRect, p.m_SpeakerAvatarImg);
            GUIUtil.PopGUIColor();
        }

        Rect textRect = new Rect(wndRect);
        textRect.xMin = imgRect.width + 20f;
        textRect.xMax -= 20f;
        GUILayout.BeginArea(textRect);
        {
            GUILayout.Label(p.m_SpeakerName);

            GUIUtil.PushGUIEnable(false);
            GUILayout.TextArea(p.m_Text);
            GUIUtil.PopGUIEnable();
        }
        GUILayout.EndArea();

        GUIUtil.PopSkin();
    }


    void Update()
    {
        m_TimeSinceParagraphStart += Time.deltaTime;
        
        if (_IsSkipped() ||
            (m_ParagraphLimitTime > 0 && m_TimeSinceParagraphStart >= m_ParagraphLimitTime))
        {
            m_ParagraphIdx++;
            m_TimeSinceParagraphStart = 0f;
            if (m_ParagraphIdx >= m_Paragraphs.Count)
            {
                Finish();
            }
        }
    }

    #endregion "unity event handlers"

    #region "public method"
    // public method

    public void Finish()
    {
        Time.timeScale = m_prevTimeScale; //resume the anim

        enabled = false;
    }

    #endregion "public method"

    #region "private method"
    // private method

    private bool _IsSkipped()
    {
        if (m_AllowSkip && Input.GetMouseButtonUp(0))
            return true;
        else
            return false;
    }

    #endregion "private method"

    #region "constant data"
    // constant data

    public const float WND_HEIGHT = 100f;

    #endregion "constant data"

}
}