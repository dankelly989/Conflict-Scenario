//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace MH
//{

///// <summary>
///// used to replace an object with another
///// </summary>
//public class CC_Replace : CC_EvtActions
//{
//    #region "configurable data"
//    // configurable data

//    public CCSrcObj m_ExtObj;
//    public CCTrPath m_TargetObj;

//    #endregion "configurable data"

//    #region "data"
//    // data

//    #endregion "data"

//    #region "unity event handlers"
//    // unity event handlers

//    void Start()
//    {
//        Dbg.Assert(m_ExtObj.Valid, "CC_Replace.Start: the m_ExtObj is not valid");
//        Dbg.Assert(m_TargetObj.Valid, "CC_Replace.Start: the m_TargetObj is not valid");
//    }

//    #endregion "unity event handlers"

//    #region "public method"
//    // public method

//    public override void OnAnimEvent()
//    {
//        Transform ccroot = CC.transform;
//        GameObject extGO = m_ExtObj.GetObj() as GameObject;
//        Dbg.Assert(extGO != null, "CC_Replace.OnAnimEvent: failed to get external GO");
//        Transform extTr = extGO.transform;

//        Transform intTr = m_TargetObj.GetTransform(ccroot);
//        GameObject intGO = intTr.gameObject;

//        CC.SwapGO(extGO, intGO);
//    }

//    #endregion "public method"

//    #region "private method"
//    // private method

//    #endregion "private method"

//    #region "constant data"
//    // constant data

//    #endregion "constant data"
//}

//}
