using System;
using System.Collections.Generic;
using System.Reflection;


namespace MH
{

    /// <summary>
    /// generic pool impl.
    /// we didn't use a stack but a List to track all objects, so it's not very fit to a great total number of objects
    /// if that turns out to be performance problem, we will revert to stack impl.
    /// </summary>
    /// <typeparam name="T"> must be reference type</typeparam>
    public class ObjectPool<T> : IPool where T : class, new()
    {
        #region "data"
        // data

        //private List<T> m_ObjList;
        //private int m_availIdx = 0; //the idx pointing to an available object, if >= count means no available

        private Stack<T> m_FreeList; //store available elements

        private string m_Name;
        private Action<T> m_ResetAction;
        private Action<T> m_FirstInitAction;
        private Action<T> m_TakeOutAction; //called in Spawn(), not firstInit

        private int m_SpawnedCnt = 0;
        private int m_DespawnedCnt = 0;

        private int m_WarningCnt = 100; //when ObjOut beyonds this threshold, will issue a warning and raise the warning threshold
        private int m_WarningCntIncre = 50; //how much to be added to m_WarningCnt each time

        #endregion

        #region "public method"
        // public method

        public ObjectPool(string name) : this(name, null, null, null)
        { }
        public ObjectPool(string name, Action<T> resetAction) : this(name, resetAction, null, null)
        { }
        public ObjectPool(string name, Action<T> resetAction, Action<T> firstInitAction) : this(name, resetAction, firstInitAction, null)
        { }
        public ObjectPool(string name, Action<T> resetAction, Action<T> firstInitAction, Action<T> takeOutAction)
        {
            m_Name = name;
            m_ResetAction = resetAction;
            m_FirstInitAction = firstInitAction;
            m_TakeOutAction = takeOutAction;
            //m_ObjList = new List<T>();

            m_FreeList = new Stack<T>();

            Type t = typeof(T);
            if (t.GetInterface("IRecyclable") != null)
            {
                if (m_ResetAction == null)
                {
                    m_ResetAction = o => ((IRecyclable)o).OnDespawn();
                }
                if (m_TakeOutAction == null)
                {
                    m_TakeOutAction = o => ((IRecyclable)o).OnSpawn();
                }
            }
        }


        public string Name { get { return m_Name; } }

        public Action<T> ResetAction
        {
            get { return m_ResetAction; }
            set { m_ResetAction = value; }
        }

        public Action<T> InitAction
        {
            get { return m_FirstInitAction; }
            set { m_FirstInitAction = value; }
        }

        public Action<T> TakeOutAction
        {
            get { return m_TakeOutAction; }
            set { m_TakeOutAction = value; }
        }

        /// <summary>
        /// get a object
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            T obj = null;

            #region "old version, with m_availIdx"
            /*// "old version, with m_availIdx" 

            if (m_availIdx >= m_ObjList.Count)
            { // no available
                obj = new T();
                if (m_FirstInitAction != null)
                    m_FirstInitAction(obj);
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);

                m_ObjList.Add(obj);
            }
            else
            { // get from pool
                obj = m_ObjList[m_availIdx];
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);
            }

            ++m_availIdx;
            */
            #endregion "old version, with m_availIdx"

            if (m_FreeList.Count == 0)
            { //no available
                obj = new T();

                if (m_FirstInitAction != null)
                    m_FirstInitAction(obj);
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);

