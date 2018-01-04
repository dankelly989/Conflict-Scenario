using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    [ExecuteInEditMode]
    public class MPB_SetColor : MonoBehaviour
    {
        [SerializeField]
        public string m_param = "_Color";
        [SerializeField]
        private Color m_color = Color.white;

        public bool m_doUpdate = false;

        private Renderer m_renderer;

        public string PropName
        {
            get { return m_param; }
            set { m_param = value; }
        }

        public Color Color
        {
            get { return m_color; }
            set { m_color = value; _SetProperty(); }
        }

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

        private void _SetProperty()
        {
            var blk = MPB_Base.propBlock;
            blk.SetColor(m_param, m_color);
            
            if (m_renderer != null)
            {
                m_renderer.SetPropertyBlock(blk);
            }
        }
    }
}
