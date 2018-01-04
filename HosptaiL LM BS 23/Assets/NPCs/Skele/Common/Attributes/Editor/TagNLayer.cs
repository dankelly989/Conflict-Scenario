using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    /// <summary>
    /// used to modify the Unity's tag & layer settings programmatically
    /// </summary>
    public class TagNLayer 
    {
        public static void AddTag(string tag)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty tags = so.FindProperty("tags");
                for (int i = 0; i < tags.arraySize; ++i)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        return; // Tag already present, nothing to do.
                    }
                }
                tags.InsertArrayElementAtIndex(0);
                tags.GetArrayElementAtIndex(0).stringValue = tag;
                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        public static void AddLayer(int layerIdx, string name)
        {
            if (layerIdx < 8 || layerIdx > 31)
            {
                Dbg.LogErr("TagNLayer.AddLayer: unexpected layerIdx: {0}", layerIdx);
                return;
            }

            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty layers = so.FindProperty("layers");
                for (int i = 0; i < layers.arraySize; ++i)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue == name)
                    {
                        return; // layer already present, nothing to do.
                    }
                }

                SerializedProperty sp = layers.GetArrayElementAtIndex(layerIdx);
                sp.stringValue = name;

                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        public static bool HasLayerAt(int layerIdx)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty layers = so.FindProperty("layers");
                SerializedProperty sp = layers.GetArrayElementAtIndex(layerIdx);
                return !string.IsNullOrEmpty(sp.stringValue);
            }
            else
            {
                Dbg.LogErr("TagNLayer.HasLayerAt: failed to access TagManager.asset");
                return false;
            }
        }

        /// <summary>
        /// try best to find empty slot to put layer in
        /// </summary>
        public static bool TryAddLayer(string layerName)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty layers = so.FindProperty("layers");
                for (int i = 0; i < layers.arraySize; ++i)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    {
                        return true; // layer already present, nothing to do.
                    }
                }

                for (int i = 8; i < 32; ++i)
                {
                    SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(sp.stringValue))
                    {
                        sp.stringValue = layerName;
                        so.ApplyModifiedProperties();
                        so.Update();
                        return true;
                    }
                }

                Dbg.LogWarn("TagNLayer.TryAddLayer: all layers are occupied already");
                return false;
            }
            else
            {
                Dbg.LogErr("TagNLayer.TryAddLayer: failed to access TagManager.asset");
                return false;
            }
        }
    }
}
