using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshEditor
{
    using RVLst = System.Collections.Generic.List<int>;
    using RTriCont = System.Collections.Generic.List<int>;

    using VELst = System.Collections.Generic.List<VEdge>;
    using VVertCont = System.Collections.Generic.Dictionary<int, VVert>; // real-vert idx -> virtual vert 
    using VEdgeCont = System.Collections.Generic.HashSet<VEdge>; //all virtual edges
    using VFaceCont = System.Collections.Generic.Dictionary<int, VFace>; //real-tri idx -> virtural face( tris or quads )

    using DegradeRTriCont = System.Collections.Generic.HashSet<int>; //all degraded real-tri

    using VVLst = System.Collections.Generic.List<VVert>;
    using VFLst = System.Collections.Generic.List<VFace>;

    /// <summary>
    /// VMesh represents "virtual-mesh",
    /// We will pre-process a mesh to generate VMesh, which:
    /// 1. merge verts at same location to make VVert; provide double-mapping realVerts《=》VVert
    /// 2. re-construct VEdge info based on VVert
    /// 3. create Quad;
    /// </summary>
	public class VMesh 
	{
	    #region "data"
        // data

        private static VMesh sm_Instance;

        private EditableMesh m_EditMesh;
        private Mesh m_RealMesh;

        private VVertCont m_VVertCont;
        private VEdgeCont m_VEdgeCont;
        private VFaceCont m_VFaceCont;

        private VVert[] m_VVertArr;
        private VEdge[] m_VEdgeArr; //the original vedges
        private VEdge[] m_VActiveEdgeArr; //the edges that survived "quadralization"

        private DegradeRTriCont m_DegRTriCont;

        private VVLst m_tmpVVLst = new VVLst();

        #endregion "data"

	    #region "public method"
        // public method

        public static VMesh Instance
        {
            get {
                Dbg.Assert(sm_Instance != null, "VMesh.Instance: the instance is null");
                return sm_Instance; 
            }
        }

        public void Init(EditableMesh m)
        {
            sm_Instance = this;

            m_EditMesh = m;
            m_RealMesh = m_EditMesh.mesh;

            //ETimeProf prof = new ETimeProf();
            m_DegRTriCont = new DegradeRTriCont();

            // map VV <-> RV
            m_VVertCont = new VVertCont();
            _InitVVertTable();

            //prof.Click("VMesh.Init.VVertTable:");

            // establish VEdge table
            m_VEdgeCont = new VEdgeCont();
            _InitVEdgeTable();
            //prof.Click("VMesh.Init.VEdgeTable:");

            // establish VQuad table
            m_VFaceCont = new VFaceCont();
            _InitVFaceTable();
            //prof.Click("VMesh.Init.VQuadTable:");
        }

        public void Fini()
        {
            sm_Instance = null;
        }

        /// <summary>
        /// given a real vert, return the virtual vert
        /// </summary>
        public VVert GetVV(int rvIdx)
        {
            Dbg.Assert(m_VVertCont.ContainsKey(rvIdx), "VMesh.GetVV: real vert idx: {0}", rvIdx);
            VVert vvert = m_VVertCont[rvIdx];
            return vvert;
        }

        /// <summary>
        /// given a real vert idx,
        /// return all real verts under same virtual vert
        /// </summary>
        public RVLst GetRVertsFromRVert(int rvIdx)
        {
            VVert vvert = GetVV(rvIdx);
            return vvert.GetRVerts();
        }

        /// <summary>
        /// given a real vert idx,
        /// return all virtual edges connected to this vert
        /// </summary>
        public VELst GetVEdgesFromRVert(int rvIdx)
        {
            VVert vvert = GetVV(rvIdx);
            return vvert.GetVEdges();
        }

        /// <summary>
        /// given a real-tri idx,
        /// return the 3 virtual verts belonging to this tri
        /// [BE WARNED: there're chances that the vverts are not three distinct verts, if the rtri is degraded into edge or point]
        /// </summary>
        public void GetVVertsFromRTri(int rtriIdx, out VVert vv0, out VVert vv1, out VVert vv2)
        {
            MeshCache cache = MeshCache.Instance;
            int[] tris = cache.triangles;

            int rvIdx0 = tris[rtriIdx * 3];
            int rvIdx1 = tris[rtriIdx * 3 + 1];
            int rvIdx2 = tris[rtriIdx * 3 + 2];

            vv0 = GetVV(rvIdx0);
            vv1 = GetVV(rvIdx1);
            vv2 = GetVV(rvIdx2);
        }
        public void GetVVertsFromRTri(int rtriIdx, List<VVert> vvs)
        {
            MeshCache cache = MeshCache.Instance;
            int[] tris = cache.triangles;

            for(int i=0; i<3; ++i)
            {
                int rvIdx = tris[rtriIdx * 3 + i];
                VVert vv = GetVV(rvIdx);
                if(!vvs.Contains(vv))
                {
                    vvs.Add(vv);
                }
            }
        }

        /// <summary>
        /// rTriIdx might be degraded rtri, but we can get the VFace to which it belongs anyway
        /// </summary>
        public VFace GetVFaceFromRTri(int rTriIdx, RaycastHit hit)
        {
            VFace vf = null;
            if( m_VFaceCont.TryGetValue(rTriIdx, out vf) )
            {
                return vf;
            }
            else
            {
                Dbg.Assert(m_DegRTriCont.Contains(rTriIdx), "VMesh.GetVFaceFromRTri: not in m_VFaceCont or degradedTriCont!?");

                m_tmpVVLst.Clear();
                GetVVertsFromRTri(rTriIdx, m_tmpVVLst);

                float minDist = float.MaxValue;
                VVert retVV = null;
                for( int i=0; i<m_tmpVVLst.Count; ++i)
                {
                    VVert vv = m_tmpVVLst[i];
                    float dist = Vector3.Distance(hit.point, vv.GetWorldPos());
                    if( dist < minDist )
                    {
                        minDist = dist;
                        retVV = vv;
                    }
                }

                Dbg.Assert(retVV != null, "VMesh.GetVFaceFromRTri: no VVert for given rtri!? {0}", rTriIdx);

                VFLst vfLst = retVV.GetAllVFaces();
                Dbg.Assert(vfLst.Count > 0, "VMesh.GetVFaceFromRTri: the vvert has no VFace, {0}", retVV);

                return vfLst[0];
            }
        }

        /// <summary>
        /// assume the rTriIdx is not degraded
        /// </summary>
        public VFace GetVFaceFromRTri(int rTriIdx)
        {
            VFace vf = null;
            if( m_VFaceCont.TryGetValue(rTriIdx, out vf) )
            {
                return vf;
            }
            else
            {
                Dbg.LogErr("VMesh.GetVFaceFromRTri: the rTriIdx is not found in m_VFaceCont, {0}", rTriIdx);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VVert[] GetAllVVerts()
        {
            return m_VVertArr;
        }

        /// <summary>
        /// 
        /// </summary>
        public VEdge[] GetAllVEdges()
        {
            return m_VEdgeArr;
        }

        /// <summary>
        /// get all VEdges that belong to quad
        /// </summary>
        public VEdge[] GetAllVActiveEdges()
        {
            return m_VActiveEdgeArr;
        }

        #endregion "public method"

	    #region "private method"
        // private method

	    #region "VVert table init"
	    // "VVert table init" 

        private void _InitVVertTable()
        {
            Vector3[] verts = m_RealMesh.vertices;
            List<_VertUnit> vunits = verts.Select((v, idx) => { return new _VertUnit(v, idx); }).ToList();
            vunits.Sort(_VertUnit.Compare);

            m_VVertCont.Clear();
            if (vunits.Count == 0)
                return;

            // init vVertCont
            List<VVert> vvertLst = new List<VVert>();

            float sqrDistThres = DEF_SAME_VERT_DIST_THRES * DEF_SAME_VERT_DIST_THRES;
            _VertUnit guard = vunits[0];
            VVert guardVV = _ForceGetVVert(guard);
            vvertLst.Add(guardVV);

            for (int i = 1; i < vunits.Count; ++i)
            {
                _VertUnit one = vunits[i];
                Vector3 diff = guard.pos - one.pos;

                if (Vector3.SqrMagnitude(diff) < sqrDistThres)
                {
                    _AddToVVert(one.idx, guardVV);
                }
                else
                {
                    guard = one; //advance guard to 'one'
                    guardVV = _ForceGetVVert(guard);
                    vvertLst.Add(guardVV);
                }
            }

            // init m_VVertArr
            m_VVertArr = vvertLst.ToArray();
        }

        private void _AddToVVert(int rvidx, VVert vvert)
        {
            Dbg.Assert(!m_VVertCont.ContainsKey(rvidx), "VMesh._AddToVVert: rvIdx already in m_VVertCont: {0}", rvidx);
            Dbg.Assert(!vvert.Contains(rvidx), "VMesh._AddToVVert: rvIdx already in vvert: {0}", rvidx);

            m_VVertCont.Add(rvidx, vvert);
            vvert.AddRVert(rvidx);
        }

        private VVert _ForceGetVVert(_VertUnit v)
        {
            int idx = v.idx;
            VVert vvert = null;

            if (!m_VVertCont.TryGetValue(idx, out vvert))
            {
                vvert = new VVert(idx);
                m_VVertCont.Add(idx, vvert);
            }
            return vvert;
        }
	
	    #endregion "VVert table init"
        
	    #region "VEdge table init"
	    // "VEdge cont init" 


        /// <summary>
        /// [BE WARNED: the degraded triangle case, the triangle might degrade into an edge or a point]
        /// 
        /// 
        /// </summary>
        private void _InitVEdgeTable()
        {
            int[] rvIdxs = m_RealMesh.triangles;
            int triCnt = rvIdxs.Length / 3;

            //Dbg.Log("triCnt = {0}", triCnt);
            //ETimeProf prof = new ETimeProf();

            // loop over all real-tris, and generate VEdges
            for(int triIdx=0; triIdx<triCnt; triIdx++ )
            {
                //prof.SecStart(0);
                int rv0 = rvIdxs[triIdx*3];
                int rv1 = rvIdxs[triIdx*3 + 1];
                int rv2 = rvIdxs[triIdx*3 + 2];

                VVert vv0 = GetVV(rv0);
                VVert vv1 = GetVV(rv1);
                VVert vv2 = GetVV(rv2);

                bool bDegradedTri = (vv0 == vv1) || (vv1 == vv2) || (vv0 == vv2);
                if( bDegradedTri )
                    m_DegRTriCont.Add(triIdx); //record the degraded real-tri

                //prof.SecEnd(0);

                if( vv0 != vv1 )
                {
                    VEdge vedge = null;
                    //prof.SecStart(1);
                    if ((vedge = vv0.GetVEdge(vv1)) == null)
                    {
                        vedge = _CreateNewVEdge(vv0, vv1);
                    }
                    Dbg.Assert(vedge != null, "VMesh._InitVEdgeTable: vedge is null");

                    if( !bDegradedTri )
                        vedge.AddRTri(triIdx);
                    //prof.SecEnd(1);
                }
                
                if( vv1 != vv2 )
                {
                    VEdge vedge = null;
                    //prof.SecStart(2);
                    if ((vedge = vv1.GetVEdge(vv2)) == null)
                    {
                        vedge = _CreateNewVEdge(vv1, vv2);
                    }
                    if( !bDegradedTri )
                        vedge.AddRTri(triIdx);
                    //prof.SecEnd(2);
                }
                
                if( vv0 != vv2 )
                {
                    VEdge vedge = null;
                    //prof.SecStart(3);
                    if ((vedge = vv2.GetVEdge(vv0)) == null)
                    {
                        vedge = _CreateNewVEdge(vv2, vv0);
                    }
                    if( !bDegradedTri )
                        vedge.AddRTri(triIdx);
                    //prof.SecEnd(3);
                }

            } // end of for

            m_VEdgeArr = m_VEdgeCont.ToArray();
            //prof.SecShowAll();

        }

        private VEdge _CreateNewVEdge(VVert vv0, VVert vv1)
        {
            // new
            VEdge e = new VEdge(vv0, vv1);

            // add to vv0
            vv0.AddVEdge(e);

            // add to vv1
            vv1.AddVEdge(e);

            // add to vedgeCont
            m_VEdgeCont.Add(e);

            return e;
        }
	
	    #endregion "VEdge table init"
        
	    #region "VQuad table init"
	    // "VQuad table init" 

        /// <summary>
        /// this method will try to make VMesh from tri-mesh into hybrid tri-quad-mesh,
        /// it will mark some vedges as "ActiveEdge" which will be used,
        /// 
        /// the result will be used in:
        /// * edge marker,
        /// * edge loop
        /// * etc
        /// 
        /// [BE WARNED]: 
        /// </summary>
        private void _InitVFaceTable()
        {
            //////////////////////////////////////////////////
            // 1. foreach vedge of the vmesh
            //       if vedge has and only has 2 tris
            //          calc a score and store in sorted_vEdges
            // 2. foreach vedge in sorted_vEdges
            //       if the 2 tris not be 'marked' yet, then 
            //          take it as the 'non-quad' edge;
            //          mark the 2 tris;
            // 3. get all active-edge into m_VActiveEdgeArr
            //   3.1 call all vvert, to make activeVEdge
            // 4. prepare the VFaceCont:
            //        create the rTriIdx <=> VFace first;
            //        merge VFace with those markedTris;
            // 5. process the degraded r-tris, assign them to some good VFace (so we can handle click-selection)
            //////////////////////////////////////////////////

            int totalTriCnt = MeshCache.Instance.triangles.Length / 3;
            VVert[] tmpVertArr = new VVert[4];

            // step 1
            SortedDictionary<_ScoredEdge, bool> sortedVEdges = new SortedDictionary<_ScoredEdge, bool>();
            foreach( var vedge in m_VEdgeCont )
            {
                if (vedge.GetRTriCount() != 2)
                    continue;

                RTriCont rtris = vedge.GetRTris();
                _GetAsQuadVerts(rtris[0], rtris[1], vedge, tmpVertArr);

                float score = _CalcQuadScore(tmpVertArr); // calc score for the quad-to-be
                sortedVEdges.Add(new _ScoredEdge(score, vedge), false);
            }

            // step 2
            //int cnt = 0;
            Dictionary<int, int> markedTris = new Dictionary<int, int>();
            for( var ie = sortedVEdges.GetEnumerator(); ie.MoveNext(); )
            {
                var vedge = ie.Current.Key.vEdge;
                //if (cnt < 100)
                    //Dbg.Log("sortedVEdges: {0}: {1}, score:{2}", cnt++, vedge, ie.Current.Key.score);
                RTriCont triCont = vedge.GetRTris();
                Dbg.Assert(triCont.Count == 2, "VMesh._InitVFaceTable: the tri-cont count of vEdge != 2");
                if(!markedTris.ContainsKey(triCont[0]) && !markedTris.ContainsKey(triCont[1]))
                {
                    //Dbg.Log("Add markedTris: {0}<=>{1}", triCont[0], triCont[1]);
                    markedTris.Add(triCont[0], triCont[1]);
                    markedTris.Add(triCont[1], triCont[0]);
                    vedge.IsActiveEdge = false;

                    if (markedTris.Count >= totalTriCnt)
                        break; // all tris covered, no need to loop on
                }
            }

            // step 3
            List<VEdge> quadEdges = new List<VEdge>();
            for(int i=0; i<m_VEdgeArr.Length; ++i)
            {
                if (m_VEdgeArr[i].IsActiveEdge)
                    quadEdges.Add(m_VEdgeArr[i]);
            }
            m_VActiveEdgeArr = quadEdges.ToArray();
            
            //step 3.1
            for(int i=0; i<m_VVertArr.Length; ++i)
            {
                m_VVertArr[i].CreateActiveVEdgesList();
            }

            // step 4
            int[] rTriIdxs = MeshCache.Instance.triangles;
            int rTriCnt = rTriIdxs.Length / 3;
            for(int i=0; i<rTriCnt; ++i)
            {
                VFace vFace = new VFace();
                vFace.AddRTri(i);

                m_VFaceCont.Add(i, vFace);
            }

            for ( var ie = markedTris.GetEnumerator(); ie.MoveNext(); )
            {
                int rTriIdx0 = ie.Current.Key;
                int rTriIdx1 = ie.Current.Value;

                if (rTriIdx0 > rTriIdx1)
                    continue;

                VFace vf0 = m_VFaceCont[rTriIdx0];
                VFace vf1 = m_VFaceCont[rTriIdx1];

                _MergeVFace(vf0, vf1);
            }
            markedTris.Clear();

            // step 5
            _MapVVertToVFaces();
        }

        /// <summary>
        /// tell each VVert which VFace it's in
        /// </summary>
        private void _MapVVertToVFaces()
        {
            // 
            HashSet<VFace> faces = new HashSet<VFace>();
            for(var ie = m_VFaceCont.GetEnumerator(); ie.MoveNext(); )
            {
                VFace f = ie.Current.Value;
                faces.Add(f);
            }

            for(var ie = faces.GetEnumerator(); ie.MoveNext(); )
            {
                VFace f = ie.Current;
                List<VVert> vvLst = f.GetVVerts();
                foreach(VVert vv in vvLst)
                {
                    vv.AddVFace(f);
                }
            }
        }

        /// <summary>
        /// calculate quad's score with local position,
        /// so it will not be under less effect of the transform scale
        /// </summary>
        private float _CalcQuadScore(VVert[] tmpVertArr)
        {
            Vector3 pos0 = tmpVertArr[0].GetLocalPos();
            Vector3 pos1 = tmpVertArr[1].GetLocalPos();
            Vector3 pos2 = tmpVertArr[2].GetLocalPos();
            Vector3 pos3 = tmpVertArr[3].GetLocalPos();

            Vector3 seg01 = pos0 - pos1;
            Vector3 seg02 = pos0 - pos2;
            Vector3 seg31 = pos3 - pos1;
            Vector3 seg32 = pos3 - pos2;

            Vector3 seg01n = seg01.normalized;
            Vector3 seg02n = seg02.normalized;
            Vector3 seg31n = seg31.normalized;
            Vector3 seg32n = seg32.normalized;

            Vector3 seg12 = pos1 - pos2;

            float lenOff1;
            {
                float len03 = (pos0 - pos3).magnitude;
                float len12 = seg12.magnitude;
                float avgLen = (len03 + len12) * 0.5f;
                lenOff1 = Mathf.Abs(len03 - avgLen) / avgLen; // the offset from average length of cross lines
            }

            float angleMinus;
            {
                float c102 = Vector3.Cross(seg01n, seg02n).magnitude;
                float c132 = Vector3.Cross(seg31n, seg32n).magnitude;
                float c013 = Vector3.Cross(seg01n, seg31n).magnitude;
                float c023 = Vector3.Cross(seg02n, seg32n).magnitude;
                angleMinus = 4f - c102 - c132 - c013 - c023;
            }

            float szTri0 = Vector3.Cross(seg01, seg02).magnitude * 0.5f;
            float szTri1 = Vector3.Cross(seg31, seg32).magnitude * 0.5f;
            Vector3 nor0 = Vector3.Cross(seg01n, seg02n).normalized;
            Vector3 nor1 = Vector3.Cross(seg32n, seg31n).normalized;

            float areaScore = (szTri0 + szTri1);
            float score = areaScore *Vector3.Dot(nor0, nor1);
            score -= lenOff1 * areaScore;
            score -= angleMinus * areaScore;

            //Dbg.Log("{0}<=>{1} ==> score: {2}", tmpVertArr[1].RepVert, tmpVertArr[2].RepVert, score);

            return score;
        }

        /// <summary>
        /// DEBUG only
        /// </summary>
        public static float CalcScore(int v0, int v1, int v2, int v3)
        {
            var inst = sm_Instance;
            VVert vv0 = inst.GetVV(v0);
            VVert vv1 = inst.GetVV(v1);
            VVert vv2 = inst.GetVV(v2);
            VVert vv3 = inst.GetVV(v3);

            return inst._CalcQuadScore(new VVert[] { vv0, vv1, vv2, vv3 });
        }

        /// <summary>
        /// calculate the size of the triangle, use the WORLD position of the verts,
        /// </summary>
        private float _CalcTriSize(Vector3 pos0, Vector3 pos1, Vector3 pos2)
        {
            Vector3 e0 = pos0 - pos1;
            Vector3 e1 = pos0 - pos2;

            float sz = Vector3.Cross(e0, e1).magnitude * 0.5f;
            return sz;
        }

        /// <summary>
        /// get the normal vector of a triangle
        /// </summary>
        private Vector3 _GetTriWorldNormal(Vector3 pos0, Vector3 pos1, Vector3 pos2)
        {
            Vector3 dir0 = pos0 - pos1;
            Vector3 dir1 = pos0 - pos2;

            Vector3 nor = Vector3.Cross(dir0, dir1).normalized;

            //Dbg.Log("nor: {0}, v0={1}, v1={2}, v2={3}", nor, wPos0, wPos1, wPos2);

            return nor;
        }

        /// <summary>
        /// given two real tri indices, 
        /// put the VVerts into the allocated 4-elem array, 
        /// 0 for triIdx0, 1-2 for vedge, 3 for triIdx1
        /// </summary>
        private void _GetAsQuadVerts(int triIdx0, int triIdx1, VEdge vedge, VVert[] tmpVertArr)
        {
            VVert ev0 = vedge.GetVVert(0);
            VVert ev1 = vedge.GetVVert(1);

            GetVVertsFromRTri(triIdx0, out tmpVertArr[0], out tmpVertArr[1], out tmpVertArr[2]);
            // put the non-edge vert at idx-0
            for(int specialVertIdx = 0; specialVertIdx < 3; ++specialVertIdx)
            {
                if( tmpVertArr[specialVertIdx] != ev0 && tmpVertArr[specialVertIdx] != ev1 )
                {
                    var tmp = tmpVertArr[0];
                    tmpVertArr[0] = tmpVertArr[specialVertIdx];
                    tmpVertArr[specialVertIdx] = tmp;
                    break;
                }
            }

            GetVVertsFromRTri(triIdx1, out tmpVertArr[1], out tmpVertArr[2], out tmpVertArr[3]);
            // put the non-edge vert at idx-3
            for (int specialVertIdx = 1; specialVertIdx < 4; ++specialVertIdx)
            {
                if (tmpVertArr[specialVertIdx] != ev0 && tmpVertArr[specialVertIdx] != ev1)
                {
                    var tmp = tmpVertArr[3];
                    tmpVertArr[3] = tmpVertArr[specialVertIdx];
                    tmpVertArr[specialVertIdx] = tmp;
                    break;
                }
            }
        }

        /// <summary>
        /// merge two VFaces into lhs, 
        /// modify other related data-structures as well
        /// </summary>
        private void _MergeVFace(VFace lhs, VFace rhs)
        {
            //int lhsTriIdx = lhs.GetRTriIdx(0);
            int rhsTriIdx = rhs.GetRTriIdx(0);
            m_VFaceCont[rhsTriIdx] = lhs; //link rhs's rTri to lhs

            // merge the data from rhs to lhs
            lhs.AddRTri(rhsTriIdx);
        }
	
	    #endregion "VQuad table init"
        

        #endregion "private method"

	    #region "Inner struct"
	    // "Inner struct" 

        class _VertUnit
        {
            public Vector3 pos;
            public int idx;

            public _VertUnit(Vector3 p, int i) { pos = p; idx = i; }
            public static int Compare(_VertUnit lhs, _VertUnit rhs)
            {
                Vector3 l = lhs.pos;
                Vector3 r = rhs.pos;
                if (l.x < r.x) return -1;
                else if (l.x > r.x) return 1;

                if (l.y < r.y) return -1;
                else if (l.y > r.y) return 1;

                if (l.z < r.z) return -1;
                else if (l.z > r.z) return 1;

                return 0;
            }
        }

        class _ScoredEdge : IComparable<_ScoredEdge>
        {
            public float score;
            public VEdge vEdge;

            public _ScoredEdge(float score, VEdge e)
            {
                this.score = score;
                this.vEdge = e;
            }

            // bigger at the front
            // else use the vert-id to sort
            public int CompareTo(_ScoredEdge rhs)
            {
                float r = (score - rhs.score);
                if( Mathf.Abs(r) < 0.000001f )
                {
                    return vEdge.CompareTo(rhs.vEdge);
                }
                else
                {
                    return Math.Sign(-r);
                }
            }
        }

	    #endregion "Inner struct"

	    #region "constant data"
        // constant data

        private const float DEF_SAME_VERT_DIST_THRES = 0.001f;

        #endregion "constant data"
	}
}
}
