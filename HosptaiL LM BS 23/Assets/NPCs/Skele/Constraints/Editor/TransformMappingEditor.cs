using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(TransformMapping))]
    public class TransformMappingEditor : Editor
    {
        private bool m_foldoutMapping = (false);
        private bool m_foldoutSpace = (false);
        private bool m_foldoutSrcRange = (false);
        private bool m_foldoutDstRange = (false);

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            TransformMapping cp = (TransformMapping)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);
            if (cp.Target && !ConstraintEditorUtil.IsTargetHasAllUniformScaleInHierarchy(cp.Target))
                ConstraintEditorUtil.NonUniformScaleWarning(cp.Target);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                //mapping
                m_foldoutMapping = EditorGUILayout.Foldout(m_foldoutMapping, "Mapping");
                if (m_foldoutMapping)
                {
                    //source (from target)
                    ETransformData eSrcType = cp.SrcDataType;
                    EConUtil.DrawChooseTransformData(ref eSrcType, "Src data type", "which data of transform of target is taken as source");
                    cp.SrcDataType = eSrcType;

                    //destination (to owner)
                    ETransformData eDstType = cp.DstDataType;
                    EConUtil.DrawChooseTransformData(ref eDstType, "Dst data type", "which data of transform of target is taken as source");
                    cp.DstDataType = eDstType;

                    // XYZ mapping
                    GUILayout.Label("Source to Destination Mapping:");
                    for(int i=0; i<3; ++i)
                    {
                        EditorGUILayout.BeginHorizontal();
                            cp.Mapping[i] = (EAxis)EditorGUILayout.EnumPopup(cp.Mapping[i]);
                            GUILayout.Label(" >> " + (char)('X' + i));
                        EditorGUILayout.EndHorizontal();
                    }

                    // extrapolate
                    cp.Extrapolate = EditorGUILayout.Toggle(new GUIContent("Extrapolate", "will extend the range outside specified range"), cp.Extrapolate);

                    GUILayout.Space(5f);
                }

                //source data range
                m_foldoutSrcRange = EditorGUILayout.Foldout(m_foldoutSrcRange, "Source Range");
                if( m_foldoutSrcRange )
                {
                    Vector3 srcFrom = cp.SrcFrom;
                    Vector3 srcTo = cp.SrcTo;
                    EConUtil.DrawAxisRange(ref srcFrom, ref srcTo);
                    cp.SrcFrom = srcFrom;
                    cp.SrcTo = srcTo;
                    GUILayout.Space(5f);
                }

                //dest data range
                m_foldoutDstRange = EditorGUILayout.Foldout(m_foldoutDstRange, "Destination Range");
                if (m_foldoutDstRange)
                {
                    Vector3 dstFrom = cp.DstFrom;
                    Vector3 dstTo = cp.DstTo;
                    EConUtil.DrawAxisRange(ref dstFrom, ref dstTo);
                    cp.DstFrom = dstFrom;
                    cp.DstTo = dstTo;
                    GUILayout.Space(5f);
                }

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
