using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to represent an transform path for CC
/// 
/// "/" means null -- the scene root
/// "/XXX" means /XXX , the path from scene root
/// empty means ccroot
/// X means ccroot/X
/// 
/// </summary>
[Serializable]
public class CCTrPath 
{
	#region "data"
    // data
    public bool m_Valid;
    public string m_trPath;

    #endregion "data"

	#region "public method"
    // public method

    public CCTrPath(string trPath)
    {
        m_trPath = trPath;
    }

    public bool Valid
    {
        get { return m_Valid; }
    }

    /// <summary>
    /// return null for SceneRoot or failure
    /// return CCRoot if empty
    /// return the corresponding transform for valid transformPath
    /// </summary>
    public Transform GetTransform(Transform ccroot)
    {
        if( m_trPath == SceneRoot )
        {
            return null;
        }
        else if( m_trPath == CCRoot )
        {
            return ccroot;
        }
        else if( m_trPath.StartsWith(SceneRoot) )
        {
            GameObject go = GameObject.Find(m_trPath);
            if( go == null )
            {
                Dbg.LogWarn("CCTrPath: cannot find GameObject at path: {0}", m_trPath);
                return null;
            }
            else
            {
                return go.transform;
            }
        }
        else
        {
            Transform tr = ccroot.Find(m_trPath);
            Dbg.Assert(tr != null, "CCTrPath.GetTransform: failed to get transform for transformPath: {0}", m_trPath);
            return tr;
        }
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string SceneRoot = "/";
    public const string CCRoot = "";

    #endregion "constant data"
}

}
