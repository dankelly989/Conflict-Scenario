using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// used to change the pivot of selected meshFilter
    /// </summary>
	public class MeshPivotFixerEditorWindow : EditorWindow
	{
	    #region "data"
        // data

        private static MeshPivotFixerEditorWindow ms_Instance = null;

        private MeshFilter m_InEditMF = null;

        private EditMode m_EditMode = EditMode.None;
        private EditTool m_EditTool = EditTool.Move;

        private bool m_bRightBtnIsDown = false;

        private bool m_bInplace = false;

        // pos & rot both in local space
        private Vector3 m_PivotPos; 
        private Quaternion m_PivotRot;

        #endregion "data"

	    #region "Unity event methods"
        // public method

        [MenuItem("Window/Skele/Mesh Pivot Fixer")]
        public static void OpenWindow()
        {
            if (ms_Instance == null)
            {
                var inst = ms_Instance = (MeshPivotFixerEditorWindow)GetWindow(typeof(MeshPivotFixerEditorWindow));
                EditorApplication.playmodeStateChanged += inst.OnPlayModeChanged;

                inst.minSize = new Vector2(200, 200);
                inst.Show();
            }  
        }

        // only register when click the "start edit" button
        void OnUpdate()
        {
            if (EditorApplication.isCompiling)
            {
                if (ms_Instance != null)
                    ms_Instance.Close();
                return;
            }

            // check target object is still existed, or shutdown
            if( !m_InEditMF )
            {
                _EnsureStopEditing();
            }
            else
            {
                GameObject curGO = Selection.activeGameObject;
                if (curGO != m_InEditMF.gameObject)
                {
                    Selection.activeGameObject = m_InEditMF.gameObject;
                    EUtil.ShowNotification("GO re-selection is forbidden");
                }
            }
        }

        // only called when inEdit
        void OnSceneGUI(SceneView view)
        {
            UnityEditor.Tools.current = UnityEditor.Tool.None; // clear the default position/rotation/scale handles

            Event e = Event.current;

            _ShortCut_EditMode(e);

            if( m_EditMode == EditMode.Fixer )
            { //control the handle to manipulate the m_PivotPos / m_PivotRot

                switch( m_EditTool )
                {
                    case EditTool.Move:
                        {
                            var tr = m_InEditMF.transform;
                            Vector3 worldPos = tr.TransformPoint(m_PivotPos);

                            Vector3 localDir = m_PivotRot * Vector3.forward;
                            Vector3 worldDir = tr.TransformDirection(localDir);
                            Quaternion worldRot = Quaternion.FromToRotation(Vector3.forward, worldDir);

                            EditorGUI.BeginChangeCheck();
                            Vector3 newWorldPos = Handles.PositionHandle(worldPos, worldRot);
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_PivotPos = tr.InverseTransformPoint(newWorldPos);
                            }
                        }
                        break;
                    case EditTool.Rotate:
                        {
                            var tr = m_InEditMF.transform;
                            Vector3 worldPos = tr.TransformPoint(m_PivotPos);

                            Vector3 localDir = m_PivotRot * Vector3.forward;
                            Vector3 worldDir = tr.TransformDirection(localDir);
                            Quaternion worldRot = Quaternion.FromToRotation(Vector3.forward, worldDir);

                            EditorGUI.BeginChangeCheck();
                            Quaternion newWorldRot = Handles.RotationHandle(worldRot, worldPos);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Vector3 newWorldDir = newWorldRot * Vector3.forward;
                                Vector3 newLocalDir = tr.InverseTransformDirection(newWorldDir);
                                m_PivotRot = Quaternion.FromToRotation(Vector3.forward, newLocalDir);
                            }
                        }
                        break;
                }
                
            }
        }

        // main entry of GUI
        void OnGUI()
        {
            if (m_InEditMF == null)
            { // not started editing
                _OnGUI_NotStarted();
            }
            else
            { // started editing
                _OnGUI_Started();
            }
        }

        private void _OnGUI_Started()
        {
            _OnGUI_Started_Toolbar();

            switch(m_EditMode)
            {
                case EditMode.Fixer:
                    {
                        GUILayout.Space(3f);

                        GUILayout.BeginHorizontal(GUILayout.Height(40f));
                        {
                            GUILayout.Space(10f);
                            if (GUILayout.Button("Reset", GUILayout.ExpandHeight(true)))
                            {
                                _SetTempPivotToZero(); // call repaint
                            }
                            GUILayout.Space(3f);
                            if( GUILayout.Button("Center", GUILayout.ExpandHeight(true)))
                            {
                                m_PivotPos = _GetCenterPoint();
                                EUtil.RepaintSceneView();
                            }                            
                            GUILayout.Space(10f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(10f);

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(10f);
                            if (EUtil.Button("Confirm", "Apply the pivot change to mesh", Color.green, GUILayout.ExpandHeight(true)))
                            {
                                MeshPivotFixer.Apply(m_InEditMF, m_PivotPos, m_PivotRot, m_bInplace);
                                _SetTempPivotToZero();
                                Dbg.Log("Modify Pivot: parameter: {0}, {1}", m_PivotPos, m_PivotRot);
                            }
                            GUILayout.Space(10f);
                        }
                        GUILayout.EndHorizontal();
                        
                        GUILayout.Space(10f);

                        m_bInplace = EditorGUILayout.ToggleLeft("Try Edit In-place (no Undo!)", m_bInplace);

                        GUILayout.Space(10f);
                    }
                    break;
                case EditMode.Saver:
                    {
                        GUILayout.Space(10f);

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(10f);
                            if( GUILayout.Button("Save Mesh", GUILayout.ExpandHeight(true)) )
                            {
                                EUtil.SaveMeshToAssetDatabase(m_InEditMF.sharedMesh);
                            }
                            GUILayout.Space(10f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(10f);
                    }
                    break;
            }
        }

        private void _SetTempPivotToZero()
        {
            m_PivotPos = Vector3.zero;
            m_PivotRot = Quaternion.identity;
            EUtil.RepaintSceneView();
        }

        private void _OnGUI_Started_Toolbar()
        {
            GUILayout.BeginHorizontal();
            {
                EUtil.PushGUIEnable(m_EditMode != EditMode.Fixer);
                if (GUILayout.Button("Fixer", EditorStyles.toolbarButton))
                {
                    _SwitchEditMode(EditMode.Fixer);
                }
                EUtil.PopGUIEnable();

                EUtil.PushGUIEnable(m_EditMode != EditMode.Saver);
                if (GUILayout.Button("Saver", EditorStyles.toolbarButton))
                {
                    _SwitchEditMode(EditMode.Saver);
                }
                EUtil.PopGUIEnable();

                EUtil.PushGUIEnable(m_EditMode != EditMode.None);
                EUtil.PushBackgroundColor(Color.red);
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton))
                {
                    _EnsureStopEditing();
                }
                EUtil.PopBackgroundColor();
                EUtil.PopGUIEnable();
            }
            GUILayout.EndHorizontal();
        }

        private void _OnGUI_NotStarted()
        {
            GameObject curGO = Selection.activeGameObject;
            bool bValid = false;

            if( curGO != null )
            {
                MeshFilter mf = curGO.GetComponent<MeshFilter>();
                bValid = (mf != null);
            }

            Vector2 sz = new Vector2(position.width, position.height);
            EUtil.PushGUIEnable(bValid);
            {
                Rect rc = new Rect(30f, 30f, sz.x - 60f, sz.y - 60f);
                if(GUI.Button(rc,
                    bValid ? "Start Edit" : "Need MeshFilter" 
                    ))
                {
                    _StartEdit();
                }
            }
            EUtil.PopGUIEnable();
        }

        void OnDestroy()
        {
            _EnsureStopEditing();
            ms_Instance = null;
        }

        void OnPlayModeChanged()
        {
            if (ms_Instance != null)
            {
                ms_Instance.Close();
            }
        }

        void OnSelectionChange()
        {
            
            Repaint();
        }

        

        #endregion "Unity event methods"

        #region "private method"
        // private method

        private Vector3 _GetCenterPoint()
        {
            Mesh m = m_InEditMF.sharedMesh;
            Vector3[] vertices = m.vertices;
            if (vertices.Length == 0)
                return Vector3.zero;

            Vector3 v = Vector3.zero;
            float inv = 1f / vertices.Length;
            for(int i=0; i<vertices.Length; ++i)
            {
                v += vertices[i] * inv;
            }

            return v;
        }

        private void _EnsureStopEditing()
        {
            if( m_InEditMF != null )
            {
                _SwitchEditMode(EditMode.None);

                EditorApplication.playmodeStateChanged -= this.OnPlayModeChanged;
                EditorApplication.update -= this.OnUpdate;
                SceneView.onSceneGUIDelegate -= this.OnSceneGUI;

                //// show wireframe
                //{
                //    Renderer ren = m_InEditMF.renderer;
                //    Dbg.Assert(ren != null, "MeshPivotFixerEditorWindow._EnsureStopEditing: there is not renderer on specified GameObject");
                //    EditorUtility.SetSelectedWireframeHidden(ren, false);
                //}

                m_InEditMF = null;
            }
        }

        private void _StartEdit()
        {
            var curGO = Selection.activeGameObject;
            Dbg.Assert(curGO != null, "MeshPivotFixerEditorWindow._StartEdit: no selection GameObject");
            m_InEditMF = curGO.GetComponent<MeshFilter>();
            Dbg.Assert(m_InEditMF != null, "MeshPivotFixerEditorWindow._StartEdit: MeshFilter is not found");

            m_PivotPos = Vector3.zero;
            m_PivotRot = Quaternion.identity;

            _SwitchEditMode(EditMode.Fixer);

            // subscribe onSceneGUI
            {
                SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            }

            //// hide wireframe
            //{
            //    Renderer ren = m_InEditMF.renderer;
            //    Dbg.Assert(ren != null, "MeshPivotFixerEditorWindow._StartEdit: there is not renderer on specified GameObject");
            //    EditorUtility.SetSelectedWireframeHidden(ren, true);
            //}

            // subscribe onUpdate
            EditorApplication.update += this.OnUpdate;

        }

        private void _SwitchEditMode(EditMode mode)
        {
            if( mode != m_EditMode )
            {
                m_EditMode = mode;

                m_PivotRot = Quaternion.identity;
                m_PivotPos = Vector3.zero;

                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private void _ShortCut_EditMode(Event e)
        {
            if (e.rawType == EventType.MouseUp)
            {
                if (e.button == 1)
                    m_bRightBtnIsDown = false;
            }

            if( e.type == EventType.MouseDown )
            {
                if (e.button == 1)
                    m_bRightBtnIsDown = true;
            }

            if (e.type == EventType.KeyUp && !m_bRightBtnIsDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.W:
                        {
                            m_EditTool = EditTool.Move;
                            EUtil.RepaintSceneView();
                        }
                        break;
                    case KeyCode.E:
                        {
                            m_EditTool = EditTool.Rotate;
                            EUtil.RepaintSceneView();
                        }
                        break;
                }
            }
        }



        #endregion "private method"

	    #region "constant data"
        // constant data

        public enum EditMode
        {
            None,
            Fixer,
            Saver,
        }

        public enum EditTool 
        {
            Move,
            Rotate,
        }


        #endregion "constant data"
	}


    /// <summary>
    /// the real worker, extract it out to prepare for integration in other place
    /// </summary>
    public class MeshPivotFixer
    {
        /// <summary>
        /// will make a new mesh, set to the mf.mesh property
        /// </summary>
        public static void Apply(MeshFilter mf, Vector3 posOff, Quaternion rotOff, bool bInplace = false)
        {
            Mesh m = mf.sharedMesh; // get the sharedMesh first, used to retrieve data,
            if (posOff == Vector3.zero && rotOff == Quaternion.identity)
                return;

            // if the mesh is in AssetDatabase, then instantiate a new one, otherwise just modify in-place
            Mesh newMesh = null;

            //string assetPath = AssetDatabase.GetAssetPath(m);
            //bool bInplace = string.IsNullOrEmpty(assetPath);
            if (bInplace)
            { //only save in scene, could edit in-place
                newMesh = m;
            }
            else
            { //is in AssetDatabase, therefore need to instantiate
                newMesh = (Mesh)Mesh.Instantiate(m);
                string strippedName = _StripName(m.name);
                newMesh.name = strippedName + MagicName + UnityEngine.Random.Range(0, RandNameMax);
            }

            Vector3[] vertices = newMesh.vertices;

            Quaternion invRot = Quaternion.Inverse(rotOff);
            Vector3 invPos = -posOff;
            Matrix4x4 matPos = Matrix4x4.TRS(invPos, Quaternion.identity, Vector3.one);
            Matrix4x4 matRot = Matrix4x4.TRS(Vector3.zero, invRot, Vector3.one);
            Matrix4x4 mat = matRot * matPos;

            for(int i=0; i<vertices.Length; ++i)
            {
                vertices[i] = mat.MultiplyPoint3x4(vertices[i]);
            }

            // Undo record
            if (bInplace)
            {
                Undo.RecordObject(mf, "Modify Pivot_I");
                newMesh.vertices = vertices; //set back;
                newMesh.RecalculateBounds();
            }
            else
            {
                Undo.RecordObject(mf, "Modify Pivot");
                newMesh.vertices = vertices; //set back;
                newMesh.RecalculateBounds();
                mf.mesh = newMesh; // create new mesh instance if needed;
            }


        }

        private static string _StripName(string p)
        {
            int lidx = p.LastIndexOf(MagicName);
            if (-1 == lidx)
                return p;
            else
                return p.Substring(0, lidx);
        }

        private const int RandNameMax = 10000000;
        private const string MagicName = "_Pivot";
    }
}
