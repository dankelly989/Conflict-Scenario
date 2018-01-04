using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(LimitRotation))]
    public class LimitRotationEditor : Editor
    {
        private bool m_foldoutLimit = (false);
        //private bool m_foldoutSpace = (false);

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            LimitRotation cp = (LimitRotation)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint);
            {
                //affect X/Y/Z
                m_foldoutLimit = EditorGUILayout.Foldout(m_foldoutLimit, "Limits");
                if (m_foldoutLimit)
                {
                    ELimitEuler eLimit = cp.LimitEuler;
                    Vector3 limitMin = cp.LimitMin;
                    Vector3 limitMax = cp.LimitMax;

                    EConUtil.DrawEulerLimitField(ref eLimit, "X", ref limitMin, ref limitMax, ELimitEuler.X, -180f, 180f);
                    EConUtil.DrawEulerLimitField(ref eLimit, "Y", ref limitMin, ref limitMax, ELimitEuler.Y, -180f, 180f);
                    EConUtil.DrawEulerLimitField(ref eLimit, "Z", ref limitMin, ref limitMax, ELimitEuler.Z, -180f, 180f);
                    
                    cp.LimitEuler = eLimit;
                    cp.LimitMin = limitMin;
                    cp.LimitMax = limitMax;

                    GUILayout.Space(5f);
                }

                //space mapping
                //m_foldoutSpace = EditorGUILayout.Foldout(m_foldoutSpace, "Space Mapping");
                //if (m_foldoutSpace)
                //{
                //    // owner space
                //    cp.OwnerSpace = (ESpace)EditorGUILayout.EnumPopup("Owner Space", cp.OwnerSpace);
                //    GUILayout.Space(5f);
                //}

                //record modify
                //cp.ModifyInternalData = EditorGUILayout.Toggle(new GUIContent("Modify Internal Data", "This modification will modify the ConstraintStack's internal transform data"), cp.ModifyInternalData);

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
