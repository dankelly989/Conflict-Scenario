using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// mark the edges of the mesh
    /// </summary>
	public class EdgeMarker
	{
	    #region "data"
        // data

        private Mesh m_Mesh; //private mesh
        private Material m_Mat;
        private int m_Layer;

        private MaterialPropertyBlock m_matProps;

        #endregion "data"

	    #region "public method"
        // public method

        public void Init(Material mat, int layer = 0)
        {
            m_Mesh = new Mesh();
            MeshUtil.MarkDynamic(m_Mesh);

            m_Mat = mat;
            m_Layer = layer;

            m_matProps = new MaterialPropertyBlock();
            m_matProps.Clear();
        }

        public void Fini()
        {
#if UNITY_EDITOR
            Mesh.DestroyImmediate(m_Mesh);
#else
            Mesh.Destroy(m_Mesh);
#endif
        }

        public void SetVerts(Vector3[] verts)
        {
            if (verts.Length != m_Mesh.vertexCount)
                m_Mesh.Clear();
            
            m_Mesh.vertices = verts;
        }

        public void SetColors(Color32[] colors)
        {
            m_Mesh.colors32 = colors;
        }

        public void SetIndices(int[] indices)
        {
            m_Mesh.SetIndices(indices, MeshTopology.Lines, 0);
        }

        public void Draw()
        {
            Graphics.DrawMesh(m_Mesh, Matrix4x4.identity, m_Mat, m_Layer,
                EUtil.GetSceneView().camera, 0,
                m_matProps, false, false);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
	}
}
