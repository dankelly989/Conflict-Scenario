using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH.Constraints
{
    /// <summary>
    /// the base class of constraints
    /// </summary>
    [RequireComponent(typeof(ConstraintStack))]
    [ExecuteInEditMode]
    public class BaseConstraint : MonoBehaviour
    {
		#region "configurable data"
	    // configurable data

	    #endregion "configurable data"
	
		#region "data"
        
        [SerializeField][Tooltip("Only active constraint will modify the GO")]
        protected bool m_active = true; //whether this constraint is active
        [SerializeField][Tooltip("whether show gizmos for this constraint")]
        protected bool m_showGizmos = true;

        protected Transform m_tr;
        protected ConstraintStack m_cstack;

	    #endregion "data"
	
		#region "unity event handlers"
	
        void Awake()
        {//to handle auto added ConstraintStack
            m_tr = transform;
            m_cstack = m_tr.ForceGetComponent<ConstraintStack>();
            m_cstack.Add(this); //will ignore repetitive object
        }

	    #endregion "unity event handlers"

		#region "prop"

        public bool IsActiveConstraint 
        {
            get { return m_active; }
            set {
                if (m_active != value)
                {
                    if (m_cstack)
                    {
                        m_active = value;
                        m_cstack.OnConstraintActiveChanged(this, m_active);
                        OnConstraintActiveChanged();
                    }
                }
            }
        }

        public virtual float Influence
        {
            get { return 1.0f; }
            set { }
        }

        public bool ShowGizmos
        {
            get { return m_showGizmos; }
            set { m_showGizmos = value; }
        }

        public virtual bool HasGizmos
        {
            get { return true; }
        }
		
		#endregion "prop"
	
		#region "public method"

        public virtual void DoAwake()
        {
            if( m_tr == null )
                m_tr = transform;
            if (m_cstack == null)
            {
                m_cstack = m_tr.ForceGetComponent<ConstraintStack>();
                m_cstack.Add(this); //will ignore repetitive object
            }
            
        }

        public virtual void DoRemove()
        {
        }

        public virtual void DoStart()
        {
        }

        public virtual void DoUpdate()
        {
        }

        public virtual void DoLateUpdate()
        {
        }

        public virtual void DoDrawGizmos()
        {
        }

        public virtual void OnConstraintActiveChanged()
        {
        }

	    #endregion "public method"
	
		#region "private method"
	    // private method

        protected void _DrawLine(Transform fromTr, Transform toTr)
        {
            DrawUtil.DrawLine(fromTr.position, toTr.position, ConUtil.GizmosColor, 0.02f);
        }
	
	    #endregion "private method"
	
		#region "constant data"
	    // constant data


	
	    #endregion "constant data"
        
    }

}

