using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to control the Timescale
/// </summary>
public class CC_PlaySpeed : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public float m_PlaybackSpeed = 1.0f;

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
        //AnimationState astate = CutsceneController.GetAnimState(m_CC);
        Time.timeScale = m_PlaybackSpeed;
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
}

}
