using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH.Curves;

namespace MH.Constraints
{
    [CustomEditor(typeof(ClampTo))]
    public class ClampToEditor : Editor
    {
        private bool m_foldoutAxis = (true);
        private bool m_foldoutPos = (true);
        private Editor m_splineEditor = null;

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            ClampTo cp = (ClampTo)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Spline = (BaseSplineBehaviour)EditorGUILayout.ObjectField("Target Spline", cp.Spline, typeof(BaseSplineBehaviour), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Spline);
            {
                //axis and offset
                m_foldoutAxis = EditorGUILayout.Foldout(m_foldoutAxis, "Affect");
                if (m_foldoutAxis)
                {
                    cp.MainAxis = (EAxis)EConUtil.DrawEnumBtns(AllAxis, AllAxisStr, cp.MainAxis, "Main Axis", "Choose the axis used to decide the parameter t for evaluate spline");

                    // offset
                    cp.UseOffset = EditorGUILayout.Toggle(new GUIContent("Use Offset", "Add offset onto the result"), cp.UseOffset);
                    if (cp.UseOffset)
                    {
                        cp.Offset = EUtil.DrawV3P(new GUIContent("Offset", "Offset in world space"), cp.Offset);
                    }

                    cp.Cyclic = EditorGUILayout.Toggle(new GUIContent("Cyclic", "the object will revert to the beginning of spline when exceed the dimension"), cp.Cyclic);

                    GUILayout.Space(5f);
                }

                // dimension and startVal
                m_foldoutPos = EditorGUILayout.Foldout(m_foldoutPos, "Pos Define");
                if (m_foldoutPos)
                {
                    cp.Dimension = EditorGUILayout.FloatField(new GUIContent("Dimension", "the projection length on mainAxis"), cp.Dimension);
                    cp.StartVal = EditorGUILayout.FloatField(new GUIContent("StartVal", "the low value on mainAxis"), cp.StartVal);
                    if (GUILayout.Button(new GUIContent("Recalculate Dimension", "Recalculate dimension & startVal based on spline")))
                    {
                        var behaviour = cp.Spline;
                        Bounds bd = behaviour.CalculateTransformedBounds();
                        _SetDimensionByBounds(cp, bd);
                    }
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

        void OnSceneGUI()
        {
            ClampTo cp = (ClampTo)target;
            if (!cp.ShowGizmos)
            {
                return;
            }

            BaseSplineBehaviour be = cp.Spline;
            if (be == null)
                return;

            if (m_splineEditor == null || m_splineEditor.target != be)
                m_splineEditor = Editor.CreateEditor(be);

            ISplineEditor ed = m_splineEditor as ISplineEditor;
            if (ed == null)
                return;

            EditorGUI.BeginChangeCheck();
            ed.OnSceneGUI();
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(be); //without this, when dragging spline control points, the object will not follow the changed spline
        }

        private void _SetDimensionByBounds(ClampTo cp, Bounds bd)
        {
            switch (cp.MainAxis)
            {
                case EAxis.X: cp.Dimension = bd.size.x; cp.StartVal = bd.min.x; break;
                case EAxis.Y: cp.Dimension = bd.size.y; cp.StartVal = bd.min.y; break;
                case EAxis.Z: cp.Dimension = bd.size.z; cp.StartVal = bd.min.z; break;
                default: Dbg.LogErr("ClampToEditor._SetDimensionByBounds: unexpected mainAxis: {0}", cp.MainAxis); break;
            }
        }

        private readonly static Enum[] AllAxis = { EAxis.X, EAxis.Y, EAxis.Z};
        private readonly static string[] AllAxisStr = { "X", "Y", "Z"};
    }
}
