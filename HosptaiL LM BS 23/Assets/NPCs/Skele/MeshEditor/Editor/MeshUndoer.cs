using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
	public class MeshUndoer : ScriptableObject
	{
	    #region "data"
        // data
        [NonSerialized] private Mesh m_Mesh;
        [NonSerialized] private bool m_AllowRecordUndo;

        [NonSerialized] private List<Vector3[]> m_Buffer;
        [NonSerialized] private int m_CurIdx;

        public int m_UndoableIdx;

        #endregion "data"

        #region "event"
        // "event" 

        public delegate void MeshModified();
        //public static event MeshModified evtMeshModified;
        public static event MeshModified evtMeshUndoRedo;

        #endregion "event"

	    #region "public method"
        // public method

        void OnEnable()
        {
            
        }

        void OnDestroy()
        {
            //Dbg.Log("MeshUndoer.OnDestroy");
            Undo.undoRedoPerformed -= _OnUndoRedo;
        }

        public void Init(Mesh m)
        {
            m_Mesh = m;
            m_AllowRecordUndo = true;
            m_Buffer = new List<Vector3[]>(BUFFER_MAXLEN);

            //m_Buffer.Add(m_Mesh.vertices);
            m_CurIdx = m_UndoableIdx = 0; 

            Undo.undoRedoPerformed += _OnUndoRedo;

            //Dbg.Log("MeshUndoer.Init: {0}", GetInstanceID());
        }

        public void SetEnableRecord(bool bEnable)
        {
            if (m_AllowRecordUndo == bEnable)
                return;

            m_AllowRecordUndo = bEnable;
            //Dbg.Log("MeshUndoer.SetEnableRecord: {0}", bEnable);
        }

        public void SetVerts(Vector3[] verts)
        {
            if (m_AllowRecordUndo)
            {
                _AddToUndoBuffer(m_Mesh.vertices);
            }

            m_Mesh.vertices = verts;
            NotifyMeshModified();
        }

        public static void NotifyMeshModified()
        {
            //if( evtMeshModified != null)
            //    evtMeshModified();

            MeshModifyEvt.FireEvent();
        }

        public static void AddDeleMeshModified(MeshModifyEvt.MeshModified del)
        {
            MeshModifyEvt.AddDele(del);
        }

        public static void DelDeleMeshModified(MeshModifyEvt.MeshModified del)
        {
            MeshModifyEvt.DelDele(del);
        }

        public static void NotifyMeshUndoRedo()
        {
            if( evtMeshUndoRedo != null)
                evtMeshUndoRedo();
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private void _AddToUndoBuffer(Vector3[] verts)
        {
            Undo.RecordObject(this, "Set Verts");

            // full undostack, pop the oldest entry from bottom
            if (m_CurIdx == BUFFER_MAXLEN)
            {
                m_Buffer.RemoveAt(0); //make a room for new entry
                m_CurIdx--;
                m_UndoableIdx--;
            }

            // clear undostack on top, this happens when new input comes after undo operation
            if( m_CurIdx <= m_Buffer.Count -1)
            {
                m_Buffer.RemoveRange(m_CurIdx, m_Buffer.Count - m_CurIdx);
            }
            
            m_Buffer.Add(verts); 
            m_CurIdx++; //point to top past 1
            m_UndoableIdx = m_CurIdx;

            //Dbg.Log("_AddToUndoBuffer: curIdx = {0}", m_CurIdx);
        }

        private void _ExecuteUndo()
        {
            Vector3[] vs = m_Mesh.vertices;
            if (m_CurIdx >= m_Buffer.Count)
                m_Buffer.Add(vs);
            else
                m_Buffer[m_CurIdx] = vs;

            m_CurIdx = m_UndoableIdx;

            m_Mesh.vertices = m_Buffer[m_UndoableIdx];

            //Dbg.Log("_ExecuteUndo: curIdx = {0}", m_CurIdx);
        }

        private void _ExecuteRedo()
        {
            m_CurIdx = m_UndoableIdx;
            m_Mesh.vertices = m_Buffer[m_UndoableIdx];

            //Dbg.Log("_ExecuteRedo: curIdx = {0}", m_CurIdx);
        }

        private void _OnUndoRedo()
        {
            //Dbg.Log("MeshUndoer._OnUndoRedo: {0}", GetInstanceID());
            if( m_CurIdx != m_UndoableIdx )
            {
                if( m_CurIdx > m_UndoableIdx )
                {
                    _ExecuteUndo();
                }
                else
                {
                    _ExecuteRedo();
                }
                
                NotifyMeshModified();

                NotifyMeshUndoRedo();
            }

            
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        private const int BUFFER_MAXLEN = 100;

        #endregion "constant data"
        
	}

}
