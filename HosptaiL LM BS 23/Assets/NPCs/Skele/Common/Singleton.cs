using System;
using UnityEngine;

namespace MH
{

    public class AutoCreateInstance : Attribute { }


    /// <summary>
    /// Singleton class is base class for all singletons
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>, new()
    {

        // the static instance, you know...
        protected static T sm_instance = null;

        //protected bool m_awaken = false; //flag whether Awake is called

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <exception cref='InvalidOperationException'>
        /// Is thrown when the instance not created yet.
        /// </exception>
        public static T Instance
        {
            get
            {
                if ( ! sm_instance )
                {
                    Type tp = typeof(T);
                    string tpName = tp.Name; 

                    //bool autoCreateInstance = Attribute.GetCustomAttribute(tp, typeof(AutoCreateInstance)) != null;
                    bool autoCreateInstance = RCall.HasAttribute(tp, typeof(AutoCreateInstance));

                    if (Application.isPlaying)
                    {
                        if (!autoCreateInstance)
                        {
                            Dbg.LogErr("Singleton.sm_instance is null: " + typeof(T).Name);
                        }
                        else
                        {
                            GameObject newGo = new GameObject(tpName);
                            newGo.AddComponent<T>(); //Awake will be called to set sm_instance;
                            newGo.hideFlags = sm_instance.AutoHideFlags;
                        }
                    }
                    else
                    { //for editor
                        T obj = (T)GameObject.FindObjectOfType(tp);
                        if (obj != null)
                        {
                            obj.Awake();
                        }
                        else
                        {
                            if (autoCreateInstance)
                            {
                                GameObject newGo = new GameObject(tpName);
                                newGo.AddComponent(tp); //awake will be called, sm_instance is set
                                newGo.hideFlags = sm_instance.AutoHideFlags;
                            }
                        }
                    }
                }
                return sm_instance;
            }
        }

        /// <summary>
        /// check if already created
        /// </summary>
        public static bool HasInst
        {
            get { return sm_instance != null; }
        }

        /// <summary>
        /// only effective for those auto-create instance
        /// </summary>
        public virtual HideFlags AutoHideFlags
        {
            get { return HideFlags.None; }
        }

        /// <summary>
        /// Awake this instance.
        /// create the instance
        /// </summary>
        /// <exception cref='InvalidOperationException'>
        /// Is thrown when instance already created
        /// </exception>
        public void Awake()
        {
            //if (m_awaken) // must be put after sm_instance assignment, for recover after recompile
            //{
            //    if(  ! sm_instance )
            //        sm_instance = this as T; //for editor, when recompile, use this to recover sm_instance

            //    return;
            //}

            if (null != sm_instance)
            {
                throw new InvalidOperationException("Instance already exists: " + typeof(T).ToString());
            }

            sm_instance = this as T;
            Dbg.Log("Singleton.Awake: {0}", typeof(T).Name);
            sm_instance.Init();

            //m_awaken = true;
        }

        /// <summary>
        /// call the callback Fini
        /// </summary>
        public void OnDestroy()
        {
            Fini();
        }

        /// <summary>
        /// Init this instance. for subclasses
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// destroy callback
        /// </summary>
        public virtual void Fini() { }



        /// <summary>
        /// Destroy this instance.
        /// </summary>	
        public static void Destroy()
        {
            Dbg.Log("Singleton.Destroy: {0}", typeof(T).Name);
            sm_instance = null;
        }
    }

    /// <summary>
    /// this class will not inherit behaviour
    /// </summary>
    public class NonBehaviourSingleton<T> where T : NonBehaviourSingleton<T>, new()
    {
        // the static instance, you know...
        protected static T sm_instance = null;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <exception cref='InvalidOperationException'>
        /// Is thrown when the instance not created yet.
        /// </exception>
        public static T Instance
        {
            get
            {
                if ( sm_instance == null)
                {
                    if (Application.isPlaying)
                    {
                        Type tp = typeof(T);
                        string tpName = tp.Name;
                        bool autoCreateInstance = RCall.HasAttribute(tp, typeof(AutoCreateInstance));

                        if (!autoCreateInstance)
                        {
                            Dbg.LogErr("Singleton.sm_instance is null: " + tpName);
                        }
                        else
                        {
                            Create();
                        }
                    }
                    else
                    {
                        Create(); // for editor
                    }
                }
                return sm_instance;
            }
        }

        /// <summary>
        /// check if already created
        /// </summary>
        public static bool HasInst
        {
            get { return sm_instance != null; }
        }

        /// <summary>    
        /// create the instance
        /// </summary>
        /// <exception cref='InvalidOperationException'>
        /// Is thrown when instance already created
        /// </exception>
        public static void Create()
        {
            if (null != sm_instance)
            {
                throw new InvalidOperationException("Instance already exists: " + typeof(T).ToString());
            }

            sm_instance = new T();
            Dbg.Log("NonBehaviourSingleton.Create: {0}", typeof(T).Name);
            sm_instance.Init();
        }

        /// <summary>
        /// Init this instance. for subclasses
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// destroy callback
        /// </summary>
        public virtual void Fini() { }

        /// <summary>
        /// Destroy this instance.
        /// </summary>	
        public static void Destroy()
        {
            if (sm_instance != null)
            {
                Dbg.Log("NonBehaviourSingleton.Destroy: {0}", typeof(T).Name);

                sm_instance.Fini();
                sm_instance = null;
            }
        }
    }

}