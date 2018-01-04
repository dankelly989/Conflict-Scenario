using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    [CustomEditor(typeof(BaseConstraint))]
    public class BaseConstraintEditor : Editor
    {
		#region "configurable data"
	    // configurable data
	
	    #endregion "configurable data"
	
		#region "data"
	    // data
	
	    #endregion "data"
	
		#region "unity event handlers"

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }
	    

	    #endregion "unity event handlers"
	
		#region "public method"
	    // public method
	
	    #endregion "public method"
	
		#region "private method"
	    // private method
	
	    #endregion "private method"
	
		#region "constant data"
	    // constant data
	
	    #endregion "constant data"

    }

    public class ConstraintEditorUtil
    {
        /// <summary>
        /// check if target and its ancestors have non-uniform scale
        /// </summary>
        public static bool IsTargetHasAllUniformScaleInHierarchy(Transform target)
        {
            while( target != null )
            {
                Vector3 s = target.localScale;
                if (!(Mathf.Approximately(s.x, s.y) && Mathf.Approximately(s.y, s.z)))
                    return false;
                target = target.parent;
            }

            return true;
        }

        public static void NonUniformScaleWarning(Transform target)
        {
            EditorGUILayout.HelpBox(string.Format("Found non-uniform scaling in the target '{0}' or its ancestors\n\nPlease avoid Non-uniform scaling, it could lead to error", AnimationUtility.CalculateTransformPath(target, null)), MessageType.Warning);
        }
    }

}
