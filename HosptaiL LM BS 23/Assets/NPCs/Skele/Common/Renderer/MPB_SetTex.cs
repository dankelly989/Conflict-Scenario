using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    [ExecuteInEditMode]
    public class MPB_SetTex : MonoBehaviour
    {
        [SerializeField]
        private string m_param = "_MainTex";
        [SerializeField]
        private Texture m_tex = null;

        public bool m_doUpdate = false;

        private Renderer m_renderer;

        public string PropName
        {
            get { return m_param; }
            set { m_param = value; }
        }

        public Texture tex
        {
            get { return m_tex; }
            set { m_tex = value; _SetProperty(); }
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
            if (m_tex == null)
                return;

            var blk = MPB_Base.propBlock;
            blk.SetTexture(m_param, m_tex);

            if (m_renderer != null)
            {
                m_renderer.SetPropertyBlock(blk);
            }
        }
    }
}
