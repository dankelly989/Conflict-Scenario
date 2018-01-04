using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to control Camera movement in cutscene
/// </summary>
public class CC_Camera : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public Effect m_kEffect = Effect.Shake; //the camera effect type
    public CCTrPath m_CamTrPath; //the camera's tr path
    public CamShakeParam m_ShakeParam = null;

    #endregion "configurable data"

	#region "data"
    // data

    private bool m_bStarted = false;
    private bool m_bInvalid = false;
    private Transform m_CCTr;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	void Start()
	{
        if( !m_CamTrPath.Valid)
        {
            Dbg.LogWarn("CC_Camera.Start: didn't set CamTrPath");
            m_bInvalid = true;
        }

        m_CCTr = m_CC.transform;
    }

	void LateUpdate()
	{
        if (m_bInvalid || !m_bStarted)
            return;

        switch( m_kEffect )
        {
            case Effect.Shake:
                {
                    if( m_ShakeParam == null )
                    {
                        Dbg.LogWarn("CC_Camera.Update: ShakeParam is null");
                        m_bInvalid = true;
                        return;
                    }

                    var s = m_ShakeParam;
                    if( s.m_timeSinceStart > s.m_Duration )
                    { //time up
                        m_bStarted = false;
                        s.m_timeSinceStart = 0f;
                    }

                    Transform camTr = m_CamTrPath.GetTransform(m_CCTr);
                    Vector3 campos = CCPosInfo.ToWorldPos(m_CCTr, m_CamTrPath);
                    Vector3 randDelta = _GenRandomShake();
                    campos += randDelta - s.m_PrevShake;
                    camTr.position = campos;
                    s.m_PrevShake = randDelta;

                    m_ShakeParam.m_timeSinceStart += Time.deltaTime;
                }
                break;
        }

    }

    #endregion "unity event handlers"

	#region "public method"
    // public method
    public override void OnAnimEvent()
    {
        m_bStarted = true;
    }
    #endregion "public method"

	#region "private method"
    // private method

    private Vector3 _GenRandomShake()
    {
        var s = m_ShakeParam;
        Vector3 delta = UnityEngine.Random.insideUnitSphere * s.m_Amplitude;
        return delta;
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public enum Effect 
    {
        None,
        Shake
    }

    #endregion "constant data"

	#region "Inner Struct"
	// "Inner Struct" 

    [Serializable]
    public class CamShakeParam
    {
        public float m_Duration;
        public float m_Amplitude;

        [HideInInspector]
        public float m_timeSinceStart; //how much time have passed since shake begin, use Time.deltaTime to increment
        [HideInInspector]
        public Vector3 m_PrevShake;
    }
	
	#endregion "Inner Struct"
}

}
