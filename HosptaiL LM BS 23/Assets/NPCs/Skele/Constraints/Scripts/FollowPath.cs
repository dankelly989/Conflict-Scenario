using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
using MH.Curves;

namespace MH.Constraints
{
    public class FollowPath : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target Spline")]
        private BaseSplineBehaviour m_targetSpline;
        [SerializeField][Tooltip("the T for spline")]
        private float m_offset = 0;
        [SerializeField][Tooltip("owner will follow the spline's direction/tilt/scale")]
        private bool m_followCurve = false;
        [SerializeField][Tooltip("when follow curve, this axis of owner will be taken as forward")]
        private EAxisD m_forwardDir = EAxisD.Z;
        [SerializeField][Tooltip("when follow curve, this axis of owner will be taken as up")]
        private EAxisD m_upDir = EAxisD.Y;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;


        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"

        #endregion "unity event handlers"

        #region "props"
        public BaseSplineBehaviour Spline
        {
            get { return m_targetSpline; }
            set { m_targetSpline = value; }
        }
        public float Offset
        {
            get { return m_offset; }
            set { m_offset = value; }
        }
        public bool FollowCurve
        {
            get { return m_followCurve; }
            set { m_followCurve = value; }
        }
        public EAxisD ForwardDir
        {
            get { return m_forwardDir; }
            set { m_forwardDir = value; }
        }
        public EAxisD UpDir
        {
            get { return m_upDir; }
            set { m_upDir = value; }
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

            if (!m_targetSpline)
                return; //do nothing if no target is specified

            Vector3 initPos = m_tr.GetPosition(ESpace.World); //use world space
            Quaternion initRot = m_tr.GetQuaternion(ESpace.World);
            Vector3 endPos = initPos;
            Quaternion endRot = m_tr.rotation;
            Vector3 endTan = Vector3.forward;
            Vector3 endUp = Vector3.up;


            
            if (m_followCurve)
            { // calculate pos/tan/up together
                m_targetSpline.CalcTransformed(m_offset, out endPos, out endTan, out endUp);
                endRot = QUtil.LookAt(m_forwardDir, m_upDir, endTan, endUp);
            }
            else
            { // calculate pos only
                endPos = m_targetSpline.GetTransformedPosition(m_offset);
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(initPos, endPos, m_influence);
                if (m_followCurve)
                    endRot = Quaternion.Slerp(initRot, endRot, m_influence);
            }

            m_tr.SetPosition(endPos, ESpace.World);
            m_tr.SetQuaternion(endRot, ESpace.World);
        }

        //public override void DoDrawGizmos()
        //{
        //    base.DoDrawGizmos();

        //    if (m_target)
        //    {
        //        var oldC = Gizmos.color;
        //        Gizmos.color = Color.blue;
        //        Gizmos.DrawLine(m_tr.position, m_target.position);
        //        Gizmos.color = oldC;
        //    }
        //}

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"
    }
}
