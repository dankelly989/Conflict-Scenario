using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.IKConstraint
{
    [ExecuteInEditMode]
    public class AngleConstraintMB : IKConstraintMB
    {
		#region "configurable data"
	    // configurable data

        [SerializeField][Tooltip("next joint in the IK chain")]
        private Transform m_nextJoint;
        [SerializeField]
        [Tooltip("the axis that the joint rotates around, in parent-space")]
        private Vector3 m_rotAxis = Vector3.up;
        [SerializeField]
        [Tooltip("the parent-this bone primary axis direction vector, in parent-space")]
        private Vector3 m_primAxis = Vector3.right;
        [SerializeField]
        [Tooltip("min limit")]
        private float m_minLimit = -180f;
        [SerializeField]
        [Tooltip("max limit")]
        private float m_maxLimit = 180f;
        [SerializeField]
        [Tooltip("the joint's localRotation when parent-this-child form a straight line")]
        private Quaternion m_startLocalRot = Quaternion.identity; //the localRotation of this joint at Init
	
	    #endregion "configurable data"
	
		#region "data"
	    // data
	
	    #endregion "data"
	
		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            if( m_nextJoint == null )
            {
                TryAutoSelectNextJoint();
            }
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        public Vector3 rotAxis
        {
            get { return m_rotAxis; }
            set { m_rotAxis = value; }
        }

        public Vector3 primAxis
        {
            get { return m_primAxis; }
            set { m_primAxis = value; }
        }

        public float minLimit
        {
            get { return m_minLimit; }
            set { m_minLimit = value; }
        }

        public float maxLimit
        {
            get { return m_maxLimit; }
            set { m_maxLimit = value; }
        }

        public Quaternion startLocalRot
        {
            get { return m_startLocalRot; }
            set { m_startLocalRot = value; }
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


        public override void Apply(ISolver solver, int jointIdx)
        {
            if (m_nextJoint == null)
            {
                Dbg.CLogWarn(this, "AngleConstraintMB.Apply: nextJoint not set");
                return;
            }

            if (jointIdx == solver.Count)
                return; //if no child joint, cannot apply angle constraint
            var joints = solver.GetJoints();
            Transform j = joints[jointIdx];

            if (m_rotAxis == Vector3.zero)
            {
                Dbg.LogErr("AngleConstraintMB.Apply: the axis is zero vector");
                return;
            }
            if (m_minLimit > m_maxLimit)
            {
                Misc.Swap(ref m_minLimit, ref m_maxLimit);
            }

            Transform nextJ = joints[jointIdx + 1];
            Transform parentJ = j.parent; //THIS could be NULL, use Misc.TransformDirectoin/InverseTransformDirection
            Vector3 jpos = j.position;
            Vector3 nextJpos = nextJ.position;
            Vector3 rotAxisWorld = Misc.TransformDirection(parentJ, m_rotAxis); //axis convert from parentSpace to worldSpace

            // project to the rotation plane
            Vector3 diff0 = nextJpos - jpos; //world space
            Vector3 projDiff = Vector3.ProjectOnPlane(diff0, rotAxisWorld); //world space
            Vector3 worldPrimAxis = Misc.TransformDirection(parentJ, m_primAxis);

            float angle = Misc.ToAngleAxis(worldPrimAxis, projDiff, rotAxisWorld);
            if (angle < m_minLimit || m_maxLimit < angle)
            { //need clamp
                angle = Mathf.Clamp(angle, m_minLimit, m_maxLimit);
            }

            j.localRotation = Quaternion.AngleAxis(angle, m_rotAxis) * m_startLocalRot; //local
        }

        public void CalcInitData()
        {
            m_startLocalRot = Quaternion.identity;
            Dbg.Assert(m_nextJoint != null, "AngleConstraintMB.CalcInitData: nextJoint not set");

            Transform j = transform;
            Transform cj = m_nextJoint;
            Transform parentJ = j.parent; //THIS could be NULL, use Misc.TransformDirectoin/InverseTransformDirection
            Vector3 jpos = j.position;
            Vector3 cjPos = cj.position;
            Vector3 rotAxisWorld = Misc.TransformDirection(parentJ, m_rotAxis); //axis convert from parentSpace to worldSpace
            Vector3 primAxisWorld = Misc.TransformDirection(parentJ, m_primAxis);

            float angle = Misc.ToAngleAxis(cjPos - jpos, primAxisWorld, rotAxisWorld);
            m_startLocalRot = Quaternion.AngleAxis(angle, m_rotAxis) * j.localRotation;
        }

        public bool TryAutoSelectNextJoint()
        {
            var tr = transform;
            if( tr.childCount > 0 && nextJoint == null)
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

        #endregion "public method"

        #region "private method"
        // private method

        private void _OnNextJointChanged()
        {
            AngleConstraintMB mb = this;
            var nextJoint = mb.m_nextJoint;
            if (nextJoint != null)
            {
                if (nextJoint.parent == mb.transform)
                    AutoSetParameters();
                else
                {
                    Dbg.CLogWarn(mb, nextJoint.name + " is not children of " + mb.name);
                    mb.m_nextJoint = null;
                }
            }
        }

        public void AutoSetParameters()
        {
            AngleConstraintMB mb = this;
            Transform j = mb.transform;
            Transform jparent = j.parent;
            Transform jchild = mb.nextJoint;

            // decide the axis
            {
                if (jparent == null)
                {
                    mb.rotAxis = j.up;
                }
                else if (jchild == null)
                {
                    mb.rotAxis = j.up;
                }
                else
                {
                    var ppos = jparent.position;
                    var spos = j.position;
                    var cpos = jchild.position;

                    Vector3 cross = Misc.VecCross(spos, cpos, ppos, spos);
                    if (cross == Vector3.zero)
                    {
                        mb.rotAxis = j.up;
                    }
                    else
                    {
                        mb.rotAxis = cross.normalized;
                    }

                    mb.rotAxis = Misc.InverseTransformDirection(jparent, mb.rotAxis); //convert to parent space
                }
            }

            // decide the primAxis
            {
                Vector3 fwd = j.right;
                if (jparent != null)
                {
                    fwd = (j.position - jparent.position).normalized;
                }
                else if (jchild != null)
                {
                    fwd = (jchild.position - j.position).normalized;
                }
                mb.primAxis = Misc.InverseTransformDirection(jparent, fwd);
            }

            // decide the min/max limit
            {
                mb.minLimit = DEF_MINLIM;
                mb.maxLimit = DEF_MAXLIM;
            }

            mb.CalcInitData(); //calc startlocalRot
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        private const float DEF_MINLIM = -135f;
        private const float DEF_MAXLIM = 0f;

        #endregion "constant data"





    }
}
