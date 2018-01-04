using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

/// <summary>
/// used to specify an object that used to instantiate from,
/// could be Prefab reference or using ResMgr to get it
/// </summary>
[Serializable]
public class CCSrcObj
{
    public GameObject m_PrefabRef;
    private string m_ResID = null; //use ResMgr to get the object

    public bool Valid
    {
        get { return m_PrefabRef != null || !string.IsNullOrEmpty(m_ResID); }
    }

    public UnityEngine.Object GetObj()
    {
        if( m_PrefabRef != null )
        {
            return m_PrefabRef;
        }
        else
        {
            if( string.IsNullOrEmpty(m_ResID) )
            {
                Dbg.LogWarn("CCSrcObj.GetObj: nothing is specified, return null");
                return null;
            }

            ResMgr.Res r = ResMgr.Instance.GetRes(m_ResID);
            if( !r.Valid )
            {
                Dbg.LogWarn("CCSrcObj.GetObj: cannot get resource identified by: {0}", m_ResID);
                return null;
            }

            return r.m_Resource;
        }

    }
}

}
