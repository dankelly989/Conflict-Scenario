using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace VertAnim
{
    public class EditorRes
    {
        public static Texture2D texDetail;
        public static Texture2D texDelete;
        public static Texture2D tex100Per;
        public static GUISkin skinMorphProc;
        public static GUIStyle styleBtnMorphProc;

        public static Texture2D texSample;
        public static Texture2D texAdd;
        public static Texture2D texApplyToMesh;

        static EditorRes()
        { 
            texDetail = AssetDatabase.LoadAssetAtPath(TEX_DETAIL, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texDetail != null, "EditorRes.sctor: failed to load texDetail at: {0}", TEX_DETAIL);

            texDelete = AssetDatabase.LoadAssetAtPath(TEX_DELETE, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texDelete != null, "EditorRes.sctor: failed to load texDelete at: {0}", TEX_DELETE);

            tex100Per = AssetDatabase.LoadAssetAtPath(TEX_APPLY, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texDelete != null, "EditorRes.sctor: failed to load tex100Per at: {0}", TEX_APPLY);

            skinMorphProc = AssetDatabase.LoadAssetAtPath(SKIN_PATH, typeof(GUISkin)) as GUISkin;
            Dbg.Assert(skinMorphProc != null, "EditorRes.sctor: failed to load skinMorphProc at: {0}", SKIN_PATH);

            styleBtnMorphProc = skinMorphProc.button;

            texSample = AssetDatabase.LoadAssetAtPath(TEX_SAMPLE, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texSample != null, "EditorRes.sctor: failed to load texSample at: {0}", TEX_SAMPLE);

            texAdd = AssetDatabase.LoadAssetAtPath(TEX_ADD, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texAdd != null, "EditorRes.sctor: failed to load texAdd at: {0}", TEX_ADD);

            texApplyToMesh = AssetDatabase.LoadAssetAtPath(TEX_APPLYMESH, typeof(Texture2D)) as Texture2D;
            Dbg.Assert(texApplyToMesh != null, "EditorRes.sctor: failed to load texApplyToMesh at: {0}", TEX_APPLYMESH);
        }

        private const string TEX_DETAIL = "Assets/Skele/VertAnimation/Editor/Res/Detail.png";
        private const string TEX_DELETE = "Assets/Skele/VertAnimation/Editor/Res/Delete.png";
        private const string TEX_APPLY = "Assets/Skele/VertAnimation/Editor/Res/100Percent.png";
        private const string SKIN_PATH = "Assets/Skele/VertAnimation/Editor/Res/MorphProc.guiskin";
        private const string TEX_SAMPLE = "Assets/Skele/VertAnimation/Editor/Res/Sample.png";
        private const string TEX_ADD = "Assets/Skele/VertAnimation/Editor/Res/Add.png";
        private const string TEX_APPLYMESH = "Assets/Skele/VertAnimation/Editor/Res/ApplyToMesh.png";
    }
}
}
