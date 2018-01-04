using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace MH
{
	[Serializable]
    public class CCSpawnData
    {
        public CCSrcObj m_SrcObj;
        public CCPosInfo m_Pos;
        public CCRotInfo m_Rot;
        public CCScaInfo m_Scale;
        public CCTrPath m_Parent; //if not specified, new object will be added under CC_Spawn.transform

        public GameObject Spawn(Transform ccroot)
        {
            if( !m_SrcObj.Valid)
            {
                Dbg.LogWarn("CCSpawnData.Spawn: didn't specify SrcObj!");
                return null;
            }

            Object o = m_SrcObj.GetObj();
            if( o == null )
            {
                Dbg.LogWarn("CCSpawnData.Spawn: m_SrcObj.GetObj returned null");
                return null;
            }

            GameObject newObj = Object.Instantiate(o) as GameObject;
            Transform tr = newObj.transform;

            Dbg.Assert( m_Pos != null, "CCSpawnData.Spawn: m_Pos is null");

            tr.position = m_Pos.ToWorldPos(ccroot);

            if( m_Rot.Valid )
            {
                tr.rotation = m_Rot.ToWorldRotation(ccroot);
            }

            if( m_Scale.Valid )
            {
                tr.localScale = m_Scale.ToLocalScale(ccroot);
            }

            if( m_Parent.Valid )
            {
                Transform newParent = m_Parent.GetTransform(ccroot);
                tr.parent = newParent;
            }

            return newObj;
        }
    }
}
