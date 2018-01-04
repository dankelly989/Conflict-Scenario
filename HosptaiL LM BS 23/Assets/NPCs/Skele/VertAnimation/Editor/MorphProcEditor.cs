using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace VertAnim
{
    //using Deforms = System.Collections.Generic.List<ShapeKeyMorphSO>;

    [CustomEditor(typeof(MorphProc))]
    public class MorphProcEditor : Editor
    {
	    #region "data"
        // data

        private SerializedProperty m_propDeforms;
        private SerializedProperty[] m_propAnimWeights;
        private SerializedProperty m_propUseMeshCache;
        private SerializedProperty m_propEditableMesh;
        private SerializedProperty m_propIndependentMesh;

        private static Texture2D m_texDelete;
        private static Texture2D m_texApply;
        private static Texture2D m_texDetail;
        private static GUIStyle m_styleBtn;

        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

	    void OnEnable()
	    {
            _InitStaticTexRes();

            m_propDeforms = serializedObject.FindProperty("m_Deforms");
            Dbg.Assert(m_propDeforms != null, "MorphProcEditor.OnEnable: failed to get m_Deforms");

            m_propUseMeshCache = serializedObject.FindProperty("m_UseMeshCache");
            Dbg.Assert(m_propUseMeshCache != null, "MorphProcEditor.OnEnable: failed to get m_UseMeshCache");

            m_propEditableMesh = serializedObject.FindProperty("m_Mesh");
            Dbg.Assert(m_propEditableMesh != null, "MorphProcEditor.OnEnable: failed to get m_Mesh");

            m_propIndependentMesh = m_propEditableMesh.FindPropertyRelative("m_independentMesh");
            Dbg.Assert(m_propIndependentMesh != null, "MorphProcEditor.OnEnable: failed to get m_independentMesh");

            m_propAnimWeights = new SerializedProperty[MorphProc.MAX_SHAPEKEY_CNT];
            for (int i = 0; i < MorphProc.MAX_SHAPEKEY_CNT; ++i )
            {
                string propName = "m_Weight"+i;
                m_propAnimWeights[i] = serializedObject.FindProperty(propName);
                Dbg.Assert(m_propAnimWeights[i] != null, "MorphProcEditor.OnEnable: failed to get {0}", propName);
            }
        }

	    public override void OnInspectorGUI()
	    {
            serializedObject.Update();

            MorphProc proc = (MorphProc)serializedObject.targetObject;

            EditorGUILayout.ObjectField("Mesh", proc.GetMesh().mesh, typeof(Mesh), true);
            
            m_propIndependentMesh.boolValue = EditorGUILayout.Toggle(new GUIContent("Indie Animation",
                    "by default, all instances of one animated prefab will be animated in sync;\nIf you need to make this object animated independently, check this option"),
                    m_propIndependentMesh.boolValue);
            
            m_propUseMeshCache.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Use MeshCache",
                    "This is a very important performance optimization, check this option as long as you don't manipulate this mesh yourself"),
                    m_propUseMeshCache.boolValue);

            if (proc.MorphCount > 0)
            {
                _DrawDeformEntry(proc, 0, true);

                for (int deformIdx = 1; deformIdx < m_propDeforms.arraySize; ++deformIdx)
                {
                    _DrawDeformEntry(proc, deformIdx, false);
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(30f);
                    if (GUILayout.Button("New Deform"))
                    {
                        if (proc.MorphCount >= MorphProc.MAX_SHAPEKEY_CNT)
                        {
                            EUtil.ShowNotification("Reached Max Deform Count");
                        }
                        else
                        {
                            proc.AddCurrentMeshAsNewShapeKeyMorph();
                        }
                    }
                    GUILayout.Space(30f);
                }
                GUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion "unity event handlers"

	    #region "public method"
        // public method

        #endregion "public method"

	    #region "private method"

        private void _DrawDeformEntry(MorphProc proc, int deformIdx, bool isBasis = true)
        {
            ShapeKeyMorphSO deform = (ShapeKeyMorphSO)m_propDeforms.GetArrayElementAtIndex(deformIdx).objectReferenceValue;
            EditorGUILayout.BeginHorizontal();
            {
                float newWeight = 0f;

                // weight
                if (isBasis)
                    EUtil.PushGUIEnable(false);

                EditorGUI.BeginChangeCheck();
                newWeight = EditorGUILayout.FloatField(deform.name, m_propAnimWeights[deformIdx].floatValue);
                if( EditorGUI.EndChangeCheck() )
                    m_propAnimWeights[deformIdx].floatValue = Mathf.Clamp(newWeight, 0, 100f);

                if (isBasis)
                    EUtil.PopGUIEnable();

                // after weight
                if( isBasis )
                { //the basis
                    GUILayout.Space(20f);
                    if (EUtil.Button("R", "Reset as Basis", Color.green, GUILayout.Width(42f)))
                    {
                        Undo.RecordObject(proc, "MorphProc Inspector");
                        proc.ResetToBasisShape();
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(m_texDetail, "Edit details of this deform"), m_styleBtn, GUILayout.Width(20f)))
                    {
                        var e = (BaseMorphSOEditor)Editor.CreateEditor(deform);
                        e.m_MorphProcEditor = this;
                        e.m_MorphProc = proc;
                        e.m_MorphIdx = deformIdx;
                        EditorEditorWindow.OpenWindowWithEditor(e);
                    }

                    if (GUILayout.Button(new GUIContent(m_texApply, "Apply only this deform 100%"), m_styleBtn, GUILayout.Width(20f)))
                    {
                        Undo.RecordObject(proc, "MorphProc Inspector");
                        proc.ApplyOnlyMorphAt(deformIdx);
                    }

                    if (GUILayout.Button(new GUIContent(m_texDelete, "Delete this deform"), m_styleBtn, GUILayout.Width(20f)))
                    {
                        if( EditorUtility.DisplayDialog("To be or not to be", "Are you sure to delete this Morph?", "Go Ahead", "No No No"))
                        {
                            Undo.RecordObject(proc, "MorphProc Inspector");
                            proc.RemoveShapeKeyMorphAt(deformIdx);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void _InitStaticTexRes()
        {
 	        if( m_texApply == null )
            {
                m_texApply = MH.VertAnim.EditorRes.tex100Per;
                m_texDelete = MH.VertAnim.EditorRes.texDelete;
                m_texDetail = MH.VertAnim.EditorRes.texDetail;
                m_styleBtn = MH.VertAnim.EditorRes.styleBtnMorphProc;
            }
        }

        #endregion "private method"

	    #region "constant data"
        // constant data



        #endregion "constant data"
    }

}
}
