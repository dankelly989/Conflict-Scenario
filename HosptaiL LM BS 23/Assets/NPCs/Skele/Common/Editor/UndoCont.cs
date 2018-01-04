using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    /// <summary>
    /// this container is used to deal with BIG data-struct not fit for Unity Undo
    /// </summary>
    public class UndoCont<T>
    {
        #region "data"
        // data

        private List<T> m_Buffer;
        private int m_CurIdx;
        private T m_CurData; //the real data

        private static readonly int MAX_BUFFERLEN = 100;

        public IntObject m_UndoIdx; //used for unity undo/redo system

        #endregion "data"

        #region "event"
        // "event" 

        public delegate void OnUndoRedo();
        public event OnUndoRedo evtContUndo;
        public event OnUndoRedo evtContRedo;

        #endregion "event"

        public void Init(T initData) { Init(initData, MAX_BUFFERLEN); }
        public void Init(T initData, int capacity)
        {
            m_Buffer = new List<T>(capacity);
            m_CurIdx = 0;
            m_UndoIdx = ScriptableObject.CreateInstance<IntObject>();
            m_UndoIdx.val = 0;
            m_CurData = initData;

            Undo.undoRedoPerformed += this._OnUndoRedoPerformed;
        }

        public void Fini()
        {
            Undo.undoRedoPerformed -= this._OnUndoRedoPerformed;

            m_Buffer.Clear();
            m_CurIdx = m_UndoIdx.val = 0;

            ScriptableObject.DestroyImmediate(m_UndoIdx);
        }

        public void SetData(T data, bool bDoRecord = true, string msg = null)
        {
            if (bDoRecord)
            {
                _DoRecord(msg);
            }

            m_CurData = data; //apply modification after recording
        }

        public T Data
        {
            get { return m_CurData; }
        }

        public void _DoRecord(string msg)
        {
            Undo.RecordObject(m_UndoIdx, string.IsNullOrEmpty(msg) ? "Modify Data" : msg);

            //full undostack, pop the oldest entry from bottom
            if (m_CurIdx == MAX_BUFFERLEN)
            {
                m_Buffer.RemoveAt(0); //make a room for new entry
                m_CurIdx--;
                m_UndoIdx.val--;
            }

            //clear undostack on top, this happens when new input comes after undo operation
            if (m_CurIdx <= m_Buffer.Count - 1)
            {
                m_Buffer.RemoveRange(m_CurIdx, m_Buffer.Count - m_CurIdx);
            }

            m_Buffer.Add(m_CurData);
            m_CurIdx++; //point to top past 1
            m_UndoIdx.val = m_CurIdx;
        }

        private void _ExecuteUndo()
        {
            if (m_CurIdx >= m_Buffer.Count)
                m_Buffer.Add(m_CurData);
            else
                m_Buffer[m_CurIdx] = m_CurData;

            m_CurIdx = m_UndoIdx.val;

            m_CurData = m_Buffer[m_UndoIdx.val];

            if (evtContUndo != null)
                evtContUndo();
            //Dbg.Log("_ExecuteUndo: curIdx = {0}", m_CurIdx);
        }

        private void _ExecuteRedo()
        {
            m_CurIdx = m_UndoIdx.val;
            m_CurData = m_Buffer[m_UndoIdx.val];

            if (evtContRedo != null)
                evtContRedo();
            //Dbg.Log("_ExecuteRedo: curIdx = {0}", m_CurIdx);
        }

        private void _OnUndoRedoPerformed()
        {
            if (m_CurIdx != m_UndoIdx.val)
            {
                if (m_CurIdx > m_UndoIdx.val)
                {
                    _ExecuteUndo();
                }
                else
                {
                    _ExecuteRedo();
                }

            }
        }
    }

    public class IntObject : ScriptableObject
    {
        public int val;
    }
}
