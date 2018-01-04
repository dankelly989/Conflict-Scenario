using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshEditor
{
    using RVLst = System.Collections.Generic.List<int>;
    using VELst = System.Collections.Generic.List<VEdge>;
    using VFLst = System.Collections.Generic.List<VFace>;


    /// <summary>
    /// used to represent "virtual vert", it corresponds to a group of real-verts that cluster together.
    /// </summary>
	public class VVert : IComparable<VVert>
	{
	    #region "data"
        // data

        private RVLst m_rvLst; //the list of real vert's idx
        private VELst m_veLst; //the list of virtual edges connected to this vvert
        private VELst m_activeVELst; //the list of virtual edges connected to this vvert, and is active
        private VFLst m_vfLst; //the list of virtual face containing this vvert

        #endregion "data"

	    #region "public method"
        // public method

        public VVert(int rvidx)
        {
            m_rvLst = new RVLst();
            m_veLst = new VELst();
            m_activeVELst = new VELst();
            m_vfLst = new VFLst();

            AddRVert(rvidx); //make sure we VVert is not empty
        }

        /// <summary>
        /// get the local-position of the VVert
        /// </summary>
        public Vector3 GetLocalPos()
        {
            return MeshCache.Instance.vertices[m_rvLst[0]];
        }

        /// <summary>
        /// get the world-position of the VVert
        /// </summary>
        public Vector3 GetWorldPos()
        {
            Transform meshTr = MeshCache.Instance.GetTransform();
            return meshTr.TransformPoint(GetLocalPos());
        }

        public Vector3 GetLocalNormal()
        {
            return MeshCache.Instance.normals[m_rvLst[0]];
        }

        public Vector3 GetWorldNormal()
        {
            Transform meshTr = MeshCache.Instance.GetTransform();
            return meshTr.TransformDirection(GetLocalNormal());
        }

	    #region "vert"
	    // "vert" 

        /// <summary>
        /// how many real-vert in this virtual vertex
        /// </summary>
        public int RealVertCnt
        {
            get { return m_rvLst.Count; }
        }

        /// <summary>
        /// the representative real-vert for this VVert, i.e.: the smallest idx
        /// </summary>
        public int RepVert
        {
            get { return m_rvLst[0]; }
        }

        public RVLst GetRVerts()
        {
            return m_rvLst;
        }

        public void AddRVert(int rvIdx)
        {
            if (Contains(rvIdx))
                return;

            m_rvLst.Add(rvIdx);
            m_rvLst.Sort();
        }

        public bool Contains(int rvIdx)
        {
            return m_rvLst.Contains(rvIdx);
        }
	
	    #endregion "vert"

	    #region "edge"
	    // "edge" 

        public void AddVEdge(VEdge e)
        {
            Dbg.Assert(!m_veLst.Contains(e), "VVert.AddVEdge: the edge already in vvert: {0}", this.RepVert);
            m_veLst.Add(e);
        }

        /// <summary>
        /// return all virtual edge connected to 'this'
        /// </summary>
        public VELst GetVEdges()
        {
            return m_veLst;
        }

        public void CreateActiveVEdgesList()
        {
            for(int i=0; i<m_veLst.Count; ++i)
            {
                VEdge e = m_veLst[i];
                if( e.IsActiveEdge )
                {
                    m_activeVELst.Add(e);
                }
            }
        }

        public VELst GetActiveVEdges()
        {
            Dbg.Assert(m_activeVELst != null, "VVert.GetActiveVEdges: the activeVELst is null");
            return m_activeVELst;
        }

        /// <summary>
        /// try to find VEdge connected to 'this' and 'rhs'
        /// if not found, return null
        /// </summary>
        public VEdge GetVEdge(VVert rhs)
        {
            for(int i=0; i<m_veLst.Count; ++i)
            {
                VEdge e = m_veLst[i];
                if (e.Contains(rhs))
                    return e;
            }
            return null;
        }

        public VEdge CheckedGetVEdge(VVert rhs)
        {
            for (int i = 0; i < m_veLst.Count; ++i)
            {
                VEdge e = m_veLst[i];
                if (e.Contains(rhs))
                    return e;
            }

            Dbg.LogErr("VVerts.CheckedGetVEdge: failed to find an edge for: {0}=>{1}", this, rhs);
            return null;
        }
	
	    #endregion "edge"
        
	    #region "vface"
	    // "vface" 

        public void AddVFace(VFace vf)
        {
            m_vfLst.Add(vf);
        }

        public VFLst GetAllVFaces()
        {
            return m_vfLst;
        }
	
	    #endregion "vface"
        

	    #region "equal"
	    // "equal" 

        public override int GetHashCode()
        {
            return m_rvLst[0];
        }

        public override bool Equals(object obj)
        {
            VVert rhs = (VVert)obj;
            if( m_rvLst[0] == rhs.m_rvLst[0] )
            {
                Dbg.Assert(m_rvLst.Count == rhs.m_rvLst.Count, "VVert.Equals: rvLst length not matched: {0}, {1}", m_rvLst.Count, rhs.m_rvLst.Count);
                return true;
            }
            else
                return false;
        }
	
	    #endregion "equal"

	    #region "compare"
	    // "compare" 

        public int CompareTo(VVert rhs)
        {
            return m_rvLst[0] - rhs.m_rvLst[0];
        }
	
	    #endregion "compare"

        public override string ToString()
        {
            return m_rvLst[0].ToString();
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
}
