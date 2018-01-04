using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    /// <summary>
    /// used to draw a pass-in Editor in a EditorWindow
    /// </summary>
	public class EditorEditorWindow : EditorWindow
	{
	    #region "data"
        private Editor m_Editor = null;
        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

        public static EditorEditorWindow OpenWindow(UnityEngine.Object o)
        {
            return OpenWindow(o, DEF_MIN_SIZE);
        }

        public static EditorEditorWindow OpenWindow(UnityEngine.Object o, Vector2 minSz)
        {
            Editor e = Editor.CreateEditor(o);
            return OpenWindowWithEditor(e, minSz);            
        }

        public static EditorEditorWindow OpenWindowWithActivatorRect(UnityEngine.Object o, Rect activatorRect)
        {
            EditorEditorWindow wnd = OpenWindow(o);

            Rect rc = EUtil.GetRectByActivatorRect(wnd.position, activatorRect);
            
            wnd.position = rc;

            wnd.ShowUtility();

            return wnd;
        }

        public static EditorEditorWindow OpenWindowWithEditor(Editor e)
        {
            return OpenWindowWithEditor(e, DEF_MIN_SIZE);
        }

        public static EditorEditorWindow OpenWindowWithEditor(Editor e, Vector2 minSz)
        {
            var inst = (EditorEditorWindow)GetWindow(typeof(EditorEditorWindow), true, "Details", true);
            inst.m_Editor = e;
            inst.minSize = minSz;
            return inst;
        }

        void OnGUI()
        {
            m_Editor.OnInspectorGUI();
        }
	    
        #endregion "unity event handlers"

	    #region "public method"
        // public method

        #endregion "public method"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        private static readonly Vector2 DEF_MIN_SIZE = new Vector2(300f, 50f);

        #endregion "constant data"
	}
}
