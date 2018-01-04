//#define IMDEBUGGING

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using ExtMethods;
using MH;
using MH.Constraints;
using MH.Skele;
using System.Linq;
using System.Text;

using Job = System.Collections.IEnumerator; 
using LimbConDict = System.Collections.Generic.Dictionary<UnityEngine.Transform, MH.ConstraintInfo>; // < joint, ConstraintInfo>
using PoseDataDict = System.Collections.Generic.Dictionary<string, MH.XformData>;
using JointList = System.Collections.Generic.List<UnityEngine.Transform>;
#pragma warning disable 618
namespace MH
{
    using Tool = SMREditor.ETool;
    using PivotRotation = SMREditor.EPivotRotation;

    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    public class SMREditor : Editor
    {

        #region "data"
        // data

        public delegate Color ColorFunc(Transform joint);

        #region "Base Data"
        // "Editors" 
        private static Editor ms_origEditor = null; //the UnityEditor.SkinnedMeshRendererEditor
        //private static Editor ms_tmpTrEditor = null; //Transform editor used to draw selected bone info

        private static bool ms_bEditing = false;
        private static bool ms_bAnimWndOpen = false; //if the animation window is open
        private static SkinnedMeshRenderer ms_SMR = null; //current selected SMR

        // Make bigger
        private static Transform ms_TheTopMost = null;

        // request end
        private static bool ms_EndEdit_Req = false;

        private static bool ms_bShowTransformInspector = true;

        private static bool ms_autoStartEditWhenEditorFocus = false;

        #endregion "Base Data"

        #region "setting data"
        // "setting data" 
        private static EShowVert ms_eShowVertices = EShowVert.HIDE;
        private static bool ms_bHideWireframe = true;
        public static float ms_boneSize = 0.1f;
        public static float ms_vertSize = 0.4f;
        public static float ms_boneLineWidth = 5f;
        private static bool ms_bShowOptimzedJoint = false; //those joints without vertices are optimized from SMR.bones
        private static bool ms_LockSelection = true;
        // Help
        private static bool ms_bShowHelp = false;
        // GUI switch
        private static bool ms_bDrawGUI = true;

        // GUI mode
        private static EOPMode ms_eOpMode = EOPMode.FK;
        private static EOPMode ms_eOpMode_Req = EOPMode.FK; //the requested OP Mode

        #endregion "setting data"

        #region "Bones"
        // "Bones" 
        private static SelectionCtrl ms_SelCtrl = null;
        //private static Transform ms_SelectedBone = null;
        //private static Transform ms_SelectedBone_Req = null; //request to change to this bone selected
        private static List<Transform> ms_ExtendedJoints = null; //as some joints without vertices will not be included in SMR's bone array, I need to add it here
        private static Transform[] ms_ExtendedJointsCacheArray = null; // = ms_ExtendedJoints.ToArray()
        private static List<XformData> ms_ExtTrDataCopy = null; //make a copy of all joints transform data
        private static int ms_TrDataCopyTimer = 0;

        private static int ms_IKSolverBoneLen_Req = -1; //used to change the main IKSolver's bone count, only valid when larger than 0
        #endregion "Bones"

        #region "Handles Tools"
        // "Handles Tools" 
        private static Tool ms_CurHandleTool = Tool.None;
        private static Tool ms_CurHandleTool_Req = Tool.None;
        private static PivotRotation ms_CurPivotRotation = PivotRotation.Local;
        #endregion "Handles Tools"

        #region "Controls data"
        // "Controls data" 

        private static bool ms_bRightBtnIsDown = false;
        private static double ms_timeLMBDown = -1; //negative means LMB not down
        private static GameObject ms_CurSelectedGO = null;
        private static int ms_RepaintCnter = 0; // call repaint continually for * updates
        private static EHandleState ms_DraggingHandle = EHandleState.NoDragging; //whether is dragging move/rotate/scale handle
        private static bool ms_bLMBJustUp = false;
        #endregion "Controls data"

        #region "IK plane lock"
        private static bool ms_bEnableIKPlaneLock = false; // IK Plane Lock will lock all joints on IK link to the current IK plane when move end effector
        private static bool ms_bEnableIKPlaneLock_Req = false;
        private static Vector3 ms_CachedIKPlaneNormal = Vector3.up; //used to tackle the zero vector problem
        #endregion

        #region "PS"
        private static ParticleSystem ms_PS;
        private static ParticleSystem.Particle[] ms_particles = null;
        #endregion

        #region "Extra GO"
        private static GameObject ms_ExtraGO;
        private static GameObject ms_IKTargetGO; //used as IK target
        private static Transform ms_BindPoseTr; //used for BindPose fixer
        private static List<Transform> ms_BindPoseFixerSkeleton = new List<Transform>();
        private static GameObject ms_IKPlaneGO; //used to render IK plane
        #endregion

        #region "materials"
        // "materials" 
        private static Material ms_BackupMat = null;
        private static Material ms_NormalVertMat = null;
        private static Material ms_PassThroughMat = null;
        private static Material ms_RotateArrowMat = null;
        private static Texture2D ms_Background = null;
        //private static Texture ms_BackgroundW = null;
        #endregion "materials"

        #region "IK Solvers"
        // "IK Solvers" 
        // IK Solver parts
        private static ISolver ms_IKSolver = null;

        // IK Solvers used in IKPinned mode
        private static List<ISolver> ms_PinIKSolvers = new List<ISolver>();
        private static Transform ms_ReqChangePin = null; //the transform requested to change, if already in PinIKSolver, then del; if not, then add

        private static bool ms_bUseCCDIK = true;

        #endregion "IK Solvers"

        #region "BindPoseFixer"
        // "BindPoseFixer" 
        private static EBindPoseFixerMode ms_BindPoseFixerMode = EBindPoseFixerMode.NORMAL;
        #endregion "BindPoseFixer"

        #region "Pose Manager"
        // "Pose Manager" 
        //private static JointList ms_MultiSelectJoints; //the multi-selection joints, merged to SelectionCtrl
        private static PoseSet ms_PoseSet = null; //the pose set
        private static bool[] ms_BoneInSelectionArray = null; //corresponding to the SMR.bones
        #endregion "Pose Manager"

        #region "Mirror edit"

        private static bool ms_bMirrorEdit = false;
        private static MirrorCtrl ms_MirrorCtrl = null;

        #endregion "Mirror edit"

        #region "Misc"
        // "Misc" 
        private static bool ms_bNeedRecalcVerts = true; //whether need to recalc vert pos
        private static bool ms_bHasModalWindow = false;
        // Skele_CRCont
        private static MH.Skele.Skele_CRCont ms_crPaint; //paint event coroutine-container    

        private static bool ms_inited = false;

        #endregion "Misc"

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void OnEnable()
        {
            if (!ms_inited)
            {
                bool hasPro = Application.HasProLicense();
                if (hasPro)
                    ms_Background = AssetDatabase.LoadAssetAtPath(BACKGROUND_TEX, typeof(Texture2D)) as Texture2D;
                else
                    ms_Background = AssetDatabase.LoadAssetAtPath(BACKGROUND_W_TEX, typeof(Texture2D)) as Texture2D;

                Dbg.Assert(ms_Background != null, "SMREditor.sctor: failed to load background at {0}", hasPro ? BACKGROUND_TEX : BACKGROUND_W_TEX);

                //ms_BackgroundW = AssetDatabase.LoadAssetAtPath(BACKGROUND_W_TEX, typeof(Texture)) as Texture;
                //Dbg.Assert(ms_Background != null, "SMREditor.sctor: failed to load background at {0}", BACKGROUND_W_TEX);

                ms_PassThroughMat = AssetDatabase.LoadAssetAtPath(PassThroughMat, typeof(Material)) as Material;
                Dbg.Assert(ms_PassThroughMat != null, "SMREditor.sctor: failed to load passThroughMat at {0}", PassThroughMat);

                ms_NormalVertMat = AssetDatabase.LoadAssetAtPath(NormalVertMat, typeof(Material)) as Material;
                Dbg.Assert(ms_NormalVertMat != null, "SMREditor.sctor: failed to load NormalVertMarkerMat at {0}", NormalVertMat);

                ms_RotateArrowMat = AssetDatabase.LoadAssetAtPath(RotateArrowMat, typeof(Material)) as Material;
                Dbg.Assert(ms_RotateArrowMat != null, "SMREditor.sctor: failed to load RotateArrowMat at {0}", RotateArrowMat);

                ms_IKSolver = null;

                //ms_MultiSelectJoints = new JointList();

                ms_MirrorCtrl = new MirrorCtrl();

                ms_inited = true;
            }
            
        }

        /// <summary>
        /// use EditorApplication.update to callback,
        /// enable/disable by SMR's button
        /// 
        /// force to keep selection at the same GO
        /// </summary>
        public static void OnUpdate()
        {
            if (!ms_CurSelectedGO ||  //delete the GO or switched scene
                EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _OnEndEdit();
                return;
            }

            if (ms_LockSelection)
            {
                if (Selection.activeGameObject != ms_CurSelectedGO)
                {
                    if (!ms_bAnimWndOpen)
                    { //dont show this when AnimationWindow is up
                        EUtil.ShowNotification("GO_Reselect_Prohibited");
                    }
                    else
                    {
                        var sel = Selection.activeTransform;
                        if (ms_ExtendedJoints.Contains(sel))
                            ms_SelCtrl.Select(sel);
                        _SceneRepaintAll(); //complementary for CCEditor's recalc calling
                    }

                    Selection.activeGameObject = ms_CurSelectedGO;
                }
            }

            ms_PS.SetParticles(ms_particles,
                ms_eShowVertices != EShowVert.HIDE ? ms_particles.Length : 0);

            if (ms_RepaintCnter > 0)
            {
                ms_RepaintCnter--;
                EUtil.GetSceneView().Repaint();
            }
        }

        /// <summary>
        /// 0. Event Type check,
        /// 0.1 mode switch
        /// 0.2 check the right-button state
        /// 
        /// route-A: NormalMode
        /// A.1. mark all bones
        ///   A.1.1 find out the bone currently selected by clicking handle
        ///   A.1.2 draw markers for bones
        /// A.2. mark all vertices if needed
        /// A.3. manipulate the bone 

        ///   A.3.1 if a bone handle is selected, use different mode to draw different handles, and affect corresponding transform
        ///   
        /// route-B: IK-Mode
        /// B.0 mark all vertices if needed
        /// B.1 select endJoint first ...
        ///   B.1.1 find out the bone currently selected by clicking handle
        ///   B.1.2 draw markers for bones
        /// B.2 endJoint is decided, move the IKtarget to affect the bones
        ///   B.2.1 draw the IK link
        ///   B.2.2 if not setting constraint, draw IK target and manipulate it
        /// 
        /// route-C: IK-Pinnned-Mode
        /// C.1 draw the Handle on the root bone of character;
        /// C.2 draw the markers for selecting pin/unpin;
        /// C.3 if Transform is changed, call the IKSolver's Execute
        /// 
        /// route-D: Set-Constraint (obsoleted)
        /// D.1 case SelectLimbType:
        /// D.2 case RotateMidJoint:
        ///     
        /// route-E: Pose-Manage
        /// E.1 select / add / remove joints for multi-joint-selection
        /// E.2 draw the markers for joints and bones
        /// 
        /// route-F: BindPose-fixer
        /// F.1 mark all bones
        ///   F.1.1 find out the bone currently selected ty clicking around joint
        ///   F.1.2 draw marker for joint and bone
        /// F.2 manipulate the bone
        /// 
        /// 0. draw the GUI in scene view (put it at last so GUI won't be covered by other handles)
        /// </summary>
        void OnSceneGUI()
        {
            if (!ms_bEditing)
            {
                return;
            }

            Event e = Event.current;

            UnityEditor.Tools.current = UnityEditor.Tool.None; // clear the default position/rotation/scale handles
            //SkinnedMeshRenderer smr = ((SkinnedMeshRenderer)target);

            //////////////////////////////////////////////////
            // 0. event type pre-process
            //////////////////////////////////////////////////

            ms_bLMBJustUp = false; //reset
            if (e.rawType == EventType.MouseUp)
            {
                //Dbg.Log("mouseUp");
                if (e.button == 1)
                    ms_bRightBtnIsDown = false;
                else if (e.button == 0)
                {
                    ms_crPaint.Start(_Job_ClearLMBDownTime());
                    ms_bLMBJustUp = true;

                    if( e.alt )
                    {
                        GameObject picked = HandleUtility.PickGameObject(e.mousePosition, true);
                        if( picked )
                        {
                            var newSmr = picked.GetComponentInChildren<SkinnedMeshRenderer>();
                            if (newSmr != null && newSmr != ms_SMR)
                            {
                                _OnEndEdit();
                                Selection.activeGameObject = newSmr.gameObject;
                                ms_autoStartEditWhenEditorFocus = true;
                                GUIUtility.ExitGUI();
                            }
                        }                        
                    }
                }
            }

            switch (e.type)
            {
                // 0.1
                case EventType.Layout:
                    {
                        if (!ms_bHasModalWindow)
                        {
                            int defControlID = GUIUtility.GetControlID(FocusType.Passive);
                            HandleUtility.AddDefaultControl(defControlID);
                        }

                        if (ms_SelCtrl.SelectionChanged)
                        {
                            ms_SelCtrl.ApplyMultiJointChange();
                            _OnChangeSelectedBone(ms_SelCtrl.SingleJoint, ms_SelCtrl.SingleJointReq);
                        }
                        if (ms_CurHandleTool != ms_CurHandleTool_Req)
                        {
                            _OnSwitchHandleTool(ms_CurHandleTool, ms_CurHandleTool_Req);
                        }
                        if (ms_bEnableIKPlaneLock != ms_bEnableIKPlaneLock_Req)
                        {
                            _OnSwitchIKPlaneLock(ms_bEnableIKPlaneLock, ms_bEnableIKPlaneLock_Req);
                        }
                        if (ms_ReqChangePin != null)
                        {
                            _OnChangeIKPin();
                        }
                        if (ms_IKSolverBoneLen_Req > 0)
                        {
                            _OnChangeIKSolverBoneLen(ms_IKSolver.Count, ms_IKSolverBoneLen_Req);
                            ms_IKSolverBoneLen_Req = -1; //reset the req val to invalid
                        }
                        if (ms_eOpMode != ms_eOpMode_Req)
                        {
                            _OnSwitchOpMode(ms_eOpMode, ms_eOpMode_Req);
                        }

                        // as it's possible the selCtrl is changed in above functions, so we make a finish touch here
                        ms_SelCtrl.ApplyMultiJointChange();

                        _CheckAnimWndOpen();

                        _CheckMakingBoneSnapshot(); //used for making snapshot of all bones when editing animation

                    }
                    break;
                //0.2
                case EventType.MouseDown:
                    {
                        if (e.button == 1)
                            ms_bRightBtnIsDown = true;
                        else if (e.button == 0)
                            ms_timeLMBDown = EditorApplication.timeSinceStartup;
                    }
                    break;
                case EventType.MouseDrag:
                    {
                    }
                    break;
                case EventType.Repaint:
                    {
                        ms_crPaint.Execute();
                        if (ms_eOpMode == EOPMode.FK)
                        {
                            if (ms_DraggingHandle != EHandleState.Dragging && ms_DraggingHandle != EHandleState.JustStartDragging)
                                _RenewTmpPosRot(); //why add this line???
                        }
                        else if (ms_eOpMode == EOPMode.IK)
                        {
                            Transform extTr = ms_IKTargetGO.transform;
                            if (ms_IKSolver != null && ms_IKSolver.Count != 0)
                            {
                                var joints = ms_IKSolver.GetJoints();
                                var lastJoint = joints[joints.Length - 1];
                                extTr.position = lastJoint.position;
                            }
                        }
                    }
                    break;
            }

            // execute shortcuts
            _Shortcut_Execute();

            //Route-A
            if (ms_eOpMode == EOPMode.FK)
            {
                Transform rootBone;
                Transform[] bones;
                _GetBonesArray(out rootBone, out bones);
                _SetSelectedJointByEvent(e, bones, true);

                //A.1.2 draw markers for bones
                _DrawMarkersForBones(rootBone, bones);

                //A.2. 
                _CalcMarkersForVertices();

                //A.3. 
                if (!ms_SelCtrl.NoSelection)
                {
                    _ManipulateFKBones();

                    _ProcessMirrorEdit();
                }
            }
            else if (ms_eOpMode == EOPMode.IK) //Route-B
            {
                //B.0
                _CalcMarkersForVertices();

                //B.1
                if (ms_IKSolver == null)
                {
                    Transform rootBone = null;
                    Transform[] bones = null;
                    _GetBonesArray(out rootBone, out bones);
                    _SetSelectedJointByEvent(e, bones, false);

                    //B.1.2 draw markers for bones
                    _DrawMarkersForBones(rootBone, bones);
                }
                //B.2
                else
                {
                    //B.2.1
                    Transform[] IKLnk = ms_IKSolver.GetJoints();
                    _DrawMarkersForBones(IKLnk[0], IKLnk);

                    _ManipulateMarkerForIKTarget();

                    _ProcessMirrorEdit();
                }
            }
            else if (ms_eOpMode == EOPMode.IK_Pinned) //Route-C
            {
                // C.1
                Transform theRoot = ms_ExtendedJoints[0];
                bool bMovedRoot = false;

                {
                    Quaternion rot = GetQuaternionByPivotRotation(theRoot);
                    Vector3 newPos = Handles.PositionHandle(theRoot.position, rot);
                    if (GUI.changed)
                    {
                        Undo.RecordObject(theRoot, "IK_Pin MoveRoot");
                        theRoot.position = newPos;
                        bMovedRoot = true;
                    }
                }

                // C.2
                {
                    Transform rootBone = null;
                    Transform[] bones = null;
                    _GetBonesArray(out rootBone, out bones);
                    _SetSelectedJointByEvent(e, bones);

                    //draw markers for bones
                    _DrawMarkersForBones(rootBone, bones);

                    // for each IK-link in ms_PinIKSolvers, draw the un-pin mark by it
                    for (int i = 0; i < ms_PinIKSolvers.Count; ++i)
                    {
                        Transform[] joints = ms_PinIKSolvers[i].GetJoints();
                        Transform endJoint = joints[joints.Length - 1];
                        Handles.BeginGUI();
                        {
                            Vector2 pt = HandleUtility.WorldToGUIPoint(endJoint.position) + new Vector2(10, 10);
                            EUtil.PushGUIColor(Color.red);
                            if (GUI.Button(new Rect(pt.x, pt.y, 40, 40), "X"))
                            {
                                _DelIKPin(endJoint);
                            }
                            EUtil.PopGUIColor();
                        }
                        Handles.EndGUI();
                    }

                    //draw add-pin marker if needed
                    Transform selJoint = ms_SelCtrl.SingleJoint;
                    if (selJoint != null && selJoint != theRoot)
                    {
                        bool bValidTarget = true;
                        for (int i = 0; i < ms_PinIKSolvers.Count; ++i)
                        {
                            Transform[] joints = ms_PinIKSolvers[i].GetJoints();
                            Transform endJoint = joints[joints.Length - 1];
                            if (endJoint == selJoint)
                            {
                                bValidTarget = false;
                                break;
                            }
                        }

                        if (bValidTarget)
                        {
                            Handles.BeginGUI();
                            {
                                Vector2 pt = HandleUtility.WorldToGUIPoint(selJoint.position) + new Vector2(10, 10);
                                EUtil.PushGUIColor(Color.green);
                                if (GUI.Button(new Rect(pt.x, pt.y, 40, 40), "V"))
                                {
                                    _AddIKPin(selJoint);
                                    ms_SelCtrl.Select(null);
                                }
                                EUtil.PopGUIColor();
                            }
                            Handles.EndGUI();
                        }
                    }
                }

                // C.3
                {
                    for (int i = 0; i < ms_PinIKSolvers.Count; ++i)
                    {
                        ISolver ik = ms_PinIKSolvers[i];
                        Transform[] joints = ik.GetJoints();
                        Transform endJoint = joints[joints.Length - 1];

                        if (bMovedRoot)
                        {
                            Quaternion cacheRot = endJoint.rotation;

                            Undo.RecordObjects(joints, "IK_Pin ExecuteIKSolver");
                            ik.Execute();
                            HotFix.FixRotation(ik.GetJoints());
                            endJoint.rotation = cacheRot; //keep the rotation of the end_joint
                        }
                    }

                }

            }
            else if (ms_eOpMode == EOPMode.PoseManage) //Route-E
            {
                //E.1 
                if (e.type == EventType.MouseUp && !ms_bRightBtnIsDown && e.button == 0 && !ms_bHasModalWindow)
                {
                    Transform joint = _FindSelectedJoint(e.mousePosition, ms_ExtendedJoints.ToArray());

                    if (null != joint)
                    {
                        if (e.shift)
                        {
                            if (ms_SelCtrl.IsSelectedJoint(joint))
                            {
                                ms_SelCtrl.DecSelect(joint);
                            }
                            else
                            {
                                ms_SelCtrl.IncSelect(joint);
                            }
                        }
                        else
                        {
                            _SelectJointAndAllDescendants(joint);
                        }

                        //ms_SelCtrl.Select(joint);
                    }

                    _SceneRepaintAll();

                } //if ( e.type == EventType.MouseUp... 

                // E.2
                _DrawMarkersForBones(ms_ExtendedJoints[0], ms_ExtendedJoints.ToArray(),
                    _ColorFunc_JointIsInMultiSelectJoint);
            }
            else if (ms_eOpMode == EOPMode.BindPoseFixer)
            {
                // F.1.1 find out the bone currently selected ty clicking around joint
                Transform rootBone = ms_BindPoseTr;
                Transform[] bones = ms_BindPoseFixerSkeleton.ToArray();
                _SetSelectedJointByEvent(e, bones);

                // F.1.2 draw marker for joint and bone
                _DrawMarkersForBones(rootBone, bones);

                // F.2 manipulate the bone
                if (!ms_SelCtrl.NoSelection)
                {
                    _ManipulateBones_BindPoseFixer();
                }
            }
            else
            {
                Dbg.LogErr("SMREditor.OnSceneGUI: unexpected ms_eOpMode: {0}", ms_eOpMode);
            }

            // 0.
            _DrawSceneGUI();

            //Handles.DrawCamera(new Rect(0, 0, 300, 300), ms_depthCam);
        }

        public override void OnInspectorGUI()
        {
            string msg = ms_bEditing ? "EndEdit" : "StartEdit";
            Color c = ms_bEditing ? Color.red : Color.green;
            bool bClick = EUtil.Button(msg, c);
            if (bClick ||
                 (ms_bEditing && ms_EndEdit_Req) ||
                 ms_autoStartEditWhenEditorFocus
                )
            {
                if (!ms_bEditing)
                {
                    _OnStartEdit();
                }
                else
                {
                    _OnEndEdit();
                }
                _SceneRepaintAll();
            }

            if (ms_bEditing)
            {
                // looks not fit for position keyframe, disable temporarily

                GUILayout.BeginHorizontal();
                {
                    bool bHasTopmost = (ms_TheTopMost != null);
                    c = bHasTopmost ? Color.red : Color.green;
                    if (EUtil.Button(bHasTopmost ? "Resume CamSpeed" : "Lower CamSpeed", c))
                    {
                        if (bHasTopmost)
                        {
                            _MoveAllSceneRootsBack();
                        }
                        else
                        {
                            _MoveAllSceneRootUnderTheTopMost();
                        }

                        bHasTopmost = !bHasTopmost;
                    }

                    msg = ms_bDrawGUI ? "HideGUI" : "ShowGUI";
                    c = ms_bDrawGUI ? Color.red : Color.green;
                    if (EUtil.Button(msg, c))
                    {
                        ms_bDrawGUI = !ms_bDrawGUI;
                        _SceneRepaintAll();
                    }

                    msg = ms_bShowTransformInspector ? "Hide Xform" : "Show Xform";
                    c = ms_bShowTransformInspector ? Color.red : Color.green;
                    if (EUtil.Button(msg, c))
                    {
                        ms_bShowTransformInspector = !ms_bShowTransformInspector;
                        _SceneRepaintAll();
                    }
                }
                GUILayout.EndHorizontal();

                msg = ms_bUseCCDIK ? "New IK Sys Enabled" : "New IK Sys Disabled";
                c = ms_bUseCCDIK ? Color.green : Color.white;
                if (EUtil.Button(msg, c))
                {
                    ms_bUseCCDIK = !ms_bUseCCDIK;
                    ms_SelCtrl.Select(null);
                    _SceneRepaintAll();
                }

                //EditorGUILayout.LabelField(string.Format("MultiSelectBone:{0}, SingleJoint:{1}", ms_SelCtrl.Joints.Count, ms_SelCtrl.SingleJoint));
            }
            else
            {
                if (EUtil.Button("Fix Hierarchy", Color.green))
                {
                    bool bFixedSomething = false;

                    GameObject go = GameObject.Find("/" + TopMostGOName);
                    if (go != null)
                    {
                        ms_TheTopMost = go.transform;
                        _MoveAllSceneRootsBack();
                        bFixedSomething = true;
                    }

                    do
                    {
                        go = GameObject.Find("/" + SkeleEditorGOName);
                        if (null == go) break;

                        GameObject.DestroyImmediate(go);
                        bFixedSomething = true;
                    } while (true);

                    if (!bFixedSomething)
                    {
                        EUtil.ShowNotification("Everything_is_Fine, Nothing_to_Fix");
                    }
                }

            }

            // draw default inspector
            _DrawUnitySMREditor();
        }

        void OnDestroy()
        {
            if (ms_origEditor != null)
            {
                Editor.DestroyImmediate(ms_origEditor);
                ms_origEditor = null;
            }
        }

        #endregion "unity event handlers"

        #region "public methods"
        // "public methods" 

        public static JointList GetExtendedJoints()
        {
            return ms_ExtendedJoints;
        }

        public static MirrorCtrl GetMirrorCtrl()
        {
            return ms_MirrorCtrl;
        }

        public static SkinnedMeshRenderer GetSMR()
        {
            return ms_SMR;
        }

        public static Texture2D GetBG()
        {
            return ms_Background;
        }

        #endregion "public methods"

        #region "private method"

        #region "Init & Fini"
        // "Init & Fini" 

        private void _OnStartEdit()
        {
            Dbg.Log("Skele_SMREditor: Starting...");

            SkinnedMeshRenderer smr = (SkinnedMeshRenderer)target;
            if (smr.sharedMesh == null)
            {
                Dbg.LogWarn("This SMR is not properly setup... is it added manually? Please use the one automatically created by Unity");
                return;
            }

            ms_bEditing = true;
            ms_autoStartEditWhenEditorFocus = false; //reset flag

            //EUtil.AlignViewToObj(smr.gameObject);
            ms_CurSelectedGO = smr.gameObject;
            ms_SMR = smr;
            EditorApplication.update += OnUpdate;

            ms_BoneInSelectionArray = new bool[smr.bones.Length];

            ms_eOpMode = EOPMode.FK;
            ms_bShowHelp = false;

            Highlighter.Stop();

            ms_SelCtrl = new SelectionCtrl();

            //////////////////////////////////////////////////
            // ParticleSystem as VertMarker
            //////////////////////////////////////////////////
            int vcnt = smr.sharedMesh.vertexCount;
            ms_ExtraGO = new GameObject(SkeleEditorGOName);
            //ms_ExtraGO.transform.parent = smr.transform;
            ms_PS = ms_ExtraGO.AddComponent<ParticleSystem>();
            ms_PS.simulationSpace = ParticleSystemSimulationSpace.World; //required if use world-system
            ms_PS.startLifetime = 1000000f;
            ms_PS.maxParticles = 1000000;
            //ms_PS.Play();
            ms_PS.Simulate(0.1f); //Simulate will pause the PS, required

            ms_particles = new ParticleSystem.Particle[vcnt];
            SceneView.lastActiveSceneView.camera.depthTextureMode = DepthTextureMode.Depth;

            //backup material and set new material
            ms_BackupMat = ms_PS.GetComponent<Renderer>().sharedMaterial;
            _SetVertMaterial();

            ms_BindPoseTr = null;
            //////////////////////////////////////////////////
            // IK target / IK plane
            //////////////////////////////////////////////////
            ms_IKTargetGO = new GameObject("__MH_IKTarget");
            Misc.AddChild(ms_ExtraGO, ms_IKTargetGO);

            ms_IKPlaneGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ms_IKPlaneGO.name = "__MH_IKPlane";
            ms_IKPlaneGO.transform.localScale = new Vector3(100, 100, 1);
            var planeMat = ms_IKPlaneGO.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
            planeMat.color = new Color(0, 1, 0, 0.3f);
            Misc.AddChild(ms_ExtraGO, ms_IKPlaneGO);

            //////////////////////////////////////////////////
            // SelectedWireFrame init
            //////////////////////////////////////////////////
            _SwitchSelectedWireframe();

            //////////////////////////////////////////////////
            // Set the init parameters according to EditorPrefs
            // if there's no values in EditorPrefs, use the SMR's bounds
            //////////////////////////////////////////////////
            _SetParametersByBoundsAndPrefs();

            //////////////////////////////////////////////////
            // Init the extended Joints array
            //////////////////////////////////////////////////
            _InitExtendedJoints();

            //////////////////////////////////////////////////
            // Skele_CRCont init
            //////////////////////////////////////////////////
            ms_crPaint = new MH.Skele.Skele_CRCont();

            //add callback for undo/redo
            _RegisterUndoRedo(true);

            ms_bNeedRecalcVerts = true;

            Transform animRoot = _GetAnimationRoot();
            ms_MirrorCtrl.Init(animRoot);

            _OnSwitchOpMode(EOPMode.INVALID, ms_eOpMode);
        }


        private static void _OnEndEdit()
        {
            Dbg.Log("Skele_SMREditor: Ending...");

            EditorApplication.update -= OnUpdate;

            ms_bEditing = false;

            ms_crPaint.Clear();
            ms_crPaint = null;

            ms_MirrorCtrl.Fini();


            ms_PS = null;
            ms_particles = null;

            if (ms_IKPlaneGO)
            {
                GameObject.DestroyImmediate(ms_IKPlaneGO.GetComponent<Renderer>().sharedMaterial); //clean the new-ed material
                ms_IKPlaneGO = null;
            }

            ms_IKTargetGO = null;

            if (ms_ExtraGO)
            {
                GameObject.DestroyImmediate(ms_ExtraGO);
            }
            SceneView.lastActiveSceneView.camera.depthTextureMode = DepthTextureMode.None;

            //del callback for undo/redo
            _RegisterUndoRedo(false);

            _SwitchSelectedWireframe(false);

            ms_SelCtrl.Clear();
            ms_ExtendedJoints.Clear();
            ms_ExtTrDataCopy.Clear();
            ms_IKSolver = null;

            ms_CurHandleTool = ms_CurHandleTool_Req = Tool.None;

            ms_PinIKSolvers.Clear();

            ms_PoseSet = null;

            ms_bHasModalWindow = false;

            ms_eOpMode = ms_eOpMode_Req = EOPMode.FK;

            _MoveAllSceneRootsBack();

            _SaveParametersToPref();

            ms_EndEdit_Req = false;

            ms_CurSelectedGO = null;


        }

        private static void _MoveAllSceneRootUnderTheTopMost()
        {
            //List<Transform> topmostTrs = new List<Transform>();

            //var prop = new HierarchyProperty(HierarchyType.GameObjects);
            //var expanded = new int[0];
            //while (prop.Next(expanded))
            //{
            //    GameObject oneGO = prop.pptrValue as GameObject;
            //    topmostTrs.Add(oneGO.transform);
            //}

            Transform[] trs = Transform.FindObjectsOfType<Transform>();

            GameObject go = new GameObject(TopMostGOName);
            ms_TheTopMost = go.transform;

            for (int idx = 0; idx < trs.Length; ++idx)
            {
                Transform tr = trs[idx];
                if (tr.parent == null)
                    tr.parent = ms_TheTopMost;
            }

            ms_TheTopMost.localScale = new Vector3(10, 10, 10);

            EUtil.AlignViewToObj(ms_CurSelectedGO);
        }

        private static void _MoveAllSceneRootsBack()
        {
            if (ms_TheTopMost != null)
            {
                ms_TheTopMost.localScale = Vector3.one;

                ms_TheTopMost.DetachChildren();

                GameObject.DestroyImmediate(ms_TheTopMost.gameObject);
                ms_TheTopMost = null;

                EUtil.AlignViewToObj(ms_CurSelectedGO);
            }
        }



        #endregion "Init & Fini"



        #region "DrawSceneGUI"
        /// <summary>
        /// 
        /// </summary>
        private static void _DrawSceneGUI()
        {

            //////////////////////////////////////////////////
            // UI 
            //////////////////////////////////////////////////
            if (ms_bDrawGUI)
            {
                Handles.BeginGUI();
                {
                    if (ms_bHasModalWindow)
                        GUI.enabled = false;

                    //0. help
                    if (ms_bShowHelp)
                    {
                        _DrawSceneGUI_Help();
                    }

                    //1. Animation helper
                    _DrawSceneGUI_AnimHelper();

                    //2. edit UI
                    switch (ms_eOpMode)
                    {
                        case EOPMode.FK:
                            {
                                _DrawSceneGUI_Normal();
                            }
                            break;
                        case EOPMode.IK:
                            {
                                _DrawSceneGUI_IK();
                            }
                            break;
                        case EOPMode.IK_Pinned:
                            {
                                _DrawSceneGUI_IKPin();
                            }
                            break;
                        case EOPMode.PoseManage:
                            {
                                _DrawSceneGUI_PoseManage();
                            }
                            break;
                        case EOPMode.BindPoseFixer:
                            {
                                _DrawSceneGUI_BindPosFixer();
                            }
                            break;
                        default: Dbg.LogErr("SMREditor._DrawSceneGUI: unexpected eUIMode: {0}", ms_eOpMode); break;
                    }

                    //GUI.ModalWindow(100, new Rect(200, 200, 200, 200), (x) => { GUI.DrawTexture(new Rect(0, 0, 400, 400), EditorGUIUtility.whiteTexture); }, "XXXX");

                    // draw those GUIWindows
                    ms_bHasModalWindow = GUIWindowMgr.Instance.OnGUI();
                }
                Handles.EndGUI();
            }

            //if put AddDeafultControl at the front or no AddDefaultControl, the gizmo at the TopRight of SceneView will be un-clickable
            //  also must call SceneView.Repaint at proper times
            //GL immediate mode, should check out sometime later

            //if( ! ms_bHasModalWindow )
            //{
            //    int controlID = GUIUtility.GetControlID(FocusType.Passive);
            //    HandleUtility.AddDefaultControl(controlID); 
            //}
        }

        private static void _DrawSceneGUI_Help()
        {
            Rect rc = new Rect(0, 0, WND_HELP_WIDTH, WND_HELP_HEIGHT);
            GUILayout.BeginArea(rc);
            {
                GUILayout.TextArea(
    @"Common:
    C: Focus on current selected bone's position
    P: toggle LOCAL/GLOBAL PivotRotation mode;
    [ ] \ : switch to Parent / child / sibling bone

FK Mode:
    ESCAPE: to cancel current operation mode;
    W/E/R: switch to MOVE/ROTATE/SCALE mode;
    `(backquote): change to IK mode;

IK Mode:
    ESCAPE: deselect bone / fallback to FK mode;
    1/2/3: change IK LinkLength;
    W: enter IK Move mode;
    E: enter IK Rotate / IK Root Rotate mode;
    X: toggle IK plane lock;
    T: flip the current IK link's middle joint
    `(backquote): change to FK mode directly;
");
            }
            GUILayout.EndArea();
        }

        private static void _DrawSceneGUI_Common()
        {
            GUILayout.BeginHorizontal();
            {
                string msg = null;
                Color c = Color.white;

                if (EUtil.Button("C", "Focus the Camera to Selected Joint or Reset Camera"))
                {
                    //EUtil.FrameSelected();
                    if (!ms_SelCtrl.NoSelection)
                    {
                        EUtil.SceneViewLookAt(ms_SelCtrl.SingleJoint.position);
                    }
                    else
                    {
                        EUtil.AlignViewToObj(ms_CurSelectedGO);
                    }
                    EUtil.ShowNotification("Camera_Reset");
                }

                if (EUtil.Button("R", "Reset All Bones"))
                {
                    _ResetAllBonesToPrefabPose();
                    _ResetIKTargetToEndJointIfNeeded();
                    EUtil.ShowNotification("Reset_All_Bones_To_PrefabPose");
                }

                c = ms_eOpMode == EOPMode.PoseManage ? Color.green : Color.white;
                if (EUtil.Button("G", "Pose Management", c))
                {
                    ms_eOpMode_Req = (ms_eOpMode == EOPMode.PoseManage) ? EOPMode.FK : EOPMode.PoseManage;
                    EUtil.ShowNotification(ms_eOpMode_Req == EOPMode.PoseManage ? "Enter_Pose_Manager" : "Leave_Pose_Manager");
                }

                ///------------------deprecated BindPose--------------------///
                //c = ms_eOpMode == EOPMode.BindPoseFixer ? Color.green : Color.white;
                //if (EUtil.Button("I", "Bind Pose Fixer", c))
                //{
                //    ms_eOpMode_Req = (ms_eOpMode == EOPMode.BindPoseFixer) ? EOPMode.FK : EOPMode.BindPoseFixer;
                //    EUtil.ShowNotification(ms_eOpMode_Req == EOPMode.BindPoseFixer ? "Start_BindPose_Fixer" : "Stop_BindPose_Fixer");
                //}
                ///------------------deprecated BindPose, end--------------------///

                switch (ms_CurPivotRotation)
                {
                    case PivotRotation.Global: c = Color.green; break;
                    case PivotRotation.Local: c = Color.white; break;
                    case PivotRotation.Parent: c = Color.yellow; break;
                }
                if (EUtil.Button("P", "Toggle Pivot Rotation", c))
                {
                    _TogglePivotRotation_UI();
                    EUtil.ShowNotification("Pivot_" + ms_CurPivotRotation.ToString());
                    _SceneRepaintAll();
                }

                EUtil.PushGUIEnable(ms_eOpMode != EOPMode.PoseManage);
                c = ms_bShowOptimzedJoint ? Color.green : Color.white;
                if (EUtil.Button("B", "Toggle Hidden Bones Display", c))
                {
                    ms_bShowOptimzedJoint = !ms_bShowOptimzedJoint;
                    EUtil.ShowNotification(ms_bShowOptimzedJoint ? "Display_Hidden_Bones" : "Conceal_Hidden_Bones");
                    EUtil.GetSceneView().Repaint();
                }
                EUtil.PopGUIEnable();

                c = ms_bHideWireframe ? Color.red : Color.green;
                if (EUtil.Button("W", "Toggle Wireframe", c))
                {
                    ms_bHideWireframe = !ms_bHideWireframe;
                    EUtil.ShowNotification(ms_bHideWireframe ? "Hide_Wireframe" : "Show_Wireframe");
                    _SwitchSelectedWireframe();
                }

                c = _EShowVertToColor(ms_eShowVertices);
                if (EUtil.Button("V", "Switch Vertex Marker Mode", c))
                {
                    ms_eShowVertices = (EShowVert)(((int)ms_eShowVertices + 1) % (int)EShowVert.END);
                    EUtil.ShowNotification(_EShowVertToString(ms_eShowVertices));
                    _SetVertMaterial();
                    _KeepRepainting();
                }

                if (EUtil.Button("H", "Toggle Shortcut List", ms_bShowHelp ? Color.green : Color.red))
                {
                    EUtil.ShowNotification("Toggle_Shortcut_List");
                    ms_bShowHelp = !ms_bShowHelp;
                }

                EUtil.PushGUIEnable(ms_eOpMode == EOPMode.FK || ms_eOpMode == EOPMode.IK);
                if (EUtil.Button("M", "Mirror Edit", ms_bMirrorEdit ? Color.green : Color.red))
                {
                    if (!ms_bMirrorEdit)
                    {
                        GUIWindowMgr.Instance.Add(new MirrorSettingWindow(_OnMirrorSettingFinish));
                    }
                    else
                    {
                        ms_bMirrorEdit = false;
                    }
                }
                EUtil.PopGUIEnable();

                msg = ms_LockSelection ? "Lock" : "Free";
                c = ms_LockSelection ? Color.red : Color.green;
                if (EUtil.Button(msg, "You'd better not click this", c))
                {
                    ms_LockSelection = !ms_LockSelection;
                    EUtil.ShowNotification(ms_LockSelection ? "Forbid_Select_Other_GO" : "Allow_Select_Other_GO\n(Not_Recommended)");
                    _KeepRepainting();
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void _DrawSceneGUI_AnimHelper()
        {
            if (ms_bAnimWndOpen)
            {
                Rect rect = new Rect(
                    Screen.width - WND_WIDTH,
                    Screen.height - WND_HEIGHT - ANIM_HELPER_WND_HEIGHT - 1,
                    WND_WIDTH,
                    ANIM_HELPER_WND_HEIGHT
                );
                if (ms_Background != null)
                    GUI.DrawTexture(rect, ms_Background);

                GUILayout.BeginArea(rect);
                {
                    EditorGUILayout.LabelField("Animation Helpers");

                    GUILayout.BeginHorizontal();
                    {
                        //string msg = null;
                        //Color c = Color.white;

                        if (EUtil.Button("SnapShot", "make keyframes based on difference with prefab pose"))
                        {
                            _MakePoseSnapshot();
                        }

                        if (EUtil.Button("Select Curves", "select curves in UAW based on current selected joints"))
                        {
                            _SelectCurvesBySelectedJoints();
                        }

                        if (EUtil.Button("Make RootMotion", "only for Generic, use root node's position/rotation to make RootMotion"))
                        {
                            _MakeRootMotion();
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        //string msg = null;
                        //Color c = Color.white;

                        if (EUtil.Button("Set P Key", "set position keys for selected joints"))
                        {
                            _SetPositionKeys();
                        }

                        if (EUtil.Button("Set R Key", "set rotation keys for selected joints"))
                        {
                            _SetRotationKeys();
                        }

                        if (EUtil.Button("Set S Key", "set scale keys for selected joints"))
                        {
                            _SetScaleKeys();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();

            }
        }

        private static void _DrawSceneGUI_Normal()
        {
            Rect rect = new Rect(Screen.width - WND_WIDTH, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT);
            if (ms_Background != null)
                GUI.DrawTexture(rect, ms_Background);

            GUILayout.BeginArea(rect);
            {
                _DrawSceneGUI_Common();

                if (EUtil.Button("IK", "Enter IK Mode", Color.white))
                {
                    ms_eOpMode_Req = EOPMode.IK;
                    EUtil.ShowNotification("Enter_IK_Mode");
                }

                ms_boneSize = EditorGUILayout.Slider("boneSize", ms_boneSize, 0f, 0.5f);
                ms_boneLineWidth = EditorGUILayout.Slider("lineWidth", ms_boneLineWidth, 0f, 5f);

                if (ms_eShowVertices != EShowVert.HIDE)
                {
                    float newSize = EditorGUILayout.Slider("vertSize", ms_vertSize, 0f, 1f);
                    if (newSize != ms_vertSize)
                        ms_bNeedRecalcVerts = true;
                    ms_vertSize = newSize;
                }

                if (!ms_SelCtrl.NoSelection)
                {
                    if (!ms_SelCtrl.HasMulti)
                        EditorGUILayout.LabelField(string.Format("SelectedBone: {0}", ms_SelCtrl.SingleJoint.name));
                    else
                        EditorGUILayout.LabelField(string.Format("Selected {0} Bones", ms_SelCtrl.Joints.Count));
#if IMDEBUGGING
                Vector3 newLR = EditorGUILayout.Vector3Field("LocalRotate:", ms_SelCtrl.SingleJoint.localEulerAngles);
                if( newLR != ms_SelCtrl.SingleJoint.localEulerAngles )
                {
                    Undo.RecordObject(ms_SelCtrl.SingleJoint, "DbgRotate");
                    newLR.x = Misc.NormalizeAngle(newLR.x);
                    newLR.y = Misc.NormalizeAngle(newLR.y);
                    newLR.z = Misc.NormalizeAngle(newLR.z);
                    ms_SelCtrl.SingleJoint.localEulerAngles = newLR;
                }
#endif
                }

            }
            GUILayout.EndArea();

            _DrawSelectedJointInspector();

        }

        private static void _DrawSceneGUI_IK()
        {
            // Main IK UI 
            Rect mainUIRect = new Rect(Screen.width - WND_WIDTH, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT);
            if (ms_Background != null)
                GUI.DrawTexture(mainUIRect, ms_Background);

            GUILayout.BeginArea(mainUIRect);
            {
                _DrawSceneGUI_Common();

                GUILayout.BeginHorizontal();
                {
                    Color c = Color.white;

                    if (EUtil.Button("FK", "Return FK Mode", Color.white))
                    {
                        ms_eOpMode_Req = EOPMode.FK;
                    }

                    if (EUtil.Button("Pin", "Use IK_Pin"))
                    {
                        ms_eOpMode_Req = EOPMode.IK_Pinned;
                    }

                    if (EUtil.Button("Flip", "Flip Joints on IK Link", Color.white))
                    {
                        _FlipIKJoints();
                        EUtil.ShowNotification("Flip Joints");
                        _SceneRepaintAll();
                    }

                    c = ms_bEnableIKPlaneLock ? Color.green : Color.white;
                    if (EUtil.Button("IKPlane", "Lock Joints on IK Plane", c))
                    {
                        ms_bEnableIKPlaneLock_Req = !ms_bEnableIKPlaneLock;
                        EUtil.ShowNotification("IK_Plane_Lock_" + (ms_bEnableIKPlaneLock_Req ? "On" : "Off"));
                        _SceneRepaintAll();
                    }

                }
                GUILayout.EndHorizontal();

                if (ms_SelCtrl.NoSelection)
                {
                    EditorGUILayout.LabelField("Please Select A End Joint...");
                }
                else
                {
                    Transform selJoint = ms_SelCtrl.SingleJoint;
                    EditorGUILayout.LabelField(String.Format("Selected: {0}", selJoint.name));
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(string.Format("LinkLength: {0}", ms_IKSolver.Count));
                        if (EUtil.Button("+", "Increase Link Length", Color.green))
                        { //try to increase the link
                            ms_IKSolverBoneLen_Req = ms_IKSolver.Count + 1;
                            //_TryMakeIKLink(ms_SelectedBone, ms_IKSolver.Count + 1);
                        }
                        if (EUtil.Button("-", "Decrease Link Length", Color.red))
                        { //try to shorten the link
                            ms_IKSolverBoneLen_Req = ms_IKSolver.Count - 1;
                            //_TryMakeIKLink(ms_SelectedBone, ms_IKSolver.Count - 1);
                        }
                    }
                    GUILayout.EndHorizontal();

                    ms_boneSize = EditorGUILayout.Slider("boneSize", ms_boneSize, 0f, 0.5f);
                    ms_boneLineWidth = EditorGUILayout.Slider("lineWidth", ms_boneLineWidth, 0f, 5f);

                    if (GUILayout.Button("Reselect End Joint"))
                    {
                        ms_SelCtrl.Select(null);
                    }

                }

            }
            GUILayout.EndArea();

            // single selected bone transform
            _DrawSelectedJointInspector();
        }

        private static void _DrawSceneGUI_IKPin()
        {
            Rect rect = new Rect(Screen.width - WND_WIDTH, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT);
            if (ms_Background != null)
                GUI.DrawTexture(rect, ms_Background);

            GUILayout.BeginArea(rect);
            {
                _DrawSceneGUI_Common();

                GUILayout.BeginHorizontal();
                {
                    Color c = Color.white;

                    if (EUtil.Button("Back", "Go Back to IK Mode", c))
                    {
                        EUtil.ShowNotification("Return IK Mode");
                        ms_eOpMode_Req = EOPMode.IK;
                        _SceneRepaintAll();
                    }
                }
                GUILayout.EndHorizontal();

                ms_boneSize = EditorGUILayout.Slider("boneSize", ms_boneSize, 0f, 0.5f);
                ms_boneLineWidth = EditorGUILayout.Slider("lineWidth", ms_boneLineWidth, 0f, 5f);

                if (!ms_SelCtrl.NoSelection)
                {
                    EditorGUILayout.LabelField(String.Format("Selected: {0}", ms_SelCtrl.SingleJoint.name));
                }
            }
            GUILayout.EndArea();

        }

        private static Vector2 ms_PoseManage_scrollpos = Vector2.zero;
        private static void _DrawSceneGUI_PoseManage()
        {
            // main GUI area
            {
                Rect rect = new Rect(Screen.width - WND_WIDTH, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT);
                if (ms_Background != null)
                    GUI.DrawTexture(rect, ms_Background);

                GUILayout.BeginArea(rect);
                {
                    _DrawSceneGUI_Common();

                    ms_boneSize = EditorGUILayout.Slider("boneSize", ms_boneSize, 0f, 0.5f);
                    ms_boneLineWidth = EditorGUILayout.Slider("lineWidth", ms_boneLineWidth, 0f, 5f);

                    if (!ms_SelCtrl.NoSelection)
                    {
                        EditorGUILayout.LabelField(string.Format("SelectedBone: {0}", ms_SelCtrl.SingleJoint.name));
                    }
                }
                GUILayout.EndArea();
            }

            // sub GUI area
            {
                Rect rect = new Rect(0, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT - WND_UPOFF);
                if (ms_Background != null)
                    GUI.DrawTexture(rect, ms_Background);

                GUILayout.BeginArea(rect);
                {
                    // save/load
                    GUILayout.BeginHorizontal(GUILayout.Width(WND_WIDTH - 20));
                    {
                        if (EUtil.Button("Save", "Save the poses to file"))
                        {
                            if (string.IsNullOrEmpty(ms_PoseSet.FileName))
                            {
                                string defPath = _GetPoseMgrSaveLoadPath();
                                string pathName = EditorUtility.SaveFilePanel("Save_Pose_File", defPath, "defaultName", "pose");
                                if (!string.IsNullOrEmpty(pathName))
                                {
                                    ms_PoseSet.FileName = pathName;
                                    ms_PoseSet.Save();
                                    _SavePoseMgrSaveLoadPath(pathName);
                                }
                            }
                            else
                            {
                                ms_PoseSet.Save();
                            }
                        }

                        if (EUtil.Button("Load", "Load poses from file"))
                        {
                            string defPath = _GetPoseMgrSaveLoadPath();
                            string pathName = EditorUtility.OpenFilePanel("Open_Pose_File", defPath, "pose");
                            if (!string.IsNullOrEmpty(pathName))
                            {
                                var newPoseSet = PoseSet.Load(pathName, ms_ExtendedJoints[0], ms_ExtendedJoints.ToArray());
                                if (newPoseSet != null)
                                {
                                    ms_PoseSet = newPoseSet;
                                    _SavePoseMgrSaveLoadPath(pathName);
                                }
                                else
                                {
                                    Dbg.LogWarn("SMREditor._DrawSceneGUI_PoseManage: failed to load pose file: {0}", pathName);
                                }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Pose list
                    ms_PoseManage_scrollpos = GUILayout.BeginScrollView(ms_PoseManage_scrollpos, false, true);
                    {
                        for (int idx = 0; idx < ms_PoseSet.Count; ++idx)
                        {
                            PoseDesc pose = ms_PoseSet.GetPose(idx);
                            string poseName = pose.m_PoseName;
                            PoseDataDict data = pose.m_PoseData;
                            GUILayout.BeginHorizontal();
                            {
                                if (GUILayout.Button(poseName, GUILayout.Width(150f)))
                                { // click the button will apply the pose
                                    EUtil.ShowNotification("Apply_Pose: " + poseName);
                                    _ApplyPoseToCurrentModel(data);
                                }
                                if (EUtil.Button("X", "Delete", Color.red))
                                { // delete the pose data
                                    ms_PoseSet.DelPose(idx);
                                    --idx;
                                }
                                if (EUtil.Button("O", "Overwrite", Color.yellow))
                                { // overwrite the pose data with current selection pose
                                    EUtil.ShowNotification("Overwrite: " + poseName);
                                    PoseDataDict newData = _GenPoseDataDictFromMultiSelect();
                                    pose.m_PoseData = newData;
                                }
                                if (EUtil.Button("R", "Rename", Color.white))
                                { // rename the pose
                                    var theCallback = new _Rename_InputCallback(pose);
                                    EUtil.StartInputModalWindow(theCallback.OnSuccess, null, "New Name:", "New Name for Pose", ms_Background);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        // new data
                        GUILayout.BeginHorizontal();
                        {
                            if (EUtil.Button("NEW_POSE", Color.green))
                            {
                                if (ms_SelCtrl.NoSelection)
                                {
                                    EUtil.ShowNotification("Have_Not_Selected_Any_Joints_Yet");
                                }
                                else
                                {
                                    EUtil.StartInputModalWindow(_CreateNewPose, null, "Name:", "Create New Pose", ms_Background);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndArea();

            }

        }

        private static void _DrawSceneGUI_BindPosFixer()
        {
            Rect rect = new Rect(Screen.width - WND_WIDTH, Screen.height - WND_HEIGHT, WND_WIDTH, WND_HEIGHT);
            if (ms_Background != null)
                GUI.DrawTexture(rect, ms_Background);

            GUILayout.BeginArea(rect);
            {
                _DrawSceneGUI_Common();
                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal();
                {
                    if (EUtil.Button("Apply", Color.green))
                    {
                        _ApplyBindPose();
                    }
                    if (EUtil.Button("Back", Color.red))
                    {
                        ms_eOpMode_Req = EOPMode.FK;
                    }
                }
                GUILayout.EndHorizontal();

                if (EUtil.Button(ms_BindPoseFixerMode.ToString(), "Toggle BindPose Fixer Mode", BLUE_COLOR))
                {
                    ms_BindPoseFixerMode = (EBindPoseFixerMode)
                        (((int)(ms_BindPoseFixerMode + 1)) % (int)EBindPoseFixerMode.END);
                }

                ms_boneSize = EditorGUILayout.Slider("boneSize", ms_boneSize, 0f, 0.5f);
                ms_boneLineWidth = EditorGUILayout.Slider("lineWidth", ms_boneLineWidth, 0f, 5f);

                if (ms_eShowVertices != EShowVert.HIDE)
                {
                    float newSize = EditorGUILayout.Slider("vertSize", ms_vertSize, 0f, 1f);
                    if (newSize != ms_vertSize)
                        ms_bNeedRecalcVerts = true;
                    ms_vertSize = newSize;
                }

                if (!ms_SelCtrl.NoSelection)
                {
                    EditorGUILayout.LabelField(string.Format("SelectedBone: {0}", ms_SelCtrl.SingleJoint.name));
                }
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// draw the original unity SMR editor
        /// </summary>
        private void _DrawUnitySMREditor()
        {
            if (EditorApplication.isCompiling)
            {
                _EnsureDestroyUnitySMREditor();
            }

            if ((ms_origEditor == null || ms_origEditor.target != target)
                 && !EditorApplication.isCompiling)
            {
                _EnsureDestroyUnitySMREditor();
                Type tp = RCall.GetTypeFromString("UnityEditor.SkinnedMeshRendererEditor");
                if (tp != null)
                {
                    ms_origEditor = Editor.CreateEditor(target, tp);
                }
            }

            if (ms_origEditor != null)
            {
                ms_origEditor.OnInspectorGUI();
            }
            else //in case cannot get the class 
            {
                Dbg.LogWarn("Failed to get Class UnityEditor.SkinnedMeshRendererEditor");
                DrawDefaultInspector();
            }
        }

        private void _EnsureDestroyUnitySMREditor()
        {
            if (ms_origEditor != null)
            {
                Editor.DestroyImmediate(ms_origEditor);
                ms_origEditor = null;
            }
        }

        private static void _DrawSelectedJointInspector()
        {
            if (ms_bShowTransformInspector && !ms_SelCtrl.NoSelection && !ms_SelCtrl.HasMulti)
            {
                Transform selJoint = ms_SelCtrl.SingleJoint;
                //if (ms_tmpTrEditor == null)
                //{
                //    ms_tmpTrEditor = Editor.CreateEditor(selJoint);
                //}
                //else if (ms_tmpTrEditor != selJoint)
                //{
                //    Editor.DestroyImmediate(ms_tmpTrEditor);
                //    ms_tmpTrEditor = Editor.CreateEditor(selJoint);
                //}

                Rect rc = new Rect(0, 0, 260f, 60f);
                GUI.DrawTexture(rc, ms_Background);
                GUILayout.BeginArea(rc);
                {
                    //ms_tmpTrEditor.OnInspectorGUI();
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = EUtil.DrawV3(selJoint.localPosition);
                    Vector3 newEuler = EUtil.DrawV3(selJoint.localEulerAngles);
                    Vector3 newScale = EUtil.DrawV3(selJoint.localScale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(selJoint, "Modify Transform");
                        selJoint.localPosition = newPos;
                        selJoint.localEulerAngles = newEuler;
                        selJoint.localScale = newScale;
                        HotFix.FixRotation(selJoint);
                    }
                }
                GUILayout.EndArea();

            }
            else
            {
                //if (ms_tmpTrEditor != null)
                //{
                //    Editor.DestroyImmediate(ms_tmpTrEditor);
                //    ms_tmpTrEditor = null;
                //}
            }
        }

        #endregion "DrawSceneGUI"


        #region "Shortcuts"
        // "Shortcuts" 

        private void _Shortcut_Execute()
        {
            if (ms_bHasModalWindow)
                return;

            _Shortcut_Common();

            switch (ms_eOpMode)
            {
                case EOPMode.FK: _Shortcut_NormalMode(); break;
                case EOPMode.IK: _Shortcut_IKMode(); break;
                case EOPMode.IK_Pinned: _Shortcut_IKPinMode(); break;
                case EOPMode.PoseManage: _Shortcut_PoseManage(); break;
                case EOPMode.BindPoseFixer: _Shortcut_BindPoseFixer(); break;
            }
        }

        private static void _Shortcut_Common()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.C:
                        {
                            if (!ms_SelCtrl.NoSelection)
                            {
                                EUtil.SceneViewLookAt(ms_SelCtrl.SingleJoint.position);
                            }
                        }
                        break;
                    case KeyCode.P:
                        {
                            _TogglePivotRotation();
                            _SceneRepaintAll();
                        }
                        break;
                }
            }

            ViewRotate.RotateViewByEvent();
        }

        private static void _Shortcut_NormalMode()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.W:
                        {
                            ms_CurHandleTool_Req = Tool.Move;
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.E:
                        {
                            ms_CurHandleTool_Req = Tool.Rotate;
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.R:
                        {
                            ms_CurHandleTool_Req = Tool.Scale;
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.BackQuote:
                        {
                            ms_eOpMode_Req = EOPMode.IK;
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.Escape:
                        {
                            if (_IsLMBUp())
                            {
                                HandleUtility.Repaint();
                                if (ms_CurHandleTool != Tool.None)
                                {
                                    ms_CurHandleTool_Req = Tool.None;
                                }
                                else
                                {
                                    ms_SelCtrl.Select(null); //clear the selected bone
                                    return;
                                }
                            }
                        }
                        break;

                    case KeyCode.LeftBracket:
                    case KeyCode.RightBracket:
                    case KeyCode.Backslash:
                        {
                            _ChangeCurrentBoneByShortcut(ev);
                        }
                        break;
                }
            }
        }

        private static void _Shortcut_IKMode()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.BackQuote:
                        {
                            ms_eOpMode_Req = EOPMode.FK;
                            ms_CurHandleTool_Req = Tool.Rotate;
                        }
                        break;
                    case KeyCode.Escape:
                        {
                            if (!ms_SelCtrl.NoSelection)
                                ms_SelCtrl.Select(null);
                            else
                                ms_eOpMode_Req = EOPMode.FK;
                        }
                        break;
                    case KeyCode.W:
                        {
                            if (ms_CurHandleTool != Tool.Move)
                            {
                                ms_CurHandleTool_Req = Tool.Move;
                                _SceneRepaintAll();
                            }
                        }
                        break;
                    case KeyCode.E:
                        {
                            if (ms_CurHandleTool != Tool.Rotate && ms_CurHandleTool != Tool.RotateIKRoot)
                            {
                                ms_CurHandleTool_Req = Tool.Rotate;
                            }
                            else
                            {
                                if (ms_CurHandleTool == Tool.Rotate)
                                {
                                    ms_CurHandleTool_Req = Tool.RotateIKRoot;
                                }
                                else
                                {
                                    ms_CurHandleTool_Req = Tool.Rotate;
                                }
                            }
                            _SceneRepaintAll();
                        }
                        break;
                    case KeyCode.T:
                        {
                            _FlipIKJoints();
                            _SceneRepaintAll();
                        }
                        break;

                    case KeyCode.X:
                        {
                            ms_bEnableIKPlaneLock_Req = !ms_bEnableIKPlaneLock;
                            _SceneRepaintAll();
                        }
                        break;
                    case KeyCode.Alpha1:
                        {
                            if (ms_IKSolver.Count != 1)
                            {
                                ms_IKSolverBoneLen_Req = 1;
                                //_TryMakeIKLink(ms_SelectedBone, 1);
                            }
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.Alpha2:
                        {
                            if (ms_IKSolver.Count != 2)
                            {
                                ms_IKSolverBoneLen_Req = 2;
                                //_TryMakeIKLink(ms_SelectedBone, 2);
                            }
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.Alpha3:
                        {
                            if (ms_IKSolver.Count != 3)
                            {
                                ms_IKSolverBoneLen_Req = 3;
                                //_TryMakeIKLink(ms_SelectedBone, 3);
                            }
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.LeftBracket:
                    case KeyCode.RightBracket:
                    case KeyCode.Backslash:
                        {
                            _ChangeCurrentBoneByShortcut(ev);
                        }
                        break;
                }
            }
        }

        private static void _Shortcut_IKPinMode()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.BackQuote:
                        {
                            ms_eOpMode_Req = EOPMode.FK;
                            ms_CurHandleTool_Req = Tool.Rotate;
                        }
                        break;
                    case KeyCode.Escape:
                        {
                            if (!ms_SelCtrl.NoSelection)
                                ms_SelCtrl.Select(null);
                            else
                                ms_eOpMode_Req = EOPMode.IK;
                        }
                        break;

                }
            }
        }

        private static void _Shortcut_PoseManage()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.BackQuote:
                        {
                            ms_eOpMode_Req = EOPMode.FK;
                            ms_CurHandleTool_Req = Tool.Rotate;
                        }
                        break;
                    case KeyCode.Escape:
                        {
                            ms_SelCtrl.Select(null);
                            _SceneRepaintAll();
                        }
                        break;
                }
            }
        }

        private static void _Shortcut_BindPoseFixer()
        {
            Event ev = Event.current;

            if (ev.type == EventType.KeyUp && !ev.isMouse && !ms_bRightBtnIsDown)
            {
                switch (ev.keyCode)
                {
                    case KeyCode.W:
                        {
                            ms_CurHandleTool_Req = Tool.Move;
                            HandleUtility.Repaint();
                        }
                        break;
                    case KeyCode.E:
                        {
                            ms_CurHandleTool_Req = Tool.Rotate;
                            HandleUtility.Repaint();
                        }
                        break;
                }
            }
        }

        #endregion "Shortcuts"
        #region "OnChange Callback"
        // "OnChange Callback" 

        private static void _OnSwitchOpMode(EOPMode prevMode, EOPMode newMode)
        {
            switch (prevMode)
            {
                case EOPMode.IK:
                    {
                        var jointsArray = ms_SelCtrl.JointsArray;
                        if (jointsArray.Length > 0)
                        {
                            Transform rootJoint = jointsArray[0];
                            ms_SelCtrl.Select(rootJoint);
                        }
                    }
                    break;
                case EOPMode.IK_Pinned:
                    {
                        ms_PinIKSolvers.Clear();
                    }
                    break;
                case EOPMode.PoseManage:
                    {
                        //ms_MultiSelectJoints.Clear();
                        //ms_SelectedBone = null;

                        ms_SelCtrl.Clear();
                    }
                    break;
                case EOPMode.BindPoseFixer:
                    {
                        _DeleteCopyOfSkeleton();
                        ms_SelCtrl.Clear();
                    }
                    break;
            }

            switch (newMode)
            {
                case EOPMode.FK:
                    {
                        ms_IKTargetGO.transform.position = Vector3.zero;

                        ms_IKPlaneGO.SetActive(false);

                        //change to rotate tool
                        ms_CurHandleTool = Tool.Rotate;
                        ms_CurHandleTool_Req = Tool.Rotate;
                    }
                    break;
                case EOPMode.IK:
                    {
                        _OnChangePivotRotation(ms_CurPivotRotation, PivotRotation.Global); //force to global rotation

                        // hide vertices when IK
                        ms_eShowVertices = EShowVert.HIDE;

                        // change to move tool
                        ms_CurHandleTool = Tool.Move;
                        ms_CurHandleTool_Req = Tool.Move;

                        // try use current selected bone for IK...
                        if (!_ChangeBone4IKLnk(ms_SelCtrl.SingleJoint))
                            ms_SelCtrl.DirectSetSelectJoint(null);

                        // set IKPlane
                        ms_IKPlaneGO.SetActive(ms_bEnableIKPlaneLock);
                    }
                    break;
                case EOPMode.IK_Pinned:
                    {
                        // hide vertices
                        ms_eShowVertices = EShowVert.HIDE;

                        // reset to move
                        ms_CurHandleTool = Tool.Move;
                        ms_CurHandleTool_Req = Tool.Move;

                        // hide the IK_Plane
                        ms_bEnableIKPlaneLock_Req = false;

                        // force show all bones
                        ms_bShowOptimzedJoint = true;
                    }
                    break;
                case EOPMode.PoseManage:
                    {
                        ms_SelCtrl.Clear();
                        ms_bShowOptimzedJoint = true; //force show all joints

                        if (ms_PoseSet == null)
                        {
                            ms_PoseSet = new PoseSet(ms_ExtendedJoints[0], ms_ExtendedJoints.ToArray());
                        }
                    }
                    break;
                case EOPMode.BindPoseFixer:
                    {
                        _ResetAllBonesToBindPose();

                        _ClearAllBonesUndo();
                        _MakeCopyOfSkeleton();

                        ms_bShowOptimzedJoint = true; //force show all joints
                        //ms_SelectedBone_Req = null;
                        ms_CurHandleTool_Req = Tool.None;

                        ms_SelCtrl.Clear();

                        _SceneRepaintAll();
                    }
                    break;
                default:
                    Dbg.LogErr("SMREditor._OnSwitchMode: unexpected mode: {0}", newMode);
                    break;
            }

            ms_eOpMode = newMode;
        }

        private static void _OnChangeSelectedBone(Transform prevJoint, Transform newJoint)
        {
            switch (ms_eOpMode)
            {
                case EOPMode.FK:
                    {
                        ms_bNeedRecalcVerts = true;
                        _SceneRepaintAll();
                    }
                    break;
                case EOPMode.IK:
                    {
                        if (!_ChangeBone4IKLnk(newJoint))
                        {
                            newJoint = null;
                        }

                        _SceneRepaintAll();
                    }
                    break;
            }

            ms_SelCtrl.DirectSetSelectJoint(newJoint);

            if (newJoint != null)
                EditorGUIUtility.PingObject(newJoint.gameObject);

            _RenewTmpPosRot();
        }

        private static void _OnSwitchHandleTool(Tool oldTool, Tool newTool)
        {
            ms_CurHandleTool = newTool;

            if (ms_eOpMode == EOPMode.IK)
            {
                //reset the IK target to the endjoint's position
                ms_IKPlaneRot = 0;
                if (ms_IKSolver != null)
                {
                    Transform[] joints = ms_IKSolver.GetJoints();
                    ms_IKTargetGO.transform.position = joints[joints.Length - 1].position;
                }

            }
        }

        private static void _OnSwitchIKPlaneLock(bool oldVal, bool newVal)
        {
            ms_bEnableIKPlaneLock = ms_bEnableIKPlaneLock_Req;
            ms_IKPlaneGO.SetActive(newVal);
        }

        /// <summary>
        /// process the request to pin/unpin a joint
        /// </summary>
        private static void _OnChangeIKPin()
        {
            Transform cacheTr = ms_ReqChangePin;
            ms_ReqChangePin = null;

            // remove
            for (int i = 0; i < ms_PinIKSolvers.Count; ++i)
            {
                Transform[] joints = ms_PinIKSolvers[i].GetJoints();
                Transform endJoint = joints[joints.Length - 1];
                if (cacheTr == endJoint)
                {
                    ms_PinIKSolvers.RemoveAt(i);
                    return;
                }
            }

            //add
            ISolver newIK = null;
            bool bSuccess = _TryMakeIKLink(ref newIK, cacheTr, 2);
            if (bSuccess)
            {
                newIK.Target = cacheTr.position;
                ms_PinIKSolvers.Add(newIK);
            }
            else
            {
                EUtil.ShowNotification("Cannot_Add_IKPin: Not_Enough_Bones");
            }
        }

        private static void _OnChangeIKSolverBoneLen(int oldBoneLen, int newBoneLen)
        {
            if (oldBoneLen == newBoneLen)
                return;

            Transform[] joints = ms_IKSolver.GetJoints();
            _TryMakeIKLink(joints[joints.Length - 1], newBoneLen);
        }


        private static void _OnChangePivotRotation(PivotRotation oldPivot, PivotRotation newPivot)
        {
            //switch(newPivot)
            //{
            //    case PivotRotation.Global:
            //        {
            //            ms_RotTmp = Quaternion.identity;
            //        }
            //        break;
            //    case PivotRotation.Parent:
            //        {
            //            Transform selJoint = ms_SelCtrl.SingleJoint;
            //            if( selJoint != null && selJoint.parent != null)
            //            {
            //                ms_RotTmp = selJoint.parent.rotation;
            //            }
            //            else
            //            {
            //                ms_RotTmp = Quaternion.identity;
            //            }
            //        }
            //        break;
            //}

            ms_CurPivotRotation = newPivot;

            _RenewTmpPosRot();
        }

        #endregion "OnChange Callback"

        #region "Markers & Handles"
        // "Markers & Handles" 

        /// <summary>
        /// draw the marker for IK Target, and move/rotate it
        /// </summary>
        private static float ms_IKPlaneRot = 0f;
        private static void _ManipulateMarkerForIKTarget()
        {
            Transform extTr = ms_IKTargetGO.transform;
            Vector3 pos = extTr.position;

            Transform selJoint = ms_SelCtrl.SingleJoint;

            // if update IKTargetGO's rotation every frame with ms_SelectedBone's rotation, Handles.PositionHandle will go WILD!!
            if (Event.current.type == EventType.MouseUp)
            {
                if (extTr.rotation != selJoint.rotation)
                {
                    extTr.rotation = selJoint.rotation;
                    HandleUtility.Repaint();
                }
            }

            Quaternion r = (ms_CurPivotRotation == PivotRotation.Local) ? extTr.rotation : Quaternion.identity;

            if (ms_CurHandleTool == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pos, r);
                if (EditorGUI.EndChangeCheck())
                {
                    _SetDraggingHandle(true);
                    if (ms_DraggingHandle == EHandleState.JustStartDragging)
                    {
                        var arr = ms_IKSolver.GetJoints();
                        Array.Resize(ref arr, arr.Length - 1);
                        _ForceUndoRecordJoints_Rotate(arr);
                        _SetPositionKey(extTr);
                    }

                    if (ms_bEnableIKPlaneLock)
                    {
                        newPos = _LockEndEffectorToPlane(newPos);
                    }

                    //Transform[] joints = ms_IKSolver.GetJoints();
                    ms_IKSolver.Target = newPos;

                    //Undo.RecordObject(extTr, "IK_OP");
                    //Undo.RecordObjects(joints, "IK_OPs");

                    ms_IKSolver.Execute();
                    extTr.position = newPos;
                }

                if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                {
                    _SetDraggingHandle(false);
                    var arr = ms_IKSolver.GetJoints();
                    Array.Resize(ref arr, arr.Length - 1);
                    _ForceUndoRecordJoints_Rotate(arr);
                    _SetPositionKey(extTr);
                }
            }
            else if (ms_CurHandleTool == Tool.Rotate)
            {
                // draw the line from IKroot bone to IK-end-effector
                Transform[] joints = ms_IKSolver.GetJoints();
                Transform IKRootJoint = joints[0];
                Transform IKEndEffector = joints[joints.Length - 1];
                Vector3 theAxis = (IKEndEffector.position - IKRootJoint.position).normalized;
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(ms_boneLineWidth, IKRootJoint.position, IKEndEffector.position);

                Handles.BeginGUI();
                {
                    Vector2 pt = HandleUtility.WorldToGUIPoint(IKEndEffector.position) + new Vector2(10, 10);
                    Rect rc = new Rect(pt.x, pt.y, 300, 20);
                    if (ms_Background != null)
                        GUI.DrawTexture(rc, ms_Background);
                    GUILayout.BeginArea(rc);
                    {
                        EditorGUI.BeginChangeCheck();
                        float v = EditorGUILayout.Slider("Rotate:", ms_IKPlaneRot, -180, 180);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _SetDraggingHandle(true);
                            if (ms_DraggingHandle == EHandleState.JustStartDragging)
                                _UndoRecordSelectedJoints("Rotate IKPlane");

                            float delta = v - ms_IKPlaneRot;
                            ms_IKPlaneRot = v;
                            IKRootJoint.Rotate(theAxis, delta, Space.World);
                        }

                        if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                        {
                            _SetDraggingHandle(false);
                            _SetRotationKey(IKRootJoint);
                        }
                    }
                    GUILayout.EndArea();
                }
                Handles.EndGUI();
            }
            else if (ms_CurHandleTool == Tool.RotateIKRoot)
            {
                //draw the rotation handle on the IK Root
                Transform[] joints = ms_IKSolver.GetJoints();
                Transform IKRootJoint = joints[0];

                EditorGUI.BeginChangeCheck();
                Quaternion newRot = Handles.RotationHandle(IKRootJoint.rotation, IKRootJoint.position);
                if (EditorGUI.EndChangeCheck())
                {
                    _SetDraggingHandle(true);
                    if (ms_DraggingHandle == EHandleState.JustStartDragging)
                        _UndoRecordSelectedJoints("IKRoot Rotate");

                    IKRootJoint.rotation = newRot;
                }

                if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                {
                    _SetDraggingHandle(false);
                    _SetRotationKey(IKRootJoint);
                }
            }
            else
            {
                Dbg.LogErr("SMREditor._ManipulateMarkerForIKTarget: force snap to Move, unexpected HandleTool: {0}", ms_CurHandleTool);
                ms_CurHandleTool_Req = Tool.Move;
                _SceneRepaintAll();
            }

            _UpdateIKPlane(); //pos & rotate the IK plane if needs to     
        }

        /// <summary>
        /// update the IK plane normal
        /// </summary>
        private static void _UpdateIKPlane()
        {
            Transform[] joints = ms_IKSolver.GetJoints();
            Vector3 n = _CalcIKPlaneNormal();

            ms_IKPlaneGO.transform.position = joints[0].position;
            ms_IKPlaneGO.transform.rotation = Quaternion.LookRotation(n);
        }

        /// <summary>
        /// draw all the verts markers
        /// </summary>
        private static void _CalcMarkersForVertices()
        {
            if (ms_eShowVertices == EShowVert.HIDE)
            {
                return;
            }

            if (!ms_bNeedRecalcVerts && !_IsBonesTransformChangedAndResetChangedFlag())
            {
                return;
            }

            ms_bNeedRecalcVerts = false;

            SkinnedMeshRenderer smr = ms_SMR;
            //Transform smrTr = smr.transform;
            //Quaternion rot = smrTr.rotation;

            Mesh mesh = smr.sharedMesh;
            Vector3[] verts = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            Transform[] bones = smr.bones;
            Matrix4x4[] bindposes = mesh.bindposes;

            for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx)
            {
                Transform j = bones[boneIdx];
                ms_BoneInSelectionArray[boneIdx] = ms_SelCtrl.IsSelectedJoint(j);
            }
            //int selectedBoneIdx = Array.FindIndex(bones, x => { return x == ms_SelCtrl.SingleJoint; });

            //Matrix4x4 localMat = Matrix4x4.TRS(Vector3.zero, smrTr.localRotation, Vector3.one);

            for (int vidx = 0; vidx < verts.Length; ++vidx)
            {
                //Vector3 pos = smrTr.TransformPoint(verts[vidx]);
                Vector3 localPos = verts[vidx];
                Vector3 pos = Vector3.zero;

                float fUnderSelectedBone = 0f; //how much weight by selected bone

                BoneWeight bw = weights[vidx];

                if (bw.weight0 > 0)
                {
                    Matrix4x4 mat = bones[bw.boneIndex0].localToWorldMatrix /** localMat*/ * bindposes[bw.boneIndex0];
                    pos += mat.MultiplyPoint(localPos) * bw.weight0;
                    fUnderSelectedBone += ms_BoneInSelectionArray[bw.boneIndex0] ? bw.weight0 : 0;
                }
                if (bw.weight1 > 0)
                {
                    Matrix4x4 mat = bones[bw.boneIndex1].localToWorldMatrix /** localMat*/ * bindposes[bw.boneIndex1];
                    pos += mat.MultiplyPoint(localPos) * bw.weight1;
                    fUnderSelectedBone += ms_BoneInSelectionArray[bw.boneIndex1] ? bw.weight1 : 0;
                }
                if (bw.weight2 > 0)
                {
                    Matrix4x4 mat = bones[bw.boneIndex2].localToWorldMatrix /** localMat*/ * bindposes[bw.boneIndex2];
                    pos += mat.MultiplyPoint(localPos) * bw.weight2;
                    fUnderSelectedBone += ms_BoneInSelectionArray[bw.boneIndex2] ? bw.weight2 : 0;
                }
                if (bw.weight3 > 0)
                {
                    Matrix4x4 mat = bones[bw.boneIndex3].localToWorldMatrix /** localMat*/ * bindposes[bw.boneIndex3];
                    pos += mat.MultiplyPoint(localPos) * bw.weight3;
                    fUnderSelectedBone += ms_BoneInSelectionArray[bw.boneIndex3] ? bw.weight3 : 0;
                }

                float r = Mathf.Lerp(0f, 1f, Mathf.Clamp01(fUnderSelectedBone / 0.5f));
                float g = Mathf.Lerp(1f, 0f, Mathf.Clamp01((fUnderSelectedBone - 0.5f) / 0.5f));
                Color c = new Color(r, g, 0);

                ms_particles[vidx].color = c;
                ms_particles[vidx].size = ms_vertSize;
                ms_particles[vidx].position = pos;
                ms_particles[vidx].startLifetime = float.MaxValue * 0.1f;
                ms_particles[vidx].lifetime = float.MaxValue * 0.1f;

                //Handles.color = c;
                //Handles.DotCap(0, pos, rot, ms_vertSize);
            }
        }

        /// <summary>
        /// use handles to draw markers for bones
        /// </summary>
        private static void _DrawMarkersForBones(Transform rootBone, Transform[] bones, ColorFunc jointColorFunc = null)
        {
            if (bones.Length <= 0)
                return;

            Vector3 sceneCamPos = _GetSceneCamPos();

            // draw bones
            BitArray flags = new BitArray(bones.Length);
            bool bAllSet = false;
            while (!bAllSet)
            {
                int idx = 0;
                for (; idx < flags.Count; ++idx)
                {
                    if (flags[idx] != true)
                    {
                        _DrawBoneConnections(bones[idx], bones, flags);
                        break;
                    }
                }
                bAllSet = (idx == flags.Count);
            }


            // draw joints
            float jointMarkerBaseSize = 1.0f;//HandleUtility.GetHandleSize(bones[0].position); 
            if (jointColorFunc == null)
            {
                jointColorFunc = _ColorFunc_JointIsSelectedJoint;
            }

            foreach (Transform oneBone in bones)
            {
                Handles.color = jointColorFunc(oneBone);
                Vector3 jointPos = oneBone.position;

                if (oneBone == rootBone)
                {
                    Handles.SphereCap(0, jointPos, Quaternion.identity, ms_boneSize * jointMarkerBaseSize);
                }
                else
                {
                    Vector3 camDir = (sceneCamPos - oneBone.position).normalized;

                    Handles.DrawSolidDisc(jointPos, camDir, ms_boneSize * jointMarkerBaseSize * 0.2f);
                    Handles.DrawWireDisc(jointPos, oneBone.forward, ms_boneSize * jointMarkerBaseSize);
                }
            }
        }

        private static Color _ColorFunc_JointIsSelectedJoint(Transform oneBone)
        {
            return ms_SelCtrl.IsSelectedJoint(oneBone) ? Color.blue : Color.yellow;
        }

        private static Color _ColorFunc_JointIsInMultiSelectJoint(Transform oneBone)
        {
            return ms_SelCtrl.IsSelectedJoint(oneBone) ? Color.green : Color.yellow;
        }

        /// <summary>
        /// recursively draw bone connections
        /// </summary>
        private static void _DrawBoneConnections(Transform curBone, Transform[] bones, BitArray flags)
        {
            Vector3 posStart = curBone.position;
            Vector3 camPos = _GetSceneCamPos();
            int idxInBones = Array.FindIndex(bones, x => { return x == curBone; });
            if (idxInBones != -1)
                flags.Set(idxInBones, true);

            for (int idx = 0; idx < curBone.childCount; ++idx)
            {
                Transform child = curBone.GetChild(idx);
                if (bones.Contains(child))
                {
                    //DRAW
                    Vector3 posEnd = child.position;
                    Vector3 perp = Vector3.Cross(camPos - posStart, posEnd - posStart).normalized;
                    Vector3 posA = posStart + perp * ms_boneSize * 0.3f;
                    Vector3 posB = posStart - perp * ms_boneSize * 0.3f;
                    Handles.color = Color.magenta;
                    Handles.DrawAAPolyLine(ms_boneLineWidth, posEnd, posA, posB, posEnd);

                    // Recursive
                    _DrawBoneConnections(child, bones, flags);
                }
            }
        }


        /// <summary>
        /// draw the bone handle, and manipulate the bone's transform
        /// </summary>
        private static Vector3 ms_PosTmp = Vector3.zero;
        private static Quaternion ms_RotTmp = Quaternion.identity;
        private void _ManipulateFKBones()
        {
            if (ms_CurHandleTool == Tool.None)
                return;

            // draw handles and control corresponding transform

            Transform selJoint = ms_SelCtrl.SingleJoint;
            Transform[] selectedJoints = ms_SelCtrl.JointsArray;

            //Vector3 pos = ms_PosTmp;
            //Quaternion rot = selJoint.rotation;
            Vector3 scale = selJoint.localScale;

            switch (ms_CurHandleTool)
            {
                case Tool.Move:
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = Handles.PositionHandle(ms_PosTmp, ms_RotTmp);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _SetDraggingHandle(true);
                            if (ms_DraggingHandle == EHandleState.JustStartDragging)
                                _UndoRecordSelectedJoints("Move Bone");

                            Vector3 deltaPos = newPos - ms_PosTmp;
                            ms_PosTmp = newPos;

                            //Undo.RecordObjects(selectedJoints, "Move Bone");
                            for (int i = 0; i < selectedJoints.Length; ++i)
                            {
                                Transform j = selectedJoints[i];
                                if (ms_SelCtrl.IsChildOfSelectedJoint(j))
                                    continue;
                                j.position += deltaPos;
                            }
                        }

                        if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                        {
                            _SetDraggingHandle(false);
                            _ForceUndoRecordSelectedJoints_Pos();
                        }
                    }
                    break;
                case Tool.Rotate:
                    {
                        EditorGUI.BeginChangeCheck();
                        Quaternion newRot = Handles.RotationHandle(ms_RotTmp, ms_PosTmp);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _SetDraggingHandle(true);
                            if (ms_DraggingHandle == EHandleState.JustStartDragging)
                                _UndoRecordSelectedJoints("Rotate Bone");
                            Quaternion deltaRot = newRot * Quaternion.Inverse(ms_RotTmp);
                            ms_RotTmp = newRot;

                            //Undo.RecordObjects(selectedJoints, "Rotate Bone");
                            for (int i = 0; i < selectedJoints.Length; ++i)
                            {
                                Transform j = selectedJoints[i];
                                if (ms_SelCtrl.IsChildOfSelectedJoint(j))
                                    continue;
                                j.rotation = deltaRot * j.rotation;
                            }
                        }

                        if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                        {
                            _SetDraggingHandle(false);
                            _ForceUndoRecordSelectedJoints_Rotate();
                        }
                    }
                    break;
                case Tool.Scale:
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newScale = Handles.ScaleHandle(scale, ms_PosTmp, ms_RotTmp, HandleUtility.GetHandleSize(ms_PosTmp));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _SetDraggingHandle(true);
                            if (ms_DraggingHandle == EHandleState.JustStartDragging)
                                _UndoRecordSelectedJoints("Scale Bone");
                            Vector3 deltaScale = V3Ext.DivideComp(newScale, scale);
                            //Undo.RecordObjects(selectedJoints, "Scale Bone");

                            for (int i = 0; i < selectedJoints.Length; ++i)
                            {
                                Transform j = selectedJoints[i];
                                if (ms_SelCtrl.IsChildOfSelectedJoint(j))
                                    continue;
                                Vector3 lscale = j.localScale;
                                j.localScale = V3Ext.MultiplyComp(lscale, deltaScale);
                            }
                        }

                        if (_IsLMBJustUp() && ms_DraggingHandle == EHandleState.Dragging)
                        {
                            _SetDraggingHandle(false);
                            _ForceUndoRecordSelectedJoints_Scale();
                        }
                    }
                    break;
                default:
                    Dbg.LogErr("SMREditor._ManipulateFKBones: unexpected Tool enum: {0}", ms_CurHandleTool);
                    break;
            }

        }

        private void _ManipulateBones_BindPoseFixer()
        {
            if (ms_CurHandleTool == Tool.None)
                return;

            // draw handles and control corresponding transform

            Transform selJoint = ms_SelCtrl.SingleJoint;
            Vector3 pos = selJoint.position;
            Quaternion rot = selJoint.rotation;
            //Vector3 scale = selJoint.localScale;

            //Quaternion r = (ms_CurPivotRotation == PivotRotation.Local) ? rot : Quaternion.identity;
            Quaternion r = GetQuaternionByPivotRotation(selJoint);

            switch (ms_CurHandleTool)
            {
                case Tool.Move:
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = Handles.PositionHandle(pos, r);
                        if (EditorGUI.EndChangeCheck())
                        {
                            switch (ms_BindPoseFixerMode)
                            {
                                case EBindPoseFixerMode.NORMAL:
                                    {
                                        Undo.RecordObject(selJoint, "Move BindPose");
                                        selJoint.position = newPos;
                                    }
                                    break;
                                case EBindPoseFixerMode.SOLE_JOINT:
                                    {
                                        Undo.RecordObjects(ms_BindPoseFixerSkeleton.ToArray(), "Move BindPose");
                                        _CopyBindPoseCurrentPose(false);
                                        selJoint.position = newPos;
                                        _RevertChildrenJointWorldTransform(selJoint, ms_BindPoseFixerSkeleton.ToArray());
                                    }
                                    break;
                            }

                            HandleUtility.Repaint();
                        }
                    }
                    break;
                case Tool.Rotate:
                    {
                        if (ms_CurPivotRotation == PivotRotation.Local)
                        {
                            EditorGUI.BeginChangeCheck();
                            Quaternion newRot = Handles.RotationHandle(rot, pos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                switch (ms_BindPoseFixerMode)
                                {
                                    case EBindPoseFixerMode.NORMAL:
                                        {
                                            Undo.RecordObject(selJoint, "Rotate BindPose");
                                            selJoint.rotation = newRot;
                                        }
                                        break;
                                    case EBindPoseFixerMode.SOLE_JOINT:
                                        {
                                            Undo.RecordObjects(ms_BindPoseFixerSkeleton.ToArray(), "Rotate BindPose");
                                            _CopyBindPoseCurrentPose(false);
                                            selJoint.rotation = newRot;
                                            _RevertChildrenJointWorldTransform(selJoint, ms_BindPoseFixerSkeleton.ToArray());
                                        }
                                        break;
                                }
                                HandleUtility.Repaint();
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            Quaternion newRot = Handles.RotationHandle(ms_RotTmp, pos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                switch (ms_BindPoseFixerMode)
                                {
                                    case EBindPoseFixerMode.NORMAL:
                                        {
                                            Undo.RecordObject(selJoint, "Rotate BindPose");
                                            Quaternion delta = newRot * Quaternion.Inverse(ms_RotTmp);
                                            ms_RotTmp = newRot;
                                            selJoint.rotation = delta * selJoint.rotation;
                                        }
                                        break;
                                    case EBindPoseFixerMode.SOLE_JOINT:
                                        {
                                            Undo.RecordObjects(ms_BindPoseFixerSkeleton.ToArray(), "Rotate BindPose");
                                            _CopyBindPoseCurrentPose(false);

                                            Quaternion delta = newRot * Quaternion.Inverse(ms_RotTmp);
                                            ms_RotTmp = newRot;
                                            selJoint.rotation = delta * selJoint.rotation;
                                            _RevertChildrenJointWorldTransform(selJoint, ms_BindPoseFixerSkeleton.ToArray());
                                        }
                                        break;
                                }
                                HandleUtility.Repaint();
                            }
                        }
                    }
                    break;
                default:
                    Dbg.LogErr("SMREditor._ManipulateFKBones: unexpected Tool enum: {0}", ms_CurHandleTool);
                    break;
            }

        }

        /// <summary>
        /// flip joints on IK link, except the end-effector
        /// used to bend back the elbow / knee
        /// </summary>
        private static void _FlipIKJoints()
        {
            if (ms_IKSolver == null)
                return;

            Transform[] joints = ms_IKSolver.GetJoints();

            Vector3 prev = (joints[joints.Length - 1].position - joints[0].position).normalized;

            if (prev == Vector3.zero)
            {
                Dbg.LogWarn("SMREditor._FlipIKJoints: the end-effector overlapped the IKRoot, interrupt flipping...");
                return;
            }

            Undo.RecordObjects(joints, "Flip Joints");

            for (int idx = 0; idx < joints.Length - 1; ++idx)
            {
                Transform curJoint = joints[idx];
                Vector3 boneDir = joints[idx + 1].position - joints[idx].position;
                Quaternion delta = Quaternion.FromToRotation(boneDir, prev);

                Quaternion newRot = delta * delta * curJoint.rotation;
                curJoint.rotation = newRot;

                prev = joints[idx + 1].position - joints[idx].position;
            }
        }

        /// <summary>
        /// given the axis of IKRoot - IKEndEffector, rotate the IK link around it
        /// </summary>
        private static void _RotateIKJoints(float angle = 180f)
        {
            if (ms_IKSolver == null)
                return;

            Transform[] joints = ms_IKSolver.GetJoints();
            Transform rootJoint = joints[0];

            Vector3 theAxis = (joints[joints.Length - 1].position - rootJoint.position).normalized;

            if (theAxis == Vector3.zero)
            {
                Dbg.LogWarn("SMREditor._RotateIKJoints: the end-effector overlapped the IKRoot, interrupt Rotating...");
                return;
            }

            rootJoint.Rotate(theAxis, angle, Space.World);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void _SetDraggingHandle(bool bState)
        {
            if (bState)
            {
                if (ms_DraggingHandle == EHandleState.JustStopDragging || ms_DraggingHandle == EHandleState.NoDragging)
                {
                    ms_DraggingHandle = EHandleState.JustStartDragging;
                }
                else
                {
                    ms_DraggingHandle = EHandleState.Dragging;
                }
            }
            else
            {
                if (ms_DraggingHandle == EHandleState.JustStartDragging || ms_DraggingHandle == EHandleState.Dragging)
                {
                    ms_DraggingHandle = EHandleState.JustStopDragging;
                }
                else
                {
                    ms_DraggingHandle = EHandleState.NoDragging;
                }
            }
        }

        #endregion "Markers & Handles"

        #region "Undo Record"

        private static void _UndoRecordSelectedJoints(string undoTag)
        {
            Transform[] selectedJoints = ms_SelCtrl.JointsArray;
            for (int i = 0; i < selectedJoints.Length; ++i)
            {
                Transform j = selectedJoints[i];
                if (!ms_SelCtrl.IsChildOfSelectedJoint(j))
                    Undo.RecordObject(j, undoTag);
            }
        }

        private static void _ForceUndoRecordSelectedJoints_Pos()
        {
            Transform[] selectedJoints = ms_SelCtrl.JointsArray;
            for (int i = 0; i < selectedJoints.Length; ++i)
            {
                Transform j = selectedJoints[i];
                if (!ms_SelCtrl.IsChildOfSelectedJoint(j))
                {
                    _SetPositionKey(j);
                }
            }
        }

        private static void _ForceUndoRecordJoints_Rotate(IList<Transform> joints)
        {
            for (int i = 0; i < joints.Count; ++i)
            {
                Transform j = joints[i];
                _SetRotationKey(j);
            }
        }

        private static void _ForceUndoRecordSelectedJoints_Rotate()
        {
            Transform[] selectedJoints = ms_SelCtrl.JointsArray;
            for (int i = 0; i < selectedJoints.Length; ++i)
            {
                Transform j = selectedJoints[i];
                if (!ms_SelCtrl.IsChildOfSelectedJoint(j))
                {
                    _SetRotationKey(j);
                }
            }
        }

        private static void _ForceUndoRecordSelectedJoints_Scale()
        {
            Transform[] selectedJoints = ms_SelCtrl.JointsArray;
            for (int i = 0; i < selectedJoints.Length; ++i)
            {
                Transform j = selectedJoints[i];
                if (!ms_SelCtrl.IsChildOfSelectedJoint(j))
                {
                    _SetScaleKey(j);
                }
            }
        }

        #endregion "Undo Record"

        #region "mouse state"
        // "mouse state" 

        private static bool _IsLMBJustUp()
        {
            return ms_bLMBJustUp;
        }

        private static bool _IsLMBDown()
        {
            return ms_timeLMBDown > 0;
        }

        private static bool _IsLMBUp()
        {
            return ms_timeLMBDown < 0;
        }

        #endregion "mouse state"

        #region "BindPose Fixer"
        // "BindPose Fixer" 

        private static void _ClearAllBonesUndo()
        {
            foreach (Transform tr in ms_ExtendedJoints)
            {
                Undo.ClearUndo(tr);
            }
        }

        /// <summary>
        /// copy the skeleton, put under the ms_ExtraGO
        /// </summary>
        private static void _MakeCopyOfSkeleton()
        {
            ms_BindPoseFixerSkeleton = new List<Transform>();
            Transform rootParent = _MakeSkeletonOuter(ms_ExtraGO.transform, ms_ExtendedJoints[0]);
            _DoMakeCopyOfSkeleton(rootParent, ms_ExtendedJoints[0]);
        }

        /// <summary>
        /// create transforms from `rootBone's parent' to the topmost,
        /// attach to the `attachee'
        /// return the downmost created transform
        /// </summary>
        private static Transform _MakeSkeletonOuter(Transform attachee, Transform rootBone)
        {
            Transform cur = rootBone.parent;
            LinkedList<Transform> oriList = new LinkedList<Transform>();

            while (cur != null)
            {
                oriList.AddFirst(cur);
                cur = cur.parent;
            }

            cur = attachee;
            foreach (Transform tr in oriList)
            {
                GameObject newGO = new GameObject(tr.name);
                Transform newTr = newGO.transform;
                newTr.parent = cur;
                newTr.CopyLocal(tr);
                cur = newTr;

                if (ms_BindPoseTr == null)
                {
                    ms_BindPoseTr = newTr;
                }
            }

            return cur;
        }

        private static void _DoMakeCopyOfSkeleton(Transform parent, Transform cur)
        {
            GameObject newGO = new GameObject(cur.name);
            Transform tr = newGO.transform;

            tr.parent = parent;
            tr.CopyLocal(cur);
            ms_BindPoseFixerSkeleton.Add(tr);

            foreach (Transform childTr in cur)
            {
                if (ms_ExtendedJoints.Contains(childTr))
                {
                    _DoMakeCopyOfSkeleton(tr, childTr);
                }
            }
        }

        private static void _DeleteCopyOfSkeleton()
        {
            GameObject.DestroyImmediate(ms_BindPoseTr.gameObject);
            ms_BindPoseTr = null;
            ms_BindPoseFixerSkeleton = null;
        }

        /// <summary>
        /// apply the change of BindPose, and prompt to export as DAE
        /// </summary>
        private static void _ApplyBindPose()
        {
            Matrix4x4 rootParentL2W = Matrix4x4.identity; //the rootBone's parent's L2W matrix
            if (ms_BindPoseFixerSkeleton[0].parent != null)
            {
                rootParentL2W = ms_BindPoseFixerSkeleton[0].parent.localToWorldMatrix;
                //Dbg.Log("rootParent : {0}", ms_BindPoseFixerSkeleton[0].parent.name);
            }

            // get the transform where we start to look for all SMRs
            Transform allRenderRoot = _GetAllRendererRoot();
            SkinnedMeshRenderer[] allSMR = allRenderRoot.GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int idx = 0; idx < ms_BindPoseFixerSkeleton.Count; ++idx)
            {
                Transform copyBone = ms_BindPoseFixerSkeleton[idx];
                string copyBonePath = AnimationUtility.CalculateTransformPath(copyBone, ms_BindPoseFixerSkeleton[0].parent);

                // find corresponding bone
                Transform oriBone = null;
                foreach (Transform tr in ms_ExtendedJoints)
                {
                    string oriBonePath = AnimationUtility.CalculateTransformPath(tr, ms_ExtendedJoints[0].parent);
                    if (oriBonePath == copyBonePath)
                    {
                        oriBone = tr;
                        break;
                    }
                }

                if (oriBone == null)
                    continue;

                // calc bindPose matrix
                //Dbg.Log("Test mat:{3} \n{0}\noriBone.l2w:\n{1}\ncopyBone.l2w:\n{2}", oriBone.worldToLocalMatrix * rootParentL2W, oriBone.localToWorldMatrix, copyBone.localToWorldMatrix, oriBone.name); 
                Matrix4x4 invbindMat = copyBone.worldToLocalMatrix * rootParentL2W;

                // loop all SMRs, if its bindpose has this oriBone, change its bindPose
                foreach (SkinnedMeshRenderer smr in allSMR)
                {
                    int boneIdx = ArrayUtility.IndexOf(smr.bones, oriBone);
                    if (boneIdx >= 0)
                    {
                        Matrix4x4[] mats = smr.sharedMesh.bindposes;
                        //Dbg.Log("Found in SMR: {0}\n oldInvBind:\n{1}\n newInvBind:\n{2}", smr.name, mats[boneIdx], invbindMat);
                        mats[boneIdx] = invbindMat;
                        smr.sharedMesh.bindposes = mats;
                    }
                }
            }

            // apply the transform of BindPose skeleton to original skeleton and prefab
            _CopyBindPoseCurrentPose(); //copy bindpose skeleton's pose into buffer
            _PastePoseBufferToPrefab(); //paset buffer onto prefabs
            ms_crPaint.Start(_Job_DelayPasteBackPose()); //it seems that write to prefab will overwrite the instance?


        }

        private static Transform _GetAllRendererRoot()
        {
            Transform animRoot = _GetAnimationRoot();
            if (animRoot == null)
            {
                animRoot = ms_ExtendedJoints[0];
                if (animRoot.parent != null)
                    animRoot = animRoot.parent;
            }
            return animRoot;
        }

        /// <summary>
        /// revert any joint's worldTr that's child of `cur'
        /// </summary>
        private static void _RevertChildrenJointWorldTransform(Transform cur, Transform[] joints)
        {
            for (int idx = 0; idx < cur.childCount; ++idx)
            {
                Transform tr = cur.GetChild(idx);
                _DoRevertChildJointToCachedTransform(tr, joints);
            }
        }

        private static void _DoRevertChildJointToCachedTransform(Transform cur, Transform[] joints)
        {
            int idx = ArrayUtility.IndexOf(joints, cur);
            if (idx >= 0)
            {
                XformData cacheData = ms_ExtTrDataCopy[idx];
                cacheData.ApplyW(cur);
            }

            for (int i = 0; i < cur.childCount; ++i)
            {
                Transform tr = cur.GetChild(i);
                _DoRevertChildJointToCachedTransform(tr, joints);
            }
        }

        private static Job _Job_DelayPasteBackPose()
        {
            yield return 0;
            _PasteBackPose();
            // call up the DAE exporter
            _ExportDAE();
        }

        #endregion "BindPose Fixer"

        #region "Undo/Redo callback"
        // "Undo/Redo" 

        private static void _RegisterUndoRedo(bool bReg)
        {
            if (bReg)
                Undo.undoRedoPerformed += _OnUndoRedo;
            else
                Undo.undoRedoPerformed -= _OnUndoRedo;
        }

        private static void _OnUndoRedo()
        {
            _RenewTmpPosRot();
        }

        #endregion "Undo/Redo callback"

        private static void _RenewTmpPosRot()
        {
            ms_PosTmp = ms_SelCtrl.AvgPos();
            ms_RotTmp = ms_SelCtrl.AvgRot();
        }

        /// <summary>
        /// export current model
        /// </summary>
        private static void _ExportDAE()
        {
            string path = EditorUtility.SaveFilePanelInProject("Export Model", "BindPoseFixed", "DAE",
                "if you don't export the model, the result will be overwritten when you reimport the original model");

            if (path.Length > 0)
            {
                Transform rendererRoot = _GetAllRendererRoot();
                SkinnedMeshRenderer[] smrs = rendererRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                MeshFilter[] mfs = rendererRoot.GetComponentsInChildren<MeshFilter>();

                DaeExporter exporter = new DaeExporter(smrs, mfs, ms_ExtendedJoints[0]);
                exporter.Export((AnimationClip)null, path);

                AssetDatabase.Refresh();
            }
        }

        private static void _SceneRepaintAll()
        {
            //Dbg.Log("REpaint!!!");
            SceneView.RepaintAll();
        }

        /// <summary>
        /// lock end effect to joints 0-1-2 plane
        /// </summary>
        private static Vector3 _LockEndEffectorToPlane(Vector3 newPos)
        {
            Vector3 n = _CalcIKPlaneNormal();

            Transform[] joints = ms_IKSolver.GetJoints();
            float len = Vector3.Dot((newPos - joints[0].position), n);
            Vector3 rpos = newPos - n * len;

            return rpos;
        }

        /// <summary>
        /// calc plane normal of joints 0-1-2
        /// </summary>
        private static Vector3 _CalcIKPlaneNormal()
        {
            Transform[] joints = ms_IKSolver.GetJoints();

            if (joints.Length >= 3)
            {
                Vector3 v1 = (joints[1].position - joints[0].position).normalized;
                Vector3 v2 = (joints[2].position - joints[0].position).normalized;

                Vector3 n = Vector3.Cross(v1, v2);
                if (n == Vector3.zero)
                {
                    n = ms_CachedIKPlaneNormal;
                    if (n == Vector3.zero)
                    {
                        n = joints[0].forward;
                    }
                }
                ms_CachedIKPlaneNormal = n = n.normalized;

                return n;
            }
            else
            { //if there is only one bone, use cached normal or the bone's forward as normal
                Vector3 n = ms_CachedIKPlaneNormal;
                if (n == Vector3.zero)
                {
                    ms_CachedIKPlaneNormal = n = joints[0].forward;
                }
                return n;
            }


        }
        /// <summary>
        /// try using the given bone as IK endJoint,
        /// return true iff ok
        /// </summary>
        private static bool _ChangeBone4IKLnk(Transform newJoint)
        {
            if (newJoint == null)
            {
                ms_IKSolver = null;
            }
            else
            {
                if (!_TryMakeIKLink(newJoint, 2)) //default create a 2-bone link
                {
                    if (!_TryMakeIKLink(newJoint, 1)) //if failed, try a 1-bone link
                    {
                        ms_IKSolver = null;
                        newJoint = null; //this bone is not qualified...
                    }
                }

                if (newJoint != null)
                {
                    _ResetIKTargetToEndJointIfNeeded();

                }
            }

            return newJoint != null;
        }

        private static void _KeepRepainting(int cnt = 3)
        {
            ms_RepaintCnter = cnt;
        }

        private static void _SwitchSelectedWireframe()
        {
            EUtil.SetEnableWireframe(ms_SMR, !ms_bHideWireframe);
            //EditorUtility.SetSelectedWireframeHidden(ms_SMR, ms_bHideWireframe);
        }
        private static void _SwitchSelectedWireframe(bool val)
        {
            EUtil.SetEnableWireframe(ms_SMR, !val);
            //EditorUtility.SetSelectedWireframeHidden(ms_SMR, val);
        }

        private static void _SetVertMaterial()
        {
            if (ms_eShowVertices == EShowVert.TRANSPARENT)
            {
                ms_PS.GetComponent<Renderer>().sharedMaterial = ms_PassThroughMat;
            }
            else if (ms_eShowVertices == EShowVert.NORMAL)
            {
                ms_PS.GetComponent<Renderer>().sharedMaterial = ms_NormalVertMat;
            }
            else
            {
                ms_PS.GetComponent<Renderer>().sharedMaterial = ms_BackupMat;
            }
        }

        private static Color _EShowVertToColor(EShowVert eVal)
        {
            switch (eVal)
            {
                case EShowVert.HIDE:
                    return Color.red;
                case EShowVert.NORMAL:
                    return Color.yellow;
                case EShowVert.TRANSPARENT:
                    return Color.green;
                default:
                    Dbg.LogErr("SMREditor._EShowVertToColor: ain't be here");
                    break;
            }
            return Color.black;
        }

        private static string _EShowVertToString(EShowVert eVal)
        {
            switch (eVal)
            {
                case EShowVert.HIDE:
                    return "Hide Vertex Marker";
                case EShowVert.NORMAL:
                    return "Show Vertex Marker";
                case EShowVert.TRANSPARENT:
                    return "Show Transparent Vertex Marker";
                default:
                    Dbg.LogErr("SMREditor._EShowVertToString: ain't be here");
                    break;
            }
            return "";
        }

        private static Vector3 _GetSceneCamPos()
        {
            SceneView v = SceneView.lastActiveSceneView;
            Dbg.Assert(v != null, "SMREditor._GetSceneCamPos: Failed to get SceneView");
            Camera cam = v.camera;
            Dbg.Assert(cam != null, "SMREditor._GetSceneCamPos: failed to get cam");
            return cam.transform.position;
        }

        /// <summary>
        /// find out which bone is clicked
        /// </summary>
        private Transform _FindSelectedJoint(Vector2 mousePos, Transform[] bones)
        {
            float minDist = float.MaxValue;
            Transform nearestTr = null;

            foreach (Transform oneBone in bones)
            {
                Vector2 bonePos2D = HandleUtility.WorldToGUIPoint(oneBone.position);
                float dist = Vector2.Distance(mousePos, bonePos2D);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestTr = oneBone;
                }
            }

            if (nearestTr != null && minDist < HANDLE_CLICK_DIST_THRES)
            {
                //Dbg.Log("Selected Bone: {0}", nearestTr);
                return nearestTr;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// set ms_SelectedBone_Req by click event
        /// </summary>
        private void _SetSelectedJointByEvent(Event e, Transform[] bones, bool multiSelectMode = false)
        {
            //A.1.1 if mouse leftBtn clicked, find out the nearest bone
            double LMBTotalDownTime = ms_timeLMBDown >= 0 ? EditorApplication.timeSinceStartup - ms_timeLMBDown : ms_timeLMBDown;
            if (e.type == EventType.MouseUp && !ms_bRightBtnIsDown && e.button == 0 && LMBTotalDownTime <= CLICK_THRES)
            {
                Transform newJoint = _FindSelectedJoint(e.mousePosition, bones);

                if (!multiSelectMode) //single select mode
                {
                    if (ms_SelCtrl.SingleJoint != newJoint && newJoint != null)
                    {
                        ms_bNeedRecalcVerts = true;
                        ms_SelCtrl.Select(newJoint);
                    }
                }
                else //multi-select mode
                {
                    if (newJoint != null)
                    {
                        ms_bNeedRecalcVerts = true;
                        if (e.control)
                        {
                            ms_SelCtrl.RecurSelect(newJoint, !ms_SelCtrl.IsSelectedJoint(newJoint));
                        }
                        else
                        {
                            if (e.shift)
                            {
                                ms_SelCtrl.ToggleSelect(newJoint);
                            }
                            else
                            {
                                ms_SelCtrl.Select(newJoint);
                            }
                        }

                    }
                }

                _SceneRepaintAll();
            }
        }

        private void _GetBonesArray(out Transform rootBone, out Transform[] bones)
        {
            if (ms_bShowOptimzedJoint)
            {
                rootBone = ms_ExtendedJoints[0];
                bones = ms_ExtendedJointsCacheArray;
            }
            else
            {
                rootBone = ms_SMR.rootBone;
                bones = ms_SMR.bones;
            }
        }

        private static bool _IsBonesTransformChangedAndResetChangedFlag()
        {
            bool bChanged = false;
            foreach (var bone in ms_ExtendedJoints)
            {
                if (bone.hasChanged)
                {
                    bChanged = true;
                }
                bone.hasChanged = false;
            }
            return bChanged;
        }

        /// <summary>
        /// only take effect if IKSolver is working;
        /// 
        /// reset the IKTarget's pos/rot to the end joint
        /// </summary>
        private static void _ResetIKTargetToEndJointIfNeeded()
        {
            if (ms_IKSolver != null)
            {
                Transform[] joints = ms_IKSolver.GetJoints();
                Transform endJoint = joints[joints.Length - 1];
                ms_IKTargetGO.transform.position = endJoint.position;
                ms_IKTargetGO.transform.rotation = endJoint.rotation;
            }
        }

        /// <summary>
        /// given the endJoint and expected IK Link length,
        /// check if there is a qualified link of bones up from endJoint,
        /// 
        /// pass a ref ISolver instance ( could be null ), it will be changed if it's necessary to change IKSolver type;
        /// 
        /// if there is, call ms_IKSolver.SetBones
        /// </summary>
        private static bool _TryMakeIKLink(Transform endJoint, int lnkLength)
        {
            return _TryMakeIKLink(ref ms_IKSolver, endJoint, lnkLength);
        }
        private static bool _TryMakeIKLink(ref ISolver isolver, Transform endJoint, int lnkLength)
        {
            if (lnkLength <= 0 || endJoint == null)
                return false;

            // first check if the link is valid
            //Transform[] bones = ms_ExtendedJoints.ToArray();
            Transform joint = endJoint.parent;
            for (int idx = 0; idx < lnkLength; ++idx)
            {
                if (joint == null || !ms_ExtendedJoints.Contains(joint))
                {
                    return false;
                }

                joint = joint.parent;
            }

            // looks valid, set into the IKSolver
            isolver = _GetSolver(isolver, endJoint, lnkLength);
            isolver.SetBones(endJoint, lnkLength);

            // add parent bones to ms_SelCtrl
            {
                Transform pJoint = endJoint.parent;
                for (int idx = 0; idx < lnkLength; ++idx)
                {
                    ms_SelCtrl.IncSelect(pJoint, false);
                    pJoint = pJoint.parent;
                }
            }

            return true;
        }

        private static void _TogglePivotRotation()
        {
            PivotRotation newPivot = ms_CurPivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
            _OnChangePivotRotation(ms_CurPivotRotation, newPivot);
        }

        private static void _TogglePivotRotation_UI()
        {
            PivotRotation newPivot = (PivotRotation)(((int)ms_CurPivotRotation + 1) % (int)PivotRotation.END);
            _OnChangePivotRotation(ms_CurPivotRotation, newPivot);
        }

        /// <summary>
        /// get the quaternion for given transform,
        /// according to the current pivot rotation
        /// </summary>
        public static Quaternion GetQuaternionByPivotRotation(Transform tr)
        {
            Quaternion q = Quaternion.identity;
            switch (ms_CurPivotRotation)
            {
                case PivotRotation.Local:
                    {
                        q = tr.rotation;
                    }
                    break;
                case PivotRotation.Global:
                    {
                        q = Quaternion.identity;
                    }
                    break;
                case PivotRotation.Parent:
                    {
                        if (tr.parent == null)
                        {
                            q = Quaternion.identity;
                        }
                        else
                        {
                            q = tr.parent.rotation;
                        }
                    }
                    break;
                default:
                    Dbg.LogErr("SMREditor.GetQuaternionByPivotRotation: unexpected pivot rotation setting: {0}", ms_CurPivotRotation);
                    break;
            }

            return q;
        }

        #region "Reset"
        // "Reset" 

        private static void _ResetAllBonesToPrefabPose()
        {
            Undo.RecordObjects(ms_ExtendedJoints.ToArray(), "Reset_Bones_To_BindPose");
            for (int idx = 0; idx < ms_ExtendedJoints.Count; ++idx)
            {
                Transform tr = ms_ExtendedJoints[idx];

                Transform prefab = PrefabUtility.GetPrefabParent(tr) as Transform;
                if (prefab != null)
                {
                    tr.CopyLocal(prefab);
                    HotFix.FixRotation(tr);
                }
            }
        }

        private static void _ResetAllBonesToBindPose()
        {
            Transform cur = ms_ExtendedJoints[0];

            Matrix4x4[] invbinds = ms_SMR.sharedMesh.bindposes;
            Transform[] bones = ms_SMR.bones;
            _Recur_ResetAllBonesToBindPose(cur, invbinds, bones);
        }

        private static void _Recur_ResetAllBonesToBindPose(Transform cur, Matrix4x4[] invbinds, Transform[] bones)
        {
            int boneIdx = ArrayUtility.IndexOf(bones, cur);
            if (boneIdx >= 0)
            {
                Transform rootParent = ms_ExtendedJoints[0].parent != null ?
                    ms_ExtendedJoints[0].parent : ms_ExtendedJoints[0];

                // extract bindpose's pos & rot from invBindPose
                Matrix4x4 inv = invbinds[boneIdx];
                Matrix4x4 m = inv.inverse; //bone -> skin
                m = rootParent.localToWorldMatrix * m; //skin -> world

                Vector3 pos = m.MultiplyPoint(Vector3.zero);
                Vector3 vz = m.GetColumn(2);
                Vector3 vy = m.GetColumn(1);
                Quaternion q = Quaternion.LookRotation(vz, vy);

                // assign to joint
                cur.position = pos;
                cur.rotation = q;

                // use prefab's local scale
                Transform prefab = PrefabUtility.GetPrefabParent(cur) as Transform;
                if (prefab != null)
                {
                    cur.localScale = prefab.localScale;
                }

                //Dbg.Log("{0}:\nlocalPos:{1}\nlocalRot:{2}\nlocalScale:{3}\n", cur.name, cur.localPosition, cur.localRotation.eulerAngles, cur.localScale);
            }

            // recursive
            for (int idx = 0; idx < cur.childCount; ++idx)
            {
                var joint = cur.GetChild(idx);
                _Recur_ResetAllBonesToBindPose(joint, invbinds, bones);
            }
        }

        #endregion "Reset"



        private void _InitExtendedJoints()
        {
            Transform rootJoint = ms_SMR.rootBone;
            Transform[] oriBones = ms_SMR.bones;
            ms_ExtendedJoints = new List<Transform>();
            ms_ExtTrDataCopy = new List<XformData>();

            //0. check the rootbone
            if (rootJoint == null)
            {
                Dbg.LogWarn("SMREditor._InitExtendedJoints: No RootBone specified");
                Highlighter.Highlight("Inspector", "Root Bone");
                EUtil.ShowNotification("Please_Specify_Valid_RootBone_In_SkinnedMeshRenderer", 5.0f);
                ms_EndEdit_Req = true;
            }
            else
            {
                //1. first find the public root joint
                while (rootJoint != null)
                {
                    bool bOK = true;
                    for (int idx = 0; idx < oriBones.Length; ++idx)
                    {
                        if (!oriBones[idx].IsChildOf(rootJoint))
                        {
                            bOK = false;
                            break;
                        }
                    }

                    if (bOK)
                    {
                        break;
                    }
                    else
                    {
                        rootJoint = rootJoint.parent;
                    }
                } //end while

                if (rootJoint == null)
                {
                    Dbg.LogWarn("SMREditor._InitExtendedJoints: The rootBone is not ancestors for all bones");
                    Highlighter.Highlight("Inspector", "Root Bone");
                    EUtil.ShowNotification("Please_Specify_Valid_RootBone_In_SkinnedMeshRenderer", 5.0f);

                    rootJoint = ms_SMR.rootBone;
                    StringBuilder bld = new StringBuilder();
                    bld.AppendLine("The bones below is not descendants of specified rootbone:");
                    for (int idx = 0; idx < oriBones.Length; ++idx)
                    {
                        if (!oriBones[idx].IsChildOf(rootJoint))
                        {
                            bld.AppendLine(oriBones[idx].name);
                        }
                    }
                    Dbg.LogWarn(bld.ToString());

                    ms_EndEdit_Req = true;
                }
                else
                {
                    ms_SMR.rootBone = rootJoint;

                    // 2. include all children transform under rootJoint as bones
                    _IncludeExtendedJoints(rootJoint, oriBones); //recursively include joints into ms_extendedJoints
                    ms_ExtendedJointsCacheArray = ms_ExtendedJoints.ToArray();

                    for (int i = 0; i < ms_ExtendedJoints.Count; ++i)
                    {
                        ms_ExtTrDataCopy.Add(new XformData());
                    } //end for
                } //end else
            }//end else
        }

        ///// <summary>
        ///// for each bone in SMR.bones array, go up until reach the new `rootJoint', 
        ///// include every joint on the path into the ms_ExtendedJoints
        ///// </summary>
        //private void _IncludeExtendedJoints(Transform rootJoint, Transform[] oriBones)
        //{
        //    ms_ExtendedJoints.Add(rootJoint);

        //    for(int idx = 0; idx < oriBones.Length; ++idx) 
        //    {
        //        Transform j = oriBones[idx];

        //        while( !ms_ExtendedJoints.Contains(j) )
        //        {
        //            ms_ExtendedJoints.Add(j);
        //            j = j.parent;
        //        }
        //    }
        //}

        /// <summary>
        /// go down from rootJoint, 
        /// 
        /// foreach bone:
        /// 0. if is smr.rootBone then include
        /// 1. if has tag 'I_Am_Bone' then include
        /// 2. else if has tag 'I_Aint_Bone' then dont include
        /// 3. else if has Animation/Animator/SMR then dont include
        /// 4. else include
        /// </summary>
        private void _IncludeExtendedJoints(Transform joint, Transform[] oriBones)
        {
            // whether include
            bool bInclude = false;
            if (joint == ms_SMR.rootBone)
                bInclude = true;
            else if (joint.tag == TAG_I_AM_BONE)
                bInclude = true;
            else if (joint.tag == TAG_I_AINT_BONE)
                bInclude = false;
            else if (joint.GetComponent<Animation>() != null
                || joint.GetComponent<Animator>() != null || joint.GetComponent<SkinnedMeshRenderer>() != null)
                bInclude = false;
            else
                bInclude = true;

            //
            if (bInclude)
            {
                //Dbg.Log("Include: {0}", joint.name);
                ms_ExtendedJoints.Add(joint);
            }

            //recursive
            for (int idx = 0; idx < joint.childCount; ++idx)
            {
                Transform childJoint = joint.GetChild(idx);

                _IncludeExtendedJoints(childJoint, oriBones);
            }
        }

        // undocumented! HACK trick to check AnimationWnd open
        private void _CheckAnimWndOpen()
        {
            ms_bAnimWndOpen = EUtil.IsUnityAnimationWindowOpen();
        }

        /// <summary>
        /// return the IK Solver fit the condition;
        /// 
        /// if the pass-in `inSolver' is fit for the condition, then just return it,
        /// else have to create a new instance and return
        /// </summary>
        private static ISolver _GetSolver(ISolver inSolver, Transform endJoint, int lnkLen)
        {
            if (inSolver != null)
            {
                Dbg.Log("Existing Solver: {0}", inSolver.Type);
                return inSolver;
            }


            if (ms_bUseCCDIK)
            {
                var ikmb = endJoint.GetComponent<BaseSolverMB>() as CCDSolverMB;
                if (ikmb != null)
                {
                    Dbg.Log("Establish CCDIK on endJoint: {0}", endJoint.name);
                    var solver = ikmb.GetSolver();
                    solver.RefreshConstraint();
                    return solver;
                }
                else
                {
                    Dbg.Log("Establish CCDIK on endJoint: {0}", endJoint.name);
                    return new CCDSolver();
                }
            }
            else
            {
                Dbg.Log("Establish BaseIK on endJoint: {0}", endJoint.name);
                return new BaseIKSolver();
            }
            
        }

        /// <summary>
        /// used to clear lmb time
        /// </summary>
        private static Job _Job_ClearLMBDownTime()
        {
            yield return 0;

            ms_timeLMBDown = -1;

            //Dbg.Log("clear");
        }

        #region "current selected joint"

        /// <summary>
        /// get current selected joints in a list
        /// DON'T Change the list!!!
        /// </summary>
        private static JointList _GetCurrentSelectedJoints()
        {
            //JointList trLst = null;
            //if (ms_MultiSelectJoints.Count > 0)
            //{
            //    trLst = ms_MultiSelectJoints;
            //}
            //else
            //{
            //    trLst = new JointList();
            //    trLst.Add(ms_SelectedBone);
            //}
            //return trLst;

            return ms_SelCtrl.Joints;
        }

        private static void _ChangeCurrentBoneByShortcut(Event e)
        {
            KeyCode kc = e.keyCode;

            switch (kc)
            {
                case KeyCode.LeftBracket:
                    {
                        ms_SelCtrl.SelectParentBone();
                    }
                    break;
                case KeyCode.RightBracket:
                    {
                        ms_SelCtrl.SelectChildBone();
                    }
                    break;
                case KeyCode.Backslash:
                    {
                        ms_SelCtrl.SelectNextSibling();
                    }
                    break;
            }
        }

        #endregion "current selected joint"



        #region "Copy/Paste PoseBuffer"

        private static void _CopyCurrentPose(bool local = true)
        {
            for (int idx = 0; idx < ms_ExtendedJoints.Count; ++idx)
            {
                Transform tr = ms_ExtendedJoints[idx];
                //Undo.RecordObject(tr, "SnapShot of bones");

                if (local)
                {
                    ms_ExtTrDataCopy[idx].CopyFrom(tr);
                }
                else
                {
                    ms_ExtTrDataCopy[idx].CopyFromW(tr);
                }
            }
        }

        private static void _CopyBindPoseCurrentPose(bool local = true)
        {
            for (int idx = 0; idx < ms_BindPoseFixerSkeleton.Count; ++idx)
            {
                Transform tr = ms_BindPoseFixerSkeleton[idx];
                //Undo.RecordObject(tr, "SnapShot of bones");

                if (local)
                {
                    ms_ExtTrDataCopy[idx].CopyFrom(tr);
                }
                else
                {
                    ms_ExtTrDataCopy[idx].CopyFromW(tr);
                }
            }
        }

        private static void _PasteBackPose(bool local = true)
        {
            if (local)
            {
                for (int idx = 0; idx < ms_ExtendedJoints.Count; ++idx)
                {
                    Transform tr = ms_ExtendedJoints[idx];
                    ms_ExtTrDataCopy[idx].Apply(tr);
                }
            }
            else
            {
                _DoPasteBackPoseW(ms_ExtendedJoints[0]); //paste world-space transform, need top->down
            }

            _SceneRepaintAll();
        }

        private static void _DoPasteBackPoseW(Transform cur)
        {
            int idx = ms_ExtendedJoints.IndexOf(cur);
            if (idx >= 0)
            {
                ms_ExtTrDataCopy[idx].ApplyW(cur);

                foreach (Transform childTr in cur)
                {
                    _DoPasteBackPoseW(childTr);
                }
            }
        }

        private static void _ClearPoseBuffer()
        {
            for (int idx = 0; idx < ms_ExtTrDataCopy.Count; ++idx)
            {
                ms_ExtTrDataCopy[idx].Clear();
            }
        }

        private static void _PastePoseBufferToPrefab()
        {
            for (int idx = 0; idx < ms_ExtendedJoints.Count; ++idx)
            {
                Transform tr = ms_ExtendedJoints[idx];

                Transform prefab = PrefabUtility.GetPrefabParent(tr) as Transform;
                if (prefab == null)
                {
                    Dbg.LogWarn("SMREditor._PastePoseBufferToPrefab: cannot find prefab for: {0}", tr.name);
                }
                else
                {
                    ms_ExtTrDataCopy[idx].Apply(prefab);
                }
            }
        }

        #endregion "Copy/Paste PoseBuffer"

        #region "EditorPrefs"
        // "EditorPrefs" 

        /// <summary>
        /// use EditorPref and SMR's bounds to init some editor parameters
        /// </summary>
        private void _SetParametersByBoundsAndPrefs()
        {
            Bounds bd = ms_SMR.bounds;
            float baseVal = Mathf.Max(bd.size.x, bd.size.y, bd.size.z) * 0.005f;

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_JOINTSIZE))
            {
                ms_boneSize = EditorPrefs.GetFloat(EDITOR_PREF_KEY_JOINTSIZE);
            }
            else
            {
                ms_boneSize = baseVal;
            }

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_LINEWIDTH))
            {
                ms_boneLineWidth = EditorPrefs.GetFloat(EDITOR_PREF_KEY_LINEWIDTH);
            }
            else
            {
                ; //use default value
            }

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_VERTSIZE))
            {
                ms_vertSize = EditorPrefs.GetFloat(EDITOR_PREF_KEY_VERTSIZE);
            }
            else
            {
                ms_vertSize = baseVal;
            }

            if (EditorPrefs.HasKey(EDITOR_PREF_KEY_SHOWXFORM))
            {
                ms_bShowTransformInspector = EditorPrefs.GetBool(EDITOR_PREF_KEY_SHOWXFORM);
            }
            else
            {
                ms_bShowTransformInspector = true;
            }

        }

