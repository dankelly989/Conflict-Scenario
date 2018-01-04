using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MH.MeshOp;
using MH.MeshEditor;
using MH.VertAnim;
using System.Text;
using System.IO;

namespace MH
{
    using RVLst = System.Collections.Generic.List<int>;
    using RVGroup = System.Collections.Generic.List<int>; //represent a group of verts
    using VELst = System.Collections.Generic.List<MH.MeshEditor.VEdge>;

    /// <summary>
    /// this is used to manipulate mesh ( MeshFilter & SkinnedMeshRenderer )
    /// </summary>
	public class MeshManipulator : EditorWindow
	{
	    #region "input"
	    // "input" 

        private float m_VertSize = 2.7f;
        private bool m_AllowReSelect = false;
	
	    #endregion "input"

	    #region "data"
	    // "data" 

        // global
        private static MeshManipulator ms_Instance = null;
        private EditableMesh m_EditMesh = null;
        private bool m_InEdit = false;
        private bool m_bRightBtnIsDown = false; //a flag indicate whether RMB is down

        // transparent
        private bool m_bTransparent = false;
        private Material[] m_PrevNonTransMats = null; //original material of mesh
        private Material m_TransMat = null; //the transparent mesh of 'Z'

        // pivot 
        private Pivotor m_Pivotor;

        // cursor
        private EditorCursor m_Cursor;

        // selection
        private MeshSelection m_Selection;

        // mode
        private EditMode m_EditMode; //the top-level mode
        private EditTool m_EditTool;  // move/rotate/scale

        // Coroutines
        private MH.Skele.Skele_CRCont m_CRNormal; //normal mode coroutine container;

        // marker
        private MeshMarker m_Marker;

        // overlap checker
        //private OverlapChecker m_OverlapChker;

        // Virtual Mesh
        private VMesh m_VMesh; //the virtual mesh

        // handle helper
        private HandleHelper m_handle;

        // B-Sel helper
        private BSelHelper m_BSel;

        // depth buffer
        private Shader m_RenderDepthShader;

        // soft selection
        private SoftSelection m_SoftSel;

	    #endregion "data"

	    #region "MeshOps"
	    // "MeshOps" 

        private MoveVerts m_OpMoveVerts;
        private RotateVerts m_OpRotateVerts;
        private ScaleVerts m_OpScaleVerts;
        private RecalcNormalOp m_OpRecalcNormal;
        private FixBoundNormalOp m_OpFixBoundNormal;
        private EdgeLoopSelectOp m_OpEdgeLoopSelect;
	
	    #endregion "MeshOps"

	    #region "event"
	    // "event" 

        public delegate void EditToolChanged(EditTool oldTool, EditTool newTool);
        public static event EditToolChanged evtEditToolChanged;

        public delegate void EditModeChanged(EditMode oldMode, EditMode newMode);
        public static event EditModeChanged evtEditModeChanged;

        public delegate void HandleStateChanged(HandleHelper data);
        public static event HandleStateChanged evtHandleStateChanged;

        public delegate void HandleDraggingStateChanged(bool bDraggingHandle); //a subset of HandleStateChanged
        public static event HandleDraggingStateChanged evtHandleDraggingStateChanged;
	
	    #endregion "event"

	    #region "Unity methods"
	    // "Unity methods" 

        [MenuItem("Window/Skele/Mesh Manipulator %&m")]
        public static void OpenWindow()
        {
            if (ms_Instance == null)
            {
                var inst = ms_Instance = (MeshManipulator)GetWindow(typeof(MeshManipulator));
                EditorApplication.playmodeStateChanged += inst.OnPlayModeChanged;

                EUtil.SetEditorWindowTitle(inst, "Mesh Manipulator");
                //inst.minSize = new Vector2(150, 150);
                inst.Show();

                inst._OneTimeInit();
            }
        }

        void OnPlayModeChanged()
        {
            if (ms_Instance != null)
            {
                ms_Instance.Close();
            }
        }

        void OnDestroy()
        {
            if( m_InEdit )
                _EndEdit();

            ms_Instance = null;
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        //private Texture2D m_depthBufferTex = null;
        void OnGUI()
        {
            GameObject selGO = Selection.activeGameObject;

            bool hasTarget = EditableMesh.HasAvailTarget(selGO);

            

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(30f);
                EUtil.PushGUIEnable(hasTarget);
                if (EUtil.Button(
                     (!m_InEdit ? "Start Edit" : "End Edit"),
                     (!m_InEdit ? Color.green : Color.red)
                  ))
                {
                    _ToggleInEdit();
                }
                EUtil.PopGUIEnable();
                GUILayout.Space(30f);
            }            
            GUILayout.EndHorizontal();

            if (m_InEdit)
            {
                GUILayout.Space(5f);
                //GUILayout.BeginHorizontal();
                //{
                //    GUILayout.Label("VertSize: " + m_VertSize.ToString("F1"));
                //    float newSz = GUILayout.HorizontalSlider(m_VertSize, 1f, 10f);                    
                //    if( !Mathf.Approximately(newSz, m_VertSize) )
                //    {
                //        m_VertSize = newSz;
                //        m_Marker.SetVertSize(m_VertSize);
                //    }
                //}
                //GUILayout.EndHorizontal();

                float w = this.position.width / 2f - 5f;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Pivot Position:", GUILayout.Width(w));
                    string pivotOpMsg = m_Pivotor.pivotOp.ToString();
                    if( GUILayout.Button(pivotOpMsg, GUILayout.Width(w)) )
                    {
                        _SwitchPivotOp();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Pivot Orientation:", GUILayout.Width(w));
                    string orientMsg = m_Pivotor.orient.ToString();
                    if( GUILayout.Button(orientMsg, GUILayout.Width(w)) )
                    {
                        _SwitchPivotOrientation();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Soft Selection:", GUILayout.Width(w));
                    if(GUILayout.Button(m_SoftSel.Activated?"On":"Off", GUILayout.Width(w)))
                    {
                        _ToggleSoftSelection();    
                    }
                }
                GUILayout.EndHorizontal();

                //GUILayout.BeginHorizontal();
                //{
                    //string toolMsg = m_EditTool.ToString();
                    //if( GUILayout.Button(toolMsg) )
                    //{
                    //    EditTool newTool = (EditTool)((int)(m_EditTool + 1) % (int)EditTool.END);
                    //    _ChangeEditTool(newTool);
                    //}

                    //string modeMsg = m_EditMode.ToString();
                    //if( GUILayout.Button(modeMsg) )
                    //{
                    //    EditMode newMode = (EditMode)((int)(m_EditMode + 1) % (int)EditMode.END);
                    //    _ChangeEditMode(newMode);
                    //}

                    //if (GUILayout.Button("NormalCalc"))
                    //{
                    //    m_OpRecalcNormal.Execute();
                    //}
                //}
                //GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    float btnWidth = this.position.width / 4f - 5f;

                    bool hasShapeKeyProc = _HasMorphProcComponent();
                    EUtil.PushGUIEnable(!hasShapeKeyProc);
                    if(GUILayout.Button(new GUIContent("MorphProc", 
                            "add a MorphProc component on the current selected GameObject"),
                            GUILayout.Width(btnWidth))
                      )
                    {
                        _CreateMorphProcComponent();
                    }
                    EUtil.PopGUIEnable();

                    if (GUILayout.Button(new GUIContent("Prefab", 
                        "export the current selection as prefab"),
                        GUILayout.Width(btnWidth)))
                    {
                        _ExportPrefab();
                        //EUtil.SaveMeshToAssetDatabase(m_EditMesh.mesh);
                    }

                    if( GUILayout.Button(new GUIContent("Shortcuts",
                        "Show the shortcut list"),
                        GUILayout.Width(btnWidth)))
                    {
                        ShortcutListWindow.ToggleWindow();
                    }

                    if (EUtil.Button(
                        m_AllowReSelect ? "Unlocked" : "Locked", "whether allow changing gameObject selection",
                        m_AllowReSelect ? Color.green : Color.red,
                        GUILayout.Width(btnWidth)))
                    {
                        m_AllowReSelect = !m_AllowReSelect;
                    }

                }
                GUILayout.EndHorizontal();

                

                //if (m_Selection.RVCount < 5)
                //{
                //    var vlst = m_Selection.GetVertices();
                //    System.Text.StringBuilder bld = new System.Text.StringBuilder();
                //    for (int i = 0; i < vlst.Count; ++i)
                //    {
                //        bld.AppendFormat("{0} ", vlst[i]);
                //    }
                //    GUILayout.Label("Selected vert lst: " + bld.ToString());
                //}
                //else
                //GUILayout.Label("Selected verts: " + m_Selection.RVCount);

                //GUILayout.BeginHorizontal();
                //{
                //    GUILayout.Label(m_SoftSel.Activated.ToString());
                //    GUILayout.Label("Range: " + m_SoftSel.Range);
                //}
                //GUILayout.EndHorizontal();
            }

        }

