#if UNITY_5
#define U5
#endif

#if UNITY_5 && (UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER )
#define U5_1_ABOVE
#endif

#define HAS_RCALL

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Linq;

using Object = UnityEngine.Object;

#if U5
using AnimatorControllerParameter = UnityEngine.AnimatorControllerParameter;
#else
using AnimatorControllerParameter = AnimatorControllerParameter;
#endif

#if UNITY_5_3_OR_NEWER || UNITY_5_3
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor Utility
/// </summary>
namespace MH
{
    using EditorMap = Dictionary<Object, Editor>;

    public class EUtil
    {
        public static Stack<Color> ms_clrStack = new Stack<Color>();
        public static Stack<Color> ms_contentClrStack = new Stack<Color>();
        public static Stack<bool> ms_enableStack = new Stack<bool>();
        public static Stack<Color> ms_BackgroundClrStack = new Stack<Color>();
        public static Stack<float> ms_LabelWidthStack = new Stack<float>();
        public static Stack<float> ms_FieldWidthStack = new Stack<float>();
        public static Stack<Color> ms_handleColorStack = new Stack<Color>();
        public static Stack<Color> ms_handleSelColorStack = new Stack<Color>();

        private static double ms_notificationHideTime = double.MinValue;

        private static string ms_lastFocusControl = string.Empty;

        private static EditorMap ms_editorMap;

        private static GUIStyle ms_splitterStyle;

        static EUtil()
        {
            ms_editorMap = new EditorMap();

            ms_splitterStyle = new GUIStyle();
            ms_splitterStyle.normal.background = EditorGUIUtility.whiteTexture;
            ms_splitterStyle.stretchWidth = true;
            ms_splitterStyle.margin = new RectOffset(0, 0, 7, 2);
        }

        #region "controls"
        // "controls" 

        public static void PushLabelWidth(float w)
        {
            ms_LabelWidthStack.Push(EditorGUIUtility.labelWidth);
            EditorGUIUtility.labelWidth = w;
        }

        public static float PopLabelWidth()
        {
            float w = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = ms_LabelWidthStack.Pop();
            return w;
        }

        public static void PushFieldWidth(float w)
        {
            ms_FieldWidthStack.Push(EditorGUIUtility.fieldWidth);
            EditorGUIUtility.fieldWidth = w;
        }

        public static float PopFieldWidth()
        {
            float w = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.fieldWidth = ms_FieldWidthStack.Pop();
            return w;
        }

        public static void PushGUIColor(Color newClr)
        {
            ms_clrStack.Push(GUI.color);
            GUI.color = newClr;
        }

        public static Color PopGUIColor()
        {
            Color r = GUI.color;
            GUI.color = ms_clrStack.Pop();
            return r;
        }

        public static void PushBackgroundColor(Color newClr)
        {
            ms_BackgroundClrStack.Push(GUI.backgroundColor);
            GUI.backgroundColor = newClr;
        }

        public static Color PopBackgroundColor()
        {
            Color r = GUI.backgroundColor;
            GUI.backgroundColor = ms_BackgroundClrStack.Pop();
            return r;
        }

        public static void PushContentColor(Color clr)
        {
            ms_contentClrStack.Push(GUI.contentColor);
            GUI.contentColor = clr;
        }

        public static Color PopContentColor()
        {
            Color r = GUI.contentColor;
            GUI.contentColor = ms_contentClrStack.Pop();
            return r;
        }

        public static void PushGUIEnable(bool newState)
        {
            ms_enableStack.Push(GUI.enabled);
            GUI.enabled = newState;
        }

        public static bool PopGUIEnable()
        {
            bool r = GUI.enabled;
            GUI.enabled = ms_enableStack.Pop();
            return r;
        }

        public static void PushHandleColor(Color c)
        {
            ms_handleColorStack.Push(Handles.color);
            Handles.color = c;
        }

        public static Color PopHandleColor()
        {
            Color r = Handles.color;
            Handles.color = ms_handleColorStack.Pop();
            return r;
        }

        public static void PushHandleSelColor(Color c)
        {
            ms_handleSelColorStack.Push(Handles.color);
            Handles.color = c;
        }

        public static Color PopHandleSelColor()
        {
            Color r = Handles.color;
            Handles.color = ms_handleSelColorStack.Pop();
            return r;
        }

        public static bool Button(string msg, Color c)
        {
            EUtil.PushBackgroundColor(c);
            bool bClick = GUILayout.Button(msg);
            EUtil.PopBackgroundColor();
            return bClick;
        }

        public static bool Button(string msg, string tips)
        {
            bool bClick = GUILayout.Button(new GUIContent(msg, tips));
            return bClick;
        }

        public static bool Button(string msg, string tips, Color c)
        {
            EUtil.PushBackgroundColor(c);
            bool bClick = GUILayout.Button(new GUIContent(msg, tips));
            EUtil.PopBackgroundColor();
            return bClick;
        }

        public static bool Button(string msg, string tips, Color c, params GUILayoutOption[] options)
        {
            EUtil.PushBackgroundColor(c);
            bool bClick = GUILayout.Button(new GUIContent(msg, tips), options);
            EUtil.PopBackgroundColor();
            return bClick;
        }

        public static bool Button(Texture2D tex, string tips, params GUILayoutOption[] options)
        {
            bool bClick = GUILayout.Button(new GUIContent(tex, tips), options);
            return bClick;
        }

        public static bool Button(Texture2D tex, string tips, GUIStyle style, params GUILayoutOption[] options)
        {
            bool bClick = GUILayout.Button(new GUIContent(tex, tips), style, options);
            return bClick;
        }

        public static bool Button(Texture2D tex, string tips, Color c, params GUILayoutOption[] options)
        {
            EUtil.PushBackgroundColor(c);
            bool bClick = GUILayout.Button(new GUIContent(tex, tips), options);
            EUtil.PopBackgroundColor();
            return bClick;
        }

        public static bool Button(Texture2D tex, string tips, Color c, GUIStyle style, params GUILayoutOption[] options)
        {
            EUtil.PushBackgroundColor(c);
            bool bClick = GUILayout.Button(new GUIContent(tex, tips), style, options);
            EUtil.PopBackgroundColor();
            return bClick;
        }

        /// <summary>
        /// this IntField will only affect input value when enter is pressed
        /// when enter is pressed, the focus is lost
        /// 
        /// return value indicates whether confirmed
        /// </summary>
        private static int ms_editingInt = 0;
        public static bool IntField(string name, ref int val)
        {
            return IntField(name, ref val, null);
        }
        
        public static bool IntField(string name, ref int val, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(name);

            if (GUI.GetNameOfFocusedControl() != name)
            {
                EditorGUILayout.IntField(val, options);
                return false;
            }

            /////////////////////////////
            // drawing focusing field
            /////////////////////////////

            if (ms_lastFocusControl != name)
            { //when just change focus control, put value into editingWeight
                ms_lastFocusControl = name;
                ms_editingFloat = val;
            }

            bool applying = false;

            if (Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        val = ms_editingInt;
                        applying = true;
                        GUI.FocusControl(string.Empty); //lose focus
                        Event.current.Use();    // Ignore event, otherwise there will be control name conflicts!
                        break;
                }
            }

