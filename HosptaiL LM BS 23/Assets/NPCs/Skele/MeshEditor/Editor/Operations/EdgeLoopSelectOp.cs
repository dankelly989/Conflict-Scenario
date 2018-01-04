using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH.MeshEditor;

namespace MH
{
namespace MeshOp
{
    using VLst  = System.Collections.Generic.List<int>;
    using VELst = System.Collections.Generic.List<VEdge>;
    using VFLst = System.Collections.Generic.List<VFace>;

    /// <summary>
    /// try selecting edge loop
    /// </summary>
    class EdgeLoopSelectOp 
    {
	    #region "data"
        // data

        private EditableMesh m_Mesh;
        private MeshSelection m_Selection;

        private VFLst m_tmpVFaces1 = new VFLst();
        private VFLst m_tmpVFaces2 = new VFLst();

        private HashSet<VEdge> m_EdgeSet;

        #endregion "data"

	    #region "public method"
        // public method

        public void Init(EditableMesh m, MeshSelection sel)
        {
            m_Mesh = m;
            Dbg.Assert(m_Mesh != null, "EdgeLoopSelectOp.Init: m_Mesh = null");
            m_Selection = sel;

            m_EdgeSet = new HashSet<VEdge>();
        }

        public void Fini()
        {

        }

        /// <summary>
        /// given an edge, and select the edge loop based on that
        /// </summary>
        public void Execute(VEdge vedge, Op op)
        {
            VVert v0 = vedge.GetVVert(0);
            VVert v1 = vedge.GetVVert(1);

            Execute(v0, v1, op);
        }

        public void Execute(VVert v0, VVert v1, Op op)
        {
            if (op == Op.Restart)
            {
                m_Selection.Clear();
                op = Op.Add;
            }

            if( op == Op.Add )
            {
                m_Selection.AddVVert(v0);
                m_Selection.AddVVert(v1);
            }
            else
            {
                m_Selection.DelVVert(v0);
                m_Selection.DelVVert(v1);
            }

            m_EdgeSet.Add(v0.GetVEdge(v1));
            _WalkSelect(v0, v1, op);
            _WalkSelect(v1, v0, op);
            m_EdgeSet.Clear();
        }

        #endregion "public method"

	    #region "private method"
        // private method

        /// <summary>
        /// try to process v2 if possible (under given op)
        /// [v0 ==> v1 == v2] in this order
        /// </summary>
        private void _WalkSelect(VVert v0, VVert v1, Op op)
        {
            //////////////////////////////////////////////////
            // 1. if v1 doesn't have exactly 4 vedges, return;
            // 2. take the vedge E that doesn't share VFace with vedge(v0,v1)
            // 3. select the other vvert v2 of E;
            // 4. iterate as _WalkSelect(v1, v2, op)
            //////////////////////////////////////////////////

            VEdge thisEdge = v0.GetVEdge(v1);
            thisEdge.GetVFaces(m_tmpVFaces1);

            while (true)
            {
                // step 1
                VELst veLst = v1.GetActiveVEdges();
                if (veLst.Count != GOOD_EDGE_CNT)
                {
                    //Dbg.Log("_WalkSelect: {0}=>{1}, veLst.Count not 4, is {2}", v0, v1, veLst.Count);
                    break; //END
                }

                // step 2
                VEdge thatEdge = null;
                for (int i = 0; i < GOOD_EDGE_CNT; ++i)
                {
                    VEdge rhs = veLst[i];
                    if (rhs == thisEdge)
                        continue;

                    rhs.GetVFaces(m_tmpVFaces2);
                    if( !_HasIntersect(m_tmpVFaces1, m_tmpVFaces2) )
                    {
                        thatEdge = rhs;
                        break; //just out of edge loop
                    }
                }

                // step 3
                VVert v2 = null;
                if( thatEdge != null )
                {
                    v2 = thatEdge.GetVVert(0);
                    if( v2 == v1 )
                    {
                        v2 = thatEdge.GetVVert(1);
                    }

                    // check for inf loop
                    VEdge newEdge = v1.GetVEdge(v2);
                    if (m_EdgeSet.Contains(newEdge))
                        break; //END
                    m_EdgeSet.Add(newEdge);

                    // modify selection
                    if (op == Op.Add)
                        m_Selection.AddVVert(v2);
                    else
                        m_Selection.DelVVert(v2);

                    // step 4
                    v0 = v1;
                    v1 = v2; 
                    thisEdge = thatEdge;
                    Misc.Swap(ref m_tmpVFaces1, ref m_tmpVFaces2);
                }
                else
                {
                    //Dbg.Log("_WalkSelect: {0}=>{1}, not found thatEdge", v0, v1);
                    break;  //END
                }

            } // end of while(true)
            
        }

        private bool _HasIntersect(VFLst lhs, VFLst rhs)
        {
            for( int i=0; i<rhs.Count; ++i )
            {
                VFace vf = rhs[i];
                if (lhs.Contains(vf))
                    return true;
            }
            return false;
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        public enum Op
        {
            Add, //append to selection
            Sub, //remove from selection
            Restart,
        }

        private const int GOOD_EDGE_CNT = 4;

        #endregion "constant data"

    }
}
}
