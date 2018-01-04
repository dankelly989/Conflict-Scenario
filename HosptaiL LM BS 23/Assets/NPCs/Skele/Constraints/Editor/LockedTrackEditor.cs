using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(LockedTrack))]
    public class LockedTrackEditor : Editor
    {

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            LockedTrack cp = (LockedTrack)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                cp.LookAxis = (EAxisD)EConUtil.DrawEnumBtns(EConUtil.AxisDs, EConUtil.AxisDStrs, cp.LookAxis, "LookAxis", "Owner's local axis used to aim at the target", 80f);
                GUILayout.Space(5f);

                cp.RotateAxis = (EAxisD)EConUtil.DrawEnumBtns(EConUtil.AxisDsPositive, EConUtil.AxisDStrsPositive, cp.RotateAxis, "Rotate Axis", "Owner's local axis used to rotate around", 80f);
                GUILayout.Space(5f);

                cp.RotateAxis = ConUtil.EnsureAxisNotColinear(cp.LookAxis, cp.RotateAxis);

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