        void Update()
        {
            // compiling check
            if (EditorApplication.isCompiling)
            {
                if (ms_Instance != null)
                {
                    Dbg.Log("isCompiling");   
                    ms_Instance.Close();
                }
                return;
            }
        }

        void OnInspectorUpdate()
        {
            if (ms_Instance == null)
                Close(); //this happens when the window is auto-opened when Unity starts

            // check target object is still existed, or shutdown
            if (null == m_EditMesh)
            {
                if( m_InEdit )
                    _EndEdit();
            }
            else
            {
                if (!m_EditMesh.valid)
                {
                    _EndEdit();
                }
                else
                {
                    // lock selection
                    if( ! m_AllowReSelect )
                    {
                        GameObject selGO = Selection.activeGameObject;
                        GameObject meshGO = m_EditMesh.gameObject;
                        if (selGO != meshGO)
                        {
                            Selection.activeGameObject = meshGO;
                            EUtil.ShowNotification("GO re-selection is forbidden");
                        }
                    }

                    

                }                
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            UnityEditor.Tools.current = UnityEditor.Tool.None; // clear the default position/rotation/scale handles

            Event e = Event.current;

            #region "top-level event processing"

            _EventProcess_Toplevel(e);
	
	        #endregion "top-level event processing"            

            ////////////////////////////////////////////
            // event processing for each mode
            ////////////////////////////////////////////
            switch(m_EditMode)
            {
                case EditMode.Normal:
                    {
                        _Shortcut_Normal();
                        _HandleProcess(); //draw handle and modify mesh, 
                        _EventProcess_Normal(); //need to be after _HandleProcess to detect user press on handle
                    }
                    break;
                case EditMode.BSel:
                    {
                        _Shortcut_BSel();
                        _EventProcess_BSel();
                    }
                    break;
                case EditMode.CSel:
                    {
                        _EventProcess_CSel();
                    }
                    break;
            }



	        #region "test region"
            //Event ev = Event.current;
            //if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.M)
            //{
            //    var cam = EUtil.GetSceneView().camera;
            //    float oldF = cam.farClipPlane;
            //    cam.farClipPlane = ZTEST_TMP_FARPLANE;
            //    m_depthBufferTex = _CreateDepthBuffer();
            //    cam.farClipPlane = oldF;

            //    RaycastHit hit;
            //    Ray r = HandleUtility.GUIPointToWorldRay(ev.mousePosition);
            //    if (Physics.Raycast(r, out hit, float.MaxValue, -1))
            //    {
            //        float realZ = -cam.worldToCameraMatrix.MultiplyPoint(hit.point).z;

            //        float CAM_H = cam.pixelHeight;
            //        float N = cam.nearClipPlane;
            //        float F = cam.farClipPlane;
            //        float FN = F * N;
            //        float NsF = N - F;

            //        float BORDER_DIM = (Screen.width - cam.pixelWidth) * 0.5f; //the Left/right/bottom border dimension of scene view  
            //        //Dbg.Log("BORDER = {0}", BORDER_DIM);
            //        float THRESHOLD = 0.01f; //we accept some error

            //        Vector2 mousePos = ev.mousePosition;
            //        float zBufDist = _GetZBufferDistAtPixel(CAM_H, F, FN, NsF, BORDER_DIM, m_depthBufferTex, ref mousePos);

            //        Dbg.Log("realZ: " + realZ + ", zbuf: " + zBufDist + ", " + (realZ <= zBufDist + THRESHOLD)); 
            //    }
            //}

            //if (e.type == EventType.MouseDown)
            //{ //test EUtil.RaycastAll 
            //    if (e.button == 2)
            //    {
            //        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            //        RaycastHit[] hits = EUtil.RaycastAll(ray, m_EditMesh.mesh, m_EditMesh.transform.localToWorldMatrix);

            //        System.Text.StringBuilder sb = new System.Text.StringBuilder();
            //        for (int i = 0; i < hits.Length; ++i)
            //        {
            //            RaycastHit oneHit = hits[i];
            //            sb.AppendFormat("{0}, {1}\n", oneHit.triangleIndex, oneHit.point);
            //        }
            //        Dbg.Log("hits : {0}\n{1}", hits.Length, sb.ToString());

            //        //Dbg.Log("hits: {0}", hits.Length);
            //    }
            //}

            //if (e.type == EventType.MouseDown)
            //{ //test Physics.RaycastAll
            //    if (e.button == 2)
            //    {
            //        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            //        //RaycastHit[] hits = EUtil.RaycastAll(ray, m_EditMesh.mesh, m_EditMesh.transform.localToWorldMatrix);

            //        RaycastHit[] hits = Physics.RaycastAll(ray);

            //        System.Text.StringBuilder sb = new System.Text.StringBuilder();
            //        for (int i = 0; i < hits.Length; ++i)
            //        {
            //            RaycastHit oneHit = hits[i];
            //            sb.AppendFormat("{0}, {1}, {2}\n", oneHit.triangleIndex, oneHit.point, oneHit.transform.name);
            //        }
            //        Dbg.Log("hits : {0}\n{1}", hits.Length, sb.ToString());

            //        //Dbg.Log("hits: {0}", hits.Length);
            //    }
            //}
	
	        #endregion "test region"
            
        }

	    #endregion "Unity methods"

	    #region "public methods"

        public static MeshManipulator Instance
        {
            get { return ms_Instance; }
        }

        public bool IsHandleChanging
        {
            get { return m_handle.handleDragging; }
        }

        public SoftSelection GetSoftSelection()
        {
            return m_SoftSel;
        }

        public HandleHelper GetHandleHelper()
        {
            return m_handle;
        }
	
	    #endregion "public methods"
        
	    #region "private methods"
	    // "private methods" 

	    #region "start/end edit"
        // "start/end edit" 

        private void _OneTimeInit()
        {
            Dbg.Log("MeshManipulator._OneTimeInit");
        }

        private void _ToggleInEdit()
        {
            if (m_InEdit)
            {
                _EndEdit();
            }
            else
            {
                _StartEdit();
            }
        }

        private void _StartEdit()
        {
            m_InEdit = true;

            _LoadRes(); //load materials and editor prefs

            SceneView.onSceneGUIDelegate += this.OnSceneGUI;

            ETimeProf prof = new ETimeProf();
            // current target
            var selGO = Selection.activeGameObject;
            m_EditMesh = EditableMesh.New(selGO);

            // MeshCache
            MeshCache.CreateInstance();
            MeshCache.Instance.Init(m_EditMesh.transform, m_EditMesh.mesh);

            prof.Click("MeshCache.Init");

            // VMesh
            m_VMesh = new VMesh();
            m_VMesh.Init(m_EditMesh);

            prof.Click("VMesh.Init");

            // selection
            m_Selection = new MeshSelection();
            m_Selection.Init();

            // cursor
            m_Cursor = new EditorCursor();
            m_Cursor.Init(m_EditMesh);

            // pivot & orientation & edit-tool
            m_Pivotor = new Pivotor();
            m_Pivotor.Init(m_EditMesh, m_Selection, m_Cursor);
            m_EditTool = EditTool.None;
            
            // coroutine
            m_CRNormal = new MH.Skele.Skele_CRCont();

            prof.Click("Selection/Cursor/Pivotor Init");

            //markers
            m_Marker = new MeshMarker();
            m_Marker.Init(m_Selection, m_EditMesh);
            m_Marker.SetVertSize(m_VertSize);

            prof.Click("MeshMarker Init");

            //// overlap checker
            //m_OverlapChker = new OverlapChecker();
            //m_OverlapChker.Init(m_EditMesh.mesh);

            // handle helper
            m_handle = new HandleHelper(this);
            m_handle.Init();

            // B-Sel helper
            m_BSel = new BSelHelper();
            m_BSel.Init();

            // soft selection
            m_SoftSel = new SoftSelection();
            m_SoftSel.Init(m_EditMesh, m_Selection, m_Pivotor);

            prof.Click("SoftSel Init");

            _InitMeshOps();

            prof.Click("MeshOps Init");

            _LoadEditorPref();

            Dbg.Log("==== MeshMorpher Start ====");
        }

        private void _EndEdit()
        {
            _SetTransparent(false); //resume original material

            _SaveEditorPref();

            _FiniMeshOps();

            m_SoftSel.Fini();
            m_SoftSel = null;

            m_BSel.Fini();
            m_BSel = null;

            m_handle.Fini();
            m_handle = null;

            m_Marker.Fini();
            m_Marker = null;

            m_CRNormal.Clear();
            m_CRNormal = null;

            m_Selection.Fini();
            m_Selection = null;

            m_Pivotor.Fini();
            m_Pivotor = null;

            m_VMesh.Fini();
            m_VMesh = null;

            MeshCache.Instance.Fini();
            MeshCache.DestroyInstance();

            //if (m_EditMesh.valid)
            //    UndoMesh.DeleteEntry(m_EditMesh.mesh);
            m_EditMesh = null;

            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;

            m_InEdit = false;

            _ReqRepaint();

            Dbg.Log("==== MeshMorpher Stop ====");
        }
	
        private void _InitMeshOps()
        {
            m_OpMoveVerts = new MoveVerts();
            m_OpMoveVerts.Init(m_EditMesh);

            m_OpRotateVerts = new RotateVerts();
            m_OpRotateVerts.Init(m_EditMesh, m_Pivotor);

            m_OpScaleVerts = new ScaleVerts();
            m_OpScaleVerts.Init(m_EditMesh, m_Pivotor);

            m_OpRecalcNormal = new RecalcNormalOp();
            m_OpRecalcNormal.Init(m_EditMesh);

            m_OpFixBoundNormal = new FixBoundNormalOp();
            m_OpFixBoundNormal.Init(m_EditMesh);

            m_OpEdgeLoopSelect = new EdgeLoopSelectOp();
            m_OpEdgeLoopSelect.Init(m_EditMesh, m_Selection);
        }

        private void _FiniMeshOps()
        {
            m_OpEdgeLoopSelect.Fini();
            m_OpEdgeLoopSelect = null;

            m_OpFixBoundNormal.Fini();
            m_OpFixBoundNormal = null;

            m_OpRecalcNormal.Fini();
            m_OpRecalcNormal = null;

            m_OpScaleVerts.Fini();
            m_OpScaleVerts = null;

            m_OpRotateVerts.Fini();
            m_OpRotateVerts = null;

            m_OpMoveVerts.Fini();
            m_OpMoveVerts = null;
        }

        private void _LoadRes()
        {
            m_TransMat = AssetDatabase.LoadAssetAtPath(TRANSMAT_PATH, typeof(Material)) as Material;
            Dbg.Assert(m_TransMat != null, "MeshManipulator._OneTimeInit: failed to get transMat at: {0}", TRANSMAT_PATH);

            m_RenderDepthShader = AssetDatabase.LoadAssetAtPath(DEPTH_SHADER_PATH, typeof(Shader)) as Shader;
            Dbg.Assert(m_RenderDepthShader != null, "MeshManipulator._StartEdit: failed to get renderDepthShader at: {0}", DEPTH_SHADER_PATH);
        }

        private void _LoadEditorPref()
        {
            m_VertSize = EditorPrefs.HasKey(Pref_VertSize) ? EditorPrefs.GetFloat(Pref_VertSize) : Def_VertSize;
            m_SoftSel.Range = EditorPrefs.HasKey(Pref_SoftRange) ? EditorPrefs.GetFloat(Pref_SoftRange) : Def_SoftRange;
        }

        private void _SaveEditorPref()
        {
            EditorPrefs.SetFloat(Pref_VertSize, m_VertSize);
            EditorPrefs.SetFloat(Pref_SoftRange, m_SoftSel.Range);
        }

	    #endregion "start/end edit"

	    #region "transparent"
	    // "transparent" 

        private void _ToggleTransparent()
        {
            _SetTransparent(!m_bTransparent);
        }

        private void _SetTransparent(bool bTrans)
        {
            if (m_bTransparent == bTrans)
                return;

            m_bTransparent = bTrans;
            if( m_bTransparent )
            { 
                var renderer = m_EditMesh.renderer;
                m_PrevNonTransMats = renderer.sharedMaterials; //store the materials
                renderer.sharedMaterial = m_TransMat;
            }
            else
            {
                if( m_PrevNonTransMats != null )
                {
                    m_EditMesh.renderer.sharedMaterials = m_PrevNonTransMats;
                    m_PrevNonTransMats = null;
                }
            }
        }

        private bool _IsTransparentMode()
        {
            return m_bTransparent;
        }
	
	    #endregion "transparent"

	    #region "Event processing"

        private void _EventProcess_Toplevel(Event e)
        {
            int defControlID = GUIUtility.GetControlID(DEFAULT_CONTROL_HASH, FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(defControlID);

                //////////////////////////////////////////////////
                // markers update
                //////////////////////////////////////////////////
                if (m_Selection.Dirty)
                {
                    m_Marker.UpdateMarkers(); //will set m_Marker.Dirty = false;
                    m_Selection.Dirty = false;

                    _ReqRepaint();
                }

                if (m_Marker.Dirty) //if selection not changed, but mesh is changed...
                {
                    m_Marker.UpdateMarkers(); //will set m_Marker.Dirty = false;

                    _ReqRepaint();
                }

                //execute draw marker
                m_Marker.OnDraw();

            }

            // draw 3D cursor (only in cursor pivotOp)
            _Draw3DCursor();

            //execute draw soft-selection range
            _DrawSoftSelectionRange();

            // mouse event pre-processing
            _UpdateMouseState(e);

            _GlobalShortcut();
        }

        private void _UpdateMouseState(Event e)
        {
            if (e.rawType == EventType.MouseUp)
            {
                if (e.button == 1)
                    m_bRightBtnIsDown = false;
            }
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 1)
                {
                    m_bRightBtnIsDown = true;
                }
            }
        }

