using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.IKConstraint
{
    [ExecuteInEditMode]
    public class ConeConstraintMB : IKConstraintMB
    {
		#region "configurable data"
	    // configurable data

        [SerializeField][Tooltip("the next joint in IK chain ")]
        private Transform m_nextJoint;
        [SerializeField][Tooltip("the ref axis, in parent space")]
        private Vector3 m_refAxis;
        [SerializeField][Tooltip("the angle between bone and refAxis cannot exceed m_angleLimit")]
        private float m_angleLimit;
        [SerializeField][Tooltip("whether limit twist")]
        private bool m_limitTwist = true;
        [SerializeField][Tooltip("the low angle limit of bone twist around refAxis")]
        private float m_minTwistLimit = -90f;
        [SerializeField][Tooltip("the high angle limit of bone twist around refAxis")]
        private float m_maxTwistLimit = 90f;

        [SerializeField][HideInInspector]
        private Quaternion m_initRot = Quaternion.identity;
        [SerializeField][HideInInspector]
        private Vector3 m_twistAxis = Vector3.down;

	    #endregion "configurable data"
	
		#region "data"
	    // data

        private Transform m_tr;
	
	    #endregion "data"
	
		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            if (m_nextJoint == null)
            {
                TryAutoSelectNextJoint();
            }
        }
	
	    #endregion "unity event handlers"
	
		#region "public method"
	    // public method

		#region "props"
		// "props" 

        public Transform tr
        {
            get
            {
                if (m_tr == null)
                {
                    m_tr = transform;
                }
                return m_tr;
            }
        }

        public Transform nextJoint
        {
            get { return m_nextJoint; }
            set {
                if (m_nextJoint != value)
                {
                    m_nextJoint = value;
                    _OnNextJointChanged();
                }
            }
        }

        public Vector3 refAxis
        {
            get { return m_refAxis; }
            set { m_refAxis = value; }
        }

        public float angleLimit
        {
            get { return m_angleLimit; }
            set { m_angleLimit = value; }
        }

        public bool limitTwist
        {
            get { return m_limitTwist; }
            set { m_limitTwist = value; }
        }

        public float minTwistLimit
        {
            get { return m_minTwistLimit; }
            set { m_minTwistLimit = value; }
        }

        public float maxTwistLimit
        {
            get { return m_maxTwistLimit; }
            set { m_maxTwistLimit = value; }
        }
		
		#endregion "props"

        /// <summary>
        /// 1. rotate bone back from current rotation to refAxis; angle is "X"
        /// 2. clamp X;
        /// 3. rotate back with clamped X;
        /// 4. calculate and clamp twist;
        /// </summary>
        public override void Apply(ISolver solver, int jointIdx)
        {
            if( m_nextJoint == null )
            {
                Dbg.CLogWarn(this, "ConeConstraintMB.Apply: nextJoint not set: {0}", name);
                return;
            }
            Dbg.CAssert(this, m_angleLimit >= 0, "ConeConstraintMB.Apply: m_angleLimit should >= 0, but: {0}", m_angleLimit);
            Dbg.CAssert(this, -180f <= m_minTwistLimit && m_minTwistLimit <= 180f, "ConeConstraintMB.Apply: minTwistLimit: {0}", m_minTwistLimit);
            Dbg.CAssert(this, -180f <= m_maxTwistLimit && m_maxTwistLimit <= 180f, "ConeConstraintMB.Apply: maxTwistLimit: {0}", m_maxTwistLimit);
            Dbg.CAssert(this, 0 <= m_angleLimit && m_angleLimit <= 180f, "ConeConstraintMB.Apply: angleLimit: {0}", m_angleLimit);

            var joints = solver.GetJoints();
            Transform j = joints[jointIdx];
            Transform cj = m_nextJoint; 
            Transform pj = j.parent;

            //1
            Vector3 boneDirWorld = cj.position - j.position;
            Vector3 refDirWorld = Misc.TransformDirection(pj, m_refAxis);
            Quaternion q = Quaternion.FromToRotation(boneDirWorld, refDirWorld);
            float angle; Vector3 rotAxis;
            q.ToAngleAxis(out angle, out rotAxis);
            angle = Misc.NormalizeAnglePI(angle);

            j.rotation = q * j.rotation;
            //Dbg.Log("coneconstraint: angle: {0}, rotAxis: {1}", angle, rotAxis);

            //2
            angle = Mathf.Clamp(angle, -m_angleLimit, m_angleLimit);

            //3
            j.Rotate(rotAxis, -angle, Space.World);

            //4
            if (m_limitTwist)
            { //use swing-twist decomposition of quaternion to limit twist
                Quaternion deltaRot = j.localRotation * Quaternion.Inverse(m_initRot);

                Vector3 curTwistAxisDirWorld = (m_nextJoint.position - tr.position).normalized;
                Vector3 curTwistAxisDirParent = Misc.InverseTransformDirection(pj, curTwistAxisDirWorld);
                Quaternion swingRot = Quaternion.FromToRotation(m_twistAxis, curTwistAxisDirParent);
                Quaternion twistRot = Quaternion.Inverse(swingRot) * deltaRot;

                Vector3 tmpAxis; float twist;
                twistRot.ToAngleAxis(out twist, out tmpAxis);
                twist = Misc.NormalizeAnglePI(twist);
                if (float.IsInfinity(tmpAxis.x)) //SPECIAL case, some extreme data will make tmpAxis to be <inf,inf,inf>
                {
                    tmpAxis = Vector3.right;
                    twist = 0;
                }
                if (Misc.IsObtuseAngle(tmpAxis, m_twistAxis))
                {
                    twist = -twist;
                    tmpAxis = -tmpAxis;
                }

                twist = Mathf.Clamp(twist, m_minTwistLimit, m_maxTwistLimit);
                twistRot = Quaternion.AngleAxis(twist, tmpAxis);
                deltaRot = swingRot * twistRot;

                var applied = deltaRot * m_initRot;
                j.localRotation = applied;
            }
        }

        /// <summary>
        /// calc the init twist
        /// refer: shard/s29/nl/3057637/eb34b758-7e54-4f3d-9cec-2e5bcebe09e3
        /// </summary>
        public void CalcInitData()
        {
            Dbg.CAssert(this, m_nextJoint != null, "ConeConstraintMB.CalcInitData: nextJoint not set: {0}", name);

            Transform j = tr;
            Transform pj = j.parent;
            m_initRot = j.localRotation;
            m_twistAxis = Misc.InverseTransformDirection(pj, (m_nextJoint.position - j.position).normalized); //parent
        }

        public bool TryAutoSelectNextJoint()
        {
            var tr = transform;
            if (tr.childCount > 0 && nextJoint == null)
            {
                var child = tr.GetChild(0);
                this.nextJoint = child;
                return true;
            }
            else
            {
                return false;
            }
        }

        public float CalcTwist()
        {
            Transform j = tr;
            Transform pj = j.parent;
            Quaternion curRot = j.localRotation;
            Quaternion deltaRot = curRot * Quaternion.Inverse(m_initRot);

            Vector3 curTwistAxisDirWorld = (m_nextJoint.position - tr.position).normalized;
            Vector3 curTwistAxisDirParent = Misc.InverseTransformDirection(pj, curTwistAxisDirWorld);
            Quaternion swingRot = Quaternion.FromToRotation(m_twistAxis, curTwistAxisDirParent);
            Quaternion twistRot = Quaternion.Inverse(swingRot) * deltaRot;

            Vector3 tmpAxis; float twist;
            twistRot.ToAngleAxis(out twist, out tmpAxis);
            if (Misc.IsObtuseAngle(tmpAxis, m_twistAxis))
                twist = -twist;

            twist = Misc.NormalizeAnglePI(twist);

            return twist;
        }

        public void _AutoSetParameters()
        {
            ConeConstraintMB mb = this;
            Transform j = mb.transform;
            Transform jparent = j.parent;
            Transform jchild = mb.nextJoint;

            // decide the refAxis
            {
                Vector3 axis = Vector3.zero;
                if (jparent != null)
                {
                    axis = (j.position - jparent.position).normalized;
                }
                else
                {
                    axis = (jchild.position - j.position).normalized;
                }

                mb.refAxis = Misc.InverseTransformDirection(jparent, axis); //convert to parent space
            }

            //decide the angle limit
            mb.angleLimit = DEF_ANGLELIMIT;

            // decide the min/max limit
            mb.minTwistLimit = DEF_MinTwist;
            mb.maxTwistLimit = DEF_MaxTwist;

            mb.CalcInitData(); //calc startlocalRot
        }

        private void _OnNextJointChanged()
        {
            ConeConstraintMB mb = (ConeConstraintMB)this;
            var nextJoint = mb.m_nextJoint;
            if (nextJoint != null)
            {
                if (nextJoint.parent == mb.transform)
                    _AutoSetParameters();
                else
                {
                    Dbg.CLogWarn(mb, nextJoint.name + " is not children of " + mb.name);
                    mb.m_nextJoint = null;
                }
            }
        }

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        //private static readonly Quaternion INVALID_Q = new Quaternion(0, 0, 0, 0);

        private const float DEF_ANGLELIMIT = 179f;
        private const float DEF_MinTwist = -90f;
        private const float DEF_MaxTwist = 90f;



        #endregion "constant data"
    }
}
