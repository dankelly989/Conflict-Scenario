using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    using Object = UnityEngine.Object;

    /// <summary>
    /// this class is used to save/update the AMTake
    /// </summary>
    public class AMTakeSav
    {
        public static void AddObjectToAsset(Object o, Object owner)
        {
#if UNITY_EDITOR
            if (!EditorUtility.IsPersistent(o))
            {
                AssetDatabase.AddObjectToAsset(o, owner);
            }
#endif
        }

    }
}
