using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH;

[CustomEditor(typeof(CC_GUI))]
public class CC_GUI_Editor : Editor
{
	#region "configurable data"
    // configurable data

    #endregion "configurable data"

	#region "data"
    // data

    private SerializedProperty m_ScriptProp;
    private SerializedProperty m_GUIPrefabProp;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    void OnEnable()
    {
        m_ScriptProp = serializedObject.FindProperty("m_Script");
        m_GUIPrefabProp = serializedObject.FindProperty("m_DialogGO");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_ScriptProp);

        GameObject prefabGO = (GameObject)EditorGUILayout.ObjectField("Dialog GO Prefab", m_GUIPrefabProp.objectReferenceValue, typeof(GameObject), false);
        if( prefabGO != m_GUIPrefabProp.objectReferenceValue )
        {
            GameObject newGO = PrefabUtility.InstantiatePrefab(prefabGO) as GameObject;
            GameObject oldGO = (GameObject)m_GUIPrefabProp.objectReferenceValue;
            m_GUIPrefabProp.objectReferenceValue = newGO;
            Misc.AddChild(((MonoBehaviour)serializedObject.targetObject).transform, newGO);
            GameObject.DestroyImmediate(oldGO);
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

    #endregion "constant data"
}
