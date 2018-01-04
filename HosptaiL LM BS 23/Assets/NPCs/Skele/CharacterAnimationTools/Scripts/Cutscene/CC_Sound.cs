using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// 
/// </summary>
public class CC_Sound : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public Action m_ExecAction;
    public CCTrPath m_AudioSourcePath;
    public AudioClip m_Clip;

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    void Update(){}

    #endregion "unity event handlers"

	#region "public method"
    // public method
    public override void OnAnimEvent()
    {
        Transform audioTr = m_AudioSourcePath.GetTransform(m_CC.transform);
        AudioSource audioSrc = audioTr.GetComponent<AudioSource>();

        if( audioSrc == null )
        {
            Dbg.LogWarn("CC_Sound.OnAnimEvent: no AudioSource found on path {0}, do nothing...: {1}", m_AudioSourcePath.m_trPath, name);
            return;
        }

        switch ( m_ExecAction )
        {
            case Action.Start:
                {
                    if (m_Clip != null)
                    {
                        //Dbg.Log("Sound Played: {0}", m_Clip);
                        audioSrc.clip = m_Clip;
                        audioSrc.Play();
                    }
                    else
                    {
                        audioSrc.Play();
                    }
                }
                break;
            case Action.Stop:
                {
                    audioSrc.Stop();
                }
                break;
            default:
                Dbg.LogErr("CC_Sound.OnAnimEvent: unexpected exec action: {0}", m_ExecAction);
                break;
        }
        
    }
    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    public enum Action
    {
        Start,
        Stop
    }

    #endregion "constant data"
}


}
