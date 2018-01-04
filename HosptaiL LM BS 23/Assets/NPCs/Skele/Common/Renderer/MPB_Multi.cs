using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    [ExecuteInEditMode]
    public class MPB_Multi : MonoBehaviour
    {
        
        [SerializeField][Tooltip("prop unit list")]
        private List<PropUnit> _propUnits = new List<PropUnit>();
        [SerializeField][Tooltip("")]
        private bool m_doUpdate = false;

        public List<PropUnit> propUnits { get { return _propUnits; } }

        private Renderer m_renderer;
        
        void OnEnable()
        {
            m_renderer = GetComponent<Renderer>();
            _SetProperty();
        }

        void OnRenderObject()
        {
            if (!Application.isPlaying)
            {
                if (m_renderer == null)
                    m_renderer = GetComponent<Renderer>();
                _SetProperty();
            }
            else if (m_doUpdate)
            {
                _SetProperty();
            }
        }

        #region "set value"
        public void SetColor(int idx, Color v)
        {
            PropUnit unit = _propUnits[idx];
            unit.cVal = v;
            _SetProperty();
        }
        public void SetColor(string name, Color v)
        {
            PropUnit unit = _propUnits.Find(x => x.paramName == name);
            unit.cVal = v;
            _SetProperty();
        }
        public void SetFloat(int idx, float v)
        {
            PropUnit unit = _propUnits[idx];
            unit.fVal = v;
            _SetProperty();
        }
        public void SetFloat(string name, float v)
        {
            PropUnit unit = _propUnits.Find(x => x.paramName == name);
            unit.fVal = v;
            _SetProperty();
        }
        public void SetMatrix(int idx, Matrix4x4 v)
        {
            PropUnit unit = _propUnits[idx];
            unit.mVal = v;
            _SetProperty();
        }
        public void SetMatrix(string name, Matrix4x4 v)
        {
            PropUnit unit = _propUnits.Find(x => x.paramName == name);
            unit.mVal = v;
            _SetProperty();
        }
        public void SetTexture(int idx, Texture v)
        {
            PropUnit unit = _propUnits[idx];
            unit.tVal = v;
            _SetProperty();
        }
        public void SetTexture(string name, Texture v)
        {
            PropUnit unit = _propUnits.Find(x => x.paramName == name);
            unit.tVal = v;
            _SetProperty();
        }
        public void SetVector(int idx, Vector4 v)
        {
            PropUnit unit = _propUnits[idx];
            unit.vVal = v;
            _SetProperty();
        }
        public void SetVector(string name, Vector4 v)
        {
            PropUnit unit = _propUnits.Find(x => x.paramName == name);
            unit.vVal = v;
            _SetProperty();
        }
        #endregion "set value"



        private void _SetProperty()
        {
            if (m_renderer != null)
            {
                var blk = MPB_Base.propBlock;
                for (int i = 0; i < _propUnits.Count; ++i)
                {
                    var unit = _propUnits[i];
                    switch(unit.etype)
                    {
                        case EPropType.Color: blk.SetColor(unit.paramName, unit.cVal); break;
                        case EPropType.Float: blk.SetFloat(unit.paramName, unit.fVal); break;
                        case EPropType.Matrix: blk.SetMatrix(unit.paramName, unit.mVal); break;
                        case EPropType.Texture: blk.SetTexture(unit.paramName, unit.tVal); break;
                        case EPropType.Vector: blk.SetVector(unit.paramName, unit.vVal); break;
                    }
                }            
                m_renderer.SetPropertyBlock(blk);
            }
        }

        [Serializable]
        public class PropUnit
        {
            public string paramName;
            public EPropType etype;
            public Color cVal;
            public float fVal;
            public Matrix4x4 mVal;
            public Texture tVal;
            public Vector4 vVal;
        }

        public enum EPropType
        {
            Color,
            Float,
            Matrix,
            Texture,
            Vector,
        }
    }
}