            ms_editingInt = EditorGUILayout.IntField(ms_editingInt, options);
            return applying;
        }

        public static bool IntField(string name, GUIContent label, ref int val, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(name);

            if (GUI.GetNameOfFocusedControl() != name)
            {
                EditorGUILayout.IntField(label, val, options);
                return false;
            }

            /////////////////////////////
            // drawing focusing field
            /////////////////////////////

            if (ms_lastFocusControl != name)
            { //when just change focus control, put value into editingWeight
                ms_lastFocusControl = name;
                ms_editingFloat = val;
            }

            bool applying = false;

            if (Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        val = ms_editingInt;
                        applying = true;
                        GUI.FocusControl(string.Empty); //lose focus
                        Event.current.Use();    // Ignore event, otherwise there will be control name conflicts!
                        break;
                }
            }

            ms_editingInt = EditorGUILayout.IntField(label, ms_editingInt, options);
            return applying;
        }

        /// <summary>
        /// this FloatField will only affect input value when enter is pressed
        /// when enter is pressed, the focus is lost
        /// 
        /// return value indicates whether confirmed
        /// </summary>
        private static float ms_editingFloat = 0;
        public static bool FloatField(string name, ref float val)
        {
            return FloatField(name, ref val, null);
        }
        public static bool FloatField(string name, ref float val, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(name);

            if (GUI.GetNameOfFocusedControl() != name)
            {
                EditorGUILayout.FloatField(val, options);
                return false;
            }

            /////////////////////////////
            // drawing focusing field
            /////////////////////////////

            if (ms_lastFocusControl != name)
            { //when just change focus control, put value into editingWeight
                ms_lastFocusControl = name;
                ms_editingFloat = val;
            }

            bool applying = false;

            if (Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        val = ms_editingFloat;
                        applying = true;
                        GUI.FocusControl(string.Empty); //lose focus
                        Event.current.Use();    // Ignore event, otherwise there will be control name conflicts!
                        break;
                }
            }

            ms_editingFloat = EditorGUILayout.FloatField(ms_editingFloat, options);
            return applying;
        }

        private static string ms_editingString = string.Empty;
        public static bool StringField(string name, ref string val)
        {
            return StringField(name, ref val, null);
        }
        public static bool StringField(string name, ref string val, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(name);

            if (GUI.GetNameOfFocusedControl() != name)
            {
                EditorGUILayout.TextField(val, options);
                return false;
            }

            /////////////////////////////
            // drawing focusing field
            /////////////////////////////

            if (ms_lastFocusControl != name)
            { //when just change focus control, put value into editingWeight
                ms_lastFocusControl = name;
                ms_editingString = val;
            }

            bool applying = false;

            if (Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        val = ms_editingString;
                        applying = true;
                        GUI.FocusControl(string.Empty); //lose focus
                        Event.current.Use();    // Ignore event, otherwise there will be control name conflicts!
                        break;
                }
            }

            ms_editingString = EditorGUILayout.TextField(ms_editingString, options);
            return applying;
        }

        /// <summary>
        /// draw a popup to select AnimatorParameters
        /// </summary>
        public static string AnimatorParamPopup(Rect rc, Animator ator, string curParam)
        {
            var lst = new List<string>();
            EUtil.GetAllParameterNames(lst, ator);
            var names = lst.ToArray();

            int idx = Mathf.Max(0, Array.IndexOf(names, curParam));
            
            idx = EditorGUI.Popup(rc, "Param", idx, names);
            return names.Length == 0 ? string.Empty : names[idx];
        }
        public static string AnimatorParamPopup(Animator ator, string curParam)
        {
            var lst = new List<string>();
            EUtil.GetAllParameterNames(lst, ator);
            var names = lst.ToArray();

            int idx = Mathf.Max(0, Array.IndexOf(names, curParam));

            idx = EditorGUILayout.Popup("Param", idx, names);
            return names.Length == 0 ? string.Empty : names[idx];
        }

        /// <summary>
        /// draw a popup to select Animator states
        /// </summary>
        public static string AnimatorStatePopup(Rect rc, GUIContent label, Animator ator, string curParam)
        {
            var allStates = new List<string>();
            EUtil.GetAllStateNames(allStates, ator);
            var arr = allStates.Select( x=>new GUIContent(x)).ToArray();

            int idx = Mathf.Max(0, allStates.IndexOf(curParam));

            idx = EditorGUI.Popup(rc, label, idx, arr);
            return arr.Length == 0 ? string.Empty : allStates[idx];
        }
        public static string AnimatorStatePopup(GUIContent label, Animator ator, string curParam)
        {
            var allStates = new List<string>();
            EUtil.GetAllStateNames(allStates, ator);
            var arr = allStates.Select(x => new GUIContent(x)).ToArray();

            int idx = Mathf.Max(0, allStates.IndexOf(curParam));

            idx = EditorGUILayout.Popup(label, idx, arr);
            return arr.Length == 0 ? string.Empty : allStates[idx];
        }

        public static float ProgressBar(float v, float leftValue, float rightValue, string fmtstr = "{0:F2}")
        {
            Rect rc = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField);

            return ProgressBar(rc, v, leftValue, rightValue, fmtstr);
        }

        public static float ProgressBar(Rect rc, float v, float leftValue, float rightValue, string fmtstr = "{0:F2}")
        {
            v = GUI.HorizontalSlider(rc, v, leftValue, rightValue);
            EditorGUI.ProgressBar(rc, v, string.Format(fmtstr, v));

            return v;
        }

        public static void ShowSprite(Sprite spr)
        {
            Rect sprRc = spr.rect;
            ShowSprite(spr, sprRc.width, sprRc.height);
        }
        public static void ShowSprite(Sprite spr, float w, float h)
        {
            Texture2D tex = spr.texture;
            Rect sprRc = spr.rect;
            Rect rc = GUILayoutUtility.GetRect(w, w, h, h);
            rc.width = w;
            GUI.DrawTextureWithTexCoords(rc, tex, new Rect(sprRc.x / tex.width, sprRc.y / tex.height, sprRc.width / tex.width, sprRc.height / tex.height));
        }
        public static void ShowSprite(Rect rc, Sprite spr)
        {
            if (spr == null)
                return;
            Texture2D tex = spr.texture;
            Rect sprRc = spr.rect;
            GUI.DrawTextureWithTexCoords(rc, tex, new Rect(sprRc.x / tex.width, sprRc.y / tex.height, sprRc.width / tex.width, sprRc.height / tex.height));
        }

        public static Vector2 DrawV2(Vector2 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                v.x = EditorGUILayout.FloatField("X", v.x, o);
                v.y = EditorGUILayout.FloatField("Y", v.y, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }
        public static Vector2 DrawV2(string label, Vector2 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(80f));
                v.x = EditorGUILayout.FloatField("X", v.x, o);
                v.y = EditorGUILayout.FloatField("Y", v.y, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }
        public static Int2 DrawV2(string label, Int2 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(80f));
                v.x = EditorGUILayout.IntField("X", v.x, o);
                v.y = EditorGUILayout.IntField("Y", v.y, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }

        public static Vector3 DrawV3(Vector3 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                v.x = EditorGUILayout.FloatField("X", v.x, o);
                v.y = EditorGUILayout.FloatField("Y", v.y, o);
                v.z = EditorGUILayout.FloatField("Z", v.z, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }
        public static Vector3 DrawV3(string label, Vector3 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(80f));
                v.x = EditorGUILayout.FloatField("X", v.x, o);
                v.y = EditorGUILayout.FloatField("Y", v.y, o);
                v.z = EditorGUILayout.FloatField("Z", v.z, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }
        public static Int3 DrawV3(string label, Int3 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(80f));
                v.x = EditorGUILayout.IntField("X", v.x, o);
                v.y = EditorGUILayout.IntField("Y", v.y, o);
                v.z = EditorGUILayout.IntField("Z", v.z, o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }
        public static Vector3 DrawV3P(GUIContent label, Vector3 v)
        {
            var o = GUILayout.MinWidth(30f);
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 16f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(60f));
                v.x = EditorGUILayout.FloatField("X", (float)Math.Round(v.x, 3), o);
                v.y = EditorGUILayout.FloatField("Y", (float)Math.Round(v.y, 3), o);
                v.z = EditorGUILayout.FloatField("Z", (float)Math.Round(v.z, 3), o);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldWidth;

            return v;
        }

        public static void DrawMinMaxSlider(string label, ref float min, ref float max, float minLimit, float maxLimit)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(label);
                min = EditorGUILayout.FloatField(min);
                EditorGUILayout.MinMaxSlider(ref min, ref max, minLimit, maxLimit);
                max = EditorGUILayout.FloatField(max);
            }
            EditorGUILayout.EndHorizontal();
        }

        #region "DrawList"
        public static bool DrawList<T>(List<T> lst)
        {
            Type tp = typeof(T);
            if (tp == typeof(int))
            {
                return DrawIntList((List<int>)(object)lst);
            }
            else if (tp == typeof(float))
            {
                return DrawFloatList((List<float>)(object)lst);
            }
            else if (tp == typeof(bool))
            {
                return DrawBoolList((List<bool>)(object)lst);
            }
            else if (tp == typeof(string))
            {
                return DrawStringList((List<string>)(object)lst);
            }
            else if(typeof(Object).IsAssignableFrom(tp))
            {
                object o = (object)lst;
                IList xlst = (IList)o;
                return DrawObjectList<T>(xlst);
            }
            else
            {
                Dbg.LogErr("EUtil.DrawList: unexpected type: {0}", tp);
                return false;
            }
        }

        public static bool DrawIntList(List<int> lst)
        {
            return DrawList(lst, _DrawFunc_Int);
        }

        public static bool DrawFloatList(List<float> lst)
        {
            return DrawList(lst, _DrawFunc_Float);
        }

        public static bool DrawBoolList(List<bool> lst)
        {
            return DrawList(lst, _DrawFunc_Bool);
        }

        public static bool DrawStringList(List<string> lst)
        {
            return DrawList(lst, _DrawFunc_String);
        }

        public static bool DrawObjectList<T>(IList lst)
        {
            return DrawList<T>(lst, _DrawFunc_Object<T>);
        }

        public static bool DrawList<T>(IList lst, Action<IList, int> drawFunc)
        {
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < lst.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                {
                    drawFunc(lst, i);
                    if (GUILayout.Button(new GUIContent("+", "insert entry"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
                    {
                        lst.Insert(i, default(T));
                    }
                    if (GUILayout.Button(new GUIContent("-", "delete entry"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
                    {
                        lst.RemoveAt(i);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tail");
            if (GUILayout.Button(new GUIContent("+", "add entry at tail"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
            {
                lst.Add(default(T));
            }
            GUILayout.EndHorizontal();

            bool changed = EditorGUI.EndChangeCheck();
            return changed;
        }

        public static bool DrawList<T>(List<T> lst, Action<List<T>, int> drawFunc)
        {
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < lst.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                {
                    drawFunc(lst, i);
                    if (GUILayout.Button(new GUIContent("+", "insert entry"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
                    {
                        lst.Insert(i, default(T));
                    }
                    if (GUILayout.Button(new GUIContent("-", "delete entry"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
                    {
                        lst.RemoveAt(i);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tail");
            if( GUILayout.Button(new GUIContent("+", "add entry at tail"), EditorStyles.toolbarButton, GUILayout.Width(18f)))
            {
                lst.Add(default(T));
            }
            GUILayout.EndHorizontal();

            bool changed = EditorGUI.EndChangeCheck();
            return changed;
        } 

        private static void _DrawFunc_Int(List<int> lst, int idx)
        {
            lst[idx] = EditorGUILayout.IntField(lst[idx]);
        }

        private static void _DrawFunc_Float(List<float> lst, int idx)
        {
            lst[idx] = EditorGUILayout.FloatField(lst[idx]);
        }

        private static void _DrawFunc_Bool(List<bool> lst, int idx)
        {
            lst[idx] = EditorGUILayout.Toggle(lst[idx]);
        }

        private static void _DrawFunc_String(List<string> lst, int idx)
        {
            lst[idx] = EditorGUILayout.TextField(lst[idx]);
        }

        private static void _DrawFunc_Object<T>(List<T> lst, int idx) where T : Object
        {
            Type tp = typeof(T);
            lst[idx] = (T)EditorGUILayout.ObjectField(lst[idx], tp, true);
        }

        private static void _DrawFunc_Object<T>(IList lst, int idx)
        {
            Type tp = typeof(T);
            lst[idx] = EditorGUILayout.ObjectField((Object)lst[idx], tp, true);
        }
        #endregion "DrawList"

        #endregion "controls"

        public static void DrawSplitter()
        {
            Rect rc = GUILayoutUtility.GetRect(GUIContent.none, ms_splitterStyle, GUILayout.Height(1f));
            DrawSplitter(rc);
        }

        public static void DrawSplitter(Color c)
        {
            Rect rc = GUILayoutUtility.GetRect(GUIContent.none, ms_splitterStyle, GUILayout.Height(1f));
            DrawSplitter(rc, c);
        }

        public static void DrawSplitter(Rect rc)
        {
            DrawSplitter(rc, new Color(1, 1, 1, 0.5f));
        }

        public static void DrawSplitter(Rect rc, Color c)
        {
            EUtil.PushGUIColor(c);
            EditorGUI.DrawTextureAlpha(rc, Texture2D.whiteTexture);
            EUtil.PopGUIColor();
        }

        public static void DrawVSplitter()
        {
            Rect rc = GUILayoutUtility.GetRect(GUIContent.none, ms_splitterStyle, GUILayout.ExpandHeight(true), GUILayout.Width(1f));
            DrawSplitter(rc);
        }

        public static void DrawVSplitter(Color c)
        {
            Rect rc = GUILayoutUtility.GetRect(GUIContent.none, ms_splitterStyle, GUILayout.ExpandHeight(true), GUILayout.Width(1f));
            DrawSplitter(rc, c);
        }

        #region "inspector"

        /// <summary>
        /// Be sure to call EditorGUIUtility.ExitGUI() after Fold/Hide to avoid mismatched GUILayout calls
        /// </summary>

        public static EditorWindow GetInspectorWindow()
        {
            Type tp = RCall.GetTypeFromString("UnityEditor.InspectorWindow");
            var wnd = EditorWindow.GetWindow(tp);
            return wnd;
        }

        public static void FoldInspectorComponents(GameObject go, params string[] ignores)
        {
            //Transform tr = go.transform;
            var insWnd = GetInspectorWindow();
            if (insWnd == null)
                return;

            ActiveEditorTracker tracker = (ActiveEditorTracker)RCall.CallMtd("UnityEditor.InspectorWindow", "GetTracker", insWnd, null);
            var editors = tracker.activeEditors;
            for (int eidx = 0; eidx < editors.Length; ++eidx)
            {
                var e = editors[eidx];
                string tpName = e.target.GetType().Name;
                bool shouldOpen = ignores.Contains(tpName);
                tracker.SetVisible(eidx, shouldOpen ? 1 : 0); //fold
                //RCall.CallMtd("UnityEditor.Editor", "InternalSetHidden", e, true); //hide
            }
        }

        public static bool HideInspectorComponents(GameObject go, params string[] hides)
        {
            //Transform tr = go.transform;
            var insWnd = GetInspectorWindow();
            if (insWnd == null)
                return false;
            ActiveEditorTracker tracker = (ActiveEditorTracker)RCall.CallMtd("UnityEditor.InspectorWindow", "GetTracker", insWnd, null);
            var editors = tracker.activeEditors;
            for (int eidx = 0; eidx < editors.Length; ++eidx)
            {
                var e = editors[eidx];
                string tpName = e.target.GetType().Name;
                if( hides.Contains(tpName) )
                    RCall.CallMtd("UnityEditor.Editor", "InternalSetHidden", e, true); //hide
            }
            return true;
        }

        public static bool ShowAllInspectorComponents(GameObject go)
        {
            //Transform tr = go.transform;
            var insWnd = GetInspectorWindow();
            if (insWnd == null)
                return false;
            ActiveEditorTracker tracker = (ActiveEditorTracker)RCall.CallMtd("UnityEditor.InspectorWindow", "GetTracker", insWnd, null);
            var editors = tracker.activeEditors;
            for (int eidx = 0; eidx < editors.Length; ++eidx)
            {
                var e = editors[eidx];
                RCall.CallMtd("UnityEditor.Editor", "InternalSetHidden", e, false); //show
            }
            return true;
        }		
        #endregion "inspector"

        public static void RotateView(Quaternion rot, bool ortho = false)
        {
            var v = GetSceneView();
            v.orthographic = ortho;
            v.LookAt(v.pivot, rot);
        }

        /// <summary>
        /// WARN: used undocumented API
        /// </summary>
        public static void AlignViewToPos(Vector3 pos, float dist = 40f)
        {
            SceneView sv = GetSceneView();

            Transform camTr = sv.camera.transform;
            Vector3 dir = (camTr.position - pos).normalized;

            var target = new GameObject();
            Transform targetTr = target.transform;
            targetTr.position = pos + dist * dir;
            targetTr.rotation = Quaternion.LookRotation(-dir);
            sv.AlignViewToObject(target.transform);
            
            GameObject.DestroyImmediate(target);
        }

        /// <summary>
        /// WARN: used undocumented API
        /// </summary>
        public static void AlignViewToObj(GameObject go, float dist = 40f)
        {
            SceneView sv = GetSceneView();
            Transform tr = go.transform;
            Vector3 pos = tr.position;
            Renderer render = go.GetComponent<Renderer>();

            if (render != null)
            {
                Bounds bd = render.bounds;
                dist = Mathf.Max(bd.size.x, bd.size.y, bd.size.z);
            }

            Transform camTr = sv.camera.transform;
            Vector3 dir = (camTr.position - tr.position).normalized;

            var target = new GameObject();
            Transform targetTr = target.transform;
            targetTr.position = pos + dist * dir;
            targetTr.rotation = Quaternion.LookRotation(-dir);
            sv.AlignViewToObject(target.transform);
            GameObject.DestroyImmediate(target);

        }

        /// <summary>
        /// WARN: used undocumented API
        /// </summary>
        public static void FrameSelected()
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        /// <summary>
        /// WARN: used undocumented API
        /// </summary>
        public static void SceneViewLookAt(Vector3 pos)
        {
            GetSceneView().LookAt(pos);
        }
        public static void SceneViewLookAt(Vector3 pos, Quaternion rot, float sz)
        {
            SceneView sv = GetSceneView();
            sv.LookAt(pos, rot, sz);
        }
        public static void SceneViewLookAt(Vector3 pos, float sz)
        {
            SceneView sv = GetSceneView();
            Transform tr = sv.camera.transform;

            sv.LookAt(pos, tr.rotation, sz);
        }


        public static SceneView GetSceneView()
        {
            return SceneView.sceneViews.Count == 0 ?
                EditorWindow.GetWindow<SceneView>() :
                (SceneView)SceneView.sceneViews[0];
        }

        public static Camera GetSceneViewCamera()
        {
            Camera cam = Camera.current;
            return cam;
        }

        /// <summary>
        /// like HandlesUtility.GetHandleSize, but can limit the max size by limiting z
        /// </summary>
        public static float GetHandleSize(Vector3 position, float maxZ = 7f)
        {
            Camera curCam = Camera.current;
            position = Handles.matrix.MultiplyPoint(position);
            if (curCam)
            {
                Transform camTr = curCam.transform;
                Vector3 camPos = camTr.position;
                float z = Vector3.Dot(position - camPos, camTr.TransformDirection(new Vector3(0f, 0f, 1f)));
                z = Mathf.Min(z, maxZ);
                Vector3 a = curCam.WorldToScreenPoint(camPos + camTr.TransformDirection(new Vector3(0f, 0f, z)));
                Vector3 b = curCam.WorldToScreenPoint(camPos + camTr.TransformDirection(new Vector3(1f, 0f, z)));
                float magnitude = (a - b).magnitude;
                return 80f / Mathf.Max(magnitude, 0.0001f);
            }
            return 20f;
        }

        public static void RepaintSceneView()
        {
            GetSceneView().Repaint();
        }

        public static void ShowNotification(string msg, float duration = 1.5f)
        {
            if (ms_notificationHideTime < 0)
                EditorApplication.update += _NotificationUpdate;

            EUtil.GetSceneView().ShowNotification(new GUIContent(msg));
            ms_notificationHideTime = EditorApplication.timeSinceStartup + duration;
            EUtil.GetSceneView().Repaint();
        }

        public static void HideNotification()
        {
            EUtil.GetSceneView().RemoveNotification();
            EUtil.GetSceneView().Repaint();
        }

        private static void _NotificationUpdate()
        {
            if (EditorApplication.timeSinceStartup > ms_notificationHideTime)
            {
                HideNotification();
                EditorApplication.update -= _NotificationUpdate;
                ms_notificationHideTime = double.MinValue;
            }
        }




        #region "UAW"

#if HAS_RCALL
        /// <summary>
        /// [HACK_TRICK]
        /// check if UAW(UnityAnimationWindow) is open
        /// </summary>
        public static bool IsUnityAnimationWindowOpen()
        {
            return GetUnityAnimationWindow() != null;
        }

        /// <summary>
        /// [HACK TRICK]
        /// get UAW if there is, else null
        /// </summary>
        public static object GetUnityAnimationWindow()
        {
            IList lst = (IList)RCall.CallMtd("UnityEditor.AnimationWindow", "GetAllAnimationWindows", null, null);
            if (lst.Count > 0)
                return lst[0];
            else
                return null;
        }

        public static object GetUnityAnimationWindowState(object uaw)
        {
#if U5_1_ABOVE 
            object animEditor = RCall.GetField("UnityEditor.AnimationWindow", "m_AnimEditor", uaw);
            object uawstate = RCall.GetField("UnityEditor.AnimEditor", "m_State", animEditor);
#else
            object uawstate = RCall.GetProp("UnityEditor.AnimationWindow", "state", uaw);
#endif
            return uawstate;
        }

#endif

        public static void SetUAWCurFrame(object uaw, int frame)
        {
            RCall.CallMtd("UnityEditor.AnimationWindow", "PreviewFrame", uaw, frame);
        }

        #endregion

        //public static object Call(string className, string mtd, params object[] ps)
        //{
        //    Type t = typeof(AssetDatabase);
        //    Dbg.Log(t);
        //    //Dbg.Assert(t != null, "failed to get class: {0}", className);
        //    //MethodInfo method
        //    //     = t.GetMethod(mtd, BindingFlags.Static | BindingFlags.Public);
        //    //Dbg.Assert(method != null, "failed to get method: {0}", mtd); 

        //    //return method.Invoke(null, ps);
        //    return null;
        //}

        public static void StartInputModalWindow(System.Action<string> onSuccess, System.Action onCancel, string prompt = "Input", string title = "", Texture2D bg = null)
        {
            InputModalWindow wndctrl = new InputModalWindow(onSuccess, onCancel, title, prompt, bg);
            GUIWindowMgr.Instance.Add(wndctrl);
        }

        public static void StartObjRefModalWindow(System.Action<Object> onSuccess, System.Action onCancel, Type tp, string prompt = "Object Reference", Texture2D bg = null)
        {
            if( tp == null )
                tp = typeof(Object);

            ObjectRefModalWindow wndctrl = new ObjectRefModalWindow(onSuccess, onCancel, tp, prompt, bg);
            GUIWindowMgr.Instance.Add(wndctrl);
        }

        /// <summary>
        /// will create entry in assetdatabase if the path is not taken,
        /// will replace the old entry if there's already one, and keep the ref valid
        /// </summary>
        public static void SaveAnimClip(AnimationClip clip, string path)
        {
            AnimationClip existClip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            if (existClip != null)
            {
                EditorUtility.CopySerialized(clip, existClip);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }
        }

        /// <summary>
        /// cache the transform recursively
        /// </summary>
        public static List<XformData> CacheXformData(Transform root)
        {
            List<XformData> lst = new List<XformData>();

            _CacheXformData(root, lst);

            return lst;
        }

        private static void _CacheXformData(Transform cur, List<XformData> lst)
        {
            XformData d = new XformData();
            d.CopyFrom(cur);
            lst.Add(d);

            for (int idx = 0; idx < cur.childCount; ++idx )
            {
                Transform childTr = cur.GetChild(idx);
                _CacheXformData(childTr, lst);
            }
        }

        public static void ApplyXformData(Transform root, List<XformData> lst)
        {
            int idx = 0;
            _ApplyXformData(root, lst, ref idx);
        }

        private static void _ApplyXformData(Transform cur, List<XformData> lst, ref int idx)
        {
            XformData d = lst[idx];
            d.Apply(cur);

            for(int cidx =0; cidx < cur.childCount; ++cidx )
            {
                Transform childTr = cur.GetChild(cidx);
                ++idx;
                _ApplyXformData(childTr, lst, ref idx);
            }
        }

        /// <summary>
        /// make a raycast on given mesh
        /// </summary>
        public delegate bool RaycastDele(Ray ray, Mesh mesh, Matrix4x4 mat, out RaycastHit hit);
        public static bool Raycast(Ray ray, Mesh mesh, Matrix4x4 l2wMat, out RaycastHit hit)
        {
            object[] parameters = new object[] { ray, mesh, l2wMat, null };
            bool bRes = (bool)RCall.CallMtdDeleType("UnityEditor.HandleUtility", "IntersectRayMesh", 
                typeof(RaycastDele), null, parameters);

            if( bRes )
            {
                hit = (RaycastHit)parameters[3];
            }
            else
            {
                hit = new RaycastHit();
            }

            //hit = bRes ? ((RaycastHit)parameters[3]) : (new RaycastHit());

            return bRes;
        }
        public static bool Raycast(Ray ray, MeshFilter mf, out RaycastHit hit)
        {
            return Raycast(ray, mf.sharedMesh, mf.transform.localToWorldMatrix, out hit);
        }

        /// <summary>
        /// keep shooting ray until cannot get new result, 
        /// collect and return all results
        /// </summary>
        public static RaycastHit[] RaycastAll(Ray ray, Mesh mesh, Matrix4x4 mat)
        {
            List<RaycastHit> hitLst = new List<RaycastHit>();
            RaycastHit hit;

            bool bHit = true;

            int cnt = 0;
            do
            {
                if( ++cnt > 100 )
                {
                    Dbg.LogWarn("EUtil.RaycastAll: Inf-Loop!? break out!");
                    break;
                }

                bHit = Raycast(ray, mesh, mat, out hit);
                if( bHit )
                {
                    hitLst.Add(hit);
                    ray.origin = hit.point + ray.direction * 0.005f;
                }
            } while (bHit);

            return hitLst.ToArray();
        }

        public static float ScreenDist(Vector3 p0, Vector3 p1)
        {
            Vector2 sp0 = HandleUtility.WorldToGUIPoint(p0);
            Vector2 sp1 = HandleUtility.WorldToGUIPoint(p1);

            return Vector2.Distance(sp0, sp1);
        }

        public static void SetEditorWindowTitle(EditorWindow w, string title, Texture icon = null)
        {
#if U5_1_ABOVE
            if (icon != null)
                w.titleContent = new GUIContent(title, icon); 
            else
                w.titleContent = new GUIContent(title);
#else
            w.title = title;
#endif
        }

        public static bool SaveMeshToAssetDatabase(Mesh m)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Save Mesh",
                                    m.name,
                                    "asset",
                                    "Specify path for mesh asset");

            if( !string.IsNullOrEmpty(assetPath) )
            {
                return SaveMeshToAssetDatabase(m, assetPath);
            }          
            else
            {
                return false;
            }
        }
        public static bool SaveMeshToAssetDatabase(Mesh m, string assetPath)
        {
            string verbose;
            return SaveMeshToAssetDatabase(m, assetPath, out verbose);
        }
        public static bool SaveMeshToAssetDatabase(Mesh m, string assetPath, out string verbose)
        {
            verbose = string.Empty;
            if (!string.IsNullOrEmpty(assetPath))
            {
                Mesh assetMesh = m;
                //Dbg.Log("exist-path: {0}\ntargetPath:{1}", AssetDatabase.GetAssetOrScenePath(assetMesh), assetPath);
                if (AssetDatabase.Contains(assetMesh))
                {
                    string oldPath = AssetDatabase.GetAssetPath(assetMesh);
                    verbose = string.Format("SUCCESS: Mesh already in AssetDatabase: {0}", oldPath);
                    return true;
                }
                else
                {
                    AssetDatabase.CreateAsset(assetMesh, assetPath);
                    verbose = string.Format("SUCCESS: Saved mesh to path: {0}", assetPath);
                    return true;
                }
            }
            else
            {
                verbose = "FAIL: AssetPath is empty";
                return false;
            }
        }

        public static bool IsHierarchyHasFocus()
        {
            EditorWindow wnd = EditorWindow.focusedWindow;
            if (wnd == null)
                return false;

#if U5_1_ABOVE
            return wnd.titleContent.text == "UnityEditor.HierarchyWindow";
#else
            return wnd.title == "UnityEditor.HierarchyWindow";
#endif
        }

        public static void DrawSerializedObject(UnityEngine.Object obj)
        {
            SerializedObject serObj = new SerializedObject(obj);
            bool enterChildren = true;

            serObj.Update();

            for (var ie = serObj.GetIterator(); ie.NextVisible(enterChildren); )
            {
                if (ie.name == "m_Script")
                    continue;

                if (ie.hasVisibleChildren && !ie.isExpanded)
                {
                    enterChildren = false;
                }
                else
                {
                    enterChildren = true;
                }

                EditorGUILayout.PropertyField(ie);
            }

            serObj.ApplyModifiedProperties();
        }

        public static Editor GetEditor(UnityEngine.Object obj)
        {
            Editor e;
            if (ms_editorMap.TryGetValue(obj, out e) && e)
            {
                return e;
            }
            else
            {
                e = Editor.CreateEditor(obj);
                ms_editorMap.Add(obj, e);
                return e;
            }
        }

        /// <summary>
        /// used to ensure the pass-in parameter 'obj' is init-ed with the object from 'path'
        /// </summary>
        public static T LoadAsset<T>(ref T obj, string path) where T : Object
        {
            if (obj == null)
            {
                obj = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
                Dbg.Assert(obj != null, "EUtil.LoadAsset: failed to load asset at: {0}", path);
            }
            return obj;
        }

        /// <summary>
        /// check if asset exists at specified path
        /// </summary>
        public static bool AssetExists(string path)
        {
            var o = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            return o != null;
        }

        public static void ShowModal(EditorWindow w)
        {
            RCall.CallMtd("UnityEditor.EditorWindow", "ShowModal", w, null);
        }
        public static void ShowModal<T>() where T : EditorWindow
        {
            var w = ScriptableObject.CreateInstance<T>();
            RCall.CallMtd("UnityEditor.EditorWindow", "ShowModal", w, null);
        }

        /// <summary>
        /// given an activatorRect and a hint rc (providing size)
        /// return a rect on screen to best place window
        /// </summary>
        public static Rect GetRectByActivatorRect(Rect rc, Rect activatorRect)
        {
            // convert activatorRect from small-editor coord to UnityEditorWindow coord
            activatorRect.position = GUIUtility.GUIToScreenPoint(activatorRect.position);

            Vector2 mid = activatorRect.center;
            if (mid.x < Screen.width * 0.5f)
            {
                rc.x = Mathf.Min(activatorRect.xMax, Screen.width - rc.width);
            }
            else
            {
                rc.x = Mathf.Max(0, activatorRect.xMin - rc.width);
            }

            if (mid.y < Screen.height * 0.5f)
            {
                rc.y = Mathf.Min(activatorRect.yMax, Screen.height - rc.height);
            }
            else
            {
                rc.y = Mathf.Max(0, activatorRect.yMin - rc.height);
            }

            return rc;
        }

        public static void MoveComponents(GameObject src, GameObject dst)
        {
            var o = src;
            var m = dst;
            Misc.AddChild(o, m, true);

            var lst = new List<Component>();
            var cs = o.GetComponents<Component>();
            foreach (var cp in cs)
            {
                var newO = m.AddComponent(cp.GetType());
                if (newO == null)
                    continue; //could happen on Transform
                EditorUtility.CopySerialized(cp, newO);
                lst.Add(cp);
            }

            lst.ForEach(x => Component.DestroyImmediate(x));
        }

        public static void SetEnableWireframe(Renderer r, bool enable)
        {
#if UNITY_5_5_OR_NEWER
            EditorUtility.SetSelectedRenderState(r, enable ? EditorSelectedRenderState.Highlight : EditorSelectedRenderState.Hidden);
#else
            EditorUtility.SetSelectedWireframeHidden(r, !enable);
#endif
        }

        #region "Animator related"

        public static bool HasAnimatorController(Animator ator)
        {
            return ator.runtimeAnimatorController != null;
        }

        public static int GetStateNameHash(AnimatorStateInfo info)
        {
#if U5
            return info.fullPathHash;
#else
            return info.nameHash;
#endif
        }

        /// <summary>
        /// get the current states in all layers, return the list of fullNames
        /// in [layer].[state] format
        /// </summary>
        public static void GetCurrentStates(Animator ator, List<string> ret)
        {
            var allStateNames = new List<string>();
            GetAllStateNames(allStateNames, ator);
            var hashes = allStateNames.Select(x=>Animator.StringToHash(x)).ToArray();

            int layerCnt = ator.layerCount;
            for (int i = 0; i < layerCnt; ++i)
            {
                AnimatorStateInfo info = ator.GetCurrentAnimatorStateInfo(i);
                var targetId = GetStateNameHash(info);

                for (int j = 0; j < hashes.Length; ++j)
                {
                    if (targetId == hashes[j])
                    {
                        ret.Add(allStateNames[j]);
                        break;
                    }
                }
                if (ret.Count != i + 1)
                    Dbg.LogWarn("EUtil.GetCurrentStates: failed to get current stateName for layer: {0}", i);
            }
        }

        /// <summary>
        /// get all layers' default state
        /// </summary>
        public static void GetDefaultStates(Animator ator, List<string> ret)
        {
            ret.Clear();
#if U5
            var acontrol = GetAnimatorController(ator);
            var layers = acontrol.layers;
            foreach (var l in layers)
            {
                var stm = l.stateMachine;
                if (stm.defaultState != null)
                {
                    string fullName = l.name + "." + stm.defaultState.name;
                    ret.Add(fullName);
                }
                else
                {
                    ret.Add(string.Empty); //if no default state
                }                
            }
#else
            UnityEditor.Animations.AnimatorController acontrol = GetAnimatorController(ator);
            int layerCnt = acontrol.layerCount;
            for (int i=0; i<layerCnt; ++i)
            {
                var l = acontrol.GetLayer(i);
                var stm = l.stateMachine;
                if(stm.defaultState != null)
                {
                    string fullName = l.name + "." + stm.defaultState.name;
                    ret.Add(fullName);
                }
                else
                {
                    ret.Add(string.Empty); //if no default state
                }
            }
#endif
        }

        /// <summary>
        /// check if the animator has specified stateName, 
        /// stateName must be fullName
        /// </summary>
        public static bool HasState(Animator ator, string stateName)
        {
#if U5
            int id = Animator.StringToHash(stateName);
            for (int i = 0; i < ator.layerCount; ++i)
            {
                if (ator.HasState(i, id))
                    return true;
            }
            return false;
#else
            List<string> stateNames = new List<string>();
            GetAllStateNames(stateNames, ator);
            return stateNames.Contains(stateName);
#endif
        }

        public static AnimatorControllerParameter AnimatorGetParameter(Animator ator, string paramName)
        {
#if U5
            var ps = ator.parameters;
            foreach (var p in ps)
            {
                if (p.name == paramName)
                    return p;
            }
            return null;
#else
            UnityEditor.Animations.AnimatorController acontrol = GetAnimatorController(ator);
            int pcnt = acontrol.parameterCount;
            for(int i=0; i<pcnt; ++i)
            {
                var p = acontrol.GetParameter(i);
                if(p.name == paramName )
                    return p;
            }
            return null;
#endif
        }

        public static void GetAllParameters(Animator ator, List<AnimatorControllerParameter> ps)
        {
            ps.Clear();
#if U5
            ps.AddRange(ator.parameters);
#else
            UnityEditor.Animations.AnimatorController acontrol = GetAnimatorController(ator);
            int pcnt = acontrol.parameterCount;
            for(int i=0; i<pcnt; ++i)
            {
                var p = acontrol.GetParameter(i);
                ps.Add(p);
            }
#endif
        }

#if U5
        public static UnityEditor.Animations.AnimatorController GetAnimatorController(Animator ator)
        {
            var acontrol = ator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            Dbg.Assert(acontrol != null, "EUtil.GetAnimatorController failed");
            return acontrol;
        }
#else //U4
        public static UnityEditor.Animations.AnimatorController GetAnimatorController(Animator ator)
        {
            UnityEditor.Animations.AnimatorController acontrol = ator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; 
            Dbg.Assert(acontrol != null, "EUtil.GetAnimatorController failed");
            return acontrol;
        }
#endif

        public static void GetAllParameterNames(List<string> lst, Animator ator)
        {
#if U5
            var ps = ator.parameters;
            foreach (var p in ps)
            {
                lst.Add(p.name);
            }
#else
            UnityEditor.Animations.AnimatorController acontrol = GetAnimatorController(ator);
            int pcnt = acontrol.parameterCount;
            for(int i=0; i<pcnt; ++i)
            {
                var p = acontrol.GetParameter(i);
                lst.Add(p.name);
            }
#endif
        }

        /// <summary>
        /// get all state' names in [layer].[state] format
        /// </summary>
        public static void GetAllStateNames(List<string> lst, Animator ator)
        {
#if U5
            var acontrol = GetAnimatorController(ator);
            var layers = acontrol.layers;
            foreach (var l in layers)
            {
                var stm = l.stateMachine;

                // state-machine
                var sts = stm.states;
                foreach (var s in sts)
                {
                    lst.Add(l.name + "." + s.state.name);
                }

                // sub-machines
                var submac = stm.stateMachines;
                foreach (var subm in submac)
                {
                    var subSts = subm.stateMachine.states;
                    foreach (var s in subSts)
                    {
                        lst.Add(l.name + "." + s.state.name);
                    }
                }
            }
#else
            UnityEditor.Animations.AnimatorController acontrol = GetAnimatorController(ator);
            int layerCnt = acontrol.layerCount;
            for (int i=0; i<layerCnt; ++i)
            {
                var l = acontrol.GetLayer(i);
                var stm = l.stateMachine;
                int stCnt = stm.stateCount;
                for(int stIdx = 0; stIdx < stCnt; ++stIdx)
                {
                    var s = stm.GetState(stIdx);
                    lst.Add(s.uniqueName);
                }
            }
#endif
        }
        
        #endregion "Animator related"
        
        #region "Undo related"

        public static PropertyModification GetPropertyModification(UndoPropertyModification u)
        {
#if U5_1_ABOVE
            return u.currentValue;
#else
            return u.propertyModification;
#endif
        }
        
        #endregion "Undo related"

        #region "prevent sceneView deselection"

        /// <summary>
        /// when extraCond is true, this method will prevent sceneView deselect current selection by click other objects
        /// </summary>
        public static void SceneViewPreventDeselecByClick(bool extraCond)
        {
            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    {
                        //Dbg.Log("Down: {0}, {1}, hot:{2}, viewTool: {3}", e, e.button, GUIUtility.hotControl, Tools.viewTool);
                        if (extraCond && e.button == 0 && Tools.viewTool == ViewTool.Pan &&
                            !e.alt && !e.shift) //only capture mouse focus when selected point, and no shift/alt
                        {
                            //Dbg.Log("eat Down: {0}, {1}", e, e.button);
                            GUIUtility.hotControl = controlId;
                            e.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        //Dbg.Log("Up: {0}, {1}, hot:{2}, viewTool: {3}", e, e.button, GUIUtility.hotControl, Tools.viewTool);
                        if (e.button == 0 && Tools.viewTool == ViewTool.Pan && GUIUtility.hotControl == controlId)
                        {
                            //Dbg.Log("eat Up: {0}, {1}", e, e.button);
                            GUIUtility.hotControl = 0;
                            e.Use();
                        }
                    }
                    break;
            }
        }


        #endregion

        #region "editor scene managements"
        public static string GetCurrentScene()
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
#else
            return EditorApplication.currentScene;
#endif
        }

        public static void SaveScene()
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
#else
            EditorApplication.SaveScene();
#endif
        }

        public static void SaveScene(string name, bool asCopy)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEngine.SceneManagement.Scene oneScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByName(name);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(oneScene);
#else
            EditorApplication.SaveScene(name, asCopy);
#endif
        }

        public static void OpenScene(string path)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
#else
            EditorApplication.OpenScene(path);
#endif
        }

        public static void OpenSceneAdditive(string path)
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
#else
            EditorApplication.OpenSceneAdditive(path);
#endif
        }

        public static void SetActiveSceneDirty()
        {
#if UNITY_5_3_OR_NEWER || UNITY_5_3
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
#endif
        }

        public static void SetDirty(Object o)
        {
            if (o == null)
                return;
            EditorUtility.SetDirty(o);
            SetActiveSceneDirty();
        }

        #endregion
    }



    /// <summary>
    /// this is a default modal window used to get a object reference
    /// </summary>
    public class ObjectRefModalWindow : GUIWindow
    {
        private System.Action<UnityEngine.Object> m_onSuccess;
        private System.Action m_onCancel;
        private string m_Prompt = "Select An Object";
        private Texture2D m_background = null;
        private Type m_ObjType = null;

        private Object m_curInput = null;
        private State m_State = State.NONE;

        public ObjectRefModalWindow(System.Action<UnityEngine.Object> onSuccess, System.Action onCancel)            
        {
            m_onSuccess = onSuccess;
            m_onCancel = onCancel;
            m_ObjType = typeof(UnityEngine.Object);
        }
        public ObjectRefModalWindow(System.Action<UnityEngine.Object> onSuccess, System.Action onCancel, 
            Type objType, string prompt, Texture2D bg)
        {
            m_onSuccess = onSuccess;
            m_onCancel = onCancel;
            m_ObjType = objType;
            m_Prompt = prompt;
            m_background = bg;
        }

        public override EReturn OnGUI()
        {
            Rect rc = new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 50f, 300, 60);

            //GUI.ModalWindow(m_Index, rc, _Draw, m_Title);
            EUtil.PushGUIEnable(true);

            if (m_background != null)
                GUI.DrawTexture(rc, m_background);
            GUILayout.BeginArea(rc);
            {
                _Draw();
            }
            GUILayout.EndArea();

            EUtil.PopGUIEnable();

            if (m_State == State.OK)
            {
                if (m_onSuccess != null)
                    m_onSuccess(m_curInput);
                return EReturn.STOP;
            }
            else if (m_State == State.CANCEL)
            {
                if (m_onCancel != null)
                    m_onCancel();
                return EReturn.STOP;
            }

            return EReturn.MODAL;
        }

        private void _Draw()
        {
            GUILayout.Label(m_Prompt);

            m_curInput = EditorGUILayout.ObjectField(m_curInput, m_ObjType, true);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("OK"))
                {
                    m_State = State.OK;
                }
                if (GUILayout.Button("Cancel"))
                {
                    m_State = State.CANCEL;
                }
            }
            GUILayout.EndHorizontal();
        }

        private enum State
        {
            NONE,
            OK,
            CANCEL,
        }
    }

    /// <summary>
    /// this is a default modal window used to get a text input
    /// </summary>
    public class InputModalWindow : GUIWindow
    {
        private System.Action<string> m_onSuccess;
        private System.Action m_onCancel;
        private string m_Title = "Input Modal Window";
        private string m_Prompt = "Input:";
        private Texture2D m_background = null;

        private string m_curInput = string.Empty;
        private State m_State = State.NONE;

        public InputModalWindow(System.Action<string> onSuccess, System.Action onCancel)
        {
            m_onSuccess = onSuccess;
            m_onCancel = onCancel;
        }
        public InputModalWindow(System.Action<string> onSuccess, System.Action onCancel, string title, string prompt, Texture2D bg)
        {
            m_onSuccess = onSuccess;
            m_onCancel = onCancel;
            m_Title = title;
            m_Prompt = prompt;
            m_background = bg;
        }

        public string Title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        public string Prompt
        {
            get { return m_Prompt; }
            set { m_Prompt = value; }
        }

        public override EReturn OnGUI()
        {
            Rect rc = new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 50f, 300, 60);

            //GUI.ModalWindow(m_Index, rc, _Draw, m_Title);
            EUtil.PushGUIEnable(true);

            if (m_background != null)
                GUI.DrawTexture(rc, m_background);
            GUILayout.BeginArea(rc);
            {
                _Draw();
            }
            GUILayout.EndArea();

            EUtil.PopGUIEnable();

            if (m_State == State.OK)
            {
                if (m_onSuccess != null)
                    m_onSuccess(m_curInput);
                return EReturn.STOP;
            }
            else if (m_State == State.CANCEL)
            {
                if (m_onCancel != null)
                    m_onCancel();
                return EReturn.STOP;
            }

            return EReturn.MODAL;
        }

        private void _Draw()
        {
            GUILayout.Label(m_Prompt);

            m_curInput = GUILayout.TextField(m_curInput);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("OK"))
                {
                    m_State = State.OK;
                }
                if (GUILayout.Button("Cancel"))
                {
                    m_State = State.CANCEL;
                }
            }
            GUILayout.EndHorizontal();

            //Rect rc = new Rect(0, 0, Screen.width, Screen.height);
            //GUI.DrawTexture(rc, EditorGUIUtility.whiteTexture);
            //if( GUI.Button(rc, "XXSDFSDF") )
            //{
            //    Dbg.Log("xxx");
            //}
            //else
            //{
            //    Dbg.Log("yyy");
            //}
        }

        private enum State
        {
            NONE,
            OK,
            CANCEL,
        }
    }


    public class SelectObjWindow<T> : PopupWindowContent  where T : Object
    {
        private string title = string.Empty;
        private T o = null;
        private Action<T> onClose;

        public SelectObjWindow( Action<T> onClose, string title = "Select an Object" )
        {
            this.onClose = onClose;
            this.title = title;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300f, 45f);
        }

        public override void OnGUI(Rect rc)
        {
            EditorGUILayout.LabelField(title);
            o = (T)EditorGUILayout.ObjectField(GUIContent.none, o, typeof(T), true);

            Event e = Event.current;
            if (e.type == EventType.ExecuteCommand && e.commandName == "ObjectSelectorClosed")
            {
                editorWindow.Close();
            }
        }

        public override void OnClose()
        {
            onClose(o);
        }
    }

    public interface IOnSceneGUI
    {
        void OnSceneGUI();
    }
}
