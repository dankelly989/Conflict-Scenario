using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace MH.Constraints
{
    using CONT = SortedDictionary<ConstraintStack, bool>; //value is omitted

    /// <summary>
    /// used to control constraintStack update order
    /// </summary>
    [ExecuteInEditMode][AutoCreateInstance][ScriptOrder(100)]
    public class ConstraintManager : Singleton<ConstraintManager>, ISerializationCallbackReceiver
    {
		#region "configurable data"
	
	    #endregion "configurable data"
	
		#region "data"

        [SerializeField][Tooltip("used for serialization")]
        private List<ConstraintStack> m_ser_cstacks = new List<ConstraintStack>();

        private bool m_needRebuild = false;
        private CONT m_cstackCont = new CONT(new CSComp()); //runtime data

        private List<ConstraintStack> m_toDelCon = new List<ConstraintStack>();
	    	
	    #endregion "data"
	
		#region "unity event handlers"

        public override void Init()
        {
            base.Init();
            m_ser_cstacks.Clear(); //this is important, when switch scene, without clear, could cause repetitve Add()
            m_cstackCont.Clear(); 
        }

        public override void Fini()
        {
            base.Fini();
        }

        public override HideFlags AutoHideFlags { get { return HideFlags.HideInHierarchy; } }

        void LateUpdate()
        {
            for (var ie = cstackCont.GetEnumerator(); ie.MoveNext(); )
            {
                var pr = ie.Current;
                var cstack = pr.Key;
                if (!cstack)
                {
                    m_toDelCon.Add(cstack);
                }
                else if (cstack.isActiveAndEnabled)
                {
                    cstack.DoUpdate();
                }
            }

            if (m_toDelCon.Count > 0) //As ConstraintStack.OnDestroy is not guaranteed to be called in all situations, this null-processing is needed
            {
                //Dbg.Log("toDelCon.Count = {0}", m_toDelCon.Count);
                for (var ie = m_toDelCon.GetEnumerator(); ie.MoveNext(); )
                {
                    var cstack = ie.Current;
                    cstackCont.Remove(cstack);
                }
                m_toDelCon.Clear();
            }
            
        }
	    
	    #endregion "unity event handlers"

        #region "ISerialization callback"

        // NOTE!!! this will be called every frame if the editor is open in Inspector
        public void OnBeforeSerialize()
        {
            //Debug.Log("OBS: count: " + m_cstackCont.Count);
            //StringBuilder bld = new StringBuilder();

            var ie = cstackCont.GetEnumerator(); //need to call cstackCont before clearing m_ser_cstack, so we can finish deserialize before m_ser_cstack is cleared()
            m_ser_cstacks.Clear();
            for (; ie.MoveNext(); )
            {
                var cstack = ie.Current.Key;
                m_ser_cstacks.Add(cstack);
                //bld.AppendFormat("{0}: {1}\n", cstack.GetHashCode(), cstack.ExecOrder);
            }

            //Debug.Log(bld.ToString());
        }

        public void OnAfterDeserialize()
        {
            //Debug.Log("OAD: count: " + m_ser_cstacks.Count);
            //for (int i = 0; i < m_ser_cstacks.Count; ++i)
            //{
            //    m_cstackNodes.Add(m_ser_cstacks[i], false);
            //}
            m_needRebuild = true;
        }

        private CONT cstackCont
        {
            get {
                if (m_needRebuild)
                {
                    for (int i = 0; i < m_ser_cstacks.Count; ++i)
                    {
                        m_cstackCont.Add(m_ser_cstacks[i], false);
                    }
                    m_needRebuild = false;
                }
                return m_cstackCont;
            }
        }
        		
        #endregion "ISerialization callback"

        #region "public method"

        public void Add(ConstraintStack cstack)
        {
            if (!cstackCont.ContainsKey(cstack))
            {
                cstackCont.Add(cstack, false); //value is omitted
                //Dbg.Log("ConstraintManager.Add: {0}", cstack.name);
            }
        }

        public void Remove(ConstraintStack cstack)
        {
            m_cstackCont.Remove(cstack);
            //Dbg.Log("ConstraintManager.Remove: {0}", cstack.name);
        }

        public CONT.Enumerator GetContEnumerator()
        {
            return m_cstackCont.GetEnumerator();
        }

        public int ContCount
        {
            get { return m_cstackCont.Count; }
        }

	    #endregion "public method"
	
		#region "private method"
	    
	
	    #endregion "private method"
	
		#region "constant data"
	    
	
	    #endregion "constant data"

        public class CSComp : IComparer<ConstraintStack>
        {
            public int Compare(ConstraintStack s, ConstraintStack os)
            {
                int thisOrder = s.ExecOrder;
                int otherOrder = os.ExecOrder;
                if (thisOrder != otherOrder)
                {
                    return thisOrder - otherOrder;
                }
                else
                {
                    int hc = s.GetHashCode();
                    int ohc = os.GetHashCode();
                    return hc - ohc;
                }
            }
        }
    }
}
