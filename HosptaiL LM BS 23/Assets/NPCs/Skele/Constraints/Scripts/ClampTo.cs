using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
using MH.Curves;

namespace MH.Constraints
{
    public class ClampTo : BaseConstraint
    {
        #region "configurable data"

        [SerializeField][Tooltip("the target Spline")]
        private BaseSplineBehaviour m_targetSpline;
        [SerializeField][Tooltip("use offset?")]
        private bool m_useOffset = false;
        [SerializeField][Tooltip("offset value, only effect when m_useOffset is true")]
        private Vector3 m_offset = new Vector3(0, 0, 0);
        [SerializeField][Tooltip("moving on this axis will cause movement along spline")]
        private EAxis m_mainAxis = EAxis.X;
        [SerializeField][Tooltip("the dimension on the main-axis")]
        private float m_dimension = 1f;
        [SerializeField][Tooltip("the start value of the mainAxis")]
        private float m_startVal = 0f;
        [SerializeField][Tooltip("cyclic")]
        private bool m_cyclic = false;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;
        [SerializeField][HideInInspector]
        private Transform m_targetTr = null;

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
            set { 
                m_targetSpline = value;
                if (m_targetSpline != null)
                    m_targetTr = m_targetSpline.transform;
            }
        }
        public bool UseOffset
        {
            get { return m_useOffset; }
            set { m_useOffset = value; }
        }
        public UnityEngine.Vector3 Offset
        {
            get { return m_offset; }
            set { m_offset = value; }
        }
        public EAxis MainAxis
        {
            get { return m_mainAxis; }
            set { m_mainAxis = value; }
        }
        public float Dimension
        {
            get { return m_dimension; }
            set { m_dimension = value; }
        }
        public float StartVal
        {
            get { return m_startVal; }
            set { m_startVal = value; }
        }
        public bool Cyclic
        {
            get { return m_cyclic; }
            set { m_cyclic = value; }
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
            Vector3 endPos = initPos;

            // calculate endPos
            float t = 0, v = 0;
            switch (m_mainAxis)
            {
                case EAxis.X:
                    {
                        v = initPos.x;
                        // put v into [0, m_dimension]
                        if (m_cyclic) 
                            v = Mathf.Repeat((v - m_startVal), m_dimension);
                        else
                            v = Mathf.Clamp(v - m_startVal, 0, m_dimension);
                    }
                    break;
                case EAxis.Y:
                    {
                        v = initPos.y;
                        // put v into [0, m_dimension]
                        if (m_cyclic)
                            v = Mathf.Repeat((v - m_startVal), m_dimension);
                        else
                            v = Mathf.Clamp(v - m_startVal, 0, m_dimension);
                    }
                    break;
                case EAxis.Z:
                    {
                        v = initPos.z;
                        // put v into [0, m_dimension]
                        if (m_cyclic)
                            v = Mathf.Repeat((v - m_startVal), m_dimension);
                        else
                            v = Mathf.Clamp(v - m_startVal, 0, m_dimension);
                    }
                    break;
                default:
                    Dbg.LogErr("ClampTo.DoUpdate: unexpected mainAxis: {0}", m_mainAxis);
                    break;
            }

            t = v / m_dimension;
            Dbg.Assert(t <= 1f && t >= 0, "ClampTo.DoUpdate: t = {0}", t);
            endPos = m_targetSpline.Spline.Interp(t);
            endPos = m_targetTr.TransformPoint(endPos);

            // apply offset
            if (m_useOffset)
            {
                endPos += m_offset;
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(initPos, endPos, m_influence);
            }

            m_tr.SetPosition(endPos, ESpace.World);
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
