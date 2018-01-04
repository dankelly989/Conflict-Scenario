using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshEditor
{
    using VVLst = System.Collections.Generic.List<VVert>;

    /// <summary>
    /// could be 3 or 4 edges
    /// </summary>
	public class VFace
	{
	    #region "data"
        // data

        private int[] m_rTriIdxs; //one or two real-tri idx
        private int m_rTriCnt = 0;

        private VVLst m_VVertLst;

        #endregion "data"

	    #region "public method"
        // public method

        public VFace()
        {
            m_rTriIdxs = new int[2];
            m_rTriCnt = 0;
            m_VVertLst = new VVLst();
        }

        public int RTriCnt
        {
            get { return m_rTriCnt; }
        }

        public int GetRTriIdx(int slot)
        {
            Dbg.Assert(slot < m_rTriCnt, "VFace.GetRTriIdx: the slot >= m_rTriCnt: {0}, {1}", slot, m_rTriCnt);
            return m_rTriIdxs[slot];
        }

        /// <summary>
        /// NOTE: when with 2 tris, the order is:
        /// tri0: 012, tri1: 123
        /// 
        /// </summary>
        public void AddRTri(int rTriIdx)
        {
            //Dbg.Log("VFace{0}, AddRTri: {1}", m_Idx, rTriIdx);
            Dbg.Assert(m_rTriCnt < 2, "VFace.AddRTri: already have 2 rtris");
            m_rTriIdxs[m_rTriCnt] = rTriIdx;
            ++m_rTriCnt;

            VVert vv0, vv1, vv2;
            VMesh vmesh = VMesh.Instance;
            vmesh.GetVVertsFromRTri(rTriIdx, out vv0, out vv1, out vv2);

            if( m_rTriCnt == 1 )
            {
                m_VVertLst.Add(vv0);
                m_VVertLst.Add(vv1);
                m_VVertLst.Add(vv2);
            }
            else
            { //merge with another tri, must ensure order of verts

                // set lst[0]
                for(int i=0; i<3; ++i)
                {
                    if( m_VVertLst[i] != vv0 && m_VVertLst[i] != vv1 && m_VVertLst[i] != vv2 )
                    {
                        if( i != 0 )
                        {
                            VVert tmp = m_VVertLst[0];
                            m_VVertLst[0] = m_VVertLst[i];
                            m_VVertLst[i] = tmp;
                        }
                        break;
                    }
                }

                //set lst[3]
                VVert[] verts = new VVert[]{vv0, vv1, vv2};
                for( int i=0; i<3; ++i)
                {
                    if( verts[i] != m_VVertLst[1] && verts[i] != m_VVertLst[2] )
                    {
                        m_VVertLst.Add(verts[i]);
                        break;
                    }
                }
                Dbg.Assert(m_VVertLst.Count == 4, "VFace.AddTri: adding new tri, but no new vert added?!");
            }
        }

        public VVLst GetVVerts()
        {
            return m_VVertLst;
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
