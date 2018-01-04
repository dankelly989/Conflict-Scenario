using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    [Serializable]
    public class RebindCp
    {
        [SerializeField]
        [Tooltip("original component, used in edit-time")]
        private Component m_editTimeCp;
        [SerializeField]
        [Tooltip("saved transform path")]
        private string m_trPath;
        [SerializeField]
        [Tooltip("the save component type string")]
        private string m_compType;

        private Component m_runtimeCp; //runtime bind component, not serialized

        public Component cp
        {
            get
            {
                if (Application.isPlaying)
                {
                    return m_runtimeCp;
                }
                else
                {
                    return m_editTimeCp;
                }
            }
            set
            {
                if (!Application.isPlaying)
                {
                    m_editTimeCp = value;
                    _CalcTrPath();
                    _CalcCompType();
                }
                else
                {
                    Dbg.LogWarn("set cp during runtime is not allowed: {0}", value);
                }
            }
        }

        public string trPath
        {
            get { return m_trPath; }
        }

        private void _CalcTrPath()
        {
#if UNITY_EDITOR
            Dbg.Assert(RebindOption.editTimeRoot != null, "RebindCp.CalcTrPath: ms_editTimeRoot is null!");
            m_trPath = AMUtil.CalculateTransformPath(m_editTimeCp.SafeGetComponent<Transform>(), RebindOption.editTimeRoot);
#endif
        }

        private void _CalcCompType()
        {
            if (m_editTimeCp != null)
                m_compType = m_editTimeCp.GetType().FullName;
            else
                m_compType = string.Empty;
        }

        public void Rebind(RebindOption bindOpt)
        {
            if (Application.isPlaying)
            {
                Transform tr = bindOpt.FindTr(m_trPath);
                m_runtimeCp = null;
                if (tr != null)
                {
                    Type type = RCall.GetTypeFromString(m_compType, true);
                    m_runtimeCp = tr.GetComponent(type);
                    if (m_runtimeCp == null)
                        Dbg.LogWarn("RebindCp.Rebind: failed to get cp \"{0}\" on \"{1}\"", m_compType, m_trPath);
                }
                else
                {
                    Dbg.LogWarn("RebindCp.Rebind: failed to find \"{0}\"", m_trPath);
                }
            }
            else
            {
                Transform tr = bindOpt.FindTr(m_trPath); //even editTimeTr not null, it could be stale
                if (tr != null)
                {
                    Type type = RCall.GetTypeFromString(m_compType, true);
                    m_editTimeCp = tr.GetComponent(type);
                }
            }
        }

        public void Unbind()
        {
            m_runtimeCp = null;
        }
    }
}
