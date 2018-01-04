using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    [CustomEditor(typeof(AMTake))]
    public class AMTakeEditor : Editor
    {
		#region "data"
	    // data

        private static Texture2D ms_icon = null;

        private SerializedProperty m_spName;
        private SerializedProperty m_spNumFrames;
        private SerializedProperty m_spFps;
	
	    #endregion "data"
	
		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            EUtil.LoadAsset(ref ms_icon, ASSET_ICON);

            m_spName = serializedObject.FindProperty("_name");
            m_spNumFrames = serializedObject.FindProperty("numFrames");
            m_spFps = serializedObject.FindProperty("frameRate");

            Dbg.Assert(m_spName != null, "AMTakeEditor.OnEnable: failed to find property: _name");
            Dbg.Assert(m_spNumFrames != null, "AMTakeEditor.OnEnable: failed to find property: numFrames");
            Dbg.Assert(m_spFps != null, "AMTakeEditor.OnEnable: failed to find property: frameRate");
        }

        void OnDisable()
        {

        }

        protected override void OnHeaderGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(ms_icon/*, GUILayout.Height(80f), GUILayout.Width(80f)*/);
                GUILayout.BeginVertical(GUILayout.Height(80f));
                {
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 80f;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.PropertyField(m_spName, new GUIContent("Take Name"));
                    EditorGUILayout.PropertyField(m_spNumFrames, new GUIContent("Frame Num"));
                    EditorGUILayout.PropertyField(m_spFps, new GUIContent("Frame Rate"));
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 1. draw root objects;
        /// 2. draw tracks' info, 
        /// 2.1. draw verification buttons + (ref / trPath)
        /// 2.2. manual rebind
        /// 3. auto rebind (when added into AnimatorData, AnimatorDataEditor should call that too)
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 1

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

        public const string ASSET_ICON = AnimatorData.BASEDIR + "/Files/Editor/Res/TakeIcon.psd";
	
	    #endregion "constant data"
    }
}
