using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class EDataInspector : EditorWindow
    {
        private Vector2 m_scrollPos = Vector2.zero;

        [MenuItem("Window/Skele/EDataInspector")]
        public static void OpenWindow()
        {
            var wnd = GetWindow(typeof(EDataInspector));
            EUtil.SetEditorWindowTitle(wnd, "EData");
        }


        void OnGUI()
        {
            var dmap = EData.GetDMap();

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            foreach (var pr in dmap)
            {
                EditorGUILayout.LabelField(pr.Key + " ==> " + pr.Value);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
