using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH.Skele;

namespace MH.IKConstraint
{
    [CustomEditor(typeof(ConeConstraintMB))]
    public class ConeConstraintEditor : Editor, IOnSceneGUI
    {
		#region "data"
	    // data

        private static bool ms_inited = false;
        private static float ms_markerSize;
        private static Color ms_markerColor;
        private static bool ms_showDisplaySetting = false;
	
	    #endregion "data"
	
		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            if (!ms_inited)
            {
                ms_markerSize = Pref.IKConMarkerSize;
                ms_markerColor = Pref.IKConeConstraintColor;
                ms_showDisplaySetting = false;
                ms_inited = true;
            }

            //ConeConstraintMB mb = (ConeConstraintMB)target;
            //if (mb == null)
            //    return; //possible after switching scene

            //Transform mbtr = mb.transform;
            //if (mbtr.childCount > 0 && mb.nextJoint == null)
            //{
            //    var child = mbtr.GetChild(0);
            //    mb.nextJoint = child;
            //    _OnNextJointChanged();
            //    SceneView.RepaintAll();
            //}
        }

        public override void OnInspectorGUI()
        {
            ConeConstraintMB mb = (ConeConstraintMB)target;

            mb.enabled = EditorGUILayout.Toggle("Enabled", mb.enabled);

            EditorGUI.BeginChangeCheck();
            mb.nextJoint = EditorGUILayout.ObjectField("nextJoint", mb.nextJoint, typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                EUtil.RepaintSceneView();
            }

            if (mb.nextJoint == null)
            {
                EditorGUILayout.LabelField("Set the nextJoint first...");
                return;
            }
            else
            {
                EUtil.PushGUIEnable(mb.enabled);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(30f);
                    if (EUtil.Button("ReInit", "decide all the parameters with default method", Color.green))
                    {
                        Undo.RecordObject(mb, "AutoInit");
                        mb._AutoSetParameters();
                        EUtil.RepaintSceneView();
                    }
                    GUILayout.Space(30f);
                }
                EditorGUILayout.EndHorizontal();

                // angle limit
                EditorGUI.BeginChangeCheck();
                float newAngleLimit = EditorGUILayout.Slider(CONT_AngleLimit, mb.angleLimit, 0, 180f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(mb, "Modify Angle Limit");
                    mb.angleLimit = newAngleLimit;
                    EUtil.RepaintSceneView();
                }

                // ref-Axis
                EditorGUI.BeginChangeCheck();
                Vector3 newRefAxis = EUtil.DrawV3P(CONT_RefAxis, mb.refAxis);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(mb, "Modify ref axis");
                    mb.refAxis = newRefAxis;
                    mb.CalcInitData(); //!! recalc the startTwistRot
                    EUtil.RepaintSceneView();
                }

                mb.limitTwist = EditorGUILayout.Toggle(CONT_LimitTwist, mb.limitTwist);
                if (mb.limitTwist)
                {
                    float min = mb.minTwistLimit;
                    float max = mb.maxTwistLimit;
                    EditorGUI.BeginChangeCheck();
                    EUtil.DrawMinMaxSlider("Twist Limits", ref min, ref max, -180f, 180f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(mb, "Modify twist limits");
                        mb.minTwistLimit = min;
                        mb.maxTwistLimit = max;
                        EUtil.RepaintSceneView();
                    }

                    EditorGUILayout.LabelField("Current twist: " + mb.CalcTwist());
                }

                ms_showDisplaySetting = EditorGUILayout.Foldout(ms_showDisplaySetting, "Display Settings:");
                if (ms_showDisplaySetting)
                {
                    EditorGUI.BeginChangeCheck();
                    ms_markerSize = EditorGUILayout.FloatField("Marker size", ms_markerSize);
                    ms_markerColor = EditorGUILayout.ColorField("Marker color", ms_markerColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EUtil.RepaintSceneView();
                    }
                }

                EUtil.PopGUIEnable();
            }

            

        }

        public void OnSceneGUI()
        {
            ConeConstraintMB mb = (ConeConstraintMB)target;

            if (mb == null)
                return;

            Transform j = mb.transform;
            Transform jparent = j.parent;
            Transform jchild = mb.nextJoint;

            if (jchild == null)
                return;

            var saveColor = Handles.color;

            float szMul = EUtil.GetHandleSize(j.position, 3f) * ms_markerSize;

            Vector3 worldRefAxis = Misc.TransformDirection(jparent, mb.refAxis).normalized;

            //1. draw the refAxis
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(4f, j.position, j.position + worldRefAxis * szMul * 1.5f); //ref axis

            //2. draw the cone
            float angleLimit = mb.angleLimit;
            Handles.color = ms_markerColor;

            float zdist = Mathf.Cos(Mathf.Deg2Rad * angleLimit) * szMul;
            float radius = Mathf.Sin(Mathf.Deg2Rad * angleLimit) * szMul;
            Vector3 circleCenter = j.position + worldRefAxis * zdist;
            Handles.DrawWireDisc(circleCenter, worldRefAxis, radius);

            Vector3 pseudoXAxis = Vector3.Cross(Vector3.up, worldRefAxis).normalized;
            if (pseudoXAxis == Vector3.zero) pseudoXAxis = Vector3.right;
            Vector3 pseudoYAxis = Vector3.Cross(worldRefAxis, pseudoXAxis).normalized;

            Handles.DrawAAPolyLine(3f, j.position, circleCenter + radius * pseudoXAxis);
            Handles.DrawAAPolyLine(3f, j.position, circleCenter - radius * pseudoXAxis);
            Handles.DrawAAPolyLine(3f, j.position, circleCenter + radius * pseudoYAxis);
            Handles.DrawAAPolyLine(3f, j.position, circleCenter - radius * pseudoYAxis);

            Handles.color = saveColor;
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"



        #endregion "unity event handlers"

        #region "constant data"
        // constant data

        private readonly static GUIContent CONT_AngleLimit = new GUIContent("AngleLimit", "the angle limit between bone and refAxis");
        private readonly static GUIContent CONT_RefAxis = new GUIContent("RefAxis", "the reference axis");
        private readonly static GUIContent CONT_LimitTwist = new GUIContent("Limit Twist", "whether limit the twist around reference axis");

        #endregion "constant data"
    }
}
