using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH.Curves;

namespace MH.Constraints
{
    [CustomEditor(typeof(FollowPath))]
    public class FollowPathEditor : Editor
    {
        private Editor m_splineEditor = null;

        void OnEnable()
        {

        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            FollowPath cp = (FollowPath)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Spline = (BaseSplineBehaviour)EditorGUILayout.ObjectField("Target Spline", cp.Spline, typeof(BaseSplineBehaviour), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Spline);
            {
                EUtil.PushLabelWidth(100f);
                cp.Offset = EditorGUILayout.Slider(new GUIContent("Offset", "t parameter for the spline"), cp.Offset, 0, 1f);

                //axis and offset
                cp.FollowCurve = EditorGUILayout.Toggle(new GUIContent("Follow Curve", "owner's rotation will follow the spline"), cp.FollowCurve);
                if (cp.FollowCurve)
                {
                    cp.ForwardDir = (EAxisD)EConUtil.DrawEnumBtns(AllAxis, AllAxisStr, cp.ForwardDir, "Forward Axis", "the axis of owner, which will be taken as the forward direction when follow the spline");
                    cp.UpDir = (EAxisD)EConUtil.DrawEnumBtns(AllAxis, AllAxisStr, cp.UpDir, "Up Axis", "the axis of owner, which will be taken as the up direction when follow the spline");

                    cp.UpDir = ConUtil.EnsureAxisNotColinear(cp.ForwardDir, cp.UpDir);

                    GUILayout.Space(5f);
                }
                EUtil.PopLabelWidth();

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
            FollowPath cp = (FollowPath)target;
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

        private readonly static Enum[] AllAxis = { EAxisD.X, EAxisD.Y, EAxisD.Z, EAxisD.InvX, EAxisD.InvY, EAxisD.InvZ};
        private readonly static string[] AllAxisStr = { "+X", "+Y", "+Z", "-X", "-Y", "-Z" };
    }
}
