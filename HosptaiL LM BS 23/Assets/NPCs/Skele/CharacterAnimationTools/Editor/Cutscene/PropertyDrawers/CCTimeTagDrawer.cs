using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH;

[CustomPropertyDrawer(typeof(TimeTag))]
public class CCTimeTagDrawer : PropertyDrawer
{
	#region "configurable data"
    // configurable data

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	
	#region "public method"
    // public method

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        //// Draw label
        //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        float w = 0;
        float fieldW = (position.width - 110) * 0.5f;
        var nameLabelRect = new Rect(position.x + w, position.y, 35, position.height);
        w += 35 + 5;
        var nameRect = new Rect(position.x + w, position.y, fieldW, position.height);
        w += fieldW + 5;
        var equalRect = new Rect(position.x + w, position.y, 20, position.height);
        w += 20 + 5;
        var timeLabelRect = new Rect(position.x + w, position.y, 35, position.height);
        w += 35 + 5;
        var timeRect = new Rect(position.x + w, position.y, fieldW, position.height);

        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.LabelField(nameLabelRect, "Tag");
        EditorGUI.PropertyField(nameRect, property.FindPropertyRelative(F_NAME), GUIContent.none);
        EditorGUI.LabelField(equalRect, "=>");
        EditorGUI.LabelField(timeLabelRect, "Time");
        EditorGUI.PropertyField(timeRect, property.FindPropertyRelative(F_TIME), GUIContent.none);
        
        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string F_NAME = "m_Name";
    public const string F_TIME = "m_Time";

    #endregion "constant data"
}


