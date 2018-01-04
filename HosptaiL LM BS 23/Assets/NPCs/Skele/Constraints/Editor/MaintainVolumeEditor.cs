using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(MaintainVolume))]
    public class MaintainVolumeEditor : Editor
    {
        private bool m_foldoutAxis = (true);
        private bool m_foldoutSpace = (false);

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            MaintainVolume cp = (MaintainVolume)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint);
            {
                // base volume
                cp.BaseVolume = EditorGUILayout.FloatField(new GUIContent("Base", "the base value of volume, the product of xyz component of scale"), cp.BaseVolume);
                cp.BaseVolume = Mathf.Max(cp.BaseVolume, 0.01f);
                
                //affect X/Y/Z
                m_foldoutAxis = EditorGUILayout.Foldout(m_foldoutAxis, "Axis");
                if (m_foldoutAxis)
                {
                    EAxis eAffect = cp.FreeAxis;

                    EditorGUILayout.BeginHorizontal();
                    {
                        EUtil.PushBackgroundColor(eAffect == EAxis.X ? EConUtil.kSelectedBtnColor : Color.white);
                        if (GUILayout.Button(new GUIContent("X", "X axis as main axis"), EditorStyles.toolbarButton))
                        {
                            eAffect = EAxis.X;
                        }
                        EUtil.PopBackgroundColor();

                        EUtil.PushBackgroundColor(eAffect == EAxis.Y ? EConUtil.kSelectedBtnColor : Color.white);
                        if (GUILayout.Button(new GUIContent("Y", "Y axis as main axis"), EditorStyles.toolbarButton))
                        {
                            eAffect = EAxis.Y;
                        }
                        EUtil.PopBackgroundColor();

                        EUtil.PushBackgroundColor(eAffect == EAxis.Z ? EConUtil.kSelectedBtnColor : Color.white);
                        if (GUILayout.Button(new GUIContent("Z", "Z axis as main axis"), EditorStyles.toolbarButton))
                        {
                            eAffect = EAxis.Z;
                        }
                        EUtil.PopBackgroundColor();
                    }
                    EditorGUILayout.EndHorizontal();

                    cp.FreeAxis = eAffect;

                    GUILayout.Space(5f);
                }

                // vol mul
                cp.VolMul = EditorGUILayout.FloatField(new GUIContent("Multiplier", "extra multiplier on base volume"), cp.VolMul);

                // space 
                m_foldoutSpace = EditorGUILayout.Foldout(m_foldoutSpace, "Space Mapping");
                if (m_foldoutSpace)
                {
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
