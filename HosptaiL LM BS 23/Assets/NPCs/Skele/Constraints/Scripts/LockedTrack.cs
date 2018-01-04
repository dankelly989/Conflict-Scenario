using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    /// <summary>
    /// look at a specified target, but only allowed to rotate around one axis
    /// </summary>
    public class LockedTrack : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;
        [SerializeField][Tooltip("how should this constraint affects owner, local")]
        private EAxisD m_LookAxis = EAxisD.Z;
        [SerializeField][Tooltip("the axis allowed to rotate around, local")]
        private EAxisD m_RotateAxis = EAxisD.Y;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

        #endregion "configurable data"

        #region "data"

        //private Vector3 m_prevFwd = Vector3.forward;

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
            get { return m_LookAxis; }
            set { m_LookAxis = value; }
        }
        public EAxisD RotateAxis
        {
            get { return m_RotateAxis; }
            set { m_RotateAxis = value; }
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

            Quaternion initRot = m_tr.rotation;
            Vector3 selfPos = m_tr.position;
            Vector3 targetPos = m_target.position;
            
            Vector3 upDir = Vector3.up;
            switch (m_RotateAxis)
            {
                case EAxisD.X: upDir = m_tr.right; break;
                case EAxisD.Y: upDir = m_tr.up; break;
                case EAxisD.Z: upDir = m_tr.forward; break;
                default: Dbg.LogErr("LockedTrack.DoUpdate: unexpected rotate axis: {0}", m_RotateAxis); break;
            }

            Vector3 lookDir = targetPos - selfPos;
            Vector3 projLookDir = Vector3.ProjectOnPlane(lookDir, upDir);
            if (projLookDir != Vector3.zero)
            {
                Quaternion endRot = QUtil.LookAt(m_LookAxis, m_RotateAxis, projLookDir, upDir);
                if (!Mathf.Approximately(m_influence, 1f))
                    endRot = Quaternion.Slerp(initRot, endRot, m_influence);
                m_tr.SetQuaternion(endRot, ESpace.World);
            }
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

        #endregion "private method"

        #region "constant data"

        #endregion "constant data"
    }
}