        /// <summary>
        /// Z -- transparent mode toggle
        /// 1/3/5/7 (ctrl) -- view rotation
        /// </summary>
        private void _GlobalShortcut()
        {
            if (m_handle.handleDragging)
                return;

            Event e = Event.current;

            // blender-like view rotation
            ViewRotate.RotateViewByEvent();

            if (e.type == EventType.KeyDown && !m_bRightBtnIsDown)
            {
                if( !_HasCSA(e) )
                {
                    switch(e.keyCode)
                    {
                        case KeyCode.Z : _ToggleTransparent(); break;
                        case KeyCode.B : _ChangeEditMode(EditMode.BSel); break;
                        case KeyCode.O : _ToggleSoftSelection(); break;
                        case KeyCode.D : _SwitchPivotOp(); break;
                        case KeyCode.S : _SwitchPivotOrientation(); break;
                        //case KeyCode.Alpha4:
                        //    {
                        //        m_SoftSel.Prepare();
                        //        m_SoftSel.PMode = SoftSelection.PrepareMode.Stop;
                        //    }
                        //    break;
                        //case KeyCode.Alpha5:
                        //    {
                        //        m_SoftSel.PMode = SoftSelection.PrepareMode.Always;
                        //    }
                        //    break;
                        //case KeyCode.Alpha7:
                        //    {
                        //        m_OpScaleVerts.ExecuteWorldSoft(m_SoftSel, Vector3.one, Vector3.one*2);
                        //    }
                        //    break;
                        //case KeyCode.Alpha8:
                        //    {
                        //        m_OpScaleVerts.ExecuteWorldSoft(m_SoftSel, Vector3.one*2, Vector3.one);
                        //    }
                        //    break;
                        //case KeyCode.X: DBG_TestDepthChk(); break;
                    }
                }
            }
        }

	    #region "Normal mode"
	    // "Normal mode" 

        /// <summary>
        /// W/E/R -- move/rotate/scale
        /// O -- toggle orientation
        /// ESC -- edit tool -> none
        /// </summary>
        private void _Shortcut_Normal()
        {
            if (m_handle.handleDragging)
                return;

            Event e = Event.current;

            if( e.type == EventType.KeyDown && !m_bRightBtnIsDown )
            {
                if( !e.control && !e.alt && !e.shift )
                {
                    switch( e.keyCode )
                    {
                        case KeyCode.W: _ChangeEditTool(EditTool.Move); break;
                        case KeyCode.E: _ChangeEditTool(EditTool.Rotate); break;
                        case KeyCode.R: _ChangeEditTool(EditTool.Scale); break;
                        case KeyCode.A: _ToggleAllSelect(); break;
                        case KeyCode.Q: _FocusOnPivot(); e.Use(); break;

                        case KeyCode.Escape:
                            {
                                if (m_EditTool != EditTool.None)
                                    _ChangeEditTool(EditTool.None);
                                else
                                    m_Selection.Clear();
                            }
                            break;
                    }
                }
            }

            _EventProcess_SetCursor(e);
        }

