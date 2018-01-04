using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshEditor
{
    using RTriCont = System.Collections.Generic.List<int>; 

	public class VEdge : IComparable<VEdge>
	{
	    #region "data"
        // data

        private VVert[] m_VVerts;
        private RTriCont m_TriCont; //the triangle idxs which has this edge

        private bool m_IsQuadEdge; //if not, then this edge is not shown in quad mode

        #endregion "data"

	    #region "public method"
        // public method

        public VEdge(VVert v0, VVert v1)
        {
            // init the two verts
            m_VVerts = new VVert[2];
            int cmp = v0.CompareTo(v1);
            if( cmp < 0 )
            {
                m_VVerts[0] = v0;
                m_VVerts[1] = v1;
            }
            else if( cmp > 0)
            {
                m_VVerts[0] = v1;
                m_VVerts[1] = v0;
            }
            else
            {
                Dbg.LogErr("VEdge.ctor: v0 == v1: {0}, {1}", v0, v1);
            }

            // init the tri cont
            m_TriCont = new RTriCont();

            m_IsQuadEdge = true;
        }

        public bool IsActiveEdge
        {
            get { return m_IsQuadEdge; }
            set { m_IsQuadEdge = value; }
        }

        public bool Contains(VVert vvert)
        {
            return vvert.Equals(m_VVerts[0]) || vvert.Equals(m_VVerts[1]);
        }

        public VVert GetVVert(int idx)
        {
            return m_VVerts[idx];
        }

        public void SetVVert(int idx, VVert vvert)
        {
            m_VVerts[idx] = vvert;
        }

	    #region "triangles"
	    // "triangles" 

        public void AddRTri(int rtriIdx)
        {
            if( m_TriCont.Contains(rtriIdx) )
            {
                Dbg.LogWarn("VEdge.AddRTri: dense mesh: the VEdge has dup real tri: {0}, current:<{1}>", rtriIdx, Misc.ListToString(m_TriCont));

                // it's possible, when two verts of one rtri overlap, then the VEdge might represent two redge of one rtri.
                // so we just ignore it, don't report
                return;
            }
            m_TriCont.Add(rtriIdx);
        }

        public RTriCont GetRTris()
        {
            return m_TriCont;
        }

        //public bool ContainsRTri(int rtriIdx)
        //{
        //    return m_TriCont.Contains(rtriIdx);
        //}

        public int GetRTriCount()
        {
            return m_TriCont.Count;
        }

        /// <summary>
        /// get VFaces that containing this VEdge
        /// </summary>
        public void GetVFaces(List<VFace> vFaces)
        {
            vFaces.Clear();
            VMesh vmesh = VMesh.Instance;
            for(int i = 0; i < m_TriCont.Count; ++i)
            {
                int triIdx = m_TriCont[i];
                VFace vf = vmesh.GetVFaceFromRTri(triIdx);
                if( !vFaces.Contains(vf))
                {
                    vFaces.Add(vf);
                }
            }
        }
	
	    #endregion "triangles"

	    #region "equal"
	    // "equal" 

        public override bool Equals(object o)
        {
            VEdge rhs = (VEdge)o;
            return m_VVerts[0].Equals(rhs.m_VVerts[0]) && m_VVerts[1].Equals(rhs.m_VVerts[1]);
        }

        public override int GetHashCode()
        {
            return (m_VVerts[0].RepVert) | (m_VVerts[1].RepVert << 16);
        }
	
	    #endregion "equal"

	    #region "IComparable"
	    // "IComparable" 

        public int CompareTo(VEdge rhs)
        {
            int diff0 = m_VVerts[0].RepVert - rhs.m_VVerts[0].RepVert;
            if( diff0 != 0)
            {
                return diff0;
            }
            else
            {
                int diff1 = m_VVerts[1].RepVert - rhs.m_VVerts[1].RepVert;
                return diff1;
            }
        }
	
	    #endregion "IComparable"

        public override string ToString()
        {
            return string.Format("{0}<->{1}", m_VVerts[0], m_VVerts[1]);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
	}

    ///// <summary>
    ///// the comparer
    ///// </summary>
    //public class VEdgeEqComparer : EqualityComparer<VEdge>
    //{
    //    public override bool Equals(VEdge x, VEdge y)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override int GetHashCode(VEdge obj)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
}
