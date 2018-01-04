using System;
using System.Collections.Generic;
using UnityEngine;

using Job = System.Collections.IEnumerator;

namespace MH
{

/// <summary>
/// Jump to time pos or Time_tag
/// </summary>
public class CC_JumpTo : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public JumpType m_kType = JumpType.Time;
    public float m_time;
    public string m_timeTag;

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	void Start()
	{}

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnAnimEvent()
    {
        AnimationState animState = CutsceneController.GetAnimState(m_CC);
        //Dbg.Log("JumpTo.OnAnimEvent: current time: {0}, FC: {1}", animState.time, Time.frameCount);
        switch (m_kType)
        {
            case JumpType.Time:
                {
                    CutsceneController.JumpToTime(m_CC, m_time);
                    //Dbg.Log("Jumpto: {0}", m_time);
                }
                break;
            case JumpType.NormalizedTime:
                {
                    //animState.normalizedTime = m_time;
                    CutsceneController.JumpToTime(m_CC, m_time * animState.clip.length);
                    //Dbg.Log("Jumpto normalized: {0} : {1}", m_time, m_time * animState.clip.length);
                }
                break;
            case JumpType.TimeTag:
                {
                    CutsceneController.JumpToTimeTag(m_CC, m_timeTag);
                    //Dbg.Log("Jumpto tag: {0}", CutsceneController.GetTimeByTag(m_CC, m_timeTag));
                }
                break;
            default:
                Dbg.LogErr("CC_JumpTo.OnAnimEvent: unexpected JumpType: {0}", m_kType);
                break;
        }
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    public enum JumpType
    {
        Time,
        NormalizedTime,
        TimeTag,
    }

    #endregion "constant data"
}

}