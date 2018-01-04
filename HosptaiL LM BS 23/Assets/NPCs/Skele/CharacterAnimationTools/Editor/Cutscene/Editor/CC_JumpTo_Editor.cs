using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH;

[CustomEditor(typeof(CC_JumpTo))]
public class CC_JumpTo_Editor : Editor
{
	#region "data"
    // data

    private SerializedProperty m_kTypeProp;
    private SerializedProperty m_TimeProp;
    private SerializedProperty m_TimeTagProp;
    private SerializedProperty m_ScriptProp;

    #endregion "data"

	#region "Unity event handler"
	// "Unity event handler" 

    void OnEnable()
    {
        m_kTypeProp = serializedObject.FindProperty("m_kType");
        m_TimeProp = serializedObject.FindProperty("m_time");
        m_TimeTagProp = serializedObject.FindProperty("m_timeTag");
        m_ScriptProp = serializedObject.FindProperty("m_Script");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_ScriptProp);
        EditorGUILayout.PropertyField(m_kTypeProp);

        if( m_kTypeProp.enumValueIndex == (int)CC_JumpTo.JumpType.Time ||
            m_kTypeProp.enumValueIndex == (int)CC_JumpTo.JumpType.NormalizedTime)
        {
            EditorGUILayout.PropertyField(m_TimeProp);
        }
        else
        {
            EditorGUILayout.PropertyField(m_TimeTagProp);
        }

        serializedObject.ApplyModifiedProperties();
    }

    //void OnSceneGUI()
    //{
    //    Handles.BeginGUI();

    //    Rect rc = new Rect(100, 100, 300, 100);
    //    EUtil.PushGUIColor(Color.green);
    //    GUI.Box(rc, "");
    //    EUtil.PopGUIColor();

    //    GUILayout.BeginArea(rc);
    //    {
    //        GUILayout.Label("Hello World");
    //    }
    //    GUILayout.EndArea();

    //    Handles.EndGUI();
    //}
	
	#endregion "Unity event handler"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
}
