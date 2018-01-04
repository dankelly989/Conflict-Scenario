using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    //[InitializeOnLoad]
    public class CommonAttributeProcessor
    {
        //// static ctor
        //static CommonAttributeProcessorEditor()
        //{
        //    if (!EditorPrefs.HasKey(EDITPREF_FirstTime))
        //    {
        //        DoAll();
        //    }
        //    else
        //    {
        //        bool isFirstTime = EditorPrefs.GetBool(EDITPREF_FirstTime);
        //        if (isFirstTime)
        //        {
        //            DoAll();
        //        }
        //    }
        //}
        [MenuItem("Window/Skele/Meta/RefreshAll")]

        public static void RefreshAll()
        {
            RefreshScriptOrder();
            RefreshLayers();
            RefreshTags();
        }

        //[MenuItem("Window/Skele/Meta/Set Layer")]
        public static void RefreshLayers()
        {
            // execute setting tag
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == null)
                    continue;

                foreach (var attr in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(AddLayerAttribute)))
                {
                    var addLayer = (AddLayerAttribute)attr;
                    var layerName = addLayer.layerName;
                    int slotIdx = addLayer.slotIdx;
                    if (slotIdx >= 0)
                    {
                        TagNLayer.AddLayer(slotIdx, layerName);
                    }
                    else
                    {
                        TagNLayer.TryAddLayer(layerName);
                    }
                }
            }

        }

        //[MenuItem("Window/Skele/Meta/Set Tag")]
        public static void RefreshTags()
        {
            // execute setting tag
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == null)
                    continue;

                foreach (var attr in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(AddTagAttribute)))
                {
                    var tagName = ((AddTagAttribute)attr).tagName;
                    TagNLayer.AddTag(tagName);
                }
            }
        }

        // force execute script order setting
        //[MenuItem("Window/Skele/Meta/Set Script Order")]
        public static void RefreshScriptOrder()
        {
            // execute setting order
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == null)
                    continue;

                foreach (var attr in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(ScriptOrderAttribute)))
                {
                    var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                    var newOrder = ((ScriptOrderAttribute)attr).order;
                    if (currentOrder != newOrder) 
                        MonoImporter.SetExecutionOrder(monoScript, newOrder);
                }
            }

        }
    }


}
