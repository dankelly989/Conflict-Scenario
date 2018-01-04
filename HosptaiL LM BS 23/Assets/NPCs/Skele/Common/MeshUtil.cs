using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
	public class MeshUtil
	{
        // given mesh and tri-idx, return the 3 vert position
        public static int[] GetTriangleVertIdx(Mesh m, int triangleIdx)
        {
            int[] tris = m.triangles;

            int tidx3 = triangleIdx*3;
            int v0 = tris[tidx3];
            int v1 = tris[tidx3 + 1];
            int v2 = tris[tidx3 + 2];

            return new int[] { v0, v1, v2 };
        }

        public static Vector3[] GetTriangleVertPos(Mesh m, int triangleIdx)
        {
            Vector3[] v = m.vertices;
            int[] tris = m.triangles;

            int tidx3 = triangleIdx * 3;
            int v0 = tris[tidx3];
            int v1 = tris[tidx3 + 1];
            int v2 = tris[tidx3 + 2];

            return new Vector3[] { v[v0], v[v1], v[v2] };
        }

        public static List<Vector3> GetVertPos(Mesh m, List<int> vertIndices)
        {
            List<Vector3> vertPosLst = new List<Vector3>();

            Vector3[] vs = m.vertices;
            for(int i=0; i<vertIndices.Count; ++i)
            {
                int idx = vertIndices[i];
                vertPosLst.Add(vs[idx]);
            }

            return vertPosLst;
        }

        /// <summary>
        /// if no normal or empty index list, then return null
        /// </summary>
        public static List<Vector3> GetVertNormal(Mesh m, List<int> vertIndices)
        {
            List<Vector3> normalLst = new List<Vector3>();

            Vector3[] normals = m.normals;
            if (normals.Length == 0 || vertIndices.Count == 0)
                return null;

            for(int i=0; i<vertIndices.Count; ++i)
            {
                int idx = vertIndices[i];
                Vector3 n = normals[idx];
                normalLst.Add(n);
            }

            return normalLst;
        }

        public static void FixNormalAndBound(Mesh m)
        {
            m.RecalculateNormals();
            m.RecalculateBounds(); //prevent disappearing
            //EUtil.GetSceneView().Repaint();
        }

        /// <summary>
        /// there's a bug: http://issuetracker.unity3d.com/issues/markdynamic-in-combination-with-serialized-meshes-in-dx11-crashes-unity
        /// on Dx11, if marked the mesh as dynamic and modify it, Unity Editor will crash
        /// </summary>
        public static void MarkDynamic(Mesh m)
        {
            // only MarkDynamic for Dx9
            if( SystemInfo.graphicsShaderLevel < 40 )
            {
                m.MarkDynamic();
            }
        }
	}
}
