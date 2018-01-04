using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using ExtMethods;
using MH.Skele;

namespace MH.Constraints
{
    /// <summary>
    /// will draw the bone lines and call each enabled constraints' OnSceneGUI
    /// </summary>
    [CustomEditor(typeof(CCDSolverMB))]
    public class CCDSolverEditor : Editor
    {
		#region "data"
	    // data

        public enum EPanel
        {
            Normal, 
            Continuous,
            Manual,
            Debug,

            END,
        }

        private IEnumerator m_dbgStepIE = null;
        private EDOFloat m_markerSize;

        private EPanel m_panel = EPanel.Normal;

        private bool m_manualSetJoints = false;
        private int m_autoCollectBoneCount = 2; // this is used to auto-collect joints to set m_jointList

	    #endregion "data"

		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            if (!Application.isPlaying)//cannot let editor script to mess with in-game logic
            { 
                CCDSolverMB mb = (CCDSolverMB)target;
                if (mb == null)
                    return; //possible after switching scene

                var solver = mb.GetSolver();
                if( solver != null )
                    solver.RefreshConstraint();

                m_autoCollectBoneCount = mb.boneCount;
                m_manualSetJoints = !_CheckIfBoneContinuous(mb);
            }

            m_markerSize = EDOFloat.DFGet("CCDSolverEditor.m_markerSize", 0.01f);
            
        }

        

