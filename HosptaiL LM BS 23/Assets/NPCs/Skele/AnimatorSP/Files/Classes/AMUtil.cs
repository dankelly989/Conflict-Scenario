#if UNITY_5
#define U5
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Object = UnityEngine.Object;

namespace MH
{
    public class AMUtil
    {
        //private static bool ms_suppressUndo = false; //only suppress destroyObjImm, which could cause infinite loop in PostProcessModification()

        public static void destroyObjImm(Object o)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying /*&& !ms_suppressUndo */&& o != null)
            {
                Undo.DestroyObjectImmediate(o);
            }
            else
            {
                Object.DestroyImmediate(o);
            }
#else
		    Object.DestroyImmediate(o);	
#endif

        }

        public static void regUndoSelectedTrack(AnimatorData data, string name)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (data.currentTake < 0)
                    return;
                var take = data.getCurrentTake();
                if (take.selectedTrack < 0)
                    return;
                var track = take.getSelectedTrack();
                if (track != null)
                {
                    AMUtil.recordObject(track, name);
                }
            }
#endif
        }

        public static void regUndoSelectedTake(AnimatorData data, string name)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (data.currentTake < 0)
                    return;
                var take = data.getCurrentTake();
                if (take != null)
                {
                    AMUtil.recordObject(take, name);
                }
            }
#endif
        }

//        public static int prepUndoGrp(string name)
//        {
//#if UNITY_EDITOR
//            Undo.IncrementCurrentGroup();
//            int grp = Undo.GetCurrentGroup();
//#if U5
//            Undo.SetCurrentGroupName(name);
//#endif
//            return grp;
//#endif
//        }

        public static void submitUndoGrp(int grp)
        {
#if UNITY_EDITOR
            Undo.CollapseUndoOperations(grp);
#endif
        }

        public static void submitUndoGrp()
        {
#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }


        public static void recordObject(Object o, string name)
        {
#if UNITY_EDITOR
            if( /*!ms_suppressUndo &&*/ !EditorApplication.isPlaying)
                Undo.RecordObject(o, name);
#endif
        }

        //public static void suppressUndo(bool bSuppress)
        //{
        //    ms_suppressUndo = bSuppress;
        //}

        /// <summary>
        /// if obj is under root, return the trPath starting from root;
        /// else return the trPath from sceneRoot, and add "/" before the string
        /// 
        /// if obj is null, return null, root == null can be handled by CalculateTransformPath
        /// </summary>
        public static string CalculateTransformPath(Transform obj, Transform root)
        {
#if UNITY_EDITOR
            if (obj == null)
                return null;

            // calculate transformPath
            bool isAncestor = (root != null ? obj.IsChildOf(root) : false);
            string trPath = (!isAncestor ? "/" : "") + AnimationUtility.CalculateTransformPath(obj, root);
            return trPath;
#else
            return string.Empty;
#endif
        }
    }

    


}
