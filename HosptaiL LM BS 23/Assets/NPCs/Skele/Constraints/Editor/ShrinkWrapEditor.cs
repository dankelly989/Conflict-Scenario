using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(ShrinkWrap))]
    public class ShrinkWrapEditor : Editor
    {
        void OnEnable()
        {
        }

        void OnDisable()
        {
            EUtil.GetSceneView().renderMode = DrawCameraMode.Textured;
        }

        public override void OnInspectorGUI()
        {
            ShrinkWrap cp = (ShrinkWrap)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            var newTarget = (Transform)EditorGUILayout.ObjectField("Target Obj", cp.Target, typeof(Transform), true);
            if (newTarget != null && 
                (newTarget.GetComponent<MeshFilter>() == null || newTarget.GetComponent<Collider>() == null)
                )
            {
                EUtil.ShowNotification("Target must have MeshFilter & Collider");
            }
            else
            {
                cp.Target = newTarget;
            }

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint && cp.Target);
            {
                cp.Method = (ShrinkWrap.EShrinkWrapMethod)EditorGUILayout.EnumPopup(new GUIContent("ShrinkWrap Method", "select the algorithm for the action"), cp.Method);
                if (cp.Method == ShrinkWrap.EShrinkWrapMethod.NearestVertex)
                    EUtil.GetSceneView().renderMode = DrawCameraMode.TexturedWire;
                else
                    EUtil.GetSceneView().renderMode = DrawCameraMode.Textured;

                cp.Distance = EditorGUILayout.FloatField(new GUIContent("Distance", "keep distance to the projected point"), cp.Distance);

                if (cp.Method == ShrinkWrap.EShrinkWrapMethod.Project)
                {
                    cp.ProjectDir = (EAxisD)EConUtil.DrawEnumBtns(EConUtil.AxisDs, EConUtil.AxisDStrs, cp.ProjectDir, "ProjectDir", "the direction or project ray, from origin of owner");
                    cp.OwnerSpace = (ESpace)EditorGUILayout.EnumPopup(new GUIContent("OwnerSpace", "the space used for project dir"), cp.OwnerSpace);
                    cp.MaxProjectDistance = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Max Project Dist", "only execute wrap when the projected point is within the dist; 0 means infinite distance"), cp.MaxProjectDistance));
                }
                else if (cp.Method == ShrinkWrap.EShrinkWrapMethod.NearestVertex)
                {
                    //nothing here
                }

                //cp.ModifyInitInfo = EditorGUILayout.Toggle(new GUIContent("Modify InitInfo", "the constraint result will be written back to the initInfo"), cp.ModifyInitInfo);

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
