using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Curves
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CatmullRomUniformBehaviour))]
    public class CatmullRomUniformBehaviourEditor : Editor, ISplineEditor
    {
        private static bool ms_foldoutSettings = false;
        private static bool ms_foldoutTSlider = false;

        private float m_tSlider = 0;

        private static float ms_arrowLineLen = 0.4f; //the binormal len's length
        private static bool ms_drawArrow = true;
        private static bool ms_drawUp = false;

        private static bool ms_drawPts = true;
        private static float ms_ptSize = 0.1f;

        [SerializeField]
        private int m_curPtIdx = -1; // selected point, -1 means no selection

        private static float ms_lookAtDist = 5f;

        private static bool ms_isMultiEdit = false;

        #region "unity callbacks"

        void OnEnable()
        {
            Undo.undoRedoPerformed += _OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= _OnUndoRedo;
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Cannot edit multiple spline together", MessageType.Info, true);
                ms_isMultiEdit = true;
                return;
            }
            else
            {
                ms_isMultiEdit = false;
            }

            CatmullRomUniformBehaviour behaviour = (CatmullRomUniformBehaviour)target;
            CatmullRomUniform spline = (CatmullRomUniform)behaviour.Spline;

            GUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Add Point", "hotkey: I"), EditorStyles.toolbarButton))
                {
                    _AddPoint(spline);
                }
                if (GUILayout.Button(new GUIContent("Del Point", "hotkey: X"), EditorStyles.toolbarButton))
                {
                    _DelPoint(spline);
                }
                if (GUILayout.Button(spline.Cycle ? "Break Cycle" : "Make Cycle", EditorStyles.toolbarButton))
                {
                    _ToggleCycle(spline);
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5f);

            if( m_curPtIdx >= 0 )
            {
                EditorGUILayout.BeginHorizontal();
                {
                    MUndo.RecordObject(this, "change current point");
                    GUILayout.Label("Current Point");
                    if(GUILayout.Button("<<"))
                    {
                        m_curPtIdx = Mathf.Max(0, m_curPtIdx-1);
                    }

                    EditorGUI.BeginChangeCheck();
                    m_curPtIdx = EditorGUILayout.IntField(m_curPtIdx);
                    if (EditorGUI.EndChangeCheck())
                        m_curPtIdx = Mathf.Clamp(m_curPtIdx, 0, spline.PointCount);

                    GUILayout.Label(" / " + (spline.PointCount-1));
                    if (GUILayout.Button(">>"))
                    {
                        m_curPtIdx = Mathf.Min(m_curPtIdx + 1, spline.PointCount - 1);
                    }
                }
                EditorGUILayout.EndHorizontal();

                MUndo.RecordObject(target, "modify control point");
                EUtil.PushLabelWidth(80f);
                spline[m_curPtIdx] = EUtil.DrawV3P(new GUIContent("Position"), spline[m_curPtIdx]);
                spline.SetTilt(m_curPtIdx, EditorGUILayout.FloatField("Tilt", spline.GetTilt(m_curPtIdx)));
                EUtil.PopLabelWidth();
            }

            ms_foldoutSettings = EditorGUILayout.Foldout(ms_foldoutSettings, "Settings");
            if (ms_foldoutSettings)
            {
                MUndo.RecordObject(this, "change setting");
                spline.Resolution = EditorGUILayout.IntField("Point/Seg", spline.Resolution);
                spline.TwistMtd = (ETwistMethod)EditorGUILayout.EnumPopup(new GUIContent("Twist Method", "Decide how to calculate the base-up direction before applying tilt"), spline.TwistMtd);
                ms_drawArrow = EditorGUILayout.Toggle("Draw Arrow", ms_drawArrow);
                ms_drawUp = EditorGUILayout.Toggle("Draw Up", ms_drawUp);
                ms_arrowLineLen = EditorGUILayout.FloatField("Arrow LineLen", ms_arrowLineLen);

                ms_drawPts = EditorGUILayout.Toggle("Draw points", ms_drawPts);
                ms_ptSize = EditorGUILayout.FloatField("Point size", ms_ptSize);

                ms_lookAtDist = EditorGUILayout.FloatField(new GUIContent("LookAt dist", "the distance from camera to target point when camera in follow mode"), ms_lookAtDist);
            }

            ms_foldoutTSlider = EditorGUILayout.Foldout(ms_foldoutTSlider, "T slider");
            if (ms_foldoutTSlider)
            {
                EditorGUI.BeginChangeCheck();
                EUtil.PushLabelWidth(20f);
                m_tSlider = EditorGUILayout.Slider("T", m_tSlider, 0, 1f);
                EUtil.PopLabelWidth();
                if (EditorGUI.EndChangeCheck())
                {
                    //Transform tr = behaviour.transform;
                    EUtil.SceneViewLookAt(
                        behaviour.GetTransformedPosition(m_tSlider),
                        Quaternion.LookRotation(behaviour.GetTransformedTangent(m_tSlider), behaviour.GetTransformedUp(m_tSlider)),
                        ms_lookAtDist);
                }
            }

            if (GUI.changed)
                EUtil.RepaintSceneView();
        }

        /// <summary>
        /// 1. draw spline
        /// 2. draw points
        /// 3. draw handles
        /// </summary>
        public void OnSceneGUI()
        {
            CatmullRomUniformBehaviour behaviour = (CatmullRomUniformBehaviour)target;
            CatmullRomUniform spline = (CatmullRomUniform)behaviour.Spline;
            Transform tr = behaviour.transform;

            DrawSpline(spline, tr);

            if (!ms_isMultiEdit)
            {

                DrawPoints(spline, tr);

                DrawHandles(spline, tr);

                // prevent de-selection by clicking
                EUtil.SceneViewPreventDeselecByClick(m_curPtIdx >= 0);

                // shortcut
                Event e = Event.current;
                switch (e.type)
                {
                    case EventType.KeyUp:
                        {
                            if (e.keyCode == KeyCode.X)
                            {
                                if (m_curPtIdx >= 0)
                                    _DelPoint(spline);
                            }
                            else if (e.keyCode == KeyCode.I)
                            {
                                if (m_curPtIdx < 0)
                                    m_curPtIdx = spline.PointCount - 1;
                                _AddPoint(spline);
                            }
                            else if (e.keyCode == KeyCode.C)
                            {
                                if (m_curPtIdx >= 0)
                                    EUtil.SceneViewLookAt(tr.TransformPoint(spline[m_curPtIdx]), ms_lookAtDist);
                            }
                        }
                        break;
                }
            }
        }

        #endregion "unity callbacks"

        private void DrawHandles(CatmullRomUniform spline, Transform tr)
        {
            if (m_curPtIdx >= 0 && m_curPtIdx < spline.PointCount)
            {
                Tools.current = Tool.None;
            }
            else
            {
                m_curPtIdx = -1;
                return;
            }

            Event e = Event.current;
            if ( e.keyCode == KeyCode.Escape && e.rawType == EventType.KeyDown)
            {
                m_curPtIdx = -1;
                return;
            }

            Vector3 pt = tr.TransformPoint(spline[m_curPtIdx]);

            pt = Handles.PositionHandle(pt, Quaternion.identity);
            if (GUI.changed)
            {
                MUndo.RecordObject(target, "move point"); 
                spline[m_curPtIdx] = tr.InverseTransformPoint(pt);
            }
        }

        private void DrawPoints(CatmullRomUniform spline, Transform tr)
        {
            if (!ms_drawPts)
                return;

            Camera sceneViewCam = Camera.current;
            Transform camTr = sceneViewCam.transform;

            // draw point and selection
            EUtil.PushHandleColor(SplineConst.SplinePtColor);
            for (int i = 0; i < spline.PointCount; ++i)
            {
                if (spline.Cycle && i == spline.PointCount - 1) //don't draw button for last point if is cycle
                    continue;

                bool isSelPoint = false;
                if (i == m_curPtIdx)
                {
                    isSelPoint = true;
                    EUtil.PushHandleColor(Handles.selectedColor);
                }

                Vector3 pt = tr.TransformPoint(spline[i]);
                if (Handles.Button(pt, camTr.rotation, ms_ptSize, ms_ptSize, Handles.DotCap))
                {
                    m_curPtIdx = i;
                    Repaint();
                }

                if (isSelPoint)
                    EUtil.PopHandleColor();
            }
            EUtil.PopHandleColor();

            if (ms_foldoutTSlider)
            {
                Vector3 tPos = tr.TransformPoint(spline.Interp(m_tSlider));
                Handles.CircleCap(GUIUtility.GetControlID(FocusType.Passive), tPos, camTr.rotation, ms_ptSize);
            }
        }

        public static void DrawSpline(CatmullRomUniform spline, Transform owner)
        {
            List<Vector3> interPts = spline.InterPoints;
            List<Vector3> tangents = spline.InterTangents;
            List<Vector3> ups = spline.InterUps;
            Vector3 p0, p1;

            Vector3 prevBino = Vector3.right;
            p0 = owner.TransformPoint(interPts[0]);
            for (int i = 0; i < interPts.Count; ++i)
            {
                Vector3 tan = owner.TransformDirection(tangents[i]).normalized;
                Vector3 up = owner.TransformDirection(ups[i]).normalized;
                Vector3 bino = Vector3.Cross(up, tan).normalized;

                if (bino == Vector3.zero)
                    bino = prevBino;
                else
                    prevBino = bino;

                if (ms_drawArrow)
                {
                    Handles.DrawLine(p0, p0 + (bino - tan) * ms_arrowLineLen);
                    Handles.DrawLine(p0, p0 - (bino + tan) * ms_arrowLineLen);
                }
                if (ms_drawUp)
                {
                    Handles.DrawLine(p0, p0 + up * ms_arrowLineLen);
                }

                if (i + 1 < interPts.Count)
                {
                    p1 = owner.TransformPoint(interPts[i + 1]);
                    Handles.DrawLine(p0, p1);
                    p0 = p1;
                }
            }
        }

        private void _DelPoint(CatmullRomUniform spline)
        {
            if (spline.PointCount <= 2)
            {
                EUtil.ShowNotification("The spline needs at least 2 points");
                return;
            }
            if (spline.Cycle && spline.PointCount <= 4)
            {
                EUtil.ShowNotification("Cannot delete any points to maintain cycle");
                return;
            }
            if (m_curPtIdx < 0)
                return;

            MUndo.RecordObject(target, "del point");
            MUndo.RecordObject(this, "del point");
            spline.RemovePoint(m_curPtIdx);
            m_curPtIdx = Mathf.Min(spline.PointCount - 1, m_curPtIdx);
        }

        private void _AddPoint(CatmullRomUniform spline)
        {
            MUndo.RecordObject(target, "add point");
            MUndo.RecordObject(this, "add point");

            if (m_curPtIdx < 0)
                m_curPtIdx = spline.PointCount - 1;

            if (m_curPtIdx != spline.PointCount - 1)
            {
                float v = (float)m_curPtIdx + 0.5f;
                Vector3 newPos = spline.Interp(v / (spline.PointCount -1));
                spline.InsertPointAfter(m_curPtIdx, newPos);
            }
            else
            {
                float unitLen = spline.CurveLength / (spline.PointCount - 1);
                Vector3 dir = spline.Tangent(1f);
                spline.InsertPointAfter(m_curPtIdx, spline[m_curPtIdx] + dir * unitLen);
            }

            m_curPtIdx++;
        }

        private void _ToggleCycle(CatmullRomUniform spline)
        {
            if (!spline.Cycle && spline.PointCount < 3)
            {
                EUtil.ShowNotification("Need at least 3 points to make cycle");
                return;
            }

            spline.Cycle = !spline.Cycle;

            if (m_curPtIdx >= spline.PointCount)
                m_curPtIdx = spline.PointCount - 1;
        }

        private void _OnUndoRedo()
        {
            CatmullRomUniformBehaviour behaviour = (CatmullRomUniformBehaviour)target;
            if (behaviour == null)
            {
                Undo.undoRedoPerformed -= _OnUndoRedo;
                return;
            }

            CatmullRomUniform spline = (CatmullRomUniform)behaviour.Spline;
            if (spline == null)
            {
                Undo.undoRedoPerformed -= _OnUndoRedo;
                return;
            }

            spline.SetDirty();
        }
    }
}