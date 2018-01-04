using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH;

[CustomPropertyDrawer(typeof(CCTrPath))]
public class CCTrPathDrawer : PropertyDrawer
{
	#region "data"
    // data

    private bool m_bSelectingTransform = false;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var btnRect = new Rect(position.x, position.y, 25, LINEHEIGHT);
        var trPathRect = new Rect(position.x + 27, position.y, position.width - 27, LINEHEIGHT);

        // Draw fields - pass GUIContent.none to each so they are drawn without labels

        if( GUI.Button(btnRect, "C") )
        {
            m_bSelectingTransform = !m_bSelectingTransform;
        }

        if( property.FindPropertyRelative(F_VALID).boolValue )
        {
            EditorGUI.PropertyField(trPathRect, property.FindPropertyRelative(F_TRPATH), GUIContent.none);
        }
        else
        {
            EditorGUI.LabelField(trPathRect, "NULL");
        }

        if( m_bSelectingTransform )
        {
            var objSelectRect = new Rect(position.x, position.y + LINEHEIGHT, position.width, LINEHEIGHT);
            Transform selfTr = ((MonoBehaviour)(property.serializedObject.targetObject)).transform;
            Transform tr = EditorGUI.ObjectField(objSelectRect, selfTr, typeof(Transform), true) as Transform;
            if( tr != selfTr )
            {
                if( tr == null )
                {
                    _SetTrPath(property, null);
                }
                else
                {
                    Transform ccroot = _FindCCRoot(tr);

                    if (ccroot == null)
                    {
                        string trPath = CCTrPath.SceneRoot + AnimationUtility.CalculateTransformPath(tr, null);
                        _SetTrPath(property, trPath);
                        EUtil.ShowNotification("TrPath: " + trPath, 4.0f);
                    }
                    else
                    {
                        string trPath = AnimationUtility.CalculateTransformPath(tr, ccroot);
                        _SetTrPath(property, trPath);
                        EUtil.ShowNotification("TrPath: " + trPath, 4.0f);
                    }
                }

                m_bSelectingTransform = false;
            }            
        }

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return m_bSelectingTransform ? 2*LINEHEIGHT : LINEHEIGHT;
    }

    #endregion "public method"

	#region "private method"
    // private method

    private Transform _FindCCRoot(Transform tr)
    {
        while( tr != null)
        {
            if( tr.GetComponent<CutsceneController>() != null )
            {
                return tr;
            }

            tr = tr.parent;
        }

        return null;
    }

    private void _SetTrPath(SerializedProperty property, string trPath )
    {
        var trPathProp = property.FindPropertyRelative(F_TRPATH);
        Dbg.Assert(trPathProp != null, "CCTrPathDrawer._SetTrPath: failed to get field: " + F_TRPATH);
        trPathProp.stringValue = trPath;

        var validProp = property.FindPropertyRelative(F_VALID);
        Dbg.Assert(validProp != null, "CCTrPathDrawer._SetTrPath: failed to get field: " + F_VALID);
        validProp.boolValue = (trPath != null);
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string F_TRPATH = "m_trPath";
    public const string F_VALID = "m_Valid";
    public const float LINEHEIGHT = 19.0f;

    #endregion "constant data"
}

