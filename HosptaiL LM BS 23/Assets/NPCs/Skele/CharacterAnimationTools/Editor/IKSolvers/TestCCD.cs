using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MH.Skele;

namespace MH
{
    public class TestCCD : EditorWindow
    {
        private Transform m_endJoint = null; 
        private int m_boneLen = 2;
        private CCDSolver m_solver;

        //[MenuItem("MH/TestCCD")]
        public static void OpenWindow()
        {
            var wnd = (TestCCD)GetWindow(typeof(TestCCD));
            wnd.m_solver = new CCDSolver();
            wnd.Show();
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnGUI()
        {
            if (m_solver == null)
                return;

            EditorGUI.BeginChangeCheck();
            m_endJoint = (Transform)EditorGUILayout.ObjectField("endJoint", m_endJoint, typeof(Transform), true);
            m_boneLen = EditorGUILayout.IntField("boneLen", m_boneLen);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_endJoint == null || m_boneLen <= 0)
                    return;

                m_solver.SetBones(m_endJoint, m_boneLen);
                m_solver.Target = m_endJoint.position;
                EUtil.ShowNotification("Set Bones: endJoint: " + m_endJoint.name + ", bonelen: " + m_boneLen);
            }

            if (GUILayout.Button("Return To endJoint"))
            {
                m_solver.Target = m_endJoint.position;
                EUtil.RepaintSceneView();
            }

            if (GUILayout.Button("Reset all rotation"))
            {
                var joints = m_solver.GetJoints();
                foreach (var j in joints)
                {
                    j.localRotation = Quaternion.identity;
                }
            }

            if (GUILayout.Button("GO"))
            {
                m_solver.Execute();
            }
        }


        private System.Collections.IEnumerator ie;
        void OnSceneGUI(SceneView sv)
        {
            if (m_solver == null)
            {
                Close();
                return;
            }
            if (m_solver.Count == 0)
                return;

            // draw the lines
            Handles.color = Pref.IKAngleConstraintArcColor;
            var joints = m_solver.GetJoints();
            for (int i = 0; i < joints.Length-1; ++i)
            {
                var p0 = joints[i].position;
                var p1 = joints[i + 1].position;
                Handles.DrawAAPolyLine(4f, p0, p1);
            }
            Handles.color = Color.white;

            Vector3 tgtPos = Handles.PositionHandle(m_solver.Target, Quaternion.identity);
            if (tgtPos != m_solver.Target)
            {
                m_solver.Target = tgtPos;
                //m_solver.Execute();
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown )
            {
                if (e.keyCode == KeyCode.T)
                {
                    if (ie == null )
                    {
                        ie = m_solver.DBGExecute();
                    }

                    if (ie.MoveNext() == false)
                    {
                        ie = null;
                    }
                }
                else if (e.keyCode == KeyCode.R)
                {
                    m_solver.dbg_interrupt = true;
                    if( ie != null )
                        ie.MoveNext();
                }
                else if (e.keyCode == KeyCode.G)
                {
                    while (ie != null && ie.MoveNext() == true)
                    {
                        ;
                    }
                    ie = null;
                }
            }
        }
    }
}
