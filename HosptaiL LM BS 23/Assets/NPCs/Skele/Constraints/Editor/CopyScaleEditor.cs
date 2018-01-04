using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(CopyScale))]
    public class CopyScaleEditor : Editor
    {
        private EDOBool m_foldoutAffect;
        private EDOBool m_foldoutSpace;

        void OnEnable()
        {
            m_foldoutAffect = EDOBool.DFGet(GetType().FullName + ".m_foldoutAffect", true);
            m_foldoutSpace = EDOBool.DFGet(GetType().FullName + ".m_foldoutSpace", true);
        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            CopyScale cp = (CopyScale)target;

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
                    EUtil.PushLabelWidth(12f);
                    EUtil.PushFieldWidth(16f);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EAxisD eAffect = cp.Affect;

                        eAffect = EConUtil.DrawAffectField(eAffect, "+X", "apply X from target to owner", EAxisD.X, EAxisD.InvX);
                        eAffect = EConUtil.DrawAffectField(eAffect, "-X", "apply -X from target to owner", EAxisD.InvX, EAxisD.X);
                        eAffect = EConUtil.DrawAffectField(eAffect, "+Y", "apply Y from target to owner", EAxisD.Y, EAxisD.InvY);
                        eAffect = EConUtil.DrawAffectField(eAffect, "-Y", "apply -Y from target to owner", EAxisD.InvY, EAxisD.Y);
                        eAffect = EConUtil.DrawAffectField(eAffect, "+Z", "apply Z from target to owner", EAxisD.Z, EAxisD.InvZ);
                        eAffect = EConUtil.DrawAffectField(eAffect, "-Z", "apply -Z from target to owner", EAxisD.InvZ, EAxisD.Z);

                        cp.Affect = eAffect;
                    }
                    EditorGUILayout.EndHorizontal();
                    EUtil.PopFieldWidth();
                    EUtil.PopLabelWidth();

                    // offset
                    cp.UseOffset = EditorGUILayout.Toggle(new GUIContent("Use Offset", "Add offset onto the result"), cp.UseOffset);
                    if (cp.UseOffset)
                    {
                        cp.Offset = EUtil.DrawV3P(new GUIContent("Offset", "Offset in owner space"), cp.Offset);
                    }

                    GUILayout.Space(5f);
                }


                m_foldoutSpace.val = EditorGUILayout.Foldout(m_foldoutSpace.val, "Space Mapping");
                if (m_foldoutSpace.val)
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
