using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{
    /// <summary>
    /// fix mesh's bound and normal
    /// </summary>
    class FixBoundNormalOp
    {
        #region "data"
        // data

        private EditableMesh m_Mesh;
        private bool m_Blocked; //if blocked, then will not execute on Mesh modified;

        #endregion "data"

	    #region "public method"
        // public method

        public FixBoundNormalOp() { }
        public void Init(EditableMesh m)
        {
            m_Mesh = m;
            m_Blocked = false;

            MeshManipulator.evtHandleDraggingStateChanged += this._OnHandleDraggingStateChanged;
            MeshUndoer.AddDeleMeshModified(this._OnMeshModifed);
        }
        public void Fini()
        {
            MeshUndoer.DelDeleMeshModified(this._OnMeshModifed);
            MeshManipulator.evtHandleDraggingStateChanged -= this._OnHandleDraggingStateChanged;
        }

        public void Execute()
        {
            Mesh m = m_Mesh.mesh;
            //Undo.RecordObject(m, "AutoFix"); //too slow on big mesh
            MeshUtil.FixNormalAndBound(m);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private void _OnMeshModifed()
        {
            if( !m_Blocked )
            {
                Execute();
            }
        }

        private void _OnHandleDraggingStateChanged(bool bDragging)
        {
            m_Blocked = bDragging;
            if (!m_Blocked)
                Execute();
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
    }
}
}
