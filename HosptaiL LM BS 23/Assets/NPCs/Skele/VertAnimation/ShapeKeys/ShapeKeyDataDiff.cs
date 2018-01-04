using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace VertAnim
{
    /// <summary>
    /// this data structure is used to contain diff data with basis
    /// </summary>
    [Serializable]
    public class ShapeKeyDataDiff
    {
	    #region "data"
        // data

        public List<Vector3> vertices;
        public List<int> indV;

        public List<Vector3> normals;
        public List<int> indN;

        public List<Vector4> tangents;
        public List<int> indT;

        public float weight; // [0, 100]
        public bool isFullData; //if true, then indV/N/T is null, and the vertices/normals/tangents are as long as mesh
        public ShapeKeyMorphSO basisSO;


	    #region "delegations & tmp_data"
	    // "delegations" 

        private static Func<Vector3, Vector3, float, Vector3> dele_v3lerp = Vector3.Lerp;
        //private static Func<Vector4, Vector4, float, Vector4> dele_v4lerp = Vector4.Lerp;
        private static Func<Vector3, Vector3, Vector3> dele_v3sub = SubV3;
        //private static Func<Vector4, Vector4, Vector4> dele_v4sub = SubV4;
        private static List<Vector3> emptyV3Lst = new List<Vector3>();
        //private static List<Vector4> emptyV4Lst = new List<Vector4>();
        private static List<int> emptyIntLst = new List<int>();


        public delegate void ShapeKeyModifyMesh(ShapeKeyMorphSO basisSO);
        public static event ShapeKeyModifyMesh evtShapeKeyModifyMesh;

	    #endregion "delegations"
        


        #endregion "data"

	    #region "public method"
        // public method

        /// <summary>
        /// used to init temp var
        /// </summary>
        public static ShapeKeyDataDiff TempVarNew(ShapeKeyMorphSO basis)
        {
            var d = new ShapeKeyDataDiff();

            d.weight = 0;
            d.basisSO = basis;
            d.isFullData = false;

            d.vertices = new List<Vector3>();
            d.normals = new List<Vector3>();
            d.tangents = new List<Vector4>();

            d.indV = new List<int>();
            d.indN = new List<int>();
            d.indT = new List<int>();

            return d;
        }

        /// <summary>
        /// used to init real data
        /// </summary>
        public static ShapeKeyDataDiff New(ShapeKeyMorphSO basis, float weight, bool isFullData)
        {
            var d = new ShapeKeyDataDiff();
            d.weight = weight;
            d.basisSO = basis;
            d.isFullData = isFullData;

            d.vertices = new List<Vector3>();
            d.normals = new List<Vector3>();
            d.tangents = new List<Vector4>();
            d.indV = new List<int>();
            d.indN = new List<int>();
            d.indT = new List<int>();
            if (!isFullData)
            {
                d.UpdateDiffDataWithCurrentMesh();
            }
            else
            {
                d.SetFullData();
            }

            return d;
        }

        public void UpdateDiffDataWithCurrentMesh()
        {
            ShapeKeyDataDiff basisData = basisSO.GetShapeKeyDataDiff(0);
            Mesh m = basisSO.GetMesh();

            int nonMatchV = 0;
            //int nonMatchN = 0;
            //int nonMatchT = 0;

            // v
            {
                List<Vector3> basisVertices = basisData.vertices;
                Vector3[] meshVertices = m.vertices;
                Dbg.Assert(basisVertices.Count == meshVertices.Length, "ShapeKeyDataDiff.UpdateDiffDataWithCurrentMesh: length non match: basisVertices != meshVertices: {0}", m.name);
                vertices.Clear();
                indV.Clear();
                for (int i = 0; i < basisVertices.Count; ++i)
                {
                    Vector3 basisPos = basisVertices[i];
                    Vector3 meshPos = meshVertices[i];
                    if (!(basisPos == meshPos))
                    {
                        vertices.Add(meshPos);
                        indV.Add(i);
                        ++nonMatchV;
                    }
                }
            }

            //// n
            //{
            //    List<Vector3> basisNormals = basisData.normals;
            //    Vector3[] meshNormals = m.normals;
            //    Dbg.Assert(basisNormals.Count == meshNormals.Length, "ShapeKeyDataDiff.UpdateDiffDataWithCurrentMesh: length non match: basisNormal != meshNormals: {0}", m.name);
            //    normals.Clear();
            //    indN.Clear();
            //    for(int i=0; i<basisNormals.Count; ++i)
            //    {
            //        Vector3 basisN = basisNormals[i];
            //        Vector3 meshN = meshNormals[i];
            //        if( ! (basisN == meshN) )
            //        {
            //            normals.Add(meshN);
            //            indN.Add(i);
            //            ++nonMatchN;
            //        }
            //    }
            //}

            //// t
            //{
            //    List<Vector4> basisTangents = basisData.tangents;
            //    Vector4[] meshTangents = m.tangents;
            //    Dbg.Assert(basisTangents.Count == meshTangents.Length, "ShapeKeyDataDiff.UpdateDiffDataWithCurrentMesh: length non match: basisTangents != meshTangents: {0}", m.name);
            //    tangents.Clear();
            //    indT.Clear();
            //    for (int i = 0; i < basisTangents.Count; ++i)
            //    {
            //        Vector4 basisT = basisTangents[i];
            //        Vector4 meshT = meshTangents[i];
            //        if (!(basisT == meshT))
            //        {
            //            tangents.Add(meshT);
            //            indT.Add(i);
            //            ++nonMatchT;
            //        }
            //    }
            //}

            //Dbg.Log("ShapeKeyDataDiff.UpdateDiffDataWithCurrentMesh: V: {0}, N: {1}, T:{2}", nonMatchV, nonMatchN, nonMatchT);
        }

        public void SetFullData()
        {
            Mesh m = basisSO.GetMesh();

            vertices.Clear();
            vertices.AddRange(m.vertices);
            for (int i = 0; i < vertices.Count; ++i )
                indV.Add(i);

            //normals.Clear();
            //normals.AddRange(m.normals);
            //for (int i = 0; i < normals.Count; ++i)
            //    indN.Add(i);

            //tangents.Clear();
            //tangents.AddRange(m.tangents);
            //for (int i = 0; i < tangents.Count; ++i)
            //    indT.Add(i);
        } 
        

        public void ResetEmptyDiff()
        {
            vertices.Clear();
            indV.Clear();

            //normals.Clear();
            //indN.Clear();

            //tangents.Clear();
            //indT.Clear();            
        }

        public void Copy(ShapeKeyDataDiff rhs)
        {
            ResetEmptyDiff();

            this.vertices.AddRange(rhs.vertices);
            this.indV.AddRange(rhs.indV);

            //this.normals.AddRange(rhs.normals);
            //this.indN.AddRange(rhs.indN);

            //this.tangents.AddRange(rhs.tangents);
            //this.indT.AddRange(rhs.indT);

            this.weight = rhs.weight;
            this.isFullData = rhs.isFullData;

            Dbg.Assert(this.basisSO == rhs.basisSO, "ShapeKeyDataDiff.Copy: non-matched basisSO");
        }

        // Lerp [THIS FUNC is PAINPOINT of PERFORMANCE!]
        public static void Lerp(ShapeKeyDataDiff lhs, ShapeKeyDataDiff rhs, float leftWeight, float targetWeight, ShapeKeyDataDiff outData)
        {
            ShapeKeyDataDiff basisDiff = lhs.basisSO.GetShapeKeyDataDiff(0);

            float t = Mathf.InverseLerp(leftWeight, rhs.weight, targetWeight);

            outData.ResetEmptyDiff();

            bool lhsIsBasis = (lhs.isFullData);

            { //v
                var basisD = basisDiff.vertices; //data
                var outD = outData.vertices; //data
                var outInd = outData.indV; //ind
                var lhsD = lhsIsBasis ? emptyV3Lst : lhs.vertices;//data //[IMPORTANT OPTIMIZATION!] use emptyList to prevent running through whole mesh
                var lhsInd = lhsIsBasis ? emptyIntLst : lhs.indV; //ind
                var rhsD = rhs.vertices; //data
                var rhsInd = rhs.indV; //ind

                _LerpImpl(t, basisD,
                    outD, outInd,
                    lhsD, lhsInd,
                    rhsD, rhsInd,
                    dele_v3lerp
                    );
            }


            //{ // n
            //    var basisD = basisDiff.normals; //data
            //    var outD = outData.normals; //data
            //    var outInd = outData.indN; //ind
            //    var lhsD = lhsIsBasis ? emptyV3Lst : lhs.normals;//data
            //    var lhsInd = lhsIsBasis ? emptyIntLst : lhs.indN; //ind
            //    var rhsD = rhs.normals; //data
            //    var rhsInd = rhs.indN; //ind

            //    _LerpImpl(t, basisD,
            //        outD, outInd,
            //        lhsD, lhsInd,
            //        rhsD, rhsInd,
            //        dele_v3lerp
            //        );
            //}

            //{ // t
            //    var basisD = basisDiff.tangents; //data
            //    var outD = outData.tangents; //data
            //    var outInd = outData.indT; //ind
            //    var lhsD = lhsIsBasis ? emptyV4Lst : lhs.tangents;//data
            //    var lhsInd = lhsIsBasis ? emptyIntLst : lhs.indT; //ind
            //    var rhsD = rhs.tangents; //data
            //    var rhsInd = rhs.indT; //ind

            //    _LerpImpl(t, basisD,
            //        outD, outInd,
            //        lhsD, lhsInd,
            //        rhsD, rhsInd,
            //        dele_v4lerp
            //        );
            //}
            
        }

        /// <summary>
        /// outData = lhs - rhs;
        /// </summary>
        public static void Subtract(ShapeKeyDataDiff lhs, ShapeKeyDataDiff rhs, ShapeKeyDataDiff outData)
        {
            ShapeKeyDataDiff basisDiff = lhs.basisSO.GetShapeKeyDataDiff(0);

            outData.ResetEmptyDiff();

            { //v
                var basisD = basisDiff.vertices; //data
                var outD = outData.vertices; //data
                var outInd = outData.indV; //ind
                var lhsD = lhs.vertices;//data
                var lhsInd = lhs.indV; //ind
                var rhsD = rhs.vertices; //data
                var rhsInd = rhs.indV; //ind

                _SubtractImpl(basisD, outD, outInd, 
                    lhsD, lhsInd, 
                    rhsD, rhsInd,
                    dele_v3sub
                    );
            }


            //{ // n
            //    var basisD = basisDiff.normals; //data
            //    var outD = outData.normals; //data
            //    var outInd = outData.indN; //ind
            //    var lhsD = lhs.normals;//data
            //    var lhsInd = lhs.indN; //ind
            //    var rhsD = rhs.normals; //data
            //    var rhsInd = rhs.indN; //ind

            //    _SubtractImpl(basisD, outD, outInd,
            //        lhsD, lhsInd,
            //        rhsD, rhsInd,
            //        dele_v3sub
            //        );
            //}

            //{ // t
            //    var basisD = basisDiff.tangents; //data
            //    var outD = outData.tangents; //data
            //    var outInd = outData.indT; //ind
            //    var lhsD = lhs.tangents;//data
            //    var lhsInd = lhs.indT; //ind
            //    var rhsD = rhs.tangents; //data
            //    var rhsInd = rhs.indT; //ind

            //    _SubtractImpl(basisD, outD, outInd,
            //        lhsD, lhsInd,
            //        rhsD, rhsInd,
            //        dele_v4sub
            //        );
            //}
        }

        /// <summary>
        /// apply to mesh directly, not add to mesh current data
        /// </summary>
        public void ApplyToMesh(Mesh m, MeshCacheRT cache)
        {
            { // v
                int vcnt = indV.Count;
                Vector3[] newVerts = cache.GetVertices(m); 
                for (int i = 0; i < vcnt; ++i)
                {
                    int vidx = indV[i];
                    newVerts[vidx] = vertices[i]; // = not +=
                }
                cache.SetVertices(m, newVerts);
            }

            //{ // n
            //    int ncnt = indN.Count;
            //    Vector3[] newNormals = cache.GetNormals(m);
            //    for (int i = 0; i < ncnt; ++i)
            //    {
            //        int nidx = indN[i];
            //        newNormals[nidx] = normals[i];
            //    }
            //    cache.SetNormals(m, newNormals);
            //}

            //{ // t
            //    int tcnt = indT.Count;
            //    Vector4[] newTangents = cache.GetTangents(m);
            //    for (int i = 0; i < tcnt; ++i)
            //    {
            //        int tidx = indT[i];
            //        newTangents[tidx] = tangents[i];
            //    }
            //    cache.SetTangents(m, newTangents);
            //}

            if (evtShapeKeyModifyMesh != null)
                evtShapeKeyModifyMesh(basisSO);
        }

        public void ApplyToMesh(Mesh m)
        {
            { // v
                int vcnt = indV.Count;
                Vector3[] newVerts = m.vertices;
                for (int i = 0; i < vcnt; ++i)
                {
                    int vidx = indV[i];
                    newVerts[vidx] = vertices[i]; // = not +=
                }
                m.vertices = newVerts;
            }

            //{ // n
            //    int ncnt = indN.Count;
            //    Vector3[] newNormals = m.normals;
            //    for (int i = 0; i < ncnt; ++i)
            //    {
            //        int nidx = indN[i];
            //        newNormals[nidx] = normals[i];
            //    }
            //    m.normals = newNormals;
            //}

            //{ // t
            //    int tcnt = indT.Count;
            //    Vector4[] newTangents = m.tangents;
            //    for (int i = 0; i < tcnt; ++i)
            //    {
            //        int tidx = indT[i];
            //        newTangents[tidx] = tangents[i];
            //    }
            //    m.tangents = newTangents;
            //}

            if (evtShapeKeyModifyMesh != null)
                evtShapeKeyModifyMesh(basisSO);
        }

        /// <summary>
        /// ApplyToMeshAsSubtract, with MeshCache version,
        /// </summary>
        public void ApplyToMeshAsSubtract(Mesh m, MeshCacheRT cache)
        {
            // vertices
            {
                int vcnt = indV.Count;
                Vector3[] newVerts = cache.GetVertices(m);
                for (int i = 0; i < vcnt; ++i)
                {
                    int vidx = indV[i];
                    newVerts[vidx] += vertices[i];
                }
                cache.SetVertices(m, newVerts);
            }

            //// normals
            //{
            //    int ncnt = indN.Count;
            //    Vector3[] newNormals = cache.GetNormals(m);
            //    for (int i = 0; i < ncnt; ++i)
            //    {
            //        int nidx = indN[i];
            //        newNormals[nidx] += normals[i];
            //    }
            //    cache.SetNormals(m, newNormals);
            //}

            //// tangents
            //{
            //    int tcnt = indT.Count;
            //    Vector4[] newTangents = cache.GetTangents(m);
            //    for (int i = 0; i < tcnt; ++i)
            //    {
            //        int tidx = indT[i];
            //        newTangents[tidx] += tangents[i];
            //    }
            //    cache.SetTangents(m, newTangents);
            //}

            if (evtShapeKeyModifyMesh != null)
                evtShapeKeyModifyMesh(basisSO);
        }

        /// <summary>
        /// add the data onto mesh's current data
        /// </summary>
        public void ApplyToMeshAsSubtract(Mesh m)
        {
            // vertices
            Vector3[] newVerts = m.vertices;
            {
                int vcnt = indV.Count;
                for (int i = 0; i < vcnt; ++i)
                {
                    int vidx = indV[i];
                    newVerts[vidx] += vertices[i];
                }
                m.vertices = newVerts;
            }            

            //// normals
            //{
            //    int ncnt = indN.Count;
            //    Vector3[] newNormals = m.normals;
            //    if (newNormals.Length == newVerts.Length)
            //    {
            //        for (int i = 0; i < ncnt; ++i)
            //        {
            //            int nidx = indN[i];
            //            newNormals[nidx] += normals[i];
            //        }
            //        m.normals = newNormals;
            //    }
            //}

            //// tangents
            //{
            //    int tcnt = indT.Count;
            //    Vector4[] newTangents = m.tangents;
            //    if (newTangents.Length == newVerts.Length)
            //    {
            //        for (int i = 0; i < tcnt; ++i)
            //        {
            //            int tidx = indT[i];
            //            newTangents[tidx] += tangents[i];
            //        }
            //        m.tangents = newTangents;
            //    }
            //}
            
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private ShapeKeyDataDiff(){}

        private static void _LerpImpl<T>(float t, List<T> basisD,
            List<T> outD, List<int> outInd,
            List<T> lhsD, List<int> lhsInd,
            List<T> rhsD, List<int> rhsInd,
            Func<T, T, float, T> lerpFunc
            )
        {
            // loop through lhsInd & rhsInd
            T data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(lerpFunc(data1, data2, t));
                outInd.Add(vidx);
            }
        }

        private static void _LerpImpl_V3(float t, List<Vector3> basisD,
            List<Vector3> outD, List<int> outInd,
            List<Vector3> lhsD, List<int> lhsInd,
            List<Vector3> rhsD, List<int> rhsInd
            )
        {
            // loop through lhsInd & rhsInd
            Vector3 data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(Vector3.Lerp(data1, data2, t));
                outInd.Add(vidx);
            }
        }

        private static void _LerpImpl_V4(float t, List<Vector4> basisD,
            List<Vector4> outD, List<int> outInd,
            List<Vector4> lhsD, List<int> lhsInd,
            List<Vector4> rhsD, List<int> rhsInd
            )
        {
            // loop through lhsInd & rhsInd
            Vector4 data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(Vector4.Lerp(data1, data2, t));
                outInd.Add(vidx);
            }
        }

        private static void _SubtractImpl<T>(List<T> basisD, List<T> outD, List<int> outInd,
            List<T> lhsD, List<int> lhsInd,
            List<T> rhsD, List<int> rhsInd,
            Func<T, T, T> subFunc
            )
        {
            // loop through lhsInd & rhsInd
            T data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(subFunc(data1, data2));
                outInd.Add(vidx);
            }
        }

        private static void _SubtractImpl_V3(List<Vector3> basisD, List<Vector3> outD, List<int> outInd,
            List<Vector3> lhsD, List<int> lhsInd,
            List<Vector3> rhsD, List<int> rhsInd
            )
        {
            // loop through lhsInd & rhsInd
            Vector3 data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(data1 - data2);
                outInd.Add(vidx);
            }
        }

        private static void _SubtractImpl_V4(List<Vector4> basisD, List<Vector4> outD, List<int> outInd,
            List<Vector4> lhsD, List<int> lhsInd,
            List<Vector4> rhsD, List<int> rhsInd
            )
        {
            // loop through lhsInd & rhsInd
            Vector4 data1, data2;
            for (int li = 0, ri = 0; li < lhsInd.Count || ri < rhsInd.Count; )
            {
                int vidx = -1; //the index to basis array
                int lindval = li < lhsInd.Count ? lhsInd[li] : int.MaxValue;
                int rindval = ri < rhsInd.Count ? rhsInd[ri] : int.MaxValue;
                if (lindval < rindval)
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = basisD[vidx];
                    ++li;
                }
                else if (lindval > rindval)
                {
                    vidx = rindval;
                    data1 = basisD[vidx];
                    data2 = rhsD[ri];
                    ++ri;
                }
                else
                {
                    vidx = lindval;
                    data1 = lhsD[li];
                    data2 = rhsD[ri];
                    ++li; ++ri;
                }

                outD.Add(data1 - data2);
                outInd.Add(vidx);
            }
        }

        private static Vector3 SubV3(Vector3 lhs, Vector3 rhs)
        {
            return lhs - rhs;
        }

        private static Vector4 SubV4(Vector4 lhs, Vector4 rhs)
        {
            return lhs - rhs;
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
    


    }
}
}
