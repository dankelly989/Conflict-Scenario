using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
[Serializable]
public class CCRotInfo 
{
    public Vector3 m_RelDirection;
    public CCTrPath m_TrPath; //the transform path from the CCRoot, e.g.: the "/CCRoot/X" is represented by "X", the "/" represents the sceneroot, empty means ccroot

    public CCRotInfo(Vector3 relDir)
        : this(relDir, null)
    { }
    public CCRotInfo(Vector3 relDir, CCTrPath trPath)
    {
        m_RelDirection = relDir;
        m_TrPath = trPath;
    }

    public bool Valid
    {
        get { return m_RelDirection != Vector3.zero; }
    }

    public Vector3 ToWorldDir(Transform ccroot)
    {
        Vector3 finalDir;
        Transform relTr = null;

        if( m_TrPath != null )
        {
            relTr = m_TrPath.GetTransform(ccroot);
        }

        if( relTr != null )
        {
            finalDir = relTr.TransformDirection(m_RelDirection);
        }
        else
        {
            finalDir = m_RelDirection;
        }

        return finalDir;
    }

    public Quaternion ToWorldRotation(Transform ccroot)
    {
        Vector3 dir = ToWorldDir(ccroot);
        return Quaternion.LookRotation(dir);
    }

}

}
