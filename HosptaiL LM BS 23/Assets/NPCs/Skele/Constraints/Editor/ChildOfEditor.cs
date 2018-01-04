using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(ChildOf))]
    public class ChildOfEditor : Editor
    {
        private EDOBool m_foldoutAffect;

        void OnEnable()
        {
            m_foldoutAffect = EDOBool.DFGet(GetType().FullName + ".m_foldoutAffect", true);
        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            ChildOf cp = (ChildOf)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);

            if (cp.Target && !ConstraintEditorUtil.IsTargetHasAllUniformScaleInHierarchy(cp.Target))
                ConstraintEditorUtil.NonUniformScaleWarning(cp.Target);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                //affect X/Y/Z
                m_foldoutAffect.val = EditorGUILayout.Foldout(m_foldoutAffect.val, "Affect");
                if (m_foldoutAffect.val)
                {

                    cp.AffectPos = EConUtil.DrawAxisBtnMask(new GUIContent("Position", "which fields of position are affected"), cp.AffectPos);
                    cp.AffectRot = EConUtil.DrawAxisBtnMask(new GUIContent("Rotation", "which fields of rotation are affected"), cp.AffectRot);
                    cp.AffectSca = EConUtil.DrawAxisBtnMask(new GUIContent("Scale", "which fields of scale are affected"), cp.AffectSca);

                    GUILayout.Space(5f);
                }

                // influence
                cp.Influence = EUtil.ProgressBar(cp.Influence, 0, 1f, "Influence: {0:F2}");
            }
            EUtil.PopGUIEnable();

            var pseuLocTr = cp.PseudoLocTr;
            pseuLocTr.pos = EUtil.DrawV3P(new GUIContent("position", "the pseudo local position"), pseuLocTr.pos);
            pseuLocTr.rot = Quaternion.Euler(EUtil.DrawV3P(new GUIContent("rotation", "the pseudo local rotation"), pseuLocTr.rot.eulerAngles));
            pseuLocTr.scale = EUtil.DrawV3P(new GUIContent("scale", "the pseudo local scale"), pseuLocTr.scale);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(50f);
                if (GUILayout.Button(new GUIContent("Sample Data", "Use current transform data to calculate the pseudo local transform's data"), EditorStyles.toolbarButton))
                {
                    cp.RecalcPseudoLocalTransformData();
                }
                GUILayout.Space(50f);
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cp); //so ConstraintStack.Update can be called in edit-mode
            }
        }
    }
}
