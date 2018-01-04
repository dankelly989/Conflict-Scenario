using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using MH.MeshEditor;

namespace MH
{
    using RVLst = System.Collections.Generic.List<int>; //real-vert index list
    using VELst = System.Collections.Generic.List<VEdge>;
    using VFLst = System.Collections.Generic.List<VFace>;

    // contain information of currently selected 
	public class MeshSelection
	{
	    #region "data"
	    // "data" 

        private MeshTopology m_PrimType = MeshTopology.Points;
        private UndoCont<RVLst> m_Verts;
        private UndoCont<VELst> m_Edges;
        private UndoCont<VFLst> m_Tris;

        private bool m_Dirty = false;
	
	    #endregion "data"
        

	    #region "event"
	    // "event" 

        public delegate void SelectionChanged();
        public event SelectionChanged evtSelectionChanged;
	
	    #endregion "event"

        public MeshSelection() {   }

        public MeshTopology Prim
        {
            get { return m_PrimType; }
        }

        public int RVCount
        {
            get { return m_Verts.Data.Count; }
        }

        public void Init()
        {
            m_Verts = new UndoCont<RVLst>();
            m_Verts.Init(new RVLst());
            m_Verts.evtContUndo += _OnUndoRedo;
            m_Verts.evtContRedo += _OnUndoRedo;

            m_Edges = new UndoCont<VELst>();
            m_Edges.Init(new VELst());
            m_Edges.evtContUndo += _OnUndoRedo;
            m_Edges.evtContRedo += _OnUndoRedo;

            m_Tris = new UndoCont<VFLst>();
            m_Tris.Init(new VFLst());
            m_Tris.evtContUndo += _OnUndoRedo;
            m_Tris.evtContRedo += _OnUndoRedo;
        }

        public void Fini()
        {
            m_Tris.evtContUndo -= _OnUndoRedo;
            m_Tris.evtContRedo -= _OnUndoRedo;
            m_Tris.Fini();
            m_Tris = null;
            //ScriptableObject.DestroyImmediate(m_Tris);

            m_Edges.evtContUndo -= _OnUndoRedo;
            m_Edges.evtContRedo -= _OnUndoRedo;
            m_Edges.Fini();
            m_Edges = null;
            //ScriptableObject.DestroyImmediate(m_Edges);

            m_Verts.evtContUndo -= _OnUndoRedo;
            m_Verts.evtContRedo -= _OnUndoRedo;
            m_Verts.Fini();
            m_Verts = null;
            //ScriptableObject.DestroyImmediate(m_Verts);
        }

        public RVLst GetVertices()
        {
            return m_Verts.Data;
        }

        // fill the `lst' using the currently selected elements
        public void GetSelectionInVLst(RVLst lst)
        {
            switch( m_PrimType )
            {
                case MeshTopology.Points:
                    {
                        lst.Clear();
                        lst.AddRange(m_Verts.Data);
                    }
                    break;
            }
        }

	    #region "vertex"
	    // "vertex" 

        public void AddVert(int vidx)
        {
            AddVert(new int[] { vidx });
        }
        public void AddVert(int[] vindices)
        {
            RVLst vlst = m_Verts.Data;

            RVLst newLst = new RVLst(vlst);
            newLst.AddRange(vindices);
            newLst.Sort();
            newLst = newLst.Distinct().ToList();
            m_Verts.SetData(newLst, true, "Add Selection Verts");

            Dirty = true; //SetDirty
        }
        public void AddVVert(VVert vvert)
        {
            RVLst vLst = vvert.GetRVerts();
            AddVert(vLst.ToArray());
        }

        public void DelVert(int vidx)
        {
            RVLst vlst = m_Verts.Data;

            RVLst newLst = new RVLst(vlst);
            newLst.Remove(vidx);

            m_Verts.SetData(newLst, true, "Remove Selection Verts");
            Dirty = true;
        }
        public void DelVert(int[] vindices)
        {
            //m_Verts = m_Verts.Except(vindices).ToList(); //performance is bad

            HashSet<int> s = new HashSet<int>(m_Verts.Data);
            s.ExceptWith(vindices);
            m_Verts.SetData(s.ToList(), true, "Remove Selection Verts");

            Dirty = true;//SetDirty
        }
        public void DelVVert(VVert vvert)
        {
            RVLst vLst = vvert.GetRVerts();
            DelVert(vLst.ToArray());
        }

        public bool IsSelectedVert(int vidx)
        {
            return m_Verts.Data.Contains(vidx);
        }

        public bool IsSelectedVVert(VVert vvert)
        {
            return IsSelectedVert(vvert.RepVert);
        }
	
	    #endregion "vertex"

	    #region "edge"
	    // "edge" 
	
	    #endregion "edge"

	    #region "tri"
	    // "tri" 
	
	    #endregion "tri"

        public void Clear()
        {
            if( m_Verts.Data.Count > 0 )
            {
                RVLst emptyLst = new RVLst();
                m_Verts.SetData(emptyLst, true, "Clear selection");
                Dirty = true;//SetDirty
            }
            else if( m_Edges.Data.Count > 0)
            {
                VELst emptyLst = new VELst();
                m_Edges.SetData(emptyLst, true, "Clear selection");
                Dirty = true;//SetDirty
            }
            else if( m_Tris.Data.Count > 0)
            {
                VFLst emptyLst = new VFLst();
                m_Tris.SetData(emptyLst, true, "Clear selection");
                Dirty = true;//SetDirty
            }
        }

        public bool Dirty
        {
            get { return m_Dirty; }
            set { 
                m_Dirty = value;
                if( value )
                {
                    evtSelectionChanged(); //notify others selection has changed
                }
            }
        }

	    #region "private methods"
	    // "private methods" 

        private void _OnUndoRedo()
        {
            this.Dirty = true;
        }
	
	    #endregion "private methods"
	}

}