                if( ObjOut+1 >= m_WarningCnt )
                {
                    Dbg.LogWarn("/// WARNING: ObjectPool<{0}> has living objects amount to {1}", typeof(T).Name, ObjOut+1);
                    m_WarningCnt += m_WarningCntIncre;
                }
            }
            else
            {
                obj = m_FreeList.Pop();
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);
            }

            ++m_SpawnedCnt;
            return obj;
        }
        object IPool.Spawn()
        {
            return Spawn();
        }


        /// <summary>
        /// return object to pool
        /// </summary>
        public void Despawn(T obj)
        {
            if (m_ResetAction != null)
                m_ResetAction(obj);

            #region "old version, with m_availIdx"
            // "old version, with m_availIdx" 
            //--m_availIdx;

            //int thisIdx = m_ObjList.LastIndexOf(obj);
            //Dbg.Assert(thisIdx != -1, "ObjectPool.Despawn: failed to find object in pool: {0}", obj);

            //Misc.Swap(m_ObjList, thisIdx, m_availIdx); //swap the despawned to the tail
            #endregion "old version, with m_availIdx"

            m_FreeList.Push(obj);

            ++m_DespawnedCnt;
        }
        void IPool.Despawn(object o)
        {
            Despawn((T)o);
        }

        /// <summary>
        /// do nothing
        /// notify the Pool it's being destroyed
        /// </summary>
        public void OnDestroy()
        {

        }

        public int SpawnCnt { get { return m_SpawnedCnt; } } //total spawned objs since init-ed
        public int DespawnCnt { get { return m_DespawnedCnt; } } //total despawned objs since init-ed
        public int ObjOut { get { return m_SpawnedCnt - m_DespawnedCnt; } } // current spawned living objects
        public int WarningCnt { get { return m_WarningCnt; } set { m_WarningCnt = value; } }
        public int WarningCntIncr { get { return m_WarningCntIncre; } set { m_WarningCntIncre = value; } }

        /// <summary>
        /// acquire a pool from PoolMgr, if not found, create one
        /// </summary>
        public static ObjectPool<T> ForceAcquire()
        {
            var tp = typeof(T).TypeHandle;
            //string poolName = tp.ToString();
            ObjectPool<T> pool = PoolMgr.Instance.GetTypePool(tp) as ObjectPool<T>;
            if (null == pool)
            {
                pool = new ObjectPool<T>(/*poolName*/ "typepool"); //typePool didn't need name
                PoolMgr.Instance.AddTypePool(tp, pool);
            }
            return pool;
        }

        public void CleanUnused()
        {
            m_FreeList.Clear();
            Dbg.Log("CleanUnused: Pool({0}), ObjOut={1}", typeof(T).Name, ObjOut);
        }

        #endregion

        #region "private method"
        // private method

        #endregion

        #region "constant data"
        // constant data

        #endregion

    }



    /// <summary>
    /// a variation of ObjectPool, use a given create func to create instance
    /// </summary>
    public class ObjectPoolC<T> : IPool where T : class
    {
        #region "data"
        // data

        private Stack<T> m_FreeList; //store available elements

        private string m_Name;
        private Action<T> m_ResetAction;
        private Action<T> m_FirstInitAction;
        private Action<T> m_TakeOutAction; //called in Spawn(), not firstInit
        private Func<T> m_CreateFunc;

        private int m_SpawnedCnt = 0;
        private int m_DespawnedCnt = 0;

        #endregion

        #region "public method"
        // public method

        public ObjectPoolC(string name) : this(name, null, null, null)
        { }
        public ObjectPoolC(string name, Action<T> resetAction) : this(name, resetAction, null, null)
        { }
        public ObjectPoolC(string name, Action<T> resetAction, Action<T> firstInitAction) : this(name, resetAction, firstInitAction, null)
        { }
        public ObjectPoolC(string name, Action<T> resetAction, Action<T> firstInitAction, Action<T> takeOutAction)
        {
            m_Name = name;
            m_ResetAction = resetAction;
            m_FirstInitAction = firstInitAction;
            m_TakeOutAction = takeOutAction;
            m_CreateFunc = null;
            //m_ObjList = new List<T>();

            m_FreeList = new Stack<T>();

            Type t = typeof(T);
            if (t.GetInterface("IRecyclable") != null)
            {
                if (m_ResetAction == null)
                {
                    m_ResetAction = o => ((IRecyclable)o).OnDespawn();
                }
                if (m_TakeOutAction == null)
                {
                    m_TakeOutAction = o => ((IRecyclable)o).OnSpawn();
                }
            }
        }


        public string Name { get { return m_Name; } }

        public Action<T> ResetAction
        {
            get { return m_ResetAction; }
            set { m_ResetAction = value; }
        }

        public Action<T> InitAction
        {
            get { return m_FirstInitAction; }
            set { m_FirstInitAction = value; }
        }

        public Action<T> TakeOutAction
        {
            get { return m_TakeOutAction; }
            set { m_TakeOutAction = value; }
        }

        public Func<T> CreateFunc
        {
            get { return m_CreateFunc; }
            set { m_CreateFunc = value; }
        }

        /// <summary>
        /// get a object
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            T obj = null;

            #region "old version, with m_availIdx"
            /*// "old version, with m_availIdx" 

            if (m_availIdx >= m_ObjList.Count)
            { // no available
                obj = new T();
                if (m_FirstInitAction != null)
                    m_FirstInitAction(obj);
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);

                m_ObjList.Add(obj);
            }
            else
            { // get from pool
                obj = m_ObjList[m_availIdx];
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);
            }

            ++m_availIdx;
            */
            #endregion "old version, with m_availIdx"

            if (m_FreeList.Count == 0)
            { //no available
                obj = m_CreateFunc();

                if (m_FirstInitAction != null)
                    m_FirstInitAction(obj);
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);
            }
            else
            {
                obj = m_FreeList.Pop();
                if (m_TakeOutAction != null)
                    m_TakeOutAction(obj);
            }

            ++m_SpawnedCnt;
            return obj;
        }
        object IPool.Spawn()
        {
            return Spawn();
        }


        /// <summary>
        /// return object to pool
        /// </summary>
        public void Despawn(T obj)
        {
            if (m_ResetAction != null)
                m_ResetAction(obj);

            #region "old version, with m_availIdx"
            // "old version, with m_availIdx" 
            //--m_availIdx;

            //int thisIdx = m_ObjList.LastIndexOf(obj);
            //Dbg.Assert(thisIdx != -1, "ObjectPool.Despawn: failed to find object in pool: {0}", obj);

            //Misc.Swap(m_ObjList, thisIdx, m_availIdx); //swap the despawned to the tail
            #endregion "old version, with m_availIdx"

            m_FreeList.Push(obj);

            ++m_DespawnedCnt;
        }
        void IPool.Despawn(object o)
        {
            Despawn((T)o);
        }

        /// <summary>
        /// do nothing
        /// notify the Pool it's being destroyed
        /// </summary>
        public void OnDestroy()
        {

        }

        public int SpawnCnt { get { return m_SpawnedCnt; } } //total spawned objs since init-ed
        public int DespawnCnt { get { return m_DespawnedCnt; } } //total despawned objs since init-ed
        public int ObjOut { get { return m_SpawnedCnt - m_DespawnedCnt; } } // current spawned living objects

        /// <summary>
        /// acquire a pool from PoolMgr, if not found, create one
        /// </summary>
        public static ObjectPoolC<T> ForceAcquire()
        {
            var tp = typeof(T).TypeHandle;
            //string poolName = tp.ToString();
            ObjectPoolC<T> pool = PoolMgr.Instance.GetTypePool(tp) as ObjectPoolC<T>;
            if (null == pool)
            {
                pool = new ObjectPoolC<T>(/*poolName*/ "typepool"); //typePool didn't need name
                PoolMgr.Instance.AddTypePool(tp, pool);
            }
            return pool;
        }

        public void CleanUnused()
        {
            m_FreeList.Clear();

            Dbg.Log("CleanUnused: Pool({0}), ObjOut={1}", typeof(T).Name, ObjOut);
        }

        #endregion

        #region "private method"
        // private method

        #endregion

        #region "constant data"
        // constant data

        #endregion

    }
}