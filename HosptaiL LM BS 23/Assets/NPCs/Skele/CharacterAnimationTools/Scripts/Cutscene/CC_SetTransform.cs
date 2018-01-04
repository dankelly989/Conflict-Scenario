using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to change specified object's transform parent and pos/rot/scale
/// 
/// DONT use this on a object whose transform is under control of animation, 
/// the animation will keep overwriting the result
/// </summary>
public class CC_SetTransform : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public CCTrPath m_Target;
    public CCTrPath m_NewParent;

    public CCPosInfo m_Pos;
    public CCRotInfo m_Rot;
    public CCScaInfo m_Scale; 

    #endregion "configurable data"

	#region "data"
    // data

    //private bool m_bEventReached = false;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnAnimEvent()
    {
        //m_bEventReached = true;

        Transform cctr = m_CC.transform;
        if (!m_Target.Valid)
        {
            Dbg.LogWarn("CC_SetTransform.OnAnimEvent: Target not specified");
            return;
        }

        Transform targetTr = m_Target.GetTransform(cctr);

        if( m_NewParent.Valid)
        {
            Transform newPrTr = m_NewParent.GetTransform(cctr);
            targetTr.parent = newPrTr;
        }

        if( m_Pos.Valid )
        {
            targetTr.position = m_Pos.ToWorldPos(cctr);
        }
        if( m_Rot.Valid )
        {
            targetTr.rotation = m_Rot.ToWorldRotation(cctr);
        }
        if( m_Scale.Valid )
        {
            targetTr.localScale = m_Scale.ToLocalScale(cctr);
        }

    }

    //void LateUpdate()
    //{
    //    if( m_bEventReached )
    //    {
    //        Dbg.Log("frame:{0}, lateupdate", Time.frameCount);
    //    }
    //}

    #endregion "public method"

	#region "private method"
    // private method



    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
}

}
