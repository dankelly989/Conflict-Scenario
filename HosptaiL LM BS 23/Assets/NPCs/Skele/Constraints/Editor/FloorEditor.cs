using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(Floor))]
    public class FloorEditor : Editor
    {
        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            Floor cp = (Floor)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                cp.PlaneDir = (EAxisD)EConUtil.DrawEnumBtns(EConUtil.AxisDs, EConUtil.AxisDStrs, cp.PlaneDir, "Plane Normal", "the normal direction of floor plane");

                // offset
                cp.UseOffset = EditorGUILayout.Toggle(new GUIContent("Use Offset", "Add offset onto the result"), cp.UseOffset);
                if (cp.UseOffset)
                {
                    cp.Offset = EditorGUILayout.FloatField(new GUIContent("Offset", "offset along normal direction"), cp.Offset);
                }

                // raycast
                cp.UseRaycast = EditorGUILayout.Toggle(new GUIContent("Use Raycast", "use raycast to decide contact point"), cp.UseRaycast);

                EditorGUILayout.BeginHorizontal();
                {
                    EUtil.PushLabelWidth(100f);
                    EUtil.PushFieldWidth(20f);
                    cp.Sticky = EditorGUILayout.Toggle(new GUIContent("Sticky", "Prevent sliding on plane"), cp.Sticky);
                    cp.UseRotation = EditorGUILayout.Toggle(new GUIContent("Use Rotation", "Use the rotation from target object"), cp.UseRotation);
                    EUtil.PopFieldWidth();
                    EUtil.PopLabelWidth();
                }
                EditorGUILayout.EndHorizontal();

                // influence
                cp.Influence = EUtil.ProgressBar(cp.Influence, 0, 1f, "Influence: {0:F2}");
            }
            EUtil.PopGUIEnable();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cp); //so ConstraintStack.Update can be called in edit-mode
            }
        }
    }
}
