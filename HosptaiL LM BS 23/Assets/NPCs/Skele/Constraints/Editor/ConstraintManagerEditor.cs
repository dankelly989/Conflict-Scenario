using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(ConstraintManager))]
    public class ConstraintManagerEditor : Editor
    {
        private Vector2 m_scroll;

        public override void OnInspectorGUI()
        {
            ConstraintManager o = (ConstraintManager)target;

            EditorGUILayout.LabelField("Count: " + o.ContCount);

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.MaxHeight(200));
            for (var ie = o.GetContEnumerator(); ie.MoveNext(); )
            {
                var cstack = ie.Current.Key;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.ObjectField(cstack.name, cstack, typeof(ConstraintStack), true);
                    GUILayout.Label(cstack.ExecOrder.ToString(), GUILayout.Width(25f));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
