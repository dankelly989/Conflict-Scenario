using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    /// <summary>
    /// auto deactivate gameobject when awake
    /// </summary>
    [SelectionBase][ExecuteInEditMode]
    public class EmptyMarker : MonoBehaviour
    {
        #region "configurable data"

        [SerializeField]
        private Mesh m_mesh;
        [SerializeField][Tooltip("unselected material")]
        private Material m_mat;
        [SerializeField][Tooltip("selected material")]
        private Material m_selMat;

        [SerializeField]
        private MeshFilter m_mf;
        [SerializeField]
        private MeshRenderer m_rd;
        [SerializeField]
        private Transform m_jumpToTarget;

        #endregion "configurable data"

        #region "data"

        //private Transform m_tr;

        #endregion "data"

        #region "public method"

        void OnEnable()
        {
            //m_tr = transform;
            //if( m_rd )
            //    m_rd.gameObject.hideFlags = HideFlags.HideInInspector;

            if (Application.isPlaying)
            {
                if (m_rd != null)
                    m_rd.enabled = false;
            }
        }

        public Mesh mesh
        {
            get { return m_mesh; }
            set { m_mesh = value; _OnSetMesh(); }
        }

        public Material material
        {
            get { return m_mat; }
            set { m_mat = value; }
        }

        public Material selectedMaterial
        {
            get { return m_selMat; }
            set { m_selMat = value; _OnSetSelectedMaterial(); }
        }

        public MeshFilter mf
        {
            get
            {
                if (m_mf == null)
                    m_mf = GetComponent<MeshFilter>();
                return m_mf;
            }
            set
            {
                m_mf = value;
                if (m_mf != null)
                {
                    rd = m_mf.GetComponent<MeshRenderer>();
                }
            }
        }

        public MeshRenderer rd
        {
            get
            {
                if (m_rd == null && m_mf != null)
                    m_rd = m_mf.GetComponent<MeshRenderer>();
                return m_rd;
            }
            private set
            {
                m_rd = value;
            }
        }

        public Transform jumpTo
        {
            get { return m_jumpToTarget; }
            set { m_jumpToTarget = value; }
        }

        #endregion "public method"

        #region "private method"

        private void _OnSetMesh()
        {
            mf.sharedMesh = m_mesh;
        }

        private void _OnSetSelectedMaterial()
        {
            rd.sharedMaterial = m_selMat;
        }

        #endregion "private method"

        #region "constant data"


        #endregion "constant data"
    }

   
}
