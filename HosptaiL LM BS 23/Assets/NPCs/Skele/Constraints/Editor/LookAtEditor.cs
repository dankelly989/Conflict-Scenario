using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(LookAt))]
    public class LookAtEditor : Editor
    {
        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            LookAt cp = (LookAt)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {

                cp.LookAxis = (EAxisD)EConUtil.DrawEnumBtns(AllLookAxis, AllLookAxisStr, cp.LookAxis, "LookAxis", "the axis to point towards target");
                cp.UpAxis = (EAxisD)EConUtil.DrawEnumBtns(AllLookAxis, AllLookAxisStr, cp.UpAxis, "UpAxis", "the axis to point upward");

                cp.UpAxis = ConUtil.EnsureAxisNotColinear(cp.LookAxis, cp.UpAxis);

                GUILayout.Space(5f);

                //influence
                cp.Influence = EUtil.ProgressBar(cp.Influence, 0, 1f, "Influence: {0:F2}");

            }
            EUtil.PopGUIEnable();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cp); //so ConstraintStack.Update can be called in edit-mode
            }
        }

        private readonly static Enum[] AllLookAxis = { EAxisD.X, EAxisD.Y, EAxisD.Z, EAxisD.InvX, EAxisD.InvY, EAxisD.InvZ };
        private readonly static string[] AllLookAxisStr = { "+X", "+Y", "+Z", "-X", "-Y", "-Z" };
    }
}
