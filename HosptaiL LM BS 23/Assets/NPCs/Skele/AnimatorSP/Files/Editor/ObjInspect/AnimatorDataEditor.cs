using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    [CustomEditor(typeof(AnimatorData))]
    public class AnimatorDataEditor : Editor
    {
		#region "data"

        private SerializedProperty m_spPlayOnStart;
        private SerializedProperty m_spTakes;

        private static Texture2D ms_inspectTex;

	    #endregion "data"
	
		#region "unity event handlers"

        void OnEnable()
        {
            EUtil.LoadAsset(ref ms_inspectTex, ASSET_InspectIcon);

            m_spPlayOnStart = serializedObject.FindProperty(SP_PlayOnStart);
            m_spTakes = serializedObject.FindProperty(SP_Takes);

            Dbg.Assert(m_spPlayOnStart != null, "AnimatorDataEditor.OnEnable: failed to find property: {0}", SP_PlayOnStart);
            Dbg.Assert(m_spTakes != null, "AnimatorDataEditor.OnEnable: failed to find property: {0}", SP_Takes);
        }

        void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AnimatorData mb = target as AnimatorData;
            Dbg.Assert(mb != null, "AnimatorDataEditor.OnInspectorGUI: cannot get target: {0}", target.name);

            EditorGUILayout.HelpBox("If you want to modify the takes list, do it via the Timeline editor", MessageType.Info);

            if (EUtil.Button("Open Timeline Editor", Color.green))
            {
                AMTimeline.ResetWithAnimatorData((AnimatorData)target);
            }

            string playOnStartName = (m_spPlayOnStart.objectReferenceValue != null) ? ((AMTake)m_spPlayOnStart.objectReferenceValue).name : "None";
            EditorGUILayout.LabelField("Play On Start:  " + playOnStartName);

            EUtil.DrawSplitter();

            for (int i = 0; i < m_spTakes.arraySize; ++i)
            {
                var oneTake = m_spTakes.GetArrayElementAtIndex(i);
                
                GUILayout.BeginHorizontal();
                {
                    if (oneTake != null && oneTake.objectReferenceValue != null)
                    {
                        AMTake takeObj = oneTake.objectReferenceValue as AMTake;
                        EditorGUILayout.LabelField(string.Format("{0}: \"{1} fr, {2} fps\"", takeObj.name, takeObj.numFrames, takeObj.frameRate));
                        if (GUILayout.Button(new GUIContent(ms_inspectTex, "inspect this take's content"), GUILayout.Height(20f), GUILayout.Width(30f)))
                        {
                            Selection.activeObject = takeObj;
                        }
                        //EUtil.PushGUIColor(EditorUtility.IsPersistent(takeObj) ? Color.yellow : Color.green);
                        //if (GUILayout.Button(new GUIContent("S", "save asset to disk"), GUILayout.Width(30f)))
                        //{
                        //    string path = null;
                        //    if (!EditorUtility.IsPersistent(takeObj))
                        //        path = EditorUtility.SaveFilePanelInProject("Save Take", takeObj.name, "asset", "Select asset path");
                        //    else
                        //        path = AssetDatabase.GetAssetPath(takeObj);

                        //    if (!string.IsNullOrEmpty(path))
                        //    {
                        //        takeObj.SaveAsset(mb, path);
                        //        EUtil.ShowNotification("Saved Take at: " + path, 3f);
                        //    }
                        //}
                        //EUtil.PopGUIColor();
                    }
                    else
                    {
                        GUILayout.Label("This slot is null reference");
                    }
                }
                GUILayout.EndHorizontal();
                
            }

            serializedObject.ApplyModifiedProperties();
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

        private const string SP_PlayOnStart = "playOnStart";
        private const string SP_Takes = "takes";
        private const string ASSET_InspectIcon = AnimatorData.BASEDIR + "/Files/Resources/am_inspect.png";

	    #endregion "constant data"
    }
}
