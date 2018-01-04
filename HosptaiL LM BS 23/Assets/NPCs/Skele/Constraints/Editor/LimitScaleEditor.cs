using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(LimitScale))]
    public class LimitScaleEditor : Editor
    {
        private bool m_foldoutLimit = (false);
        private bool m_foldoutSpace = (false);

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            LimitScale cp = (LimitScale)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint);
            {
                //affect X/Y/Z
                m_foldoutLimit = EditorGUILayout.Foldout(m_foldoutLimit, "Limits");
                if (m_foldoutLimit)
                {
                    ELimitAffect eAffect = cp.LimitAffect;
                    Vector3 limitMin = cp.LimitMin;
                    Vector3 limitMax = cp.LimitMax;

                    limitMin.x = EConUtil.DrawLimitField(ref eAffect, "XMin", "min value of X", limitMin.x, ELimitAffect.MinX);
                    limitMax.x = EConUtil.DrawLimitField(ref eAffect, "XMax", "max value of X", limitMax.x, ELimitAffect.MaxX);
                    limitMin.y = EConUtil.DrawLimitField(ref eAffect, "YMin", "min value of Y", limitMin.y, ELimitAffect.MinY);
                    limitMax.y = EConUtil.DrawLimitField(ref eAffect, "YMax", "max value of Y", limitMax.y, ELimitAffect.MaxY);
                    limitMin.z = EConUtil.DrawLimitField(ref eAffect, "ZMin", "min value of Z", limitMin.z, ELimitAffect.MinZ);
                    limitMax.z = EConUtil.DrawLimitField(ref eAffect, "ZMax", "max value of Z", limitMax.z, ELimitAffect.MaxZ);

                    EConUtil.LimitFieldMinMaxFix(eAffect, ref limitMin, ref limitMax);

                    cp.LimitAffect = eAffect;
                    cp.LimitMin = limitMin;
                    cp.LimitMax = limitMax;

                    GUILayout.Space(5f);
                }

                m_foldoutSpace = EditorGUILayout.Foldout(m_foldoutSpace, "Space Mapping");
                if (m_foldoutSpace)
                {
                    // owner space
                    cp.OwnerSpace = (ESpace)EditorGUILayout.EnumPopup("Owner Space", cp.OwnerSpace);
                    GUILayout.Space(5f);
                }

                //record modify
                cp.ModifyInternalData = EditorGUILayout.Toggle(new GUIContent("Modify Internal Data", "This modification will modify the ConstraintStack's internal transform data"), cp.ModifyInternalData);

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
