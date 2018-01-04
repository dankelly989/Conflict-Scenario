using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class LimitLocation : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("Limit what fields")]
        private ELimitAffect m_eLimitAffect = ELimitAffect.None;
        [SerializeField][Tooltip("the space owner is evaluated in?")]
        private ESpace m_ownerSpace = ESpace.World;
        [SerializeField][Tooltip("the min limits")]
        private Vector3 m_limitMin = Vector3.zero;
        [SerializeField][Tooltip("the max limits")]
        private Vector3 m_limitMax = Vector3.zero;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"

        public MH.Constraints.ELimitAffect LimitAffect
        {
            get { return m_eLimitAffect; }
            set { m_eLimitAffect = value; }
        }

        public Vector3 LimitMin
        {
            get { return m_limitMin; }
            set { m_limitMin = value; }
        }

        public Vector3 LimitMax
        {
            get { return m_limitMax; }
            set { m_limitMax = value; }
        }
        public MH.ESpace OwnerSpace
        {
            get { return m_ownerSpace; }
            set { m_ownerSpace = value; }
        }

        public override float Influence
        {
            get { return m_influence; }
            set { m_influence = value; }
        }

        #endregion "props"

        #region "public method"
        // public method

        public override void DoAwake()
        {
            base.DoAwake();
        }

        public override void DoUpdate()
        {
            base.DoUpdate();

            Vector3 selfPos = m_tr.GetPosition(m_ownerSpace);
            Vector3 endPos = selfPos;

            // apply effect
            if ((m_eLimitAffect & ELimitAffect.MinX) != 0)
            {
                endPos.x = Mathf.Max(m_limitMin.x, endPos.x);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxX) != 0)
            {
                endPos.x = Mathf.Min(m_limitMax.x, endPos.x);
            }
            if ((m_eLimitAffect & ELimitAffect.MinY) != 0)
            {
                endPos.y = Mathf.Max(m_limitMin.y, endPos.y);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxY) != 0)
            {
                endPos.y = Mathf.Min(m_limitMax.y, endPos.y);
            }
            if ((m_eLimitAffect & ELimitAffect.MinZ) != 0)
            {
                endPos.z = Mathf.Max(m_limitMin.z, endPos.z);
            }
            if ((m_eLimitAffect & ELimitAffect.MaxZ) != 0)
            {
                endPos.z = Mathf.Min(m_limitMax.z, endPos.z);
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(selfPos, endPos, m_influence);
            }

            m_tr.SetPosition(endPos, m_ownerSpace);
        }

        private Matrix4x4 m_tmpMtx;
        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_eLimitAffect == ELimitAffect.Full)
            {
                var oldC = Gizmos.color;
                Gizmos.color = ConUtil.TransparentGizmosColor;

                var min = m_limitMin;
                var max = m_limitMax;
                //if (m_ownerSpace == ESpace.Self && m_tr.parent != null)
                //{
                //    min = m_tr.parent.TransformPoint(min);
                //    max = m_tr.parent.TransformPoint(max);
                //}
                Vector3 center = (min + max) * 0.5f;
                Vector3 size = m_limitMax - m_limitMin; //don't use transformed point, in order to keep it axis-aligned

                bool needMatrix = (m_ownerSpace == ESpace.Self && m_tr.parent != null);
                if (needMatrix)
                {
                    m_tmpMtx = Gizmos.matrix;
                    Gizmos.matrix = m_tr.parent.localToWorldMatrix;
                }
                Gizmos.DrawCube(center, size);
                if( needMatrix )
                    Gizmos.matrix = m_tmpMtx;

                Gizmos.color = oldC;
            }
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data



        #endregion "constant data"
    }
}
