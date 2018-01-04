using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    using DList = List<DPair>;
    using DMap = Dictionary<string, EDataObj>;

    [Serializable]
    public class EData : ScriptableObject
    {
        [SerializeField]
        private DList m_data = new DList();

        private DMap m_dict = new DMap();

        private static EData ms_instance = null;

        public static DMap GetDMap() { return Instance.m_dict; }

        private static EData Instance
        {
            get { _EnsureInstance(); return ms_instance; }
        }

        void OnEnable()
        {
            //Dbg.Log("EData.OnEnable: m_data: {0}", m_data.Count);
            _Reload();
        }

        void OnDisable()
        {
            //Dbg.Log("EData.OnDisable: m_dict: {0}", m_dict.Count);
            m_data.Clear();
            foreach (var pr in m_dict)
            {
                string id = pr.Key;
                EDataObj data = pr.Value;
                m_data.Add(new DPair(id, data));
            }
        }

        public static void Clear()
        {
            Instance.m_dict.Clear();
        }

        public static void Reload()
        {
            var inst = Instance;
            inst._Reload();
        }

        public static bool Contains(string id)
        {
            return Instance.m_dict.ContainsKey(id);
        }

        /// <summary>
        /// try get a obj, create one if not exist
        /// </summary>
        public static T FGet<T>(string id, out bool isNew) where T : EDataObj
        {
            isNew = false;
            var inst = Instance;
            EDataObj d = null;
            if (inst.m_dict.TryGetValue(id, out d))
            {
                T t = d as T;
                if (t == null || d == null)
                { //missing ref, happens when switch scene
                    //Dbg.LogWarn("EData.FGet: mismatch type or missing: expected {0}: stored {1}", typeof(T).Name, d.GetType().Name);
                    T newData = ScriptableObject.CreateInstance<T>();
                    inst.m_dict[id] = newData;
                    isNew = true;
                    return newData;
                }
                else
                {
                    return t;
                }
            }
            else
            {
                T newData = ScriptableObject.CreateInstance<T>();
                inst.m_dict.Add(id, newData);
                isNew = true;
                return newData;
            }
        }
        public static T FGet<T>(string id) where T : EDataObj
        {
            bool isNew = false;
            return FGet<T>(id, out isNew);
        }

        public static EDataObj Get(string id)
        {
            EDataObj d = null;
            Instance.m_dict.TryGetValue(id, out d);
            return d;
        }

        private static void _EnsureInstance()
        {
            if (ms_instance == null)
            {
                if (EUtil.AssetExists(PATH))
                {
                    //Dbg.Log("EData.sctor: exist");
                    ms_instance = AssetDatabase.LoadAssetAtPath(PATH, typeof(EData)) as EData;
                    Dbg.Assert(ms_instance != null, "EData.sctor: failed to load asset from path : {0}", PATH);
                }
                else
                {
                    //Dbg.Log("EData.sctor: non-exist");
                    ms_instance = ScriptableObject.CreateInstance<EData>();
                    Dbg.Assert(ms_instance != null, "EData.sctor: failed to create asset");
                    AssetDatabase.CreateAsset(ms_instance, PATH);
                    AssetDatabase.SaveAssets();
                } 
            }            
        }

        private void _Reload()
        {
            m_dict.Clear();
            foreach (var pr in m_data)
            {
                m_dict.Add(pr.x, pr.y);
            }
        }

        public const string PATH = "Assets/Skele/EData.asset";
    }

    [Serializable]
    public class DPair : Pair<string, EDataObj>
    {
        public DPair(string s, EDataObj o) : base(s, o) { }
    }
}
