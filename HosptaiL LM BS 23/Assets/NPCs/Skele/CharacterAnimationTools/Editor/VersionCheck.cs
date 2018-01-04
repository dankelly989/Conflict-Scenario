using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Skele.CAT
{
    [InitializeOnLoad]
    public class VersionCheck 
    {
        public const string CURRENT_VERSION = "1.9.6";

        static VersionCheck()
        {
            string prefVer = EditorPrefs.GetString(PREF_KEY);
            if (prefVer != CURRENT_VERSION)
            {
                CommonAttributeProcessor.RefreshAll();
                EditorPrefs.SetString(PREF_KEY, CURRENT_VERSION);
                Debug.Log("Successfully Updated Settings for Character_Animation_Tools v" + CURRENT_VERSION);
            }
        }

        public static void ClearVerInfo()
        {
            EditorPrefs.DeleteKey(PREF_KEY);
        }

        public const string PREF_KEY = "MH.Skele.CAT.Version";
    }
}
