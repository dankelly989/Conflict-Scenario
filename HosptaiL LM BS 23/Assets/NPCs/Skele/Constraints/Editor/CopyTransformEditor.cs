using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(CopyTransform))]
    public class CopyTransformEditor : Editor
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
            CopyTransform cp = (CopyTransform)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);
            if (cp.Target && !ConstraintEditorUtil.IsTargetHasAllUniformScaleInHierarchy(cp.Target))
                ConstraintEditorUtil.NonUniformScaleWarning(cp.Target);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
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
