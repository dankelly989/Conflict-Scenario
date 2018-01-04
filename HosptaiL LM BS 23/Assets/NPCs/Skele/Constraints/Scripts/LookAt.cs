using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class LookAt : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("the axis that will point toward target")]
        private EAxisD m_lookAxis = EAxisD.Z;
        [SerializeField][Tooltip("the axis that point up")]
        private EAxisD m_upAxis = EAxisD.Y;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"
        // data

        private Vector3 m_prevDir = Vector3.forward;

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"
        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        public EAxisD LookAxis
        {
            get { return m_lookAxis; }
            set { m_lookAxis = value; }
        }

        public EAxisD UpAxis
        {
            get { return m_upAxis; }
            set { m_upAxis = value; }
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

            if (!m_target)
                return; //do nothing if no target is specified

            Quaternion initQ = m_tr.rotation;
            Quaternion endQ = Quaternion.identity;

            Vector3 selfPos = m_tr.position;
            Vector3 targetPos = m_target.position;
            Vector3 dir = (targetPos - selfPos).normalized;
            if (dir == Vector3.zero)
                dir = m_prevDir;

            m_prevDir = dir;

            endQ = QUtil.LookAt(m_lookAxis, m_upAxis, dir);
            
            if (!Mathf.Approximately(m_influence, 1f))
            {
                endQ = Quaternion.Slerp(initQ, endQ, m_influence);
            }

            m_tr.rotation = endQ;
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_target)
            {
                _DrawLine(m_tr, m_target);
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
