using System;
using System.Collections.Generic;
using UnityEngine;

using ExtMethods;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace MH
{
    using PairLst = List<StringPair>;

    /// <summary>
    /// this represents a rebind-able transform component;
    /// it will record the transform path of the specified transform;
    /// and when try to rebind, will refer to a RebindDict and get the real transform object from stored transform_path;
    /// </summary>
    [Serializable]
    public class RebindTr
    {
        [SerializeField][Tooltip("original transform, used in edit-time")]
        private Transform m_editTimeTr;
        [SerializeField][Tooltip("saved transform path")]
        private string m_trPath;

        private Transform m_runtimeTr; //runtime bind tr, not serialized

        public Transform tr
        {
            get
            {
                if (Application.isPlaying)
                {
                    return m_runtimeTr;
                }
                else
                {
                    return m_editTimeTr;
                }
            }
            set 
            {
                if (!Application.isPlaying)
                {
                    m_editTimeTr = value;
                    _CalcTrPath();
                }
                else
                {
                    Dbg.LogWarn("set tr during runtime is not allowed: {0}", value);
                }
            }
        }

        
        public GameObject gameObject
        {
            get {
                Transform o = tr;
                if (o != null)
                    return o.gameObject;
                else
                    return null;
            }
        }

        public string trPath
        {
            get { return m_trPath; } 
        }

        private void _CalcTrPath()
        {
#if UNITY_EDITOR
            Dbg.Assert(RebindOption.editTimeRoot != null, "RebindTr.CalcTrPath: m_editTimeRoot is null!");
            m_trPath = AMUtil.CalculateTransformPath(m_editTimeTr, RebindOption.editTimeRoot);
#endif
        }

        public void Rebind(RebindOption bindOpt)
        {
            if (Application.isPlaying)
            {
                m_runtimeTr = bindOpt.FindTr(trPath);
            }
            else
            {
                m_editTimeTr = bindOpt.FindTr(trPath); //even editTimeTr not null, it could be stale
            }            
        }

        public void Unbind()
        {
            m_runtimeTr = null;
        }
    }

    /// <summary>
    /// this is used to provide extra info for rebinding,
    /// in usual case, rebinding only needs one root transform;
    /// 
    /// but if we want to make runtime switching, 
    /// </summary>
    [Serializable]
    public class RebindOption
    {
        [SerializeField][Tooltip("the rootTr used to find transform with stored transform path")]
        private Transform m_rootTr; //the root transform, used to calculate from trPath to real transform
        [SerializeField][Tooltip("remap the transform path in a prefix matching method")]
        private PairLst m_trPathRemap = new PairLst();

        private static Transform ms_editTimeRoot; //used to calc tr path in editTime, set by AMTimeline

        public static Transform editTimeRoot
        {
            get { return ms_editTimeRoot; }
            set { ms_editTimeRoot = value; }
        }

        public Transform rootTr
        {
            get { return m_rootTr; }
            set { m_rootTr = value; }
        }

        public bool hasOption
        {
            get { return m_trPathRemap.Count > 0; }
        }

        public PairLst trPathRemap
        {
            get { return m_trPathRemap; }
        }

        public Transform FindTr(string trPath)
        {
            if (trPath == null)
                return null;
            
            if (hasOption)
            {
                Transform tr = null;

                for (var ie = m_trPathRemap.GetEnumerator(); ie.MoveNext(); )
                {
                    var pr = ie.Current;
                    string prefix = pr.first;
                    if( trPath.StartsWith(prefix) )
                    {
                        string repl = pr.second;
                        trPath = repl + trPath.Substring(prefix.Length);
                        break;
                    }
                }

                tr = rootTr.Find(trPath);

                return tr;
            }
            else
            {
                return rootTr.Find(trPath);
            }            
        }
    }

    [Serializable]
    public class StringPair : Pair<string, string> { }

}
