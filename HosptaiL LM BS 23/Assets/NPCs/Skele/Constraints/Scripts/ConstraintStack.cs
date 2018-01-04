using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH.Constraints
{
    using CONT = List<BaseConstraint>;

    /// <summary>
    /// this class is used to manage and drive all substantial constraints attached on this GameObject
    /// </summary>
    [ExecuteInEditMode]
    public class ConstraintStack : MonoBehaviour
    {
		#region "configurable data"
	    // configurable data

        [SerializeField][Tooltip("this is used to help decide evaluation order among multiple stacks, the smaller the earlier")]
        private int m_execOrder = 0;
        [SerializeField][Tooltip("the container of constraints")]
        private CONT m_constraints = new CONT();
        [SerializeField][Tooltip("the init info")]
        private TrInitInfo m_initInfo;

	    #endregion "configurable data"
	
		#region "data"

        private Transform m_tr;
	
	    #endregion "data"

		#region "prop"

        public Transform tr
        {
            get { return m_tr; }
        }

        public int constraintCount
        {
            get { return m_constraints.Count; }
        }

        public int ExecOrder
        {
            get { return m_execOrder; }
            set {
                if (m_execOrder != value)
                {
                    ConstraintManager.Instance.Remove(this);
                    m_execOrder = value;
                    ConstraintManager.Instance.Add(this); //so it's sorted again
                }
            }
        }

		#endregion "prop"
	
		#region "unity event handlers"
	    // unity event handlers

        void Awake()
        {
            m_tr = transform;
            if (m_initInfo == null)
                m_initInfo = new TrInitInfo(m_tr);
            m_initInfo.TentativeResetInitInfo();

            for (int i = 0; i < m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                if( c )
                    c.DoAwake();
            }
        }

        void OnDestroy()
        { //NOTE: OnDestroy will not be called if the GO is never activated
            if( ConstraintManager.HasInst)
                ConstraintManager.Instance.Remove(this);
        }
	
		void Start()
        {
            ConstraintManager.Instance.Add(this); //cannot put in Awake, it might execute before ConstraintManager.Awake()

            for (int i = 0; i < m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                if( c )
                    c.DoStart();
            }
        }
	
        //void Update()
        //{
        //    m_initInfo.UpdateInitInfo();

        //    DoEvaluate();

        //    m_initInfo.RecordLastLocInfo();
        //}

        public void DoUpdate() //called by ConstraintManager
        {
            m_initInfo.UpdateInitInfo();

            DoEvaluate();

            m_initInfo.RecordLastLocInfo();
        }

        void OnDrawGizmos()
        {
            for (int i = 0; i < m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                if (c && c.ShowGizmos)
                    c.DoDrawGizmos();
            }
        }

        void OnTransformParentChanged()
        {
            m_initInfo.ResetInitInfo(); //but, if in a frame after Update, we change the parent AND update the loc transform info... is this still alright?
        }
	
	    #endregion "unity event handlers"
	
		#region "public method"
	    // public method

        public static int RemoveByType(GameObject go, Type tp)
        {
            ConstraintStack cs = go.GetComponent<ConstraintStack>();
            if( cs )
            {
                return cs.RemoveByType(tp);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 1. revert to init info;
        /// 2. evaluate constraints
        /// </summary>
        public void DoEvaluate()
        {
            m_initInfo.RevertToInitInfo();

            for (int i = 0; i < m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                if (c && c.IsActiveConstraint)
                    c.DoUpdate();
            }
        }

        /// <summary>
        /// NOTE: if the newly add constraint is the only active one, need reset initinfo
        /// </summary>
        public void Add(BaseConstraint c)
        {
            if (m_constraints.Contains(c))
                return;

            bool hasActive = HasActiveConstraint();

            m_constraints.Add(c);

            c.DoAwake();

            if (!hasActive && c.IsActiveConstraint)
                m_initInfo.ResetInitInfo();
        }

        public int RemoveByType(Type tp)
        {
            int found = 0;
            for(int i=0; i<m_constraints.Count; ++i)
            {
                BaseConstraint c = m_constraints[i];
                if( c.GetType() == tp )
                {
                    RemoveAt(i);
                    ++found;
                    --i;
                }
            }
            return found;
        }

        public bool Remove(BaseConstraint c)
        {
            int idx = m_constraints.IndexOf(c);
            if (idx >= 0)
            {
                RemoveAt(idx);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveAt(int idx)
        {
            Dbg.Assert(idx < m_constraints.Count, "ConstraintController.RemoveAt: idx beyond range: {0}, Count : {1}");
            var c = m_constraints[idx];
            c.DoRemove();
            m_constraints.RemoveAt(idx);

            MUndo.DestroyObj(c); //destroy constraint
        }

        public void RemoveAll()
        {
            for(int i=0; i<m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                c.DoRemove();
                MUndo.DestroyObj(c);
            }
            m_constraints.Clear();
        }

        public BaseConstraint Get(int idx)
        {
            Dbg.Assert(idx < m_constraints.Count, "ConstraintController.Get: idx beyond range: {0}, Count : {1}");
            return m_constraints[idx];
        }

        public void Swap(int idx0, int idx1)
        {
            var c = m_constraints[idx0];
            m_constraints[idx0] = m_constraints[idx1];
            m_constraints[idx1] = c;
        }

        public int IndexOf(BaseConstraint c)
        {
            return m_constraints.IndexOf(c);
        }

        public Vector3 GetInitLocPos()
        {
            return m_initInfo.locPos;
        }

        public void SetInitLocPos(Vector3 v)
        {
            m_initInfo.locPos = v;
        }

        public Quaternion GetInitLocRot()
        {
            //return m_initInfo.locRot;
            return Quaternion.Euler(m_initInfo.locRot);
        }

        public void SetInitLocRot(Vector3 euler)
        {
            //m_initInfo.locRot = Quaternion.Euler(euler);
            m_initInfo.locRot = euler;
        }

        //public void SetInitLocRot(Quaternion q)
        //{
        //    m_initInfo.locRot = q;
        //}

        public Vector3 GetInitLocScale()
        {
            return m_initInfo.locScale;
        }

        public void SetInitLocScale(Vector3 v)
        {
            m_initInfo.locScale = v;
        }

        public void OverwriteInitLocPosRotSca()
        {
            m_initInfo.locPos = m_tr.localPosition;
            //m_initInfo.locRot = m_tr.localRotation;
            m_initInfo.locRot = m_tr.localEulerAngles;
            m_initInfo.locScale = m_tr.localScale;
        }

        public bool HasActiveConstraint()
        {
            for (int i = 0; i < m_constraints.Count; ++i)
            {
                var c = m_constraints[i];
                if (c && c.IsActiveConstraint)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// </summary>
        public void OnConstraintActiveChanged(BaseConstraint baseConstraint, bool active)
        {
            //if (active && _HasOnlyOneActiveConstraint())
            //    _ResetInitInfo();
        }

	    #endregion "public method"
	
		#region "private method"
	    // private method

        

        private bool _HasOnlyOneActiveConstraint()
        {
            int activeCnt = 0;
            for (int i = 0; i < m_constraints.Count; ++i)
            {
                if (m_constraints[i].IsActiveConstraint)
                {
                    ++activeCnt;
                    if (activeCnt > 1)
                        return false;
                }
            }

            return activeCnt == 1;
        }

	    #endregion "private method"
	
		#region "constant data"
	    // constant data
	
	    #endregion "constant data"



    }


}
