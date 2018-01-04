using System;

using UnityEngine;
using UnityEditor;

namespace MH.Skele
{
    public class Pref
    {
		#region "data"
		// "data" 

        private static bool m_isLoaded = false;

        private static float m_IKConMarkerSize = 0.6f;
        public static float IKConMarkerSize
        {
            get { return m_IKConMarkerSize; }
            set { m_IKConMarkerSize = value; }
        }

        private static Color m_IKAngleConstraintArcColor = new Color(1, 1, 1, 0.25f);
        public static UnityEngine.Color IKAngleConstraintArcColor
        {
            get { return m_IKAngleConstraintArcColor; }
            set { m_IKAngleConstraintArcColor = value; }
        }

        private static Color m_IKConeConstraintColor = new Color(1, 0.3f, 0.3f, 0.5f);
        public static UnityEngine.Color IKConeConstraintColor
        {
            get { return m_IKConeConstraintColor; }
            set { m_IKConeConstraintColor = value; }
        }

        private static Color m_IKBoneLinkColor;
        public static UnityEngine.Color IKBoneLinkColor
        {
            get { return m_IKBoneLinkColor; }
            set { m_IKBoneLinkColor = value; }
        }

        private static bool m_showInitInfos;
        public static bool ShowInitInfos
        {
            get { return m_showInitInfos; }
            set { m_showInitInfos = value; }
        }

        private static SelectProd m_selectProduct = SelectProd.None;
        private static bool m_hasCAT = false;
        private static bool m_hasIMMConsole = false;
		
		#endregion "data"

		#region "constants"

        public const string Pref_IKConMarkerSize = "MH.Skele.IKConMarkerSize";
        public const string Pref_IKConArcColor = "MH.Skele.IKConArcColor";
        public const string Pref_IKConeConColor = "MH.Skele.IKConeConColor";
        public const string Pref_IKBoneLinkColor = "MH.Skele.IKBoneLinkColor";
        public const string Pref_ShowInitInfos = "MH.Skele.ShowInitInfos";

        public enum SelectProd 
        {
            CAT,
            ImmConsole,
            None,
        }

        public const string PUBLISHER_URL = "https://www.assetstore.unity3d.com/en/#!/publisher/5325/";
		
		#endregion "constants"

        static Pref()
        {
            if( !m_isLoaded)
                _LoadPrefs();
        }

        public static void SaveAll()
        {
            _SavePrefs();
        }
        
        [PreferenceItem("Skele")]
        public static void PrefGUI()
        {
            if (!m_isLoaded)
                _LoadPrefs();

            EditorGUILayout.BeginHorizontal();
            {
                EUtil.PushGUIEnable(m_selectProduct != SelectProd.CAT);
                if( GUILayout.Button(new GUIContent("Character Animation Tools", "Bone Animation, Vertex animation, animation export, and all other tools"),
                    EditorStyles.toolbarButton))
                {
                    m_selectProduct = SelectProd.CAT;
                }
                EUtil.PopGUIEnable();

                EUtil.PushGUIEnable(m_selectProduct != SelectProd.ImmConsole);
                if( GUILayout.Button(new GUIContent("Immediate Console", "Execute C# snippets immediately in edit-mode & game-mode"),
                    EditorStyles.toolbarButton))
                {
                    m_selectProduct = SelectProd.ImmConsole;
                }
                EUtil.PopGUIEnable();
            }
            EditorGUILayout.EndHorizontal();

            switch (m_selectProduct)
            {
                case SelectProd.CAT:
                    {
                        _GUI_CAT();
                    }
                    break;
                case SelectProd.ImmConsole:
                    {
                        _GUI_ImmConsole();
                    }
                    break;
                case SelectProd.None:
                    {
                        _GUI_None();
                    }
                    break;
                default:
                    Dbg.LogErr("Pref.PrefGUI: unexpected selected product: {0}", m_selectProduct);
                    break;
            }
        }

		#region "private methods"
        private static void _LoadPrefs()
        {
            m_IKConMarkerSize               = LoadPrefFloat(Pref_IKConMarkerSize, 0.6f);
            m_IKAngleConstraintArcColor     = LoadPrefColor(Pref_IKConArcColor, new Color(0.2f, 0.2f, 0.8f, 0.4f));
            m_IKConeConstraintColor         = LoadPrefColor(Pref_IKConeConColor, new Color(0.2f, 0.2f, 0.8f, 1f));
            m_IKBoneLinkColor               = LoadPrefColor(Pref_IKBoneLinkColor, new Color32(255, 31, 173, 255));
            m_showInitInfos                 = LoadPrefBool(Pref_ShowInitInfos, false);

            m_hasCAT = (RCall.GetTypeFromString("MH.SMREditor", true) != null);
            m_hasIMMConsole = (RCall.GetTypeFromString("MH.MonoCSConsole", true) != null);

            m_isLoaded = true;
        }

