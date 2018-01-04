using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to calculate the parabola
/// </summary>
public class Parabola
{
	#region "data"
    // data
    private Vector3 m_Start;
    private Vector3 m_End;
    private float m_TotalTime;
    private float m_G;

    private float m_CurTime = float.PositiveInfinity;

    private float t0;

    #endregion "data"

	#region "public method"
    // public method

    public Parabola()
    {
    }

    public void Init(Vector3 startPt, Vector3 endPt, float totalTime, float G = DEFAULT_G)
    {
        m_Start = startPt;
        m_End = endPt;
        m_TotalTime = totalTime;
        m_G = G;
        m_CurTime = 0f;

        _CalculateParam();
    }

    public void Stop()
    {
        m_CurTime = float.PositiveInfinity;
    }

    /// <summary>
    /// update internal time, return current position
    /// </summary>
    public Vector3 Update(float deltaTime)
    {
        m_CurTime += deltaTime;

        Vector3 pos = CalcPos(m_CurTime);

        return pos;
    }

    public bool isRunning
    {
        get { return m_CurTime < m_TotalTime; }
    }

    /// <summary>
    /// calculate position at given time
    /// </summary>
    public Vector3 CalcPos(float time)
    {
        //X and Z will interpolate linearly
        float x = Mathf.Lerp(m_Start.x, m_End.x, time / m_TotalTime);
        float z = Mathf.Lerp(m_Start.z, m_End.z, time / m_TotalTime);

        //Y will calculate with parabola
        float y = 0f;
        if( time < t0 )
        { // upward time
            float totalH = 0.5f * m_G * t0 * t0;
            float leftH = 0.5f * m_G * (t0 - time) * (t0 - time);
            y = totalH - leftH + m_Start.y;
        }
        else
        { //downward time
            float upwardH = 0.5f * m_G * t0 * t0;
            float downwardH = 0.5f * m_G * (time - t0) * (time - t0);
            y = m_Start.y + upwardH - downwardH;
        }

        return new Vector3(x, y, z);
    }

    #endregion "public method"

	#region "private method"
    // private method

    private void _CalculateParam()
    {
        float yStart = m_Start.y;
        float yEnd = m_End.y;

        t0 = (yEnd - yStart) / (m_TotalTime * m_G) + m_TotalTime * 0.5f; //upward time
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public const float DEFAULT_G = 9.8f;

    #endregion "constant data"
}

}
