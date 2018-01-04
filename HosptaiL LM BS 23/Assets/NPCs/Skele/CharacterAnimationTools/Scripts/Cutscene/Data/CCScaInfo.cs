using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    [Serializable]
    public class CCScaInfo 
    {
        public Vector3 m_RelScale;
        public CCTrPath m_TrPath;
        public bool m_Valid;

        public CCScaInfo(Vector3 scale)
            : this(scale, null)
        { }
        public CCScaInfo(Vector3 scale, CCTrPath trPath)
        {
            m_RelScale = scale;
            m_TrPath = trPath;
        }

        public bool Valid
        {
            get { return m_Valid; }
        }

        public Vector3 ToLocalScale(Transform ccroot)
        {
            Vector3 finalScale;
            Transform relTr = null;

            if (m_TrPath.Valid)
            {
                relTr = m_TrPath.GetTransform(ccroot);
            }

            if (relTr != null)
            {
                finalScale = Vector3.Scale(relTr.lossyScale, m_RelScale);
            }
            else
            {
                finalScale = m_RelScale;
            }

            return finalScale;
        }

    }

}
