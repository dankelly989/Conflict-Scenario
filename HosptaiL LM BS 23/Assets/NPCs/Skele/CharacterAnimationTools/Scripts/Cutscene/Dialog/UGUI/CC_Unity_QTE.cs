using System;
using System.Collections.Generic;
using UnityEngine;
using MH.GUIData;

namespace MH
{

/// <summary>
/// display QTE, with UnityGUI
/// </summary>
public class CC_Unity_QTE : MonoBehaviour
{
    #region "configurable data"
    // configurable data

    public QTEDesc m_QTEDesc;
    public float m_SetTimeScale = 0f;
    public Rect m_WndRect = new Rect(Screen.width * 0.6f, Screen.height * 0.5f, 50f, 50f);

    #endregion "configurable data"

    #region "data"
    // data

    private CC_GUI m_Dialog;
    private float m_TimeSinceDialogStart = 0f;
    private float m_prevTimeScale = 0f;
    private int m_expectedIdx = 0;

    #endregion "data"

    #region "unity event handlers"
    // unity event handlers

    void StartDialog(CC_GUI ccgui)
    {
        //Dbg.Log("Start QTE: {0}: time: {1}", ccgui.name, CutsceneController.GetAnimState(ccgui.CC).time);

        m_Dialog = ccgui;

        enabled = true;

        //AnimationState animState = CutsceneController.GetAnimState(m_Dialog.CC);
        m_prevTimeScale = Time.timeScale;
        Time.timeScale = m_SetTimeScale; //stop the anim

        m_TimeSinceDialogStart = 0f;

        m_expectedIdx = UnityEngine.Random.Range(0, m_QTEDesc.m_RandomInputs.Count);
    }

    void OnGUI()
    {

        QTEInput inputInfo = m_QTEDesc.m_RandomInputs[m_expectedIdx];

        if( inputInfo.m_DisplayContent.image != null )
        {
            GUI.DrawTexture(m_WndRect, inputInfo.m_DisplayContent.image);
        }
        else if( !string.IsNullOrEmpty(inputInfo.m_DisplayContent.text) )
        {
            GUI.Box(m_WndRect, inputInfo.m_DisplayContent.text);
        }
        else
        {
            Dbg.LogWarn("CC_Unity_QTE.OnGUI: the QTE display item is not specified: {0}", m_Dialog.name);
        }

    }

    void Update()
    {

        float timeLimit = m_QTEDesc.m_TimeLimit;
        m_TimeSinceDialogStart += Time.deltaTime;
        if (m_TimeSinceDialogStart >= timeLimit)
        {
            OnFailed();
        }

        QTEInput inputInfo = m_QTEDesc.m_RandomInputs[m_expectedIdx];
        KeyCode expected = inputInfo.m_ExpectedInput;
        if( Input.anyKeyDown )
        {
            if (Input.GetKeyDown(expected))
            {
                OnSuccess();
            }
            else
            {
                OnFailed();
            }
        }

    }

    #endregion "unity event handlers"

    #region "public method"
    // public method

    public void OnSuccess()
    {
        var cc = m_Dialog.CC;
        //AnimationState astate = CutsceneController.GetAnimState(cc);

        if( ! string.IsNullOrEmpty(m_QTEDesc.m_SuccessTimeTag) )
        {
            CutsceneController.JumpToTimeTag(cc, m_QTEDesc.m_SuccessTimeTag);
        }
        
        Time.timeScale = m_prevTimeScale;

        enabled = false;
        //Dbg.Log("OnSuccess: {0}", m_Dialog.name);
    }

    public void OnFailed()
    {
        var cc = m_Dialog.CC;
        AnimationState astate = CutsceneController.GetAnimState(cc);

        Dbg.Log("OnFailed: {1}: current time: {0}", astate.time, m_Dialog.name);

        if( ! string.IsNullOrEmpty(m_QTEDesc.m_FailTimeTag) )
        {
            CutsceneController.JumpToTimeTag(cc, m_QTEDesc.m_FailTimeTag);
        }

        Time.timeScale = m_prevTimeScale;

        enabled = false;
        //Dbg.Log("OnFailed: {0}", m_Dialog.name);
    }

    #endregion "public method"

    #region "private method"
    // private method

    #endregion "private method"

    #region "constant data"
    // constant data

    public const float WND_HEIGHT = 200f;

    #endregion "constant data"

}

}