        private void _EventProcess_SetCursor(Event e)
        {
            // use Ctrl+RMB to set cursor position
            if (e.type == EventType.MouseDown && e.control && e.button == 1)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                if (EUtil.Raycast(ray, m_EditMesh.mesh, m_EditMesh.transform.localToWorldMatrix, out hit))
                { //click on model
                    m_Cursor.Pos = hit.point;
                }
                else
                { // not on model
                    var camTr = EUtil.GetSceneView().camera.transform;
                    Vector3 cursorPos = m_Cursor.Pos; //plane pass through this point
                     
                    Vector3 S = camTr.position; //the starting pos of line
                    Vector3 N = camTr.forward; //plane's normal
                    float D = -Vector3.Dot(cursorPos, N); // the D of plane equation

                    // for line P(t) = S + V * t (S staring point, V ray direction),
                    // calc t for the intersection point with plane <N, D>
                    Vector3 V = ray.direction;
                    float t = -(Vector3.Dot(N, S) + D ) / Vector3.Dot(N, V); 

                    Vector3 newPt = S + V * t;

                    m_Cursor.Pos = newPt;
                }

                e.Use(); //prevent unexpected action
            }
        }

        private void _SwitchPivotOrientation()
        {
            m_Pivotor.orient = (Pivotor.Orientation)((int)(m_Pivotor.orient + 1) % (int)Pivotor.Orientation.END);
            _ReqRepaint();
        }

        private void _SwitchPivotOp()
        {
            m_Pivotor.pivotOp = (Pivotor.PivotOp)((int)(m_Pivotor.pivotOp + 1) % (int)Pivotor.PivotOp.END);
            _ReqRepaint();
        }

        private void _EventProcess_Normal()
        {
            Event e = Event.current;

            switch ( e.type )
            {
                case EventType.Layout:
                    {
                        m_CRNormal.Execute(); //execute delayed jobs
                    }
                    break;
                case EventType.MouseDown:
                    {
                        //if (m_handle.changed) //don't select point when handle is clicked, to prevent unexpected mis-selection
                        //    break;

                        if (e.button == 0 && !e.alt)
                        {
                            Mesh m = m_EditMesh.mesh;
                            Transform meshTr = m_EditMesh.transform;
                            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                            if( !_IsTransparentMode() )
                            { //non-transparent mode
                                RaycastHit hit;
                                if (EUtil.Raycast(ray, m, meshTr.localToWorldMatrix, out hit))
                                {
                                    //Dbg.Log("MeshManipulator._EventProcess_Normal: hit mesh @ triangleIdx = {0}", hit.triangleIndex);
                                    float minDist;

                                    if( e.control )
                                    { //loop select mode
                                        VEdge vedge = _GetNearestVEdge(hit, out minDist);
                                        if( minDist < SELECT_DIST_THRES )
                                        {
                                            _LoopSelect(vedge, !e.shift);
                                        }
                                    }
                                    else
                                    { //individual select mode
                                        int vidx = _GetNearestVert(m, meshTr, hit, out minDist);
                                        if (minDist < SELECT_DIST_THRES)
                                        {
                                            _SelectVert(vidx, !e.shift);
                                        }
                                    }
                                    
                                }
                            }
                            else
                            { //transparent mode
                                RaycastHit[] hits = EUtil.RaycastAll(ray, m, meshTr.localToWorldMatrix);

                                if (hits.Length > 0)
                                {
                                    if (!e.control) //individual mode
                                    {
                                        float allMinDist = SELECT_DIST_THRES;
                                        int vidx = -1;

                                        foreach (var hit in hits)
                                        {
                                            //Dbg.Log("MeshManipulator._EventProcess_Normal: transparent hit mesh @ triangleIdx = {0}", hit.triangleIndex);
                                            float minDist;
                                            int tmpIdx = _GetNearestVert(m, meshTr, hit, out minDist);
                                            if (minDist < allMinDist)
                                            {
                                                allMinDist = minDist;
                                                vidx = tmpIdx;
                                            }
                                        }

                                        if (vidx >= 0)
                                        {
                                            _SelectVert(vidx, !e.shift);
                                        }
                                    }
                                    else //loop select mode
                                    {
                                        float allMinDist = SELECT_DIST_THRES;
                                        VEdge nearestEdge = null;

                                        foreach (var hit in hits)
                                        {
                                            float minDist;
                                            VEdge vedge = _GetNearestVEdge(hit, out minDist);
                                            if (minDist < allMinDist)
                                            {
                                                allMinDist = minDist;
                                                nearestEdge = vedge;
                                            }
                                        }

                                        if( nearestEdge != null )
                                        {
                                            _LoopSelect(nearestEdge, !e.shift);
                                        }
                                    }
                                }
                            }

                            _ReqRepaint();

                        } //end of if(e.button == 0)
                    }
                    break;

            } // end of switch(e.type)

            _EventProcess_SoftSelection(); //soft-selection related
        }

        private void _FocusOnPivot()
        {
            Vector3 boundSize = m_EditMesh.mesh.bounds.size;
            float dist = Mathf.Max(boundSize.x, boundSize.y, boundSize.z) + 1f;
            //EUtil.AlignViewToPos(m_Pivotor.WorldPos, dist);
            EUtil.SceneViewLookAt(m_Pivotor.WorldPos, dist);
            //Dbg.Log("_FocusOnPivot: pivot:{0}, dist: {1}", m_Pivotor.WorldPos, dist);
        }

	    #endregion "Normal mode"
        
	    #region "B-Selection mode"
	    // "B-Selection mode" 

        /// <summary>
        /// ESC to return to normal mode
        /// </summary>
        private void _Shortcut_BSel()
        {
            if (m_handle.handleDragging)
                return;

            Event e = Event.current;

            if( e.type == EventType.KeyDown )
            {
                if( !m_bRightBtnIsDown && !_HasCSA(e)  )
                {
                    switch(e.keyCode)
                    {
                        case KeyCode.Escape: 
                            {
                                _ChangeEditMode(EditMode.Normal); 
                            }
                            break;
                    }
                }
            }
        }

        private void _EventProcess_BSel()
        {
            Event e = Event.current;

            switch(e.type)
            {
                case EventType.Layout:
                    {
                        if (m_BSel.started)
                        {
                            _DrawBlockSelectionArea();
                        }
                    }
                    break;
                case EventType.Repaint:
                    {
                        if (m_BSel.started)
                        {
                            _DrawBlockSelectionArea();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    {
                        if( e.button == 0 && !m_BSel.started)
                        {
                            m_BSel.started = true;
                            m_BSel.endPos = m_BSel.startPos = e.mousePosition;
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    {
                        if( m_BSel.started )
                        {
                            m_BSel.endPos = e.mousePosition;
                            EUtil.GetSceneView().Repaint();
                        }
                    }
                    break;
            }

            // Mouse release check
            if( e.rawType == EventType.MouseUp )
            {
                if( e.button == 0 && m_BSel.started )
                {
                    m_BSel.endPos = e.mousePosition;
                    _ExecuteBlockSelection(); //
                    _ChangeEditMode(EditMode.Normal);
                    m_BSel.Reset();
                    _ReqRepaint();
                }
            }

        }

        /// <summary>
        /// draw a rect indicating the selection area
        /// </summary>
        private void _DrawBlockSelectionArea()
        {
            Rect rc = new Rect();
            rc.min = m_BSel.startPos;
            rc.max = m_BSel.endPos;
            Handles.BeginGUI();
            {
                EUtil.PushGUIColor(new Color(0f, 0f, 0.5f, 0.2f));
                GUI.DrawTexture(rc, EditorGUIUtility.whiteTexture);
                EUtil.PopGUIColor();
            }
            Handles.EndGUI();

            //Dbg.Log("_DrawBlockSelectionArea: {0}", rc);
        }

        /// <summary>
        /// execute the "selection by block"
        /// </summary>
        private void _ExecuteBlockSelection()
        {
            Rect rc = new Rect();
            rc.min = m_BSel.startPos;
            rc.max = m_BSel.endPos;
            //Dbg.Log("rc.min = {0}, rc.max= {1}, rc = {2}", rc.min, rc.max, rc);

            RVLst vlst = null;
            if (_IsTransparentMode())
                vlst = _BSel_Trans(ref rc);
            else
                vlst = _BSel_Normal(ref rc);

            Event e = Event.current;
            if(e.shift)
            {
                _DelVerts(vlst);
            }
            else
            {
                _AddVerts(vlst);
            }
        }

        /// <summary>
        /// return the vertex in rect (transparent), do not count the z-buffer
        /// </summary>
        private RVLst _BSel_Trans(ref Rect rc)
        {
            RVLst vlst = new RVLst();

            Mesh m = m_EditMesh.mesh;
            Transform tr = m_EditMesh.transform;
            Vector3[] vs = m.vertices;
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector2 pt = HandleUtility.WorldToGUIPoint(tr.TransformPoint(vs[i]));
                if (rc.Contains(pt))
                {
                    vlst.Add(i);
                }
            }
            return vlst;
        }

        /// <summary>
        /// return verts in rect, take z-buffer in account
        /// </summary>
        private RVLst _BSel_Normal(ref Rect rc)
        {
            RVLst vlst = new RVLst();

            Mesh m = m_EditMesh.mesh;
            Transform tr = m_EditMesh.transform;
            Vector3[] vs = m.vertices;

            Camera cam = EUtil.GetSceneView().camera;
            float oldF = cam.farClipPlane;
            cam.farClipPlane = ZTEST_TMP_FARPLANE;

            //Dbg.Log("rc: min:{0}, max:{1}", rc.min, rc.max);
            //Dbg.Log("cam: near:{0}, far:{1}", cam.nearClipPlane, cam.farClipPlane);

            float CAM_H = cam.pixelHeight;
            float N = cam.nearClipPlane;
            float F = cam.farClipPlane;
            float FN = F*N;
            float NsF = N - F;

            float BORDER_DIM = (Screen.width - cam.pixelWidth) * 0.5f; //the Left/right/bottom border dimension of scene view  
            //Dbg.Log("BORDER = {0}", BORDER_DIM);
            float THRESHOLD = 0.01f; //we accept some error

            // use rect to do first check and zbuffer to check again
            Texture2D zbuffer = _CreateDepthBuffer(); //temp texture
            for (int i = 0; i < vs.Length; ++i)
            {
                Vector3 worldPt = tr.TransformPoint(vs[i]);
                Vector2 scrPt = HandleUtility.WorldToGUIPoint(worldPt);
                if (rc.Contains(scrPt))
                {
                    Vector3 camSpacePt = cam.worldToCameraMatrix.MultiplyPoint(worldPt); 
                    float zDist = -camSpacePt.z;
                    float zBufDist = _GetZBufferDistAtPixel(CAM_H, F, FN, NsF, BORDER_DIM, zbuffer, ref scrPt);

                    //Dbg.Log("{0}: zDist: {1:F3}, zBufVal: {2:F3}, zBufDist: {3:F3}", zDist < zBufDist, zDist, zBufVal, zBufDist);

                    if (zDist <= zBufDist + THRESHOLD)
                    {
                        vlst.Add(i);
                    }
                }
            }
            Texture2D.DestroyImmediate(zbuffer); //clear texture
            cam.farClipPlane = oldF;

            return vlst;
        }

        private static float _GetZBufferDistAtPixel(float CAM_H, float F, float FN, float NsF, 
            float BORDER_DIM, Texture2D zbuffer, ref Vector2 scrPt)
        {
            //Color zBufColor = zbuffer.GetPixel((int)(scrPt.x + BORDER_DIM), (int)(CAM_H - scrPt.y + BORDER_DIM));
            float norX = (scrPt.x + BORDER_DIM) / zbuffer.width;
            float norY = (CAM_H - scrPt.y + BORDER_DIM) / zbuffer.height;
            Color zBufColor = zbuffer.GetPixelBilinear(norX, norY);
            float zBufVal = RTUtil.DecodeFloatRGBA(zBufColor);

            float zBufDist = FN / (F + zBufVal * NsF);//IMPORTANT!!! z-depth is linearized based on reciprocal
            return zBufDist;
        }
	
	    #endregion "B-Selection mode"

	    #region "C-Selection mode"
	    // "C-Selection mode" 

        private void _EventProcess_CSel()
        {
            //throw new NotImplementedException();
        }
	
	    #endregion "C-Selection mode"

	    #region "Soft-Selection"
	    // "Soft-Selection" 

        private void _EventProcess_SoftSelection()
        {
            if (!m_SoftSel.Activated)
                return;

            Event e = Event.current;
            if( e.rawType == EventType.KeyDown && !m_bRightBtnIsDown && !_HasCSA(e))
            {
                switch(e.keyCode)
                {
                    case KeyCode.LeftBracket:
                    case KeyCode.RightBracket:
                        {
                            float newCoef = m_SoftSel.Range * ((e.keyCode == KeyCode.LeftBracket) ? 0.97f : 1.03f);
                            if( Mathf.Abs(newCoef) > MIN_SOFTSELECTION_RANGE ) //prevent it goes too small
                                _ChangeSoftSelectionRange(newCoef);
                        }
                        break;
                }
            }
        }

	    #endregion "Soft-Selection"

	    #endregion "Event processing"

	    #region "Selection"
	    // "Selection" 

	    #region "Soft selection"

        /// <summary>
        /// MOVE & SCALE calculate from a cached vert array,
        /// but ROTATE is calculated by delta, as the rotation is looped, info will lose if start from cached array
        /// </summary>
        private void _ChangeSoftSelectionRange(float newRange)
        {
            if (Mathf.Approximately(newRange, m_SoftSel.Range))
                return;

            //if dragging, revert cur modification and apply with new range
            if (m_handle.handleDragging)
            {
                switch (m_EditTool)
                {
                    case EditTool.Move:
                        {
                            //m_OpMoveVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragCurWorldPos, m_handle.dragStartWorldPos);
                            m_SoftSel.Range = newRange;
                            m_OpMoveVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragStartWorldPos, m_handle.dragCurWorldPos);
                        }
                        break;
                    case EditTool.Rotate:
                        {
                            m_OpRotateVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragCurWorldRot, m_handle.dragStartWorldRot);
                            m_SoftSel.Range = newRange;
                            m_OpRotateVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragStartWorldRot, m_handle.dragCurWorldRot);
                        }
                        break;
                    case EditTool.Scale:
                        {
                            //m_OpScaleVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragCurWorldScale, m_handle.dragStartWorldScale/*, true*/);
                            m_SoftSel.Range = newRange;
                            m_OpScaleVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragStartWorldScale, m_handle.dragCurWorldScale, m_handle.pos);
                        }
                        break;
                    case EditTool.None:
                        {
                            m_SoftSel.Range = newRange;
                        }
                        break;
                    default:
                        Dbg.LogErr("MeshManipulator._ChangeSoftSelectionRange: unexpected EditTool: {0}", m_EditTool);
                        break;
                }
            }
            else
            {
                m_SoftSel.Range = newRange;
            }

            _ReqRepaint();
        }
	
	    // "Soft selection" 

        private void _ToggleSoftSelection()
        {
            if (m_SoftSel.CurMode == SoftSelection.Mode.Off)
            {
                _SetSoftSelectionMode(SoftSelection.Mode.Space3D);
            }
            else
            {
                _SetSoftSelectionMode(SoftSelection.Mode.Off);
            }
        }

        private void _SetSoftSelectionMode(SoftSelection.Mode mode)
        {
            if( m_SoftSel.CurMode != mode )
            {
                m_SoftSel.CurMode = mode;
                _ReqRepaint();
            }
        }

	    #endregion "Soft selection"
        

        // find out which vert is nearest to the raycast point
        private int _GetNearestVert(Mesh m, Transform meshTr, RaycastHit hit)
        {
            float minDist;
            return _GetNearestVert(m, meshTr, hit, out minDist);
        }
        private int _GetNearestVert(Mesh m, Transform meshTr, RaycastHit hit, out float minDist)
        {
            Vector3 hitpos = meshTr.InverseTransformPoint(hit.point); //to local-space
            Vector3[] vertices = m.vertices;
            int[] vidxs = MeshUtil.GetTriangleVertIdx(m, hit.triangleIndex);

            int minI = -1;
            minDist = float.MaxValue*0.1f;
            for(int i=0; i<3; ++i)
            {
                Vector3 vertPos = vertices[vidxs[i]];
                float dist = Vector3.Distance(hitpos, vertPos);
                if( dist < minDist )
                {
                    minDist = dist;
                    minI = i;
                }
            }

            return vidxs[minI];
        }

        /// <summary>
        /// find out the nearest VEdge to the contact point
        /// </summary>
        private VEdge _GetNearestVEdge(RaycastHit hit, out float minDist)
        {
            int triIdx = hit.triangleIndex;
            Vector3 pt = hit.point;

            VMesh vmesh = VMesh.Instance;
            VFace vface = vmesh.GetVFaceFromRTri(triIdx, hit);

            List<VVert> vverts = vface.GetVVerts();
            minDist = float.MaxValue * 0.1f;

            ////////////////////////////////////////////////
            // prepare the edges, 
            // we ASSUME that the vverts from vFace is in this order:
            // 012 is the "tri0", 123 is the "tri1"
            ////////////////////////////////////////////////
            List<VEdge> vedges = new List<VEdge>();
            if( vverts.Count == 3) //tri
            {
                vedges.Add(vverts[0].CheckedGetVEdge(vverts[1]));
                vedges.Add(vverts[0].CheckedGetVEdge(vverts[2]));
                vedges.Add(vverts[1].CheckedGetVEdge(vverts[2]));
            }
            else if( vverts.Count == 4) //quad
            {
                vedges.Add(vverts[0].CheckedGetVEdge(vverts[1]));
                vedges.Add(vverts[0].CheckedGetVEdge(vverts[2]));
                vedges.Add(vverts[3].CheckedGetVEdge(vverts[1]));
                vedges.Add(vverts[3].CheckedGetVEdge(vverts[2]));
            }
            else
            {
                Dbg.LogErr("MeshManipulator._GetNearestVEdge: vverts.count = {0}", vverts.Count);
            }

            //check the distance to each edge
            VEdge retEdge = null;
            for( int i=0; i<vedges.Count; ++i)
            {
                VEdge e = vedges[i];
                float d = VMeshUtil.DistPointToVEdge(pt, e);
                if(d < minDist)
                {
                    minDist = d;
                    retEdge = e;
                }
            }

            return retEdge;
        }

        private void _LoopSelect(VEdge vedge, bool resetSel)
        {
            EdgeLoopSelectOp.Op op = EdgeLoopSelectOp.Op.Restart;
            if( !resetSel )
            {
                op = _IsEdgeSelected(vedge) ? EdgeLoopSelectOp.Op.Sub : EdgeLoopSelectOp.Op.Add;
            }

            m_OpEdgeLoopSelect.Execute(vedge, op);
        }

        private bool _IsEdgeSelected(VEdge vedge)
        {
            VVert v0 = vedge.GetVVert(0);
            VVert v1 = vedge.GetVVert(1);

            return m_Selection.IsSelectedVVert(v0) && m_Selection.IsSelectedVVert(v1);            
        }
	
        // select a vert by idx, 
        // if `bNewSelect' is true, then will clear existing selection, 
        // else will toggle the specified vert's selection state
        private void _SelectVert(int rvidx, bool bNewSelect = true)
        {
            if (bNewSelect)
                m_Selection.Clear();

            bool bSelected = m_Selection.IsSelectedVert(rvidx);

            RVLst rvLst = m_VMesh.GetRVertsFromRVert(rvidx); //get all real verts in same virtual vert as 'rvidx'
            if(rvLst.Count > 1)
            {
                if (!bSelected)
                    m_Selection.AddVert(rvLst.ToArray());
                else
                    m_Selection.DelVert(rvLst.ToArray());
            }
            else
            {
                if (!bSelected)
                    m_Selection.AddVert(rvidx);
                else
                    m_Selection.DelVert(rvidx);
            }            
        }

        private void _AddVerts(RVLst vlst)
        {
            m_Selection.AddVert(vlst.ToArray());
        }
        private void _DelVerts(RVLst vlst)
        {
            m_Selection.DelVert(vlst.ToArray());
        }

        private void _ToggleAllSelect()
        {
            if( m_Selection.RVCount > 0 )
            {
                m_Selection.Clear();
            }
            else
            {
                int vcount = m_EditMesh.mesh.vertexCount;
                int[] vidxs = Enumerable.Range(0, vcount).ToArray();
                m_Selection.AddVert(vidxs);
            }
        }

        private void _AddRTri(int triIdx)
        {
            VVert vv0, vv1, vv2;
            m_VMesh.GetVVertsFromRTri(triIdx, out vv0, out vv1, out vv2);
            m_Selection.AddVVert(vv0);
            m_Selection.AddVVert(vv1);
            m_Selection.AddVVert(vv2);
        }

        private void _DelRTri(int triIdx)
        {
            VVert vv0, vv1, vv2;
            m_VMesh.GetVVertsFromRTri(triIdx, out vv0, out vv1, out vv2);
            m_Selection.DelVVert(vv0);
            m_Selection.DelVVert(vv1);
            m_Selection.DelVVert(vv2);
        }

	    #endregion "Selection"

	    #region "Handle"

        private void _ChangeEditTool(EditTool newTool)
        {
            if (newTool == m_EditTool)
                return;
            EditTool oldTool = m_EditTool;
            m_EditTool = newTool;

            evtEditToolChanged(oldTool, newTool); //fire event
            _ReqRepaint();
        }

        private void _ChangeEditMode(EditMode newMode)
        {
            if (newMode == m_EditMode)
                return;
            EditMode oldMode = m_EditMode;
            m_EditMode = newMode;

            _ChangeEditTool(EditTool.None); //ensure editTool is none

            //////////////////////////////////////////////////
            // 1. B-Sel mode, force transparent;
            //////////////////////////////////////////////////
            switch(oldMode)
            {
                case EditMode.BSel:
                    {
                        //_SetTransparent(false);
                    }
                    break;
            }

            switch(newMode)
            {
                case EditMode.BSel:
                    {
                        //_SetTransparent(true);
                    }
                    break;
            }

            if( evtEditModeChanged != null)
                evtEditModeChanged(oldMode, newMode); //fire event
            _ReqRepaint();
        }

        /// <summary>
        /// draw handles and modify mesh
        /// </summary>
        private void _HandleProcess()
        {
            Event e = Event.current;

            bool bPassThrough = false; //if true, then don't interact with handles

            if (e.rawType == EventType.MouseUp )
            {
                if( e.button == 0 )
                {
                    m_handle.handleDragging = false;
                    UndoMesh.SetEnableRecord(m_EditMesh.mesh, true);
                }                
            }
            else if( e.type == EventType.MouseDown )
            {
                if (e.shift || e.control)
                    bPassThrough = true;
            }


            if( ! bPassThrough )
            {
                switch (m_EditTool)
                {
                    case EditTool.Move:
                        {
                            EditorGUI.BeginChangeCheck();
                            Vector3 newPivotPos = Handles.PositionHandle(m_handle.pos, m_handle.rot);
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_handle.handleDragging = true;

                                if (!m_SoftSel.Activated)
                                    m_OpMoveVerts.ExecuteWorld(m_Selection.GetVertices(), m_handle.pos, newPivotPos);
                                else
                                    m_OpMoveVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragStartWorldPos, newPivotPos);

                                // fire event of handle state changed
                                m_handle.dragCurWorldPos = newPivotPos;
                                if (MeshManipulator.evtHandleStateChanged != null)
                                    MeshManipulator.evtHandleStateChanged(m_handle);

                                m_handle.pos = newPivotPos;
                            }
                        }
                        break;
                    case EditTool.Rotate:
                        { //rotate selected verts around pivot
                            EditorGUI.BeginChangeCheck();
                            Quaternion newRot = Handles.RotationHandle(m_handle.rot, m_handle.pos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_handle.handleDragging = true;

                                if (!m_SoftSel.Activated)
                                    m_OpRotateVerts.ExecuteWorld(m_Selection.GetVertices(), m_handle.rot, newRot);
                                else
                                    m_OpRotateVerts.ExecuteWorldSoft(m_SoftSel, m_handle.rot, newRot); //as Quaternion will lose rotation info above 180d, we'll use delta instead

                                m_handle.dragCurWorldRot = newRot;
                                if (MeshManipulator.evtHandleStateChanged != null)
                                    MeshManipulator.evtHandleStateChanged(m_handle);

                                m_handle.rot = newRot;
                            }
                        }
                        break;
                    case EditTool.Scale:
                        {
                            EditorGUI.BeginChangeCheck();
                            Vector3 newScale = Handles.ScaleHandle(m_handle.scale, m_handle.pos, m_handle.rot, HandleUtility.GetHandleSize(m_handle.pos));
                            if (EditorGUI.EndChangeCheck() )
                            {
                                m_handle.handleDragging = true;

                                if (!m_SoftSel.Activated)
                                    m_OpScaleVerts.ExecuteWorld(m_Selection.GetVertices(), m_handle.scale, newScale, m_handle.pos);
                                else
                                    m_OpScaleVerts.ExecuteWorldSoft(m_SoftSel, m_handle.dragStartWorldScale, newScale, m_handle.pos);

                                m_handle.dragCurWorldScale = newScale;
                                if (MeshManipulator.evtHandleStateChanged != null)
                                    MeshManipulator.evtHandleStateChanged(m_handle);

                                m_handle.scale = newScale;
                            }
                        }
                        break;
                    case EditTool.None:
                        {

                        }
                        break;
                    default:
                        Dbg.LogErr("MeshManipulator._HandleProcess: unexpected edit-tool: {0}", m_EditTool);
                        break;
                }//switch(m_EditTool)

                if (m_handle.handleDragging)
                {
                    UndoMesh.SetEnableRecord(m_EditMesh.mesh, false);
                }
            } //if (! bPassThrough )
        }

	    #endregion "Handle"

	    #region "depth buffer"
	    // "depth buffer" 

        /// <summary>
        /// get wrong screen at Unity5
        /// </summary>
        private Texture2D _CreateDepthBuffer_Direct()
        {
            var cam = EUtil.GetSceneView().camera;

            Dbg.Assert(m_RenderDepthShader != null, "_CreateDepthBuffer_Direct: depth shader is null!");
            cam.RenderWithShader(m_RenderDepthShader, string.Empty);

            Texture2D tex = RTUtil.GetScreenTex();

            //byte[] img = tex.EncodeToJPG();
            //System.IO.File.WriteAllBytes("Assets/depthBuffer.jpg", img);
            //Dbg.Log("write to depthBuffer: {0}, {1}x{2}", tex.GetInstanceID(), tex.width, tex.height);

            return tex;
        }

        private Texture2D _CreateDepthBuffer()
        {
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);

            var cam = EUtil.GetSceneView().camera;

            Dbg.Assert(m_RenderDepthShader != null, "_CreateDepthBuffer: depth shader is null!");
            cam.targetTexture = rt;
            cam.RenderWithShader(m_RenderDepthShader, string.Empty);
            cam.targetTexture = null;

            Texture2D tex = RTUtil.GetRTPixels(rt);

            RenderTexture.ReleaseTemporary(rt);

            //byte[] img = tex.EncodeToJPG();
            //System.IO.File.WriteAllBytes("Assets/depthBuffer.jpg", img);
            //Dbg.Log("write to depthBuffer: {0}, {1}x{2}", tex.GetInstanceID(), tex.width, tex.height);


            return tex;
        }
	
	    #endregion "depth buffer"

	    #region "draw markers"
        /// <summary>
        /// draw a circle indicating range
        /// </summary>
        private void _DrawSoftSelectionRange()
        {
            if (m_SoftSel.Activated)
            {
                if( m_handle.handleDragging )
                {
                    Vector3 pos = m_handle.dragStartWorldPos;

                    var cam = EUtil.GetSceneView().camera;
                    Handles.DrawWireDisc(pos, cam.transform.forward, m_SoftSel.Range);
                }                
            }
        }

        /// <summary>
        /// draw 3D cursor
        /// </summary>
        private void _Draw3DCursor()
        {
            if( m_Pivotor.pivotOp == Pivotor.PivotOp.Cursor )
                m_Cursor.DrawCursor();
        }

	    #endregion "draw markers"

	    #region "Shape Keys"

        /// <summary>
        /// export the current selection and all related MorphSO to AssetDatabase and make one prefab
        /// </summary>
        private void _ExportPrefab()
        {
            string meshName = m_EditMesh.mesh.name;
            GameObject meshGO = m_EditMesh.gameObject;

            Directory.CreateDirectory(DEF_PREFABS_PATH);
            string savePrefabPath = EditorUtility.SaveFilePanelInProject("Export Prefab", meshName, "prefab", "Select prefab path", DEF_PREFABS_PATH);
            if (String.IsNullOrEmpty(savePrefabPath))
                return;
            string dir = savePrefabPath.Substring(0, savePrefabPath.Length - 7);
            Directory.CreateDirectory(dir); //the dir to save 
            //Dbg.Log("dir = {0}", dir);

            StringBuilder logs = new StringBuilder();
            logs.AppendLine("----Start Exporting Prefab----");

            string verbose;
            string meshPath = dir + "/" + meshName + ".asset";
            if (!EUtil.SaveMeshToAssetDatabase(m_EditMesh.mesh, meshPath, out verbose))
            {
                logs.AppendFormat("WARN: MeshManipulator._ExportPrefab: failed at saving mesh to: {0}\n", meshPath);
            }
            else
            {
                logs.AppendLine(verbose);
            }

            MorphProc proc = m_EditMesh.gameObject.GetComponent<MorphProc>();
            if( proc != null )
            {
                for(int i=0; i<proc.MorphCount; ++i)
                {
                    ShapeKeyMorphSO morph = proc.GetMorphAt(i);
                    if( !AssetDatabase.Contains(morph) )
                    {
                        string morphPath = string.Format("{0}/{1}_Morph{2}.asset", dir, meshName, i);
                        AssetDatabase.CreateAsset(morph, morphPath);
                        logs.AppendFormat("INFO: saved morph {0} at: {1}\n", i, morphPath);
                    }
                    else
                    {
                        string existPath = AssetDatabase.GetAssetPath(morph);
                        logs.AppendFormat("INFO: morph {0} already at: {1}\n", i, existPath);
                    }
                }
            }

            //Dbg.Log("meshGO path: {0}", AssetDatabase.GetAssetOrScenePath(meshGO));

            if (File.Exists(savePrefabPath))
            {
                GameObject prefabObj = AssetDatabase.LoadAssetAtPath(savePrefabPath, typeof(GameObject)) as GameObject;
                PrefabUtility.ReplacePrefab(meshGO, prefabObj, ReplacePrefabOptions.ConnectToPrefab);
                logs.AppendFormat("INFO: updated prefab at: {0}\n", savePrefabPath);
            }
            else
            {
                PrefabUtility.CreatePrefab(savePrefabPath, meshGO, ReplacePrefabOptions.ConnectToPrefab);
                logs.AppendFormat("INFO: create new prefab at: {0}\n", savePrefabPath);
            }


            logs.AppendLine("----End Exporting Prefab----");
            Dbg.Log(logs.ToString());

            AssetDatabase.SaveAssets();
        }

        private bool _HasMorphProcComponent()
        {
            return m_EditMesh.gameObject.GetComponent<MorphProc>() != null;
        }

        private void _CreateMorphProcComponent()
        {
            if (_HasMorphProcComponent())
                return;

            MorphProc comp = m_EditMesh.gameObject.AddComponent<MorphProc>();
            //comp.AddCurrentMeshAsNewShapeKeyMorph();
            comp.InitWhenCreatedByEditor();

            var basisDeform = comp.GetMorphAt(0);
            basisDeform.name = "Basis";
        }

	
	    #endregion "Shape Keys"

        private void _ReqRepaint()
        {
            EUtil.GetSceneView().Repaint();
            Repaint();
        }

        /// <summary>
        /// check if the event has ctrl / shift / alt pressed
        /// </summary>
        private bool _HasCSA(Event e)
        {
            return ((int)e.modifiers & 7) != 0;
        }


	    #endregion "private methods"

	    #region "debug methods"

        private void DBG_GrabPic()
        {
            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 16); //be sure to set depth buffer, or transparent object will be above opaque objects

            var cam = EUtil.GetSceneView().camera;

            Dbg.Assert(m_RenderDepthShader != null, "NULL shader!");
            cam.targetTexture = rt;
            cam.RenderWithShader(m_RenderDepthShader, "");
            cam.targetTexture = null; //set to null or will report error

            Texture2D tex = RTUtil.GetRTPixels(rt);
            byte[] jpg = tex.EncodeToJPG();

            System.IO.File.WriteAllBytes("Assets/rt.jpg", jpg);
            Dbg.Log("write to Assets/rt.jpg");

            RenderTexture.ReleaseTemporary(rt);
        }

        private void DBG_TestDepthChk()
        {
            Event e = Event.current;

            Camera cam = EUtil.GetSceneView().camera;
            Transform camTr = cam.transform;
            float oldF = cam.farClipPlane;
            cam.farClipPlane = 100f;

            float N = cam.nearClipPlane;
            float F = cam.farClipPlane;
            float FN = N * F;
            float NsF = N - F;
            float CAM_W = cam.pixelWidth;
            float CAM_H = cam.pixelHeight;
            float WOFF = (Screen.width - CAM_W) * 0.5f;
            //float HDIFF = Screen.height - CAM_H;

            float zDist = float.PositiveInfinity; //real z depth
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if( Physics.Raycast(ray, out hit) )
            {
                Vector3 pt = hit.point;
                zDist = camTr.InverseTransformPoint(pt).z;

                Dbg.Assert(m_RenderDepthShader != null, "NULL shader!");
                cam.RenderWithShader(m_RenderDepthShader, "");

                Texture2D tex = RTUtil.GetScreenTex();
                Dbg.Log("CAM_H = {0}, ScreenH = {1}, CAM_W = {2}, ScreenW = {3}, mipcnt = {4}", CAM_H, Screen.height, CAM_W, Screen.width, tex.mipmapCount);

                //Color zBufColor = tex.GetPixels((int)(e.mousePosition.x + 2.5f), (int)(CAM_H - e.mousePosition.y - 0.5f), 1, 1, 0)[0];
                Color zBufColor = tex.GetPixel((int)(e.mousePosition.x + WOFF), (int)(CAM_H-e.mousePosition.y + WOFF));
                float zBufVal = RTUtil.DecodeFloatRGBA(zBufColor);

                float zBufDist = FN / (F + zBufVal * NsF);//IMPORTANT!!! z-depth is linearized based on reciprocal

                Dbg.Log("{0}: zDist: {1:F6}, zBufVal: {2:F6}, zBufDist: {3:F6}, mouse: {4}", zDist <= zBufDist ? "VVV" : "---", 
                    zDist, zBufVal, zBufDist, e.mousePosition);

                DBG_FixAndOutputImg(tex);
                //System.IO.File.WriteAllBytes("Assets/tmp.png", tex.EncodeToPNG());

                Texture2D.DestroyImmediate(tex);
            }

            cam.farClipPlane = oldF;
        }

        private void DBG_FixAndOutputImg(Texture2D tex)
        {
            int found = 0;
            Color[] cs = tex.GetPixels(0);
            for (int i = 0; i < cs.Length; ++i )
            {
                float val = RTUtil.DecodeFloatRGBA(cs[i]);
                if( Mathf.Abs(val - 0.807087f) < 0.01f )
                {
                    ++found;
                    cs[i] = Color.red;
                }
            }
            tex.SetPixels(cs, 0);
            if( found > 100 )
                Dbg.Log("Found {0}", found);

            System.IO.File.WriteAllBytes("Assets/tmp.png", tex.EncodeToPNG());
        }
        
        public void DBG_AddRTri(int rTriIdx)
        {
            _AddRTri(rTriIdx);
        }
	
	    #endregion "debug methods"

	    #region "inner struct"
	    // "inner struct" 

        /// <summary>
        /// utility data-structure for drawing B-Selection
        /// </summary>
        class BSelHelper
        {
            public Vector2 startPos;
            public Vector2 endPos;
            public bool started;

            public void Init()
            {
                Reset();
            }

            public void Fini()
            {

            }

            public void Reset()
            {
                started = false;
                startPos = Vector2.zero;
                endPos = Vector2.zero;
            }
        }

        /// <summary>
        /// utility data-structure for drawing Handles
        /// </summary>
        public class HandleHelper
        {
            // cur states, used to display handle
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scale;

            // dragging start states
            public Vector3 dragStartWorldPos, dragCurWorldPos;
            public Quaternion dragStartWorldRot, dragCurWorldRot;
            public Vector3 dragStartWorldScale, dragCurWorldScale;

            private bool m_HandleDragging;

            private MeshManipulator m_MM;

            public HandleHelper(MeshManipulator mm)
            {
                m_MM = mm;
            }

            public void Init()
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                scale = Vector3.one;

                _ResetDragData();

                m_HandleDragging = false;

                MeshManipulator.evtEditToolChanged += this._OnEditToolChanged;
                m_MM.m_Selection.evtSelectionChanged += this._OnSelectionChanged;
                m_MM.m_Pivotor.evtPivotOpChanged += this._OnPivotOpChanged;
                m_MM.m_Pivotor.evtPivotOrientationChanged += this._OnPivotOrientationChanged;
                //m_MM.m_Pivotor.evtPivotUpdated += this._OnPivotUpdated;
                
                MeshUndoer.evtMeshUndoRedo += this._OnMeshUndoRedo;

                ShapeKeyDataDiff.evtShapeKeyModifyMesh += this._OnShapeKeyModifyMesh;

            }
            public void Fini()
            {
                ShapeKeyDataDiff.evtShapeKeyModifyMesh -= this._OnShapeKeyModifyMesh;

                MeshUndoer.evtMeshUndoRedo -= this._OnMeshUndoRedo;

                //m_MM.m_Pivotor.evtPivotUpdated -= this._OnPivotUpdated;
                m_MM.m_Pivotor.evtPivotOrientationChanged -= this._OnPivotOrientationChanged;
                m_MM.m_Pivotor.evtPivotOpChanged -= this._OnPivotOpChanged;
                m_MM.m_Selection.evtSelectionChanged -= this._OnSelectionChanged;
                MeshManipulator.evtEditToolChanged -= this._OnEditToolChanged;
            }

            public bool handleDragging
            {
                get { return m_HandleDragging; }
                set { 
                    if( value != m_HandleDragging ) 
                    {
                        m_HandleDragging = value;
                        _ResetDragData();
                        if( MeshManipulator.evtHandleDraggingStateChanged != null)
                            MeshManipulator.evtHandleDraggingStateChanged(m_HandleDragging);
                    }
                }
            }

            private void _ResetDragData()
            {
                dragStartWorldPos = dragCurWorldPos = pos;
                dragStartWorldRot = dragCurWorldRot = rot;
                dragStartWorldScale = dragCurWorldScale = scale;
            }

            private void _OnShapeKeyModifyMesh(ShapeKeyMorphSO basisSO)
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnMeshUndoRedo()
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnPivotUpdated()
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnPivotOrientationChanged(Pivotor.Orientation oldOrient, Pivotor.Orientation newOrient)
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnPivotOpChanged(Pivotor.PivotOp oldOp, Pivotor.PivotOp newOp)
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnEditToolChanged(EditTool oldTool, EditTool newTool)
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _OnSelectionChanged()
            {
                _UsePivotValue();
                scale = Vector3.one;
            }

            private void _UsePivotValue()
            {
                rot = m_MM.m_Pivotor.WorldRot;
                pos = m_MM.m_Pivotor.WorldPos;
            }
        }

        
	
	    #endregion "inner struct"

        #region "Constants"
        // "Constants" 

        private const string DEPTH_SHADER_PATH = "Assets/Skele/MeshEditor/Editor/Res/RenderDepth.shader";
        private const string TRANSMAT_PATH = "Assets/Skele/MeshEditor/Editor/Res/MeshEditorTransMat.mat";

        private const int DEFAULT_CONTROL_HASH = 596483147; //arbitrary number
        private const float ZTEST_TMP_FARPLANE = 1000f;

        public enum EditMode
        {
            Normal,
            BSel, //box-selection
            CSel, //circle-selection
            END,
        }

        public enum EditTool
        {
            None,
            Move,
            Rotate,
            Scale,
            END,
        }

        private const float SELECT_DIST_THRES = 0.5f; 
        private const float MIN_SOFTSELECTION_RANGE = 0.001f;

        private const string DEF_PREFABS_PATH = "Assets/Prefabs";

	    #region "EditorPref keys"
	    // "EditorPref keys" 

        private const string Pref_VertSize = "Skele_MeshManipulator_VertSize";
        private const float Def_VertSize = 2.7f;
        private const string Pref_SoftRange = "Skele_MeshManipulator_SoftRange";
        private const float Def_SoftRange = 0.5f;
	
	    #endregion "EditorPref keys"

        #endregion "Constants"

	}
    
}
