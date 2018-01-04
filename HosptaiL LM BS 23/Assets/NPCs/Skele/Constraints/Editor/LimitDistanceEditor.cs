using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(LimitDistance))]
    public class LimitDistanceEditor : Editor
    {
        private bool m_foldoutSpace = (false);

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            LimitDistance cp = (LimitDistance)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                // distance & reset
                EditorGUILayout.BeginHorizontal();
                cp.Distance = EditorGUILayout.FloatField(new GUIContent("Distance", "limit distance"), cp.Distance);
                if (GUILayout.Button(new GUIContent("R", "recalculate the distance"), EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    Transform tr = cp.transform;
                    cp.Distance = (tr.position - cp.Target.position).magnitude;
                }
                EditorGUILayout.EndHorizontal();

                // clamp region
                cp.ClampRegion = (EClampRegion)EditorGUILayout.EnumPopup(new GUIContent("Clamp Region", "decide how to limit the distance"), cp.ClampRegion);

                // space mapping
                m_foldoutSpace = EditorGUILayout.Foldout(m_foldoutSpace, "Space Mapping");
                if (m_foldoutSpace)
                {
                    // target space 
                    cp.TargetSpace = (ESpace)EditorGUILayout.EnumPopup("Target Space", cp.TargetSpace);

                    // owner space
                    cp.OwnerSpace = (ESpace)EditorGUILayout.EnumPopup("Owner Space", cp.OwnerSpace);
                    GUILayout.Space(5f);
                }

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
