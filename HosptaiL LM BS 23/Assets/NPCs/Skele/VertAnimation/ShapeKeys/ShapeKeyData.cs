#if false

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace VertAnim
{
    [Serializable]
    public class ShapeKeyData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;

        public float weight; // 0 - 100

        public ShapeKeyMorphSO basisSO;

	    #region "public methods"
        public static ShapeKeyData New(float weight, ShapeKeyMorphSO basisSO)
        {
            ShapeKeyData d = new ShapeKeyData();

            //int vcnt = m.vertexCount;

            d.basisSO = basisSO;

            Mesh m = basisSO.GetMesh();
            d.vertices = m.vertices;
            d.normals = m.normals;
            d.tangents = m.tangents;

            d.weight = weight;

            return d;
        }

        public void Copy(ShapeKeyData rhs)
        {
            Dbg.Assert(this.vertices.Length == rhs.vertices.Length, "ShapeKeyData.Copy: vertices cnt: {0}!={1}", vertices.Length, rhs.vertices.Length);
            Dbg.Assert(this.normals.Length == rhs.normals.Length, "ShapeKeyData.Copy: normals cnt: {0}!={1}", normals.Length, rhs.normals.Length);
            Dbg.Assert(this.tangents.Length == rhs.tangents.Length, "ShapeKeyData.Copy: tangents cnt: {0}!={1}", tangents.Length, rhs.tangents.Length);

            for (int i = 0; i < vertices.Length; ++i)
                this.vertices[i] = rhs.vertices[i];
            for (int i = 0; i < normals.Length; ++i)
                this.normals[i] = rhs.normals[i];
            for (int i = 0; i < tangents.Length; ++i)
                this.tangents[i] = rhs.tangents[i];

            this.weight = rhs.weight;

            this.basisSO = rhs.basisSO;
        }

        public int Count
        {
            get { return vertices.Length; }
        }

        public bool HasNormals() { return normals.Length != 0; }

        public bool HasTangents() { return tangents.Length != 0; }

        public bool IsValid(int vcnt)
        {
            return vertices.Length == vcnt &&
                (normals.Length == vcnt || normals.Length == 0) &&
                (tangents.Length == vcnt || tangents.Length == 0);
        }

        /// <summary>
        /// given two ShapeKeyDeformSO structures, and a interpolate t,
        /// generate the output data and fill the ShapeKeyData structure;
        /// </summary>
        public static void Lerp(ShapeKeyData lhs, ShapeKeyData rhs, float targetWeight, ShapeKeyData outData)
        {
            Lerp(lhs, rhs, lhs.weight, targetWeight, outData);
        }

        public static void Lerp(ShapeKeyData lhs, ShapeKeyData rhs, float leftWeight, float targetWeight, ShapeKeyData outData)
        {
            Dbg.Assert(outData != null && outData.IsValid(lhs.Count), "ShapeKeyData.Lerp: output data is not initialized");

            int vcnt = lhs.Count;

            bool hasNormals = (outData.normals.Length == vcnt);
            bool hasTangents = (outData.tangents.Length == vcnt);

            float t = Mathf.InverseLerp(leftWeight, rhs.weight, targetWeight);

            // Lerp [THIS PART is PAINPOINT of PERFORMANCE!]
            Vector3[] outVerts = outData.vertices;
            Vector3[] outNorms = outData.normals;
            Vector4[] outTangs = outData.tangents;
            Vector3[] lhsVerts = lhs.vertices;
            Vector3[] lhsNorms = lhs.normals;
            Vector4[] lhsTangs = lhs.tangents;
            Vector3[] rhsVerts = rhs.vertices;
            Vector3[] rhsNorms = rhs.normals;
            Vector4[] rhsTangs = rhs.tangents;

            for (int i = 0; i < vcnt; ++i)
                outVerts[i] = Vector3.Lerp(lhsVerts[i], rhsVerts[i], t);

            if (hasNormals)
            {
                for (int i = 0; i < vcnt; ++i)
                    outNorms[i] = Vector3.Lerp(lhsNorms[i], rhsNorms[i], t);
            }
            if (hasTangents)
            {
                for (int i = 0; i < vcnt; ++i)
                    outTangs[i] = Vector4.Lerp(lhsTangs[i], rhsTangs[i], t);
            }
        }

        /// <summary>
        /// outData = lhs - rhs;
        /// </summary>
        public static void Subtract(ShapeKeyData lhs, ShapeKeyData rhs, ShapeKeyData outData)
        {
            Vector3[] outVerts = outData.vertices;
            Vector3[] outNorms = outData.normals;
            Vector4[] outTangs = outData.tangents;
            Vector3[] lhsVerts = lhs.vertices;
            Vector3[] lhsNorms = lhs.normals;
            Vector4[] lhsTangs = lhs.tangents;
            Vector3[] rhsVerts = rhs.vertices;
            Vector3[] rhsNorms = rhs.normals;
            Vector4[] rhsTangs = rhs.tangents;

            int vcnt = outData.Count;
            int outNormsLen = outNorms.Length;
            int outTangsLen = outTangs.Length;

            for (int i = 0; i < vcnt; ++i)
            {
                outVerts[i] = lhsVerts[i] - rhsVerts[i];
            }

            for (int i = 0; i < outNormsLen; ++i)
            {
                outNorms[i] = lhsNorms[i] - rhsNorms[i];
            }

            for (int i = 0; i < outTangsLen; ++i)
            {
                outTangs[i] = lhsTangs[i] - rhsTangs[i];
            }
        }

        public void ApplyToMesh(Mesh m)
        {
            int vcnt = m.vertexCount;
            m.vertices = vertices;

            if (m.normals.Length == vcnt && HasNormals())
            {
                m.normals = normals;
            }

            if (m.tangents.Length == vcnt && HasTangents())
            {
                m.tangents = tangents;
            }
        }

        /// <summary>
        /// ApplyToMeshAsDiff, with MeshCache version,
        /// </summary>
        public void ApplyToMeshAsDiff(Mesh m, MeshCacheRT cache)
        {
            int vcnt = m.vertexCount;

            // vertices
            Vector3[] newVerts = cache.GetVertices(m);
            for (int i = 0; i < vcnt; ++i)
            {
                newVerts[i] += vertices[i];
            }
            cache.SetVertices(m, newVerts);

            // normals
            Vector3[] cacheNormals = cache.GetNormals();
            if (cacheNormals != null && cacheNormals.Length == vcnt && HasNormals())
            {
                Vector3[] newNormals = cache.GetNormals(m);
                for (int i = 0; i < vcnt; ++i)
                {
                    newNormals[i] += normals[i];
                }
                cache.SetNormals(m, newNormals);
            }

            // tangents
            Vector4[] cacheTangents = cache.GetTangents();
            if (cacheTangents != null && cacheTangents.Length == vcnt && HasTangents())
            {
                Vector4[] newTangents = cache.GetTangents(m);
                for (int i = 0; i < vcnt; ++i)
                {
                    newTangents[i] += tangents[i];
                }
                cache.SetTangents(m, newTangents);
            }
        }

        public void ApplyToMeshAsDiff(Mesh m)
        {
            int vcnt = m.vertexCount;

            // vertices
            Vector3[] newVerts = m.vertices;
            for (int i = 0; i < vcnt; ++i)
            {
                newVerts[i] += vertices[i];
            }
            m.vertices = newVerts;

            // normals
            if (m.normals.Length == vcnt && HasNormals())
            {
                Vector3[] newNormals = m.normals;
                for (int i = 0; i < vcnt; ++i)
                {
                    newNormals[i] += normals[i];
                }
                m.normals = newNormals;
            }

            // tangents
            if (m.tangents.Length == vcnt && HasTangents())
            {
                Vector4[] newTangents = m.tangents;
                for (int i = 0; i < vcnt; ++i)
                {
                    newTangents[i] += tangents[i];
                }
                m.tangents = newTangents;
            }
        }

        public bool IsBasis()
        {
            return basisSO.GetShapeKeyDataDiff(0) == this;
        }

	    // "public methods" 
	
	    #endregion "public methods"

	    #region "private methods"
        private ShapeKeyData() { }

        private static void _InitTangents_Diff(Mesh m, ShapeKeyData basisShape)
        {
            throw new NotImplementedException();
        }

        private static void _InitNormals_Diff(Mesh m, ShapeKeyData basisShape)
        {
            throw new NotImplementedException();
        }

        private static void _InitVertices_Diff(Mesh m, ShapeKeyData basisShape)
        {
            throw new NotImplementedException();
        }

	    // "private methods" 
	
	    #endregion "private methods"

    }

}
}
#endif