using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH.Constraints
{
    public class CopyPosition : BaseConstraint
    {
		#region "configurable data"

        [SerializeField][Tooltip("the target transform")]
        private Transform m_target;

        [SerializeField][Tooltip("how should this constraint affects owner")]
        private EAxisD m_eAffect = EAxisD.XYZ;
        [SerializeField][Tooltip("use offset?")]
        private bool m_useOffset = true;
        [SerializeField][Tooltip("offset value, only effect when m_useOffset is true")]
        private Vector3 m_offset = new Vector3(0, 0, 0);
        [SerializeField][Tooltip("the space target is evaluated in")]
        private ESpace m_targetSpace = ESpace.World;
        [SerializeField][Tooltip("the space owner is evaluated in")]
        private ESpace m_ownerSpace = ESpace.World;
        [SerializeField][Tooltip("the weight of constraints")]
        private float m_influence = 1f;

	    #endregion "configurable data"
	
		#region "data"
	    // data
	
	    #endregion "data"
	
		#region "unity event handlers"

	    #endregion "unity event handlers"

		#region "props"
        public UnityEngine.Transform Target
        {
            get { return m_target; }
            set {
                if (m_target != value)
                {
                    m_target = value;
                    if (m_target != null)
                    {
                        AutoCalcOffset();
                    }
                }
            }
        }

		
        public EAxisD Affect
        {
            get { return m_eAffect; }
            set { m_eAffect = value; }
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
        public MH.ESpace TargetSpace
        {
            get { return m_targetSpace; }
            set { m_targetSpace = value; }
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

            if( !m_target ) 
                return; //do nothing if no target is specified

            Vector3 initPos = m_tr.GetPosition(m_ownerSpace);
            //Vector3 initPos = m_cstack.GetInitLocPos();
            Vector3 endPos = initPos;
            Vector3 targetPos = m_target.GetPosition(m_targetSpace);

            // apply effect
            if ((m_eAffect & EAxisD.X) != 0)
            {
                endPos.x = targetPos.x;
            }
            if ((m_eAffect & EAxisD.Y) != 0)
            {
                endPos.y = targetPos.y;
            }
            if ((m_eAffect & EAxisD.Z) != 0)
            {
                endPos.z = targetPos.z;
            }
            if ((m_eAffect & EAxisD.InvX) != 0)
            {
                endPos.x = -targetPos.x;
            }
            if ((m_eAffect & EAxisD.InvY) != 0)
            {
                endPos.y = -targetPos.y;
            }
            if ((m_eAffect & EAxisD.InvZ) != 0)
            {
                endPos.z = -targetPos.z;
            }

            // apply offset
            if (m_useOffset)
            {
                endPos += m_offset;
            }

            if (!Mathf.Approximately(m_influence, 1f))
            {
                endPos = Misc.Lerp(initPos, endPos, m_influence);
            }

            m_tr.SetPosition(endPos, m_ownerSpace);
        }

        public override void DoDrawGizmos()
        {
            base.DoDrawGizmos();

            if (m_target)
            {
                _DrawLine(m_tr, m_target);
            }            
        }


        public void AutoCalcOffset()
        {
            Vector3 selfPos = m_tr.GetPosition(m_ownerSpace);
            Vector3 targetPos = m_target.GetPosition(m_targetSpace);
            m_offset = selfPos - targetPos;
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
