using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// this is a simplified mesh-cache for runtime,
    /// if will just store the data of mesh to reduce the need of calling get_vertices() get_normals() etc.
    /// 
    /// WARNING: it assumes all modifications of the mesh goes through its interface
    /// </summary>
    [Serializable]  // this attribute is only used to keep it available as MorphProc's private member after compilation
	public class MeshCacheRT 
	{
        #region "data"
        // data
        private Vector3[] m_vertices;
        private Vector3[] m_normals;
        private Vector4[] m_tangents;
        private int[] m_triangles;

        #endregion "data"

        #region "public method"
        // public method

        public void SetVertices(Mesh m, Vector3[] verts)
        {
            m_vertices = verts;
            m.vertices = verts;
        }

        public void SetNormals(Mesh m, Vector3[] normals)
        {
            m_normals = normals;
            m.normals = normals;
        }

        public void SetTangents(Mesh m, Vector4[] tangs)
        {
            m_tangents = tangs;
            m.tangents = tangs;
        }

        public void SetTriangles(Mesh m, int[] tris)
        {
            m_triangles = tris;
            m.triangles = tris;
        }

        public Vector3[] GetVertices()
        {
            return m_vertices;
        }
        public Vector3[] GetVertices(Mesh m)
        {
            if( m_vertices == null )
            {
                m_vertices = m.vertices;
            }
            return m_vertices;
        }

        public Vector3[] GetNormals()
        {
            return m_normals;
        }
        public Vector3[] GetNormals(Mesh m)
        {
            if( m_normals == null )
            {
                m_normals = m.normals;
            }
            return m_normals;
        }

        public Vector4[] GetTangents()
        {
            return m_tangents;
        }
        public Vector4[] GetTangents(Mesh m)
        {
            if( m_tangents == null )
            {
                m_tangents = m.tangents;
            }
            return m_tangents;
        }

        public int[] GetTriangles()
        {
            return m_triangles;
        }

        
        #endregion "public method"


        #region "constant data"
        // constant data

        #endregion "constant data"

	}
}
