using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace MeshEditor
{
    /// <summary>
    /// this is used to cache specified mesh's data, 
    /// so we could avoid the Mesh.vertices / .normals / .tangents when we can
    /// </summary>
    class MeshCache
    {
	    #region "data"
        // data
        private static MeshCache sm_Instance;

        private Transform m_Tr;
        private Mesh m_Mesh;

        private Vector3[] m_vertices;
        private Vector3[] m_normals;
        private Vector4[] m_tangents;
        private int[] m_triangles;

        private bool m_DirtyVertices;
        private bool m_DirtyNormals;
        private bool m_DirtyTangents;
        private bool m_DirtyTriangles;

        #endregion "data"

	    #region "public method"
        // public method

        public static MeshCache Instance
        {
            get { return sm_Instance; }
        }

        public static void CreateInstance()
        {
            Dbg.Assert(sm_Instance == null, "MeshCache.Init: instance is already created");
            sm_Instance = new MeshCache();
        }

        public static void DestroyInstance()
        {
            sm_Instance = null;
        }

        public void Init(Transform tr, Mesh m)
        {
            m_Tr = tr;
            m_Mesh = m;
            m_DirtyTangents = m_DirtyNormals = m_DirtyVertices = true;
            m_DirtyTriangles = true;

            MeshUndoer.AddDeleMeshModified(this._OnMeshModified);
        }

        public void Fini()
        {
            MeshUndoer.DelDeleMeshModified(this._OnMeshModified);
            m_Mesh = null;
        }

        public Transform GetTransform()
        {
            return m_Tr;
        }

        public Vector3[] vertices
        {
            get
            {
                if (m_DirtyVertices)
                {
                    m_vertices = m_Mesh.vertices;
                    m_DirtyVertices = false;
                }
                return m_vertices;
            }
        }

        public Vector3[] normals
        {
            get
            {
                if (m_DirtyNormals)
                {
                    m_normals = m_Mesh.normals;
                    m_DirtyNormals = false;
                }
                return m_normals;
            }
        }

        public Vector4[] tangents
        {
            get 
            {
                if( m_DirtyTangents )
                {
                    m_tangents = m_Mesh.tangents;
                    m_DirtyTangents = false;
                }
                return m_tangents;
            }
        }

        public int[] triangles
        {
            get 
            {
                if( m_DirtyTriangles)
                {
                    m_triangles = m_Mesh.triangles;
                    m_DirtyTriangles = false;
                }
                return m_triangles;
            }
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private void _OnMeshModified()
        {
            m_DirtyVertices = m_DirtyNormals = m_DirtyTangents = true;
            //m_DirtyTriangles = true; //no need, topology should not be changed
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"


    }
}
}
