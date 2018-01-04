using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace VertAnim
{
    /// <summary>
    /// ScriptableObject editor for ShapeKeyMorphSO
    /// 1. edit name, show vcnt, has normal, has tangents
    /// 2. Mesh display
    /// 3. 
    /// </summary>
    [CustomEditor(typeof(ShapeKeyMorphSO))]
    public class ShapeKeyMorphSOEditor : BaseMorphSOEditor
    {
	    #region "configurable data"
        // configurable data

        #endregion "configurable data"

	    #region "data"
        // data

        private SerializedProperty m_propMesh;
        private SerializedProperty m_propName;

        private float m_editingWeight;
        private string m_lastFocusControl;

        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

        void OnEnable()
        {
            m_propMesh = serializedObject.FindProperty("m_Mesh");
            Dbg.Assert(m_propMesh != null, "ShapeKeyMorphSOEditor.OnEnable: failed to find property m_Mesh");

            m_propName = serializedObject.FindProperty("m_Name");
            Dbg.Assert(m_propName != null, "ShapeKeyMorphSOEditor.OnEnable: failed to find property m_Name");
        }

        void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ShapeKeyMorphSO morph = (ShapeKeyMorphSO)serializedObject.targetObject;
            Mesh m = (Mesh)m_propMesh.objectReferenceValue;

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Morph Name:");
                GUILayout.Space(20f);

                string morphName = m_propName.stringValue;
                if (EUtil.StringField("MorphName", ref morphName))
                {
                    m_propName.stringValue = morphName;
                    _ChangeAssetNameIfNeeded();
                }
            }            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.ObjectField(new GUIContent("Working Mesh", "The mesh on which this morph is applied"),
                m, typeof(Mesh), true);

            EditorGUILayout.HelpBox(
                string.Format("vertex: {0},  {1},  {2}", m.vertexCount,
                    morph.HasNormals ? "normals" : "NO normals",
                    morph.HasTangents ? "tangents" : "NO tangents"), 
                MessageType.Info);

            // show each shapeKeyData
            for( int i=0; i<morph.ShapeKeyCnt; ++i)
            {
                _DrawShapeKeyData(morph, i);
            }
 
            // add new shapeKeyData button
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(30f);
                if( GUILayout.Button("Add New Shape Key"))
                {
                    Undo.RecordObject(morph, "ShapeKeyMorphSO inspector");
                    morph.AddNewShapeKeyByMeshCurrentData();
                }
                GUILayout.Space(30f);
            }
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion "unity event handlers"

	    #region "public method"
        // public method

        #endregion "public method"

	    #region "private method"
        // private method

        private void _DrawShapeKeyData(ShapeKeyMorphSO morph, int keyIdx)
        {
            ShapeKeyDataDiff keyData = morph.GetShapeKeyDataDiff(keyIdx);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Weight:", GUILayout.Width(50f) );

                float newWeight = keyData.weight;
                if (EUtil.FloatField("weight" + keyIdx, ref newWeight) ) //only true when use enter to confirm
                {
                    morph.SetShapeKeyWeight(keyIdx, newWeight); //this will ensure all keys are sorted
                }

                if( EUtil.Button(EditorRes.texSample, "Sample current mesh status as shape key", EditorRes.styleBtnMorphProc, GUILayout.Width(20f)) )
                {
                    morph.SetMeshCurrentDataToShapeKey(keyIdx);
                }
                if( EUtil.Button(EditorRes.texApplyToMesh, "Apply this shape key to mesh", EditorRes.styleBtnMorphProc, GUILayout.Width(20f)) )
                {
                    Undo.RecordObject(m_MorphProc, "MorphProc Inspector");
                    m_MorphProc.ResetToBasisShape();
                    m_MorphProc.ApplyOnlyMorphAt(m_MorphIdx, keyData.weight);
                }
                if( EUtil.Button(EditorRes.texDelete, "Delete this shape key from this morph", EditorRes.styleBtnMorphProc, GUILayout.Width(20f)))
                {
                    if( morph.ShapeKeyCnt == 1 )
                    {
                        EditorUtility.DisplayDialog("Only one shape key", "Cannot delete shape key when there's no others", "Got it");
                    }
                    else
                    {
                        morph.DelShapeKeyDataDiff(keyIdx); 
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

        }

        private void _ChangeAssetNameIfNeeded()
        {
            string assetPath = AssetDatabase.GetAssetPath(target);
            if( !string.IsNullOrEmpty(assetPath) )
            {
                string newName = m_propName.stringValue;
                string ret = AssetDatabase.RenameAsset(assetPath, newName);
                if( !string.IsNullOrEmpty(ret) )
                {
                    Dbg.Log("err:" + ret);
                }
            }
        }
	    
        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
    }
}
}