        /// <summary>
        /// save the editor parameters to EditorPrefs
        /// </summary>
        private static void _SaveParametersToPref()
        {
            EditorPrefs.SetFloat(EDITOR_PREF_KEY_JOINTSIZE, ms_boneSize);
            EditorPrefs.SetFloat(EDITOR_PREF_KEY_LINEWIDTH, ms_boneLineWidth);
            EditorPrefs.SetFloat(EDITOR_PREF_KEY_VERTSIZE, ms_vertSize);
            EditorPrefs.SetBool(EDITOR_PREF_KEY_SHOWXFORM, ms_bShowTransformInspector);
        }

        /// <summary>
        /// retrieve poseMgr's save/load path from EditorPref
        /// </summary>
        private static string _GetPoseMgrSaveLoadPath()
        {
            string path = EditorPrefs.GetString(EDITOR_PREF_KEY_POSEMGR_SLPATH, "");
            return path;
        }

        /// <summary>
        /// given a path "A/B/C.pose", save "A/B" to EditorPrefs
        /// </summary>
        private static void _SavePoseMgrSaveLoadPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                string dir = System.IO.Path.GetDirectoryName(path);
                EditorPrefs.SetString(EDITOR_PREF_KEY_POSEMGR_SLPATH, dir);
            }
        }
        #endregion "EditorPrefs"

        #region "Animation helpers"
        // "Animation helpers" 

        /// <summary>
        /// [HACK TRICK]
        /// only work for Generic rig,
        /// 
        /// copy the curve on RootNode, make 'Animator.Motion T' and 'Animator.Motion Q'
        /// </summary>
        private static void _MakeRootMotion()
        {
            Transform animRoot = _GetAnimationRoot(); //the animator GO
            Animator animator = animRoot.GetComponent<Animator>();
            if (animator == null || animator.isHuman)
            {
                EUtil.ShowNotification("MakeRootMotion works only for Generic rig");
                return;
            }

            //if( ! animator.hasRootMotion )
            //{
            //    EUtil.ShowNotification("There's no root motion for this model");
            //    return;
            //}

            EUtil.StartObjRefModalWindow(_MakeRootMotion_Callback, null, typeof(Transform),
                "Select the Root Node", ms_Background);
        }

        /// <summary>
        /// [HACK TRICK]
        /// when user specified the rootNode of the clip
        /// this callback will try to make rootMotion
        /// </summary>
        private static void _MakeRootMotion_Callback(UnityEngine.Object tr)
        {
            Transform animRoot = _GetAnimationRoot(); //the animator GO
            Transform rootNode = tr as Transform;
            if (rootNode == null)
            {
                EUtil.ShowNotification("Selected RootNode is null");
                return;
            }

            Transform prefabAnimRoot = PrefabUtility.GetPrefabParent(animRoot) as Transform;
            if (prefabAnimRoot == null)
            {
                EUtil.ShowNotification(string.Format("AnimRoot \"{0}\" has no prefab", animRoot));
                return;
            }

            Transform prefabRootNode = PrefabUtility.GetPrefabParent(rootNode) as Transform;
            if (prefabRootNode == null)
            {
                EUtil.ShowNotification(string.Format("RootNode \"{0}\" has no prefab", rootNode));
                return;
            }

            EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
            object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
            AnimationClip curClip = RCall.GetField("UnityEditorInternal.AnimationWindowState",
                    "m_ActiveAnimationClip", uawstate) as AnimationClip;
            //string clipPath = AssetDatabase.GetAssetPath(curClip);
            //string backClipPath = PathUtil.StripExtension(clipPath) + "_backup.anim";
            //AssetDatabase.CopyAsset(clipPath, backClipPath); //backup anim clip
            //AssetDatabase.SaveAssets();

            if (curClip == null)
            {
                EUtil.ShowNotification("There is No clip in Animation Window");
                return;
            }

            string targetPath = AnimationUtility.CalculateTransformPath(rootNode, animRoot);

            _MakeRootMotion_CreateNewCurves(curClip, targetPath, prefabAnimRoot, prefabRootNode);

            #region "Old RootMotion, just copy"
            //var curveBindings = AnimationUtility.GetCurveBindings(curClip);

            //// find the rootNode's position curves, copy to 'Animator.Motion T'
            //// find the rootNode's rotation curves, copy to 'Animator.Motion Q'
            //foreach (var oneBinding in curveBindings)
            //{
            //    if( oneBinding.path == targetPath )
            //    {
            //        Dbg.Log("Found targetPath: property: {0}", oneBinding.propertyName);
            //        if( oneBinding.propertyName == "m_LocalPosition.x")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionT.x", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.x");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootPosition.x");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalPosition.y")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionT.y", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.y");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootPosition.y");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalPosition.z")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionT.z", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.z");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootPosition.z");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalRotation.x")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionQ.x", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.x");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootRotation.x");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalRotation.y")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionQ.y", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.y");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootRotation.y");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalRotation.z")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionQ.z", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.z");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootRotation.z");
            //        }
            //        else if (oneBinding.propertyName == "m_LocalRotation.w")
            //        {
            //            AnimationCurve curve = AnimationUtility.GetEditorCurve(curClip, oneBinding);
            //            //curClip.SetCurve("", typeof(Animator), "MotionQ.w", curve);
            //            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.w");
            //            AnimationUtility.SetEditorCurve(curClip, newBinding, curve);
            //            Dbg.Log("Copied rootRotation.w");
            //        }
            //    }
            //}
            #endregion "Old RootMotion"

            curClip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(curClip);
            //RCall.SetProp("UnityEditorInternal.AnimationWindowState", "refresh", uawstate, 2); //set refresh to Everything
            //RCall.CallMtd("UnityEditorInternal.AnimationWindowState", "Refresh", uawstate); //execute Refresh()
            _SceneRepaintAll();
        }

        /// <summary>
        /// bake the RootMotion curves
        /// </summary>
        private static void _MakeRootMotion_CreateNewCurves(AnimationClip curClip, string targetPath,
            Transform prefabAnimRoot, Transform prefabRootNode)
        {
            var bindingTx = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.x");
            var bindingTy = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.y");
            var bindingTz = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionT.z");
            var bindingQx = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.x");
            var bindingQy = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.y");
            var bindingQz = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.z");
            var bindingQw = EditorCurveBinding.FloatCurve("", typeof(Animator), "MotionQ.w");

            var tx = new AnimationCurve();
            var ty = new AnimationCurve();
            var tz = new AnimationCurve();
            var qx = new AnimationCurve();
            var qy = new AnimationCurve();
            var qz = new AnimationCurve();
            var qw = new AnimationCurve();

            var oPx = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.x"));
            var oPy = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.y"));
            var oPz = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.z"));
            var oRx = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalRotation.x"));
            var oRy = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalRotation.y"));
            var oRz = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalRotation.z"));
            var oRw = AnimationUtility.GetEditorCurve(curClip, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalRotation.w"));

            bool bHasPos = (oPx != null && oPy != null && oPz != null);
            bool bHasRot = (oRx != null && oRy != null && oRz != null && oRw != null);

            Transform rnParent = prefabRootNode.parent;
            Dbg.Assert(rnParent != null, "SMREditor._MakeRootMotion_CreateNewCurves: prefabRootNode has no parent!?");

            float SAMPLE_RATE = curClip.frameRate;
            float clipLen = curClip.length;
            float time = 0f;
            float deltaTime = 1f / (SAMPLE_RATE);
            for (;
                time <= clipLen || Mathf.Approximately(time, clipLen);
                )
            {
                //Dbg.Log("nt = {0}", m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

                if (bHasPos)
                {
                    float x = oPx.Evaluate(time);
                    float y = oPy.Evaluate(time);
                    float z = oPz.Evaluate(time);
                    Vector3 pos = new Vector3(x, y, z);
                    pos = rnParent.TransformPoint(pos);
                    pos = prefabAnimRoot.InverseTransformPoint(pos);
                    tx.AddKey(time, pos.x);
                    ty.AddKey(time, pos.y);
                    tz.AddKey(time, pos.z);
                }

                if (bHasRot)
                {
                    float x = oRx.Evaluate(time);
                    float y = oRy.Evaluate(time);
                    float z = oRz.Evaluate(time);
                    float w = oRw.Evaluate(time);
                    Quaternion qua = new Quaternion(x, y, z, w);
                    Vector3 dir = qua * Vector3.forward;
                    dir = rnParent.TransformDirection(dir);
                    dir = prefabAnimRoot.InverseTransformDirection(dir);
                    //Dbg.Log("time:{0}, rnEuler: {1}, rnParentDir: {2}, worldDir:{3}, animRootLocal:{4}", time, 
                    //    qua.eulerAngles,
                    //    rnParent.rotation * Vector3.forward,
                    //    rnParent.TransformDirection(qua * Vector3.forward), 
                    //    dir
                    //    );
                    qua = Quaternion.FromToRotation(Vector3.forward, dir);

                    qx.AddKey(time, qua.x);
                    qy.AddKey(time, qua.y);
                    qz.AddKey(time, qua.z);
                    qw.AddKey(time, qua.w);
                }

                if (!Mathf.Approximately(time + deltaTime, clipLen))
                {
                    time += deltaTime;
                }
                else
                {
                    time = clipLen;
                }
            }

            // set T curves
            if (!bHasPos)
            { //make fake MotionT curves, without T, Q won't work
                tx.AddKey(0, 0); tx.AddKey(clipLen, 0f);
                ty.AddKey(0, 0); ty.AddKey(clipLen, 0f);
                tz.AddKey(0, 0); tz.AddKey(clipLen, 0f);
            }
            AnimationUtility.SetEditorCurve(curClip, bindingTx, tx);
            AnimationUtility.SetEditorCurve(curClip, bindingTy, ty);
            AnimationUtility.SetEditorCurve(curClip, bindingTz, tz);

            // set Q curves
            if (bHasRot)
            {
                AnimationUtility.SetEditorCurve(curClip, bindingQx, qx);
                AnimationUtility.SetEditorCurve(curClip, bindingQy, qy);
                AnimationUtility.SetEditorCurve(curClip, bindingQz, qz);
                AnimationUtility.SetEditorCurve(curClip, bindingQw, qw);
            }

            Dbg.Log("Done Processing RootMotion for clip: {0}", curClip.name);
        }


        /// <summary>
        /// [HACK TRICK]
        /// select curves in UAW by the current selected joints
        /// if ms_MultiSelectJoints not empty, then use ms_MultiSelectJoint, else use ms_CurrentJoint
        /// </summary>
        private static void _SelectCurvesBySelectedJoints()
        {
            JointList trLst = _GetCurrentSelectedJoints();

            Type[] methodParamTypes = new Type[] { RCall.GetTypeFromString("UnityEditorInternal.DopeLine"), typeof(bool) };

            Transform animRoot = _GetAnimationRoot();

            EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
            object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
            IList dopelines = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "dopelines", uawstate);
            for (int idx = 0; idx < dopelines.Count; ++idx)
            {
                object oneDopeline = dopelines[idx]; //AnimationWindowCurve
                Array curveArray = (Array)RCall.GetField("UnityEditorInternal.DopeLine", "m_Curves", oneDopeline);
                string onePath = (string)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "path", curveArray.GetValue(0));

                //Dbg.Log("SMREditor._SelectCurvesBySelectedJoints: idx: {0}, path:{1}", idx, onePath);

                foreach (Transform joint in trLst)
                {
                    string jointTrPath = AnimationUtility.CalculateTransformPath(joint, animRoot);
                    if (onePath == jointTrPath)
                    {
                        RCall.CallMtd1("UnityEditorInternal.AnimationWindowState", "SelectHierarchyItem", methodParamTypes,
                            uawstate, oneDopeline, true);
                        break;
                    }
                }

            }

            uaw.Repaint();
            _SceneRepaintAll();
        }

        /// <summary>
        /// set position key on UAW current time pos for selected joints
        /// </summary>
        private static void _SetPositionKeys()
        {
            Vector3 delta = new Vector3(0.00001f, 0, 0);
            JointList jointLst = _GetCurrentSelectedJoints();

            foreach (Transform joint in jointLst)
            {
                joint.localPosition -= delta;
            }
            Undo.RecordObjects(jointLst.ToArray(), "Set P Keys");
            foreach (Transform joint in jointLst)
            {
                joint.localPosition += delta;
            }
            _SceneRepaintAll();
        }

        private static void _SetPositionKey(Transform joint)
        {
            Vector3 delta = new Vector3(0.000001f, 0, 0);
            joint.localPosition -= delta;
            Undo.RecordObject(joint, "Move bone");
            joint.localPosition += delta;
        }

        /// <summary>
        /// set rotation key on UAW current time pos for selected joints
        /// </summary>
        private static void _SetRotationKeys()
        {
            JointList jointLst = _GetCurrentSelectedJoints();
            Vector3 delta = new Vector3(0.001f, 0, 0);
            List<Quaternion> oldQ = new List<Quaternion>();

            foreach (Transform joint in jointLst)
            {
                oldQ.Add(joint.localRotation);
                joint.Rotate(delta);
            }
            Undo.RecordObjects(jointLst.ToArray(), "Set R Keys");
            for (int idx = 0; idx < jointLst.Count; ++idx)
            {
                Transform joint = jointLst[idx];
                joint.localRotation = oldQ[idx];
                HotFix.FixRotation(joint);
            }
            _SceneRepaintAll();
        }

        private static void _SetRotationKey(Transform joint)
        {
            var oldQ = joint.localRotation;
            joint.Rotate(new Vector3(1, 0, 0));
            Undo.RecordObject(joint, "Rotate bone");
            joint.localRotation = oldQ;
            HotFix.FixRotation(joint);
        }

        /// <summary>
        /// set scale key on UAW current time pos for selected joints
        /// </summary>
        private static void _SetScaleKeys()
        {
            JointList jointLst = _GetCurrentSelectedJoints();
            Vector3 delta = new Vector3(1.001f, 1.001f, 1.001f);
            List<Vector3> oldS = new List<Vector3>();

            foreach (Transform joint in jointLst)
            {
                oldS.Add(joint.localScale);
                Vector3 newV = Vector3.Scale(joint.localScale, delta);
                joint.localScale = newV;
            }
            Undo.RecordObjects(jointLst.ToArray(), "Set S Keys");
            for (int idx = 0; idx < jointLst.Count; ++idx)
            {
                Transform joint = jointLst[idx];
                joint.localScale = oldS[idx];
            }
            _SceneRepaintAll();
        }

        private static void _SetScaleKey(Transform joint)
        {
            Vector3 oldS = joint.localScale;
            joint.localScale = Vector3.Scale(oldS, new Vector3(2f, 2f, 2f));
            Undo.RecordObject(joint, "Scale Bone");
            joint.localScale = oldS;
        }

        /// <summary>
        /// walk from the root upward, until find a GO with Animation/Animator
        /// </summary>
        private static Transform _GetAnimationRoot()
        {
            Transform cur = ms_ExtendedJoints[0];

            while (cur != null)
            {
                if (cur.GetComponent<Animation>() != null || cur.GetComponent<Animator>() != null)
                {
                    return cur;
                }

                cur = cur.parent;
            }

            //Dbg.LogErr("SMREditor._GetAnimationRoot: going upward from {0}, not found any one with Animation/Animator", ms_ExtendedJoints[0].name);
            return null;
        }

        private static void _MakePoseSnapshot()
        {
            _CopyCurrentPose();
            _ResetAllBonesToPrefabPose();
            ms_TrDataCopyTimer = 10;
        }

        private void _CheckMakingBoneSnapshot()
        {
            if (ms_TrDataCopyTimer > 0)
            { //need set back if the .scale is not zero vector

                ms_TrDataCopyTimer--;
                if (ms_TrDataCopyTimer > 0)
                    return;

                //Dbg.Log("_CheckMakingBoneSnapshot");

                for (int idx = 0; idx < ms_ExtTrDataCopy.Count; ++idx)
                {
                    Undo.RecordObject(ms_ExtendedJoints[idx], "Snapshot");
                    ms_ExtTrDataCopy[idx].Apply(ms_ExtendedJoints[idx]);
                    ms_ExtTrDataCopy[idx].Clear();
                }

                EUtil.GetSceneView().Repaint();
            }
        }

        #endregion "Animation helpers"


        #region "IK Pin"
        // "IK Pin" 

        private void _AddIKPin(Transform endJoint)
        {
            ms_ReqChangePin = endJoint;
        }

        private void _DelIKPin(Transform endJoint)
        {
            ms_ReqChangePin = endJoint;
        }

        #endregion "IK Pin"

        #region "Set Constraint"
        // "Set Constraint" 

        /// <summary>
        /// postpone execution to repaint event
        /// because it seems that in Layout event, changed transform will not make SMR to change its vertex position
        /// </summary>
        private static Job _Job_PrepareSetConstraint()
        {
            yield return 0;

            //revert to prefab pose
            _ResetAllBonesToPrefabPose();

            // straighten current limb downward
            Transform[] joints = ms_IKSolver.GetJoints();
            Transform rootJoint = joints[0];
            Transform midJoint = joints[1];
            Transform endJoint = joints[2];

            Vector3 vec_a = (midJoint.position - rootJoint.position).normalized;
            IKSolverUtil.JointLookAt(rootJoint, Vector3.down, vec_a);
            vec_a = (endJoint.position - midJoint.position).normalized;
            IKSolverUtil.JointLookAt(midJoint, Vector3.down, vec_a);
        }

        #endregion "Set Constraint"

        #region "Pose Manage"
        // "Pose Manage" 

        /// <summary>
        /// add joint and all its descendants to the MultiSelectionJoints
        /// </summary>
        private void _SelectJointAndAllDescendants(Transform joint)
        {
            ms_SelCtrl.Select(null);
            ms_SelCtrl.RecurSelect(joint, true);
        }


        /// <summary>
        /// apply the saved pose info to current model
        /// </summary>
        private static void _ApplyPoseToCurrentModel(PoseDataDict data)
        {
            Transform animRoot = _GetAnimationRoot();
            //Transform rootJoint = ms_ExtendedJoints[0];
            //Dbg.Log("animRoot = {0}", animRoot.name);

            for (var ie = data.GetEnumerator(); ie.MoveNext(); )
            {
                var pr = ie.Current;
                string jointTrPath = pr.Key;
                XformData jointData = pr.Value;

                //Transform tr = rootJoint.FindByName(jointName);
                Transform tr = animRoot.Find(jointTrPath);
                if (tr == null)
                {
                    Dbg.LogWarn("SMREditor._ApplyPoseToCurrentModel: failed to find joint at Path: {0}", jointTrPath);
                    continue;
                }

                Undo.RecordObject(tr, "ApplyPoseToCurrentModel");
                jointData.Apply(tr);
                HotFix.FixRotation(tr);
            }

        }

        /// <summary>
        /// copy the local info from current ms_MultiSelectJoints, 
        /// and generate a PoseDataDict from the info
        /// </summary>
        private static PoseDataDict _GenPoseDataDictFromMultiSelect()
        {
            PoseDataDict dict = new PoseDataDict();
            for (var ie = ms_SelCtrl.Joints.GetEnumerator(); ie.MoveNext(); )
            {
                Transform tr = ie.Current;

                XformData data = new XformData();
                data.CopyFrom(tr);
                dict.Add(tr.name, data);
            }

            return dict;
        }

        /// <summary>
        /// use current multi-selected joints to create a new pose data;
        /// 
        /// if the name already in use, don't do anything
        /// </summary>
        private static void _CreateNewPose(string poseName)
        {
            if (string.IsNullOrEmpty(poseName))
            {
                EUtil.ShowNotification("The_PoseName_is_Empty");
                return;
            }
            if (ms_PoseSet.HasPose(poseName))
            {
                EUtil.ShowNotification("The_PoseName_is_Occupied: " + poseName);
                return;
            }

            Transform animRoot = _GetAnimationRoot();
            if (animRoot != null)
            {
                ms_PoseSet.AddPose(poseName, ms_SelCtrl.Joints.ToArray(), animRoot);
                EUtil.ShowNotification("New_Pose_is_Created: " + poseName);
            }
            else
            {
                Dbg.LogErr("SMREditor._CreateNewPose: failed to find AnimationRoot transform, abort AddPose...");
            }
        }

        #endregion "Pose Manage"

        #region "Mirror edit"
        // "Mirror edit" 

        private void _ProcessMirrorEdit()
        {
            if (!ms_bMirrorEdit)
                return;

            JointList jointLst = ms_SelCtrl.Joints;
            for (int i = 0; i < jointLst.Count; ++i)
            {
                Transform j = jointLst[i];

                ms_MirrorCtrl.ApplyMirror(j);
            }

        }

        // callback for MirrorSettingWindow
        private static void _OnMirrorSettingFinish(bool bOK)
        {
            if (bOK)
            {
                ms_bMirrorEdit = !ms_bMirrorEdit;
                EUtil.ShowNotification("Mirror_Edit_Mode: " + (ms_bMirrorEdit ? "ON" : "OFF"));
            }
        }

        #endregion "Mirror edit"


        #endregion "private method"

        #region "constant data"
        // constant data

        public enum ETool
        {
            None,
            Move,
            Rotate,
            Scale,
            RotateIKRoot,
            IKPinned,
        }

        public enum EPivotRotation
        {
            Local,
            Global,
            Parent,
            END,
        }

        public enum ESetConstraintStep
        {
            None,
            SelectLimbType,
            RotateMidJoint,
        }

        public enum EShowVert
        {
            INVALID = -1,
            HIDE,
            NORMAL,
            TRANSPARENT,
            END
        }

        public enum EOPMode
        {
            INVALID = -1,
            FK,
            IK,
            IK_Pinned,
            PoseManage, //Pose Manager
            BindPoseFixer, //fix invbind
            END
        }

        public enum EBindPoseFixerMode
        {
            INVALID = -1,
            NORMAL,
            SOLE_JOINT,
            END
        }

        public enum EHandleState
        {
            JustStartDragging,
            Dragging,
            JustStopDragging,
            NoDragging,
        }

        public readonly static Color BLUE_COLOR = new Color(52 / 255f, 112 / 255f, 151 / 255f);
        public const double CLICK_THRES = 0.2; //for LMB, if down and up time duration is lower than this value, then consider as click

        public const float WND_WIDTH = 300;
        public const float WND_HEIGHT = 180;
        public const float WND_UPOFF = 37; //window height must subtract this to prevent some part covered by console line

        public const float WND_HELP_WIDTH = 300;
        public const float WND_HELP_HEIGHT = 300;

        public const float ANIM_HELPER_WND_HEIGHT = 60;
        public const float HANDLE_CLICK_DIST_THRES = 50f; //if user tries to select a handle by clicking in sceneview, it will be taken invalid if the dist is bigger than 50pixels

        public const string BACKGROUND_TEX = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/black.jpg";
        public const string BACKGROUND_W_TEX = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/white.jpg";
        public const string PassThroughMat = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/DepPass.mat";
        public const string NormalVertMat = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/NormalVertMarker.mat";
        public const string RotateArrowMat = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/RotateArrow.mat";

        public const string TopMostGOName = "__MH_TopMost";
        public const string SkeleEditorGOName = "__MH_SkelEditor";

        public const string EDITOR_PREF_KEY_JOINTSIZE = "__MH_Skele_JointSize";
        public const string EDITOR_PREF_KEY_LINEWIDTH = "__MH_Skele_LineWidth";
        public const string EDITOR_PREF_KEY_VERTSIZE = "__MH_Skele_VertSize";
        public const string EDITOR_PREF_KEY_SHOWXFORM = "__MH_Skele_ShowXform";
        public const string EDITOR_PREF_KEY_POSEMGR_SLPATH = "__MH_Skele_PoseMgrSaveLoadPath";

        public const string TAG_I_AM_BONE = "I_AM_BONE";
        public const string TAG_I_AINT_BONE = "I_AINT_BONE";
        #endregion "constant data"

        #region "Inner struct"
        // "Inner struct" 

        /// <summary>
        /// used by Pose-Manage part, to change a pose's name
        /// </summary>
        public class _Rename_InputCallback
        {
            private PoseDesc m_Desc;

            public _Rename_InputCallback(PoseDesc desc)
            {
                m_Desc = desc;
            }

            public void OnSuccess(string newName)
            {
                m_Desc.m_PoseName = newName;
            }
        }

        #endregion "Inner struct"
    }

}

namespace MH
{


    public class ConstraintInfo
    {
        public Transform m_SkeleRootJoint; //the skeleton's root joint
        public Vector3 m_RotateAxis; // the direction relative to Limb root joint, rotate around this axis we can straighten the limb
        public Vector3 m_LimbRootRefDir; // the ref direction relative the Limb root joint, 
        public Vector3 m_SkeleRootRefDir; //the ref direction relative the root joint  
        public float m_AngleThres; //the rotate angle threshold for bone-axis
    }

    

}