        private static void _SavePrefs()
        {
            SavePrefFloat(Pref_IKConMarkerSize, m_IKConMarkerSize);
            SavePrefColor(Pref_IKConArcColor, m_IKAngleConstraintArcColor);
            SavePrefColor(Pref_IKConeConColor, m_IKConeConstraintColor);
            SavePrefColor(Pref_IKBoneLinkColor, m_IKBoneLinkColor);
            SavePrefBool(Pref_ShowInitInfos, m_showInitInfos);
        }

        private static void _GUI_None()
        {
            GUILayout.Label("Please select a product first", EditorStyles.largeLabel);
        }

        private static void _GUI_ImmConsole()
        {
            if (!m_hasIMMConsole)
            {
                _URLBtn();
                return;
            }
            else
            {
                GUILayout.Label("No Preference Settings for Immediate Console");
            }
        }

        private static void _GUI_CAT()
        {
            if (!m_hasCAT)
            {
                _URLBtn();
                return;
            }

            EditorGUI.BeginChangeCheck();

            m_IKConMarkerSize = EditorGUILayout.FloatField("IK constraint marker size", m_IKConMarkerSize);
            m_IKAngleConstraintArcColor = EditorGUILayout.ColorField("IK angle constraint arc color", m_IKAngleConstraintArcColor);
            m_IKConeConstraintColor = EditorGUILayout.ColorField("IK cone contraint color", m_IKConeConstraintColor);
            m_IKBoneLinkColor = EditorGUILayout.ColorField("IK bone link color", m_IKBoneLinkColor);
            m_showInitInfos = EditorGUILayout.Toggle("Show Init Infos in editor", m_showInitInfos);

            if (EditorGUI.EndChangeCheck())
            {
                _SavePrefs();
                EUtil.RepaintSceneView();
            }
        }

        private static void _URLBtn()
        {
            if (GUILayout.Button("This product is not installed on this project yet", EditorStyles.largeLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
                Application.OpenURL(PUBLISHER_URL);
        }

		// "private methods" 
		
		#endregion "private methods"

		#region "Load/Save editorPref"
        public static float LoadPrefFloat(string key, float def = 0)
        {
            float v = def;
            if (EditorPrefs.HasKey(key))
            {
                v = EditorPrefs.GetFloat(key);
            }
            else
            {
                EditorPrefs.SetFloat(key, v);
            }
            return v;
        }
        public static int LoadPrefInt(string key, int def = 0)
        {
            int v = def;
            if (EditorPrefs.HasKey(key))
            {
                v = EditorPrefs.GetInt(key);
            }
            else
            {
                EditorPrefs.SetInt(key, v);
            }
            return v;
        }
        public static bool LoadPrefBool(string key, bool def = false)
        {
            bool v = def;
            if (EditorPrefs.HasKey(key))
            {
                v = EditorPrefs.GetBool(key);
            }
            else
            {
                EditorPrefs.SetBool(key, v);
            }
            return v;
        }
        public static string LoadPrefString(string key, string def = "")
        {
            string v = def;
            if (EditorPrefs.HasKey(key))
            {
                v = EditorPrefs.GetString(key);
            }
            else
            {
                EditorPrefs.SetString(key, v);
            }
            return v;
        }
        public static Color LoadPrefColor(string key)
        {
            return LoadPrefColor(key, Color.white);
        }
        public static Color LoadPrefColor(string key, Color def)
        {
            Color v = def;
            if (EditorPrefs.HasKey(key))
            {
                string s = EditorPrefs.GetString(key);
                v = Json.ToObj<Color>(s);
            }
            else
            {
                string s = Json.ToStr(v);
                EditorPrefs.SetString(key, s);
            }
            return v;
        }

        public static void SavePrefFloat(string key, float v)
        {
            EditorPrefs.SetFloat(key, v);
        }
        public static void SavePrefInt(string key, int v)
        {
            EditorPrefs.SetInt(key, v);
        }
        public static void SavePrefBool(string key, bool v)
        {
            EditorPrefs.SetBool(key, v);
        }
        public static void SavePrefString(string key, string v)
        {
            EditorPrefs.SetString(key, v);
        }
        public static void SavePrefColor(string key, Color v)
        {
            string s = Json.ToStr(v);
            EditorPrefs.SetString(key, s);
        }

		#endregion "Load/Save editorPref"

    }
}