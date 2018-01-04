using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    using DespawnCB = System.Action<GameObject>;

    /// <summary>
    /// containing the info of the spawning PrefabPool of this object,
    /// </summary>
    public class PoolTicket : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        [SerializeField]
        private PrefabPool m_Pool;
        [SerializeField]
        private bool m_IsAvail = false; //if true, means it's in the freelist of PrefabPool
        [SerializeField][Tooltip("if don't need refCnt, don't modify it, it's required when despawn this refcnt == 0")]
        private int m_RefCnt = 0;

        #endregion "configurable data"

        #region "data"
        // "data" 

        private List<DespawnCB> m_DespawnCallbacks = new List<DespawnCB>();
    
        #endregion "data"

        #region "public method"
        // public method

        public PrefabPool Pool
        {
            get { return m_Pool; }
            set { m_Pool = value; }
        }

        public bool IsAvail
        {
            get { return m_IsAvail; }
            set { m_IsAvail = value; }
        }

        public int RefCnt
        {
            get { return m_RefCnt; }
            set { m_RefCnt = value; }
        }

        public void Despawn()
        {

            for (int i = 0; i < m_DespawnCallbacks.Count; ++i )
            {
                m_DespawnCallbacks[i](gameObject);
            }
            m_DespawnCallbacks.Clear();
                        
            if( m_Pool != null )
            {// no pool specified, find pool as last resort
                m_Pool.Despawn(gameObject);
            }
            else
            {
                Dbg.CLogErr(this, "PoolTicket.Despawn: no pool specified");
            }
            
        }

        public void SubscribeOnDespawn(DespawnCB cb)
        {
            m_DespawnCallbacks.Add(cb);
        }

        #endregion "public method"

    }
}
