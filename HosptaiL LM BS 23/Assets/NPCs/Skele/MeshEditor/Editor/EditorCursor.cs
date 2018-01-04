using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshEditor
{
    /// <summary>
    /// the 3D cursor
    /// </summary>
    public class EditorCursor
    {
	    #region "data"
        // data

        private EditableMesh m_Mesh;
        private Vector3 m_WorldPos; //the world space of the cursor

        private Texture2D m_CursorImg;
        private Vector2 m_HalfOff;

        #endregion "data"

	    #region "public method"
        // public method

        public void Init(EditableMesh m)
        {
            m_Mesh = m;
            m_WorldPos = m_Mesh.transform.position;

            m_CursorImg = AssetDatabase.LoadAssetAtPath(CURSOR_IMG_PATH, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(m_CursorImg != null, "EditorCursor.Init: failed to load cursor img: {0}", CURSOR_IMG_PATH);

            m_HalfOff = new Vector2(m_CursorImg.width * 0.5f, m_CursorImg.height * 0.5f);
        }

        public void Fini()
        {

        }

        public Vector3 Pos
        {
            get { return m_WorldPos; }
            set { 
                if( m_WorldPos != value )
                {
                    Vector3 oldPos = m_WorldPos;
                    m_WorldPos = value;

                    if( evtCursorPosChanged != null)
                        evtCursorPosChanged(oldPos, value);
                }
            }
        }

        public Texture2D CursorImg
        {
            get { return m_CursorImg; }
        }

        public void DrawCursor()
        {
            Handles.BeginGUI();
            {
                Rect rc = new Rect();
                Vector2 pt = HandleUtility.WorldToGUIPoint(m_WorldPos);
                rc.min = pt - m_HalfOff;
                rc.max = pt + m_HalfOff;
                GUI.DrawTexture(rc, m_CursorImg);
            }
            Handles.EndGUI();
        }

	    #region "event"
	    // "event" 

        public delegate void OnCursorPosChanged(Vector3 oldPos, Vector3 newPos);
        public static event OnCursorPosChanged evtCursorPosChanged;
	
	    #endregion "event"

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        private const string CURSOR_IMG_PATH = "Assets/Skele/MeshEditor/Editor/Res/3DCursor.psd"; 

        #endregion "constant data"
    }
}
}
