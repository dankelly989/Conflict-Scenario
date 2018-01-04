using System;
using UnityEngine;
using System.Collections.Generic;

namespace MH
{

    using StandardPoolCont = System.Collections.Generic.Dictionary<string, IPool>;
    using TypePoolCont = System.Collections.Generic.Dictionary<System.RuntimeTypeHandle, IPool>;

    public interface IPool
    {
        string Name { get; }
        object Spawn();
        void Despawn(object obj);
        void OnDestroy(); //notify the Pool it's being destroyed

        int SpawnCnt { get; } //total spawned objs since init-ed
        int DespawnCnt { get; } //total despawned objs since init-ed
        int ObjOut { get; } // current spawned living objects

    }

    /// <summary>
    /// used by Pool,
    /// </summary>
    public interface IRecyclable
    {
        void OnSpawn();
        void OnDespawn();
    }
    public class BRecyclable : IRecyclable
    {
        public virtual void OnSpawn() { }
        public virtual void OnDespawn() { }
    }


    /// <summary>
    /// manage pools, provide some utility functions
    /// </summary>
    [AutoCreateInstance]
    public class PoolMgr : NonBehaviourSingleton<PoolMgr>
    {

        private StandardPoolCont m_poolCont = new StandardPoolCont(); //the pool container
        private TypePoolCont m_TypePoolCont; //the type pool container

        #region "unity msg handler"
        //////////////////////////////////////////////////////////////////////////
        // Mono functions 
        //////////////////////////////////////////////////////////////////////////
        public override void Init()
        {
            m_TypePoolCont = new TypePoolCont(RuntimeTypeHandleComp.Instance);
        }

        #endregion

        #region "public methods"
        //////////////////////////////////////////////////////////////////////////
        // "public methods" 
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// added a pool to the management of PoolMgr
        /// check if already a pool with same name exists
        /// </summary>
        public void Add(IPool pool)
        {
            string poolName = pool.Name;
            Dbg.Assert(!m_poolCont.ContainsKey(poolName), "PoolMgr.Add: poolName {0} already registered", poolName);

            m_poolCont.Add(poolName, pool);
        }

        /// <summary>
        /// remove a pool, all inactive objects are destroyed, 
        /// </summary>
        public void Remove(string name)
        {
            Dbg.Assert(m_poolCont.ContainsKey(name), "PoolMgr.Remove: poolName {0} not regiestered", name);

            IPool pool = m_poolCont[name];
            m_poolCont.Remove(name);
            pool.OnDestroy(); //destroy not-in-use object
        }

        /// <summary>
        /// get the pool with name, if not found, return null;
        /// </summary>
        public IPool Get(string name)
        {
            IPool ret = null;
            bool bFound = m_poolCont.TryGetValue(name, out ret);
            if (!bFound)
                return null;

            return ret;
        }

        /// <summary>
        /// short hand func for Get()
        /// </summary>
        public IPool this[string name]
        {
            get
            {
                return Get(name);
            }
        }

        /// <summary>
        /// find the specified pool, and spawn a object;
        /// assert when not found the pool
        /// </summary>
        public object Spawn(string poolname)
        {
            IPool pool = null;
            bool bFound = m_poolCont.TryGetValue(poolname, out pool);
            Dbg.Assert(bFound, "PoolMgr.Spawn: failed to find pool: {0}", poolname);

            return pool.Spawn();
        }

        public T Spawn<T>(string poolname) where T : class
        {
            IPool pool = null;
            bool bFound = m_poolCont.TryGetValue(poolname, out pool);
            Dbg.Assert(bFound, "PoolMgr.Spawn: failed to find pool: {0}", poolname);

            return pool.Spawn() as T;
        }

        /// <summary>
        /// --if the poolname is never used, then assert;
        /// --if the poolname is already removed, then directly destroy object
        /// if cannot find the pool, then directly destroy object
        /// if the pool is in use, the action == PoolMgr["somepool"].Despawn(obj)
        /// </summary>
        public void Despawn(string poolname, object obj)
        {
            IPool pool = null;
            bool bFound = m_poolCont.TryGetValue(poolname, out pool);

            if (!bFound)
            {
                if (obj is UnityEngine.Object)
                {
                    UnityEngine.Object.Destroy((UnityEngine.Object)obj);
                }
            }
            else
            {
                pool.Despawn(obj);
            }
        }

        /// <summary>
        /// enumerate standard pools
        /// </summary>
        public StandardPoolCont.Enumerator EnumStandardPools()
        {
            return m_poolCont.GetEnumerator();
        }

        #endregion

        #region "Typed pool"
        // "Typed pool" 

        /// <summary>
        /// add a type pool
        /// </summary>
        public void AddTypePool(RuntimeTypeHandle tp, IPool pool)
        {
            Dbg.Assert(!m_TypePoolCont.ContainsKey(tp), "PoolMgr.AddTypePool: type {0} already registered", tp);
            m_TypePoolCont[tp] = pool;
        }

        /// <summary>
        /// remove a pool, all inactive objects are destroyed, 
        /// </summary>
        public void RemoveTypePool(RuntimeTypeHandle tp)
        {
            Dbg.Assert(m_TypePoolCont.ContainsKey(tp), "PoolMgr.RemoveTypePool: type {0} not regiestered", tp);

            IPool pool = m_TypePoolCont[tp];
            m_TypePoolCont.Remove(tp);
            pool.OnDestroy(); //destroy not-in-use object
        }

        /// <summary>
        /// get the pool with name, if not found, return null;
        /// </summary>
        public IPool GetTypePool(RuntimeTypeHandle tp)
        {
            IPool ret = null;
            bool bFound = m_TypePoolCont.TryGetValue(tp, out ret);
            if (!bFound)
                return null;

            return ret;
        }

        /// <summary>
        /// enumerate type pools
        /// </summary>
        public TypePoolCont.Enumerator EnumTypePools()
        {
            return m_TypePoolCont.GetEnumerator();
        }

        #endregion "Typed pool"

    }

    /// <summary>
    /// the standard RuntimeTypeHandle comparer
    /// </summary>
    public class RuntimeTypeHandleComp : EqualityComparer<RuntimeTypeHandle>
    {
        private static RuntimeTypeHandleComp sm_Instance;

        public static RuntimeTypeHandleComp Instance
        {
            get
            {
                if (sm_Instance == null)
                {
                    sm_Instance = new RuntimeTypeHandleComp();
                }
                return sm_Instance;
            }
        }

        public override bool Equals(RuntimeTypeHandle x, RuntimeTypeHandle y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public override int GetHashCode(RuntimeTypeHandle obj)
        {
            return obj.GetHashCode();
        }
    }

}