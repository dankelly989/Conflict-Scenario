using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// an utility to call functions
/// </summary>
public class CC_SendMsg : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public CCTrPath m_TargetGO;
    public string m_Function;
    public CC_MsgParam m_Param;

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
        Transform tr = m_TargetGO.GetTransform(m_CC.transform);
        if( tr == null )
        {
            Dbg.LogWarn("CC_SendMsg.OnAnimEvent: failed to find GO on path: {0}: {1}", m_TargetGO.m_trPath, name);
            return;
        }

        tr.SendMessage(m_Function, m_Param, m_Param.m_RequireReceiver);
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"

	#region "Inner Struct"
	// "Inner Struct" 
	
	#endregion "Inner Struct"
}

[Serializable]
public class CC_MsgParam
{
    public string m_string;
    public int m_int;
    public float m_float;
    public bool m_bool;
    public CCTrPath m_TargetPath;
    public UnityEngine.Object m_Object;
    public SendMessageOptions m_RequireReceiver = SendMessageOptions.DontRequireReceiver;
}

}
