using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    using CONT = System.Collections.Generic.List<Vector3>;

	public class VertMarker
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

        public VertMarker(){}

        public void Init(Material m, int layer)
        {
            m_Mesh = new Mesh();
            MeshUtil.MarkDynamic(m_Mesh);

            m_Mat = m;
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

            // SPECIAL CASE //
            if( SystemInfo.graphicsShaderLevel < 40 && verts.Length == 1)
            { //no GS & only one vert, needs special treatment
                //Dbg.Log("NO GS and only ONE vert");
                m_Mesh.vertices = new Vector3[] { verts[0], new Vector3(0, -1000000f, 0) };
                m_Mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Points, 0);
            }            
            else
            {
                m_Mesh.vertices = verts;

                int[] indices = new int[verts.Length];
                for (int i = 0; i < indices.Length; ++i)
                {
                    indices[i] = i;
                }

                m_Mesh.SetIndices(indices, MeshTopology.Points, 0);
            }
            
        }

        public void SetColors(Color[] colors)
        {
            if( SystemInfo.graphicsShaderLevel < 40 && colors.Length == 1)
            {
                m_Mesh.colors = new Color[] { colors[0], Color.white };
            }
            else
            {
                m_Mesh.colors = colors;
            }
        }

        public void SetColors(Color32[] colors)
        {
            if (SystemInfo.graphicsShaderLevel < 40 && colors.Length == 1)
            {
                m_Mesh.colors32 = new Color32[] { colors[0], new Color32(0, 0, 0, 0) };
            }
            else
            {
                m_Mesh.colors32 = colors;
            }
        }

        public void SetPointSize(float sz)
        {
            m_Mat.SetFloat("_PSize", sz);
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

        private const int UNKEY_BASE = 1000000; 

        #endregion "constant data"
	}
}
