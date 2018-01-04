using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    [ExecuteInEditMode]
    public class MPB_SetInt : MonoBehaviour
    {
        [SerializeField]
        public string m_param = "_Val";
        [SerializeField]
        private int m_val = 0;

        public bool m_doUpdate = false;

        private Renderer m_renderer;

        public string PropName
        {
            get { return m_param; }
            set { m_param = value; }
        }

        public int Val
        {
            get { return m_val; }
            set { m_val = value; _SetProperty(); }
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
            blk.SetFloat(m_param, m_val);

            if (m_renderer != null)
            {
                m_renderer.SetPropertyBlock(blk);
            }
        }
    }
}