        public override void OnInspectorGUI()
        {
            CCDSolverMB cp = (CCDSolverMB)target;

            EditorGUI.BeginChangeCheck();

            EConUtil.DrawActiveLine(cp);

            //constraint target
            cp.Target = (Transform)EditorGUILayout.ObjectField("Target Object", cp.Target, typeof(Transform), true);

            EUtil.DrawSplitter();

            EUtil.PushGUIEnable(cp.IsActiveConstraint);
            {
                // reset button
                GUILayout.BeginHorizontal();
                GUILayout.Space(30f);
                if (EUtil.Button("Recollect IKConstraints", "click this when add new IK-constraint on this IK-link", Color.green))
                {
                    cp.GetSolver(true);
                    EUtil.RepaintSceneView();
                }
                GUILayout.Space(30f);
                GUILayout.EndHorizontal();


                // jointList
                if (!m_manualSetJoints)
                {
                    m_autoCollectBoneCount = EditorGUILayout.IntField(CONT_BoneCnt, m_autoCollectBoneCount);
                }
                else
                {
                    var jointList = cp.jointList;
                    Undo.RecordObject(cp, "JointList");
                    for (int i = 0; i < jointList.Count; ++i)
                    {
                        EditorGUILayout.BeginHorizontal();
                        jointList[i] = (Transform)EditorGUILayout.ObjectField(jointList[i], typeof(Transform), true);
                        if (GUILayout.Button(new GUIContent("+", "add new entry")))
                        {
                            jointList.Insert(i, null);
                            GUIUtility.ExitGUI();
                        }
                        if (GUILayout.Button(new GUIContent("-", "delete this entry")))
                        {
                            jointList.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(m_manualSetJoints ? CONT_ManualSetJoints : CONT_AutoSetJoints, EditorStyles.toolbarButton))
                {
                    m_manualSetJoints = !m_manualSetJoints;
                }

                bool isIdentical = _CheckJointListIdentical(cp);
                EUtil.PushBackgroundColor(isIdentical ? Color.white : Color.blue);
                if (GUILayout.Button(new GUIContent("Apply", "Apply the joint list"), EditorStyles.toolbarButton))
                {
                    if( m_manualSetJoints )
                    {
                        if( _CheckManualSetJointList(cp) )
                            cp._RenewInitInfoAndSolver();
                    }
                    else
                    {
                        cp._RenewByCollectJointsWithBontCount(m_autoCollectBoneCount);
                    }
                    m_autoCollectBoneCount = cp.boneCount;
                }
                EUtil.PopBackgroundColor();

                EditorGUILayout.EndHorizontal();

                EUtil.DrawSplitter();

                // dist thres
                EditorGUI.BeginChangeCheck();
                float newDistThres = EditorGUILayout.FloatField(CONT_DistThres, cp.distThres);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newDistThres > 0f)
                    {
                        Undo.RecordObject(cp, "Set Dist Thres");
                        cp.distThres = newDistThres;
                    }
                }

                cp.useDamp = EditorGUILayout.Toggle(CONT_Damp, cp.useDamp);
                if (cp.useDamp)
                {
                    cp.globalDamp = EditorGUILayout.FloatField("Global damp", cp.globalDamp);
                }

                cp.useTargetRotation = EditorGUILayout.Toggle(CONT_UseTargetRotation, cp.useTargetRotation);
                cp.maxIteration = EditorGUILayout.IntField(CONT_MaxIteration, cp.maxIteration);

                cp.revertOpt = (CCDSolver.RevertOption)EditorGUILayout.EnumPopup(CONT_RevertOpt, cp.revertOpt);

                m_markerSize.val = Mathf.Max(0, EditorGUILayout.FloatField(CONT_BoneMarkSize, m_markerSize.val));

                EUtil.PushGUIEnable(!cp.Target);
                {
                    if (GUILayout.Button("Control Mode:  " + (cp.Target ? "Target" : m_panel.ToString()), EditorStyles.toolbarButton))
                    {
                        m_panel = (EPanel)((int)(m_panel + 1) % (int)EPanel.END);
                        cp.GetSolver().Target = cp.transform.position;
                    }
                    _OnGUI_IKPanel(cp);
                }
                EUtil.PopGUIEnable();

                // influence
                GUILayout.Space(5f);
                cp.Influence = EUtil.ProgressBar(cp.Influence, 0, 1f, "Influence: {0:F2}");

                //initInfos
                if(Pref.ShowInitInfos)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_initInfos"), true);
            }
            EUtil.PopGUIEnable();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cp); //so ConstraintStack.Update can be called in edit-mode
            }

        }



        void OnSceneGUI()
        {
            CCDSolverMB cp = (CCDSolverMB)target;
            CCDSolver solver = cp.GetSolver();
            if (solver == null || solver.Count < 1)
                return;

            if (cp.ShowGizmos)
            {
                var joints = solver.GetJoints();

                Camera sceneCam = EUtil.GetSceneViewCamera();
                Transform camTr = sceneCam.transform;

                EUtil.PushHandleColor(Pref.IKBoneLinkColor);
                //1. draw bone line
                for (int i = 0; i < joints.Length - 1; ++i)
                {
                    var p0 = joints[i].position;
                    var p1 = joints[i + 1].position;
                    Handles.DrawAAPolyLine(3f, p0, p1);
                    Handles.DotCap(0, p0, camTr.rotation, m_markerSize.val);
                }
                if (joints.Length > 0)
                    Handles.DotCap(0, joints.Last().position, camTr.rotation, m_markerSize.val);
                EUtil.PopHandleColor();

                //1.5 draw line from end-joint to target pos
                {
                    var p0 = joints.Last().position;
                    var p1 = solver.Target;
                    Handles.DrawDottedLine(p0, p1, 5f);
                }

                //2. call each constraint's OnSceneGUI
                for (int i = 0; i < joints.Length - 1; ++i)
                {
                    var cons = solver.GetConstraint(i);

                    foreach (var con in cons)
                    {
                        if (!con || !con.enabled)
                            continue;

                        Editor e = EUtil.GetEditor(con);
                        IOnSceneGUI igui = e as IOnSceneGUI;
                        if (igui != null)
                        {
                            igui.OnSceneGUI();
                        }
                    }
                }
            }

            //3. debug draw
            if (!cp.Target)
            {
                if (m_panel != EPanel.Normal)
                {
                    Tools.current = Tool.None;

                    // move handle 
                    if( m_panel == EPanel.Continuous ) 
                        EditorGUI.BeginChangeCheck();

                    solver.Target = Handles.PositionHandle(solver.Target, Quaternion.identity);

                    if (m_panel == EPanel.Continuous && EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(solver.GetJoints(), "IK execute");
                        solver.Execute();
                    }

                    // hotkeys
                    Event e = Event.current;
                    if (e.type == EventType.KeyUp && Tools.viewTool != ViewTool.FPS)
                    {
                        if (e.keyCode == KeyCode.E) { Tools.current = Tool.Rotate; m_panel = EPanel.Normal; }
                        else if (e.keyCode == KeyCode.R) { Tools.current = Tool.Scale; m_panel = EPanel.Normal; }
                        else if (e.keyCode == KeyCode.Q) { Tools.current = Tool.View; m_panel = EPanel.Normal; }
                    }
                }
            }

            
        }

	    #endregion "unity event handlers"
	
		#region "public method"
	    // public method
	
	    #endregion "public method"
	
		#region "private method"


	
        private void _OnGUI_IKPanel(CCDSolverMB cp)
        {
            switch (m_panel)
            {
                case EPanel.Normal: break;
                case EPanel.Continuous: break;
                case EPanel.Manual:
                    {
                        EUtil.DrawSplitter();
                        if (GUILayout.Button(CONT_GO, EditorStyles.toolbarButton))
                        {
                            var solver = cp.GetSolver();
                            Undo.RecordObjects(solver.GetJoints(), "IK Follow");
                            solver.Execute();
                        }
                    }
                    break;
                case EPanel.Debug:
                    {
                        EUtil.DrawSplitter();
                        _OnGUI_IKPanel_Debug(cp);
                    }
                    break;
                default:
                    Dbg.LogErr("CCDSolverEditor._OnGUI_IKPanel: unexpected panel mode: {0}", m_panel);
                    break;
            }

            
        }

        private void _OnGUI_IKPanel_Debug(CCDSolverMB cp)
        {
            // line 1
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(CONT_ZeroAll, EditorStyles.toolbarButton))
                { //zero-out all rotation
                    var solver = cp.GetSolver();
                    var joints = solver.GetJoints();
                    Undo.RecordObjects(joints, "Zero-out IK joints");
                    foreach (var j in joints)
                    {
                        j.localRotation = Quaternion.identity;
                    }
                }
                if (GUILayout.Button(CONT_Reset, EditorStyles.toolbarButton))
                { // return target to endJoint
                    var solver = cp.GetSolver();
                    solver.Target = cp.transform.position;
                    EUtil.RepaintSceneView();
                }
            }
            GUILayout.EndHorizontal();

            // line 2
            GUILayout.BeginHorizontal();
            {
                bool bStartedStep = m_dbgStepIE != null;
                Color c = bStartedStep ? Color.red : Color.green;
                string s = bStartedStep ? "StopStep" : "StartStep";
                if (EUtil.Button(s, c))
                {
                    var solver = cp.GetSolver();
                    if (!bStartedStep)
                    {
                        m_dbgStepIE = solver.DBGExecute();
                    }
                    else
                    {
                        solver.dbg_interrupt = true;
                        m_dbgStepIE.MoveNext();
                        m_dbgStepIE = null;
                        bStartedStep = false;
                    }
                }

                EUtil.PushGUIEnable(bStartedStep);
                {
                    if (GUILayout.Button(CONT_Step))
                    {
                        bool bNotOver = m_dbgStepIE.MoveNext();
                        if (!bNotOver)
                        {
                            m_dbgStepIE = null;
                            bStartedStep = false;
                        }
                    }
                    if (GUILayout.Button(CONT_Continue))
                    {
                        while (m_dbgStepIE.MoveNext() == true)
                        {
                            ;
                        }
                        m_dbgStepIE = null;
                        bStartedStep = false;
                    }
                }
                EUtil.PopGUIEnable();
            }
            GUILayout.EndHorizontal();
        }

        private bool _CheckManualSetJointList(CCDSolverMB cp)
        {
            var lst = cp.jointList;
            for(int i=0; i<lst.Count; ++i)
            {
                if( lst[i] == null )
                {
                    EUtil.ShowNotification("Joint List has null entry!");
                    return false;
                }
            }

            if( lst.Count > 0 && lst[lst.Count-1] != cp.transform )
            {
                EUtil.ShowNotification("Joint List's last entry must be 'this' transform");
                return false;
            }

            return true;
        }

        private bool _CheckIfBoneContinuous(CCDSolverMB mb)
        {
            var joints = mb.jointList;
            for (int i = joints.Count - 1; i >= 1; --i)
            {
                var j = joints[i];
                var ju = joints[i - 1];
                if (ju != j.parent)
                    return false;
            }
            return true;
        }

        private bool _CheckJointListIdentical(CCDSolverMB cp)
        {
            var lst = cp.jointList;
            var solver = cp.GetSolver();
            if( solver != null )
            {
                var curLst = solver.GetJoints();
                if (lst.Count != curLst.Length)
                    return false;

                for(int i=0; i<lst.Count; ++i)
                {
                    if (lst[i] != curLst[i])
                        return false;
                }
            }
            return true;
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        private readonly static GUIContent CONT_ManualSetJoints = new GUIContent("Manual", "allow user to set the joint-list manually");
        private readonly static GUIContent CONT_AutoSetJoints = new GUIContent("Auto", "automatically set joint-list by boneCount");
        private readonly static GUIContent CONT_BoneCnt = new GUIContent("Bone Count", "auto-collect X bones along the transform hierarchy");
        private readonly static GUIContent CONT_DistThres = new GUIContent("Dist Threshold", "IK calculation is considered finished if the end-joint is near the target position");
        private readonly static GUIContent CONT_Damp = new GUIContent("Use Damp", "limit the max degree a joint can rotate in one iteration");
        private readonly static GUIContent CONT_UseTargetRotation = new GUIContent("Use TargetRotation", "if checked, the endJoint will use the target's rotation");
        private readonly static GUIContent CONT_MaxIteration = new GUIContent("Max Iteration", "the max iteration number");
        private readonly static GUIContent CONT_RevertOpt = new GUIContent("Revert Option", "How to behave if IK cannot find out reasonable solution for given target");
        private readonly static GUIContent CONT_BoneMarkSize = new GUIContent("BoneMarker Size", "The joint marker's size, in world unit");

        private readonly static GUIContent CONT_ZeroAll = new GUIContent("Zero", "zero-out all rotation");
        private readonly static GUIContent CONT_Reset = new GUIContent("Reset", "return the target to endJoint");
        private readonly static GUIContent CONT_Step = new GUIContent("Step", "Make a step of the IK calculation");
        private readonly static GUIContent CONT_Continue = new GUIContent("Continue", "step on till end");
        private readonly static GUIContent CONT_GO = new GUIContent("Follow", "calculate the IK result towards the current target");
        //private readonly static GUIContent CONT_Stop = new GUIContent("Stop", "Stop current in-progress IK calculation");
        //private readonly static GUIContent CONT_Start = new GUIContent("Start");
	
	    #endregion "constant data"
        
    }


}
