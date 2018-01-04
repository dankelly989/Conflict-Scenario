using System;
using System.IO;
using System.Collections.Generic;
using ExtMethods;
using UnityEngine;
using UnityEditor;

namespace MH
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EmptyMarker))]
    public class EmptyMarkerEditor : Editor
    {
        void OnEnable()
        {
            var os = targets;
            foreach (var aObj in os)
            {
                var o = (EmptyMarker)aObj;
                if (o.rd != null)
                {
                    o.rd.sharedMaterial = o.selectedMaterial;
                    EUtil.SetEnableWireframe(o.rd, false);
                    //EditorUtility.SetSelectedWireframeHidden(o.rd, true);
                }
            }

        }

        void OnDisable()
        {
            var os = targets;
            foreach (var aObj in os)
            {
                var o = (EmptyMarker)aObj;
                if (o.rd != null )
                {
                    o.rd.sharedMaterial = o.material;
                }
            }

        }

        public override void OnInspectorGUI()
        {
            EmptyMarker o = (EmptyMarker)target;

            EditorGUI.BeginChangeCheck();
            var newMf = (MeshFilter)EditorGUILayout.ObjectField("MeshFilter", o.mf, typeof(MeshFilter), true);
            if (EditorGUI.EndChangeCheck())
            {
                o.mf = newMf;
                EUtil.SetDirty(o);
            }

            EUtil.PushGUIEnable(o.mf != null);
            {
                EditorGUI.BeginChangeCheck();
                var newMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", o.mesh, typeof(Mesh), false);
                if (EditorGUI.EndChangeCheck())
                {
                    o.mesh = newMesh;
                    EUtil.SetDirty(o);
                }

                EditorGUI.BeginChangeCheck();
                var newMat = (Material)EditorGUILayout.ObjectField("Material", o.material, typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    o.material = newMat;
                    EUtil.SetDirty(o);
                }

                EditorGUI.BeginChangeCheck();
                var newSelMat = (Material)EditorGUILayout.ObjectField("Selected Material", o.selectedMaterial, typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    o.selectedMaterial = newSelMat;
                    EUtil.SetDirty(o);
                }
            }
            EUtil.PopGUIEnable();

            EditorGUI.BeginChangeCheck();
            o.jumpTo = (Transform)EditorGUILayout.ObjectField("Jump To", o.jumpTo, typeof(Transform), true);
            if(EditorGUI.EndChangeCheck())
            {
                EUtil.SetDirty(o);
            }

            // create "mesh" child object to hold marker
            EditorGUILayout.BeginHorizontal();
            {
                Rect rc = GUILayoutUtility.GetRect(new GUIContent("Presets"), GUI.skin.button);
                if (GUI.Button(rc, new GUIContent("Presets", "select presets marker")))
                {
                    PopupWindow.Show(rc, new EmptyMarkerPresetsPopup(o));
                }

                if (GUILayout.Button(new GUIContent("Delete", "delete marker")))
                {
                    if (o.mf != null)
                        MUndo.DestroyObj(o.mf.gameObject);

                    MUndo.DestroyObj(o);

                    EditorGUIUtility.ExitGUI();
                }

                if( o.jumpTo != null )
                {
                    if(GUILayout.Button(new GUIContent("Target", "jump to the target transform")))
                    {
                        Selection.activeTransform = o.jumpTo;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public class EmptyMarkerPresetsPopup : PopupWindowContent
    {
        private EmptyMarker m_marker;
        private Material m_defMat;
        private Material m_defSelMat;
        private List<string> m_names = new List<string>();
        private List<Mesh> m_meshes = new List<Mesh>();

        private Vector2 m_scroll;

        public EmptyMarkerPresetsPopup(EmptyMarker o)
        {
            m_marker = o;

            m_defMat = (Material)AssetDatabase.LoadAssetAtPath(DefaultMaterial, typeof(Material));
            Dbg.Assert(m_defMat != null, "EmptyMarkerPresetsPopup.ctor: failed to get defMat at {0}", DefaultMaterial);
            m_defSelMat = (Material)AssetDatabase.LoadAssetAtPath(DefaultSelectedMaterial, typeof(Material));
            Dbg.Assert(m_defSelMat != null, "EmptyMarkerPresetsPopup.ctor: failed to get defSelMat at {0}", DefaultSelectedMaterial);

            var guids = AssetDatabase.FindAssets("t:Mesh", ModelsPath);
            foreach (var id in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                string name = Path.GetFileNameWithoutExtension(path);
                Mesh m = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
                Dbg.Assert(m != null, "EmptyMarkerPresetsPopup.ctor: failed to get mesh at: {0}", path);

                m_names.Add(name);
                m_meshes.Add(m);
            }
        }

        public override void OnGUI(Rect rc)
        {
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            {
                for (int i = 0; i < m_names.Count; ++i)
                {
                    if (GUILayout.Button(m_names[i], EditorStyles.toolbarButton))
                    {
                        var tr = m_marker.transform;
                        var childTr = Misc.ForceGetChildTr(tr, "mesh");
                        childTr.localPosition = Vector3.zero;
                        childTr.localRotation = Quaternion.identity;
                        childTr.localScale = Vector3.one;
                        var meshFilter = childTr.ForceGetComponent<MeshFilter>();
                        var rder = childTr.ForceGetComponent<MeshRenderer>(); //ensure renderer

                        m_marker.mf = meshFilter;

                        if (m_marker.material == null) m_marker.material = m_defMat;
                        if (m_marker.selectedMaterial == null) m_marker.selectedMaterial = m_defSelMat;
                        else rder.sharedMaterial = m_marker.selectedMaterial;

                        m_marker.mesh = m_meshes[i];

                        editorWindow.Close();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static readonly string[] ModelsPath = {"Assets/Skele/Common/Res/Marker/Models"};
        private const string DefaultMaterial = "Assets/Skele/Common/Res/Marker/Materials/DefaultMarker.mat";
        private const string DefaultSelectedMaterial = "Assets/Skele/Common/Res/Marker/Materials/DefaultSelectedMarker.mat";
    }
}
