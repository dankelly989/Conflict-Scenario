using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using MH.MeshEditor;

namespace MH
{

    using VLst = System.Collections.Generic.List<int>;
    /// <suMeshManipulatorary>
    /// used to contain data for pivot
    /// </suMeshManipulatorary>
	public class Pivotor
	{
	    #region "data"
        // data

        private Vector3 m_WorldPos; //world-space position of pivot
        private Quaternion m_WorldRot; //world-space orientation of pivot
        private PivotOp m_PivotOp = PivotOp.Median;
        private Orientation m_Orient = Orientation.Normal;

        private EditableMesh m_EditMesh;
        private Transform m_MeshTr;
        private MeshSelection m_Selection;
        private EditorCursor m_Cursor;

        #endregion "data"

	    #region "public method"

        public Pivotor() { }
        public void Init(EditableMesh mesh, MeshSelection sel, EditorCursor cursor)
        {
            m_EditMesh = mesh;
            m_MeshTr = m_EditMesh.transform;
            m_Selection = sel;
            m_Cursor = cursor;

            m_WorldPos = m_MeshTr.position;
            m_WorldRot = m_MeshTr.rotation;
            //m_PivotOp = PivotOp.Midian;
            //m_Orient = Orientation.Local;

            MeshUndoer.AddDeleMeshModified(this._OnMeshModified);
            m_Selection.evtSelectionChanged += this._OnSelectionChanged;
            EditorCursor.evtCursorPosChanged += this._OnCursorPosChanged;
        }
        public void Fini()
        {
            EditorCursor.evtCursorPosChanged -= this._OnCursorPosChanged;
            m_Selection.evtSelectionChanged -= this._OnSelectionChanged;
            MeshUndoer.DelDeleMeshModified(this._OnMeshModified);
        }

        public Quaternion WorldRot
        {
            get { return m_WorldRot; }
        }
        public Vector3 WorldPos
        {
            get { return m_WorldPos; }
        }
        public Vector3 ModelPos
        {
            get { return m_MeshTr.InverseTransformPoint(m_WorldPos); }
        }

        public PivotOp pivotOp
        {
            get { return m_PivotOp; }
            set { 
                if(m_PivotOp != value)
                {
                    _ChangePivotOp(value);
                }
            }
        }
        public Orientation orient
        {
            get { return m_Orient; }
            set { 
                if( m_Orient != value )
                {
                    _ChangePivotOrientation(value);
                }
            }
        }

	    #region "event"
	    // "event" 

        public delegate void PivotOpChanged(PivotOp oldOp, PivotOp newOp);
        public event PivotOpChanged evtPivotOpChanged;

        public delegate void PivotOrientationChanged(Orientation oldOrient, Orientation newOrient);
        public event PivotOrientationChanged evtPivotOrientationChanged;

        public delegate void PivotUpdated();
        public event PivotUpdated evtPivotUpdated; // this is called on Pivot got updated
	
	    #endregion "event"
        

        #endregion "public method"

	    #region "private method"

        private void _ChangePivotOp(PivotOp newOp)
        {
            if (m_PivotOp == newOp)
                return;

            PivotOp oldOp = m_PivotOp;
            m_PivotOp = newOp;

            _UpdatePivot();

            if( evtPivotOpChanged != null )
                evtPivotOpChanged(oldOp, newOp);
        }

        private void _ChangePivotOrientation(Orientation newOrient)
        {
            if (m_Orient == newOrient)
                return;

            Orientation oldOr = m_Orient;
            m_Orient = newOrient;

            _UpdatePivot();

            if( evtPivotOrientationChanged != null )
                evtPivotOrientationChanged(oldOr, newOrient);
        }

        /// <summary>
        /// re-calculate the pivot
        /// </summary>
        private void _UpdatePivot()
        {
            switch( m_PivotOp )
            {
                case PivotOp.Median:
                    {
                        _UpdatePivotPos_Midian();
                        _UpdatePivotRot();
                    }
                    break;
                case PivotOp.Cursor:
                    {
                        _UpdatePivotPos_Cursor();
                        _UpdatePivotRot();
                    }
                    break;
            }

            if (evtPivotUpdated != null)
                evtPivotUpdated();
        }

        private void _UpdatePivotPos_Midian()
        {
            // position
            VLst vlst = m_Selection.GetVertices();

            {
                List<Vector3> posLst = MeshUtil.GetVertPos(m_EditMesh.mesh, vlst);
                Vector3 total = Vector3.zero;
                Vector3 modelPivotPos = Vector3.zero;
                if (posLst.Count > 0)
                {
                    total = posLst.Aggregate((sum, cur) => { return sum + cur; });
                    modelPivotPos = total / posLst.Count;
                }
                else
                {
                    modelPivotPos = Vector3.zero;
                }
                m_WorldPos = m_MeshTr.TransformPoint(modelPivotPos);

                //Dbg.Log("m_WorldPos = {0}", m_WorldPos);
            }
        }

        private void _UpdatePivotPos_Cursor()
        {
            // position
            m_WorldPos = m_Cursor.Pos;
        }

        private void _UpdatePivotRot()
        {
            VLst vlst = m_Selection.GetVertices();            

            // rotation
            switch (m_Orient)
            {
                case Orientation.Global:
                    {
                        m_WorldRot = Quaternion.identity;
                    }
                    break;
                case Orientation.Normal:
                    {
                        List<Vector3> nLst = MeshUtil.GetVertNormal(m_EditMesh.mesh, vlst);
                        if (nLst == null)
                        { //no normal
                            m_WorldRot = m_MeshTr.rotation;
                        }
                        else
                        { //has normal
                            Vector3 total = nLst.Aggregate((sum, cur) => { return sum + cur; });
                            Vector3 newModelNormal = total.normalized;
                            newModelNormal = newModelNormal == Vector3.zero ? (Vector3.forward) : (newModelNormal);
                            Vector3 newWorldNormal = m_MeshTr.TransformDirection(newModelNormal);
                            m_WorldRot = Quaternion.LookRotation(newWorldNormal);
                        }
                    }
                    break;
                case Orientation.Local:
                    {
                        m_WorldRot = m_EditMesh.transform.rotation;
                    }
                    break;
                default:
                    Dbg.LogErr("Pivotor._UpdatePivot: unexpected orientation: {0}", m_Orient);
                    break;
            }
        }

        private void _OnMeshModified()
        {
            _UpdatePivot();
        }

        private void _OnSelectionChanged()
        {
            _UpdatePivot();
        }

        private void _OnCursorPosChanged(Vector3 oldPos, Vector3 newPos)
        {
            _UpdatePivot();
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        public enum PivotOp
        {
            Median,
            Cursor,
            // LastSelection,
            // FirstSelection,
            // Individual,
            END,
        }

        public enum Orientation
        {
            Global,
            Normal, //use normal vector
            Local, // use the model's transform's orientation
            //Camera,
            END,
        }

        #endregion "constant data"
	}
}
