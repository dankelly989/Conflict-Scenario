using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// used to express the position in the cutscene context;
    /// </summary>
    [Serializable]
    public class CCPosInfo
	{
        public Vector3 m_RelPos;
        public CCTrPath m_RelTr; //the transform path from the CCRoot
        public Space m_Space;
        public bool m_Valid;

        public CCPosInfo(Vector3 relpos)
            : this(relpos, null, Space.World)
        { }
        public CCPosInfo(Vector3 relpos, CCTrPath trPath)
            : this(relpos, trPath, Space.World)
        { }
        public CCPosInfo(Vector3 relpos, CCTrPath trPath, Space sp)
        {
            m_RelPos = relpos;
            m_RelTr = trPath;
            m_Space = sp;
        }

        public bool Valid
        {
            get { return m_Valid; }
            set { m_Valid = value; }
        }

        public Vector3 ToWorldPos(Transform ccroot)
        {
            Vector3 finalPos;
            Transform relTr = null;

            if( m_RelTr != null )
            {
                relTr = m_RelTr.GetTransform(ccroot);
            }

            if( relTr != null )
            {
                if( m_Space == Space.Self )
                {
                    finalPos = relTr.TransformPoint(m_RelPos);
                }
                else
                {
                    finalPos = relTr.position + m_RelPos;
                }
            }
            else
            {
                finalPos = m_RelPos;
            }

            return finalPos;
        }

        /// <summary>
        /// static version
        /// get the position of the transform specified by trPath
        /// </summary>
        public static Vector3 ToWorldPos(Transform ccroot, CCTrPath targetTrPath)
        {
            Transform relTr = null;
            if( targetTrPath != null )
            {
                relTr = targetTrPath.GetTransform(ccroot);
            }

            if( relTr != null )
            {
                return relTr.position;
            }
            else
            {
                return Vector3.zero;
            }
        }
	}
}
