using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Constraints
{
    public class EConUtil  
    {

		#region "skin"

        private static GUISkin ms_skin;
        public static GUISkin skin{get {_InitSkin();return ms_skin; }}

        private static Texture ms_upBtn;
        public static Texture upBtn{get { _InitSkin(); return ms_upBtn; }}
        private static Texture ms_downBtn;
        public static Texture downBtn{get { _InitSkin(); return ms_downBtn; }}
        private static Texture ms_deleteBtn;
        public static Texture deleteBtn{get { _InitSkin(); return ms_deleteBtn; }}
        private static Texture ms_activeBtn;
        public static Texture activeBtn { get { _InitSkin(); return ms_activeBtn; } }
        private static Texture ms_inactiveBtn;
        public static Texture inactiveBtn { get { _InitSkin(); return ms_inactiveBtn; } }
		
        private static void _InitSkin()
        {
            if (ms_skin)
                return;

            string skinPath = PathUtil.Combine(BASEPATH, "Res/constraintSkin.guiskin");
            ms_skin = (GUISkin)AssetDatabase.LoadAssetAtPath(skinPath, typeof(GUISkin));
            if (!ms_skin || ms_skin == null){
                Dbg.LogErr("EConUtil._InitSkin: failed to load skin at {0}", skinPath);
                return;
            }

            ms_upBtn = ms_skin.GetStyle("upBtn").normal.background;
            ms_downBtn = ms_skin.GetStyle("downBtn").normal.background;
            ms_deleteBtn = ms_skin.GetStyle("deleteBtn").normal.background;
            var constraintActiveBtn = ms_skin.GetStyle("constraintActiveBtn");
            ms_inactiveBtn = constraintActiveBtn.normal.background;
            ms_activeBtn = constraintActiveBtn.onNormal.background;
        }

		#endregion "skin"

        public static void DrawActiveLine(BaseConstraint cp)
        {
            EUtil.PushLabelWidth(50f);
            EUtil.PushFieldWidth(30f);
            EditorGUILayout.BeginHorizontal();
            {
                cp.IsActiveConstraint = EditorGUILayout.Toggle(new GUIContent("Active", "whether this constraint is active"), cp.IsActiveConstraint);
                GUILayout.FlexibleSpace();
                if (cp.HasGizmos)
                    cp.ShowGizmos = EditorGUILayout.Toggle(new GUIContent("Gizmos", "whether display gizmos for this constraint"), cp.ShowGizmos);
                else
                    cp.ShowGizmos = false;
            }
            EditorGUILayout.EndHorizontal();
            EUtil.PopFieldWidth();
            EUtil.PopLabelWidth();
        }

        public static EAxisD DrawAffectField(
            EAxisD eAffect,
            string label,
            string tips,
            EAxisD eTarget,
            EAxisD eInvTarget)
        {
            bool v = (eAffect & eTarget) != 0;
            bool newV = v;

            EUtil.PushBackgroundColor(v ? EConUtil.kSelectedBtnColor : Color.white);
            if (GUILayout.Button(new GUIContent(label, tips), EditorStyles.toolbarButton))
            {
                newV = !v;
            }
            EUtil.PopBackgroundColor();

            if (v != newV)
            {
                if (newV)
                {
                    eAffect |= eTarget;
                    eAffect &= (~eInvTarget);
                }
                else
                {
                    eAffect &= (~eTarget);
                }
            }
            return eAffect;
        }

        public static EAxis DrawAxisBtnMask(GUIContent label, EAxis eAffect, float labelWidth = 80f)
        {
            bool active = false;
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));

                active = (eAffect & EAxis.X) != 0;
                EUtil.PushBackgroundColor(active ? kSelectedBtnColor : Color.white);
                if (GUILayout.Button("X", EditorStyles.toolbarButton))
                {
                    if (active) eAffect &= ~EAxis.X;
                    else eAffect |= EAxis.X;
                }
                EUtil.PopBackgroundColor();

                active = (eAffect & EAxis.Y) != 0;
                EUtil.PushBackgroundColor(active ? kSelectedBtnColor : Color.white);
                if (GUILayout.Button("Y", EditorStyles.toolbarButton))
                {
                    if (active) eAffect &= ~EAxis.Y;
                    else eAffect |= EAxis.Y;
                }
                EUtil.PopBackgroundColor();

                active = (eAffect & EAxis.Z) != 0;
                EUtil.PushBackgroundColor(active ? kSelectedBtnColor : Color.white);
                if (GUILayout.Button("Z", EditorStyles.toolbarButton))
                {
                    if (active) eAffect &= ~EAxis.Z;
                    else eAffect |= EAxis.Z;
                }
                EUtil.PopBackgroundColor();
            }
            EditorGUILayout.EndHorizontal();

            return eAffect;
        }

        public static float DrawLimitField(
            ref ELimitAffect eAffect, 
            string label, 
            string tips, 
            float limitVal, 
            ELimitAffect field)
        {
            bool v = (eAffect & field) != 0;

            EditorGUILayout.BeginHorizontal();
            {
                Rect rc = GUILayoutUtility.GetRect(16f, 16f);
                bool newV = EditorGUI.Toggle(rc, v);
                if (v != newV)
                {
                    if (newV)
                    {
                        eAffect |= field;
                    }
                    else
                    {
                        eAffect &= (~field);
                    }
                }

                EUtil.PushGUIEnable(newV);
                {
                    limitVal = EditorGUILayout.FloatField(new GUIContent(label, tips), limitVal);
                }
                EUtil.PopGUIEnable();

            }
            EditorGUILayout.EndHorizontal();

            return limitVal;
        }

        public static void LimitFieldMinMaxFix(ELimitAffect eAffect, ref Vector3 limitMin, ref Vector3 limitMax)
        {
            if ((eAffect & (ELimitAffect.MaxX | ELimitAffect.MinX)) == (ELimitAffect.MaxX | ELimitAffect.MinX))
            {
                limitMax.x = Mathf.Max(limitMin.x, limitMax.x);
            }
            if ((eAffect & (ELimitAffect.MaxY | ELimitAffect.MinY)) == (ELimitAffect.MaxY | ELimitAffect.MinY))
            {
                limitMax.y = Mathf.Max(limitMin.y, limitMax.y);
            }
            if ((eAffect & (ELimitAffect.MaxZ | ELimitAffect.MinZ)) == (ELimitAffect.MaxZ | ELimitAffect.MinZ))
            {
                limitMax.z = Mathf.Max(limitMin.z, limitMax.z);
            }
        }

        public static void DrawAxisRange(ref Vector3 from, ref Vector3 to)
        {
            EUtil.PushFieldWidth(40f);
            for (int i = 0; i < 3; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label(string.Format("{0} Range: <", (char)('X' + i)));
                    from[i] = EditorGUILayout.FloatField(from[i]);
                    GUILayout.Label(" , ");
                    to[i] = EditorGUILayout.FloatField(to[i]);
                    GUILayout.Label(">");
                }
                EditorGUILayout.EndHorizontal();
            }
            EUtil.PopFieldWidth();
        }

        public static void DrawEulerLimitField(
            ref ELimitEuler eLimit, 
            string label, 
            ref Vector3 limitMin, 
            ref Vector3 limitMax, 
            ELimitEuler field,
            float minVal,
            float maxVal)
        {
            bool v = (eLimit & field) != 0;

            EditorGUILayout.BeginHorizontal();
            {
                Rect rc = GUILayoutUtility.GetRect(16f, 18f);
                bool newV = EditorGUI.Toggle(rc, v);
                if (v != newV)
                {
                    if (newV)
                    {
                        eLimit |= field;
                    }
                    else
                    {
                        eLimit &= (~field);
                    }
                }

                EUtil.PushGUIEnable(newV);
                {
                    float min = 0, max = 0;
                    switch (field)
                    {
                        case ELimitEuler.X: min = limitMin.x; max = limitMax.x; break;
                        case ELimitEuler.Y: min = limitMin.y; max = limitMax.y; break;
                        case ELimitEuler.Z: min = limitMin.z; max = limitMax.z; break;
                    }

                    GUILayout.Label(label);
                    GUILayout.Label("Min");
                    min = Mathf.Clamp(EditorGUILayout.FloatField(min), minVal, maxVal);
                    max = Mathf.Max(max, min);
                    GUILayout.Label(" => ");
                    GUILayout.Label("Max");
                    max = Mathf.Clamp(EditorGUILayout.FloatField(max), minVal, maxVal);
                    min = Mathf.Min(max, min);

                    switch (field)
                    {
                        case ELimitEuler.X: limitMin.x = min; limitMax.x = max; break;
                        case ELimitEuler.Y: limitMin.y = min; limitMax.y = max; break;
                        case ELimitEuler.Z: limitMin.z = min; limitMax.z = max; break;
                    }
                }
                EUtil.PopGUIEnable();

            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawChooseTransformData(ref ETransformData curData, string label, string tips)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(label, tips));

                EUtil.PushBackgroundColor(curData == ETransformData.Position ? kSelectedBtnColor : Color.white);
                if( GUILayout.Button("Loc", EditorStyles.toolbarButton) )
                {
                    curData = ETransformData.Position;
                    return;
                }
                EUtil.PopBackgroundColor();

                EUtil.PushBackgroundColor(curData == ETransformData.Rotation ? kSelectedBtnColor : Color.white);
                if (GUILayout.Button("Rot", EditorStyles.toolbarButton))
                {
                    curData = ETransformData.Rotation;
                    return;
                }
                EUtil.PopBackgroundColor();

                EUtil.PushBackgroundColor(curData == ETransformData.Scale ? kSelectedBtnColor : Color.white);
                if (GUILayout.Button("Sca", EditorStyles.toolbarButton))
                {
                    curData = ETransformData.Scale;
                    return;
                }
                EUtil.PopBackgroundColor();
            }
            EditorGUILayout.EndHorizontal();
        }


        public static Enum DrawEnumBtns(Enum[] es, Enum cur, string label, string tips)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(label, tips));

                for (int i = 0; i < es.Length; ++i)
                {
                    EUtil.PushBackgroundColor(es[i] == cur ? kSelectedBtnColor : Color.white);
                    if (GUILayout.Button(es[i].ToString(), EditorStyles.toolbarButton))
                    {
                        return es[i];
                    }
                    EUtil.PopBackgroundColor();
                }
            }
            EditorGUILayout.EndHorizontal();

            return cur;
        }
        public static Enum DrawEnumBtns(Enum[] es, string[] strs, Enum cur, string label, string tips, float labelWidth = 80f)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(label, tips), GUILayout.Width(labelWidth));

                for (int i = 0; i < es.Length; ++i)
                {
                    Color c = (es[i].Equals(cur) ? kSelectedBtnColor : Color.white); 
                    EUtil.PushBackgroundColor(c);
                    if (GUILayout.Button(strs[i], EditorStyles.toolbarButton))
                    {
                        return es[i];
                    }
                    EUtil.PopBackgroundColor();
                }
            }
            EditorGUILayout.EndHorizontal();

            return cur;
        }

        public const string BASEPATH = "Assets/Skele/Constraints";
        public readonly static Color kSelectedBtnColor = new Color32(40, 100, 237, 255);

        public readonly static string[] AxisDStrs = { "+X", "+Y", "+Z", "-X", "-Y", "-Z" };
        public readonly static Enum[] AxisDs = { EAxisD.X, EAxisD.Y, EAxisD.Z, EAxisD.InvX, EAxisD.InvY, EAxisD.InvZ };
        public readonly static string[] AxisDStrsPositive = { "+X", "+Y", "+Z"};
        public readonly static Enum[] AxisDsPositive = { EAxisD.X, EAxisD.Y, EAxisD.Z };
        public readonly static string[] AxisStrs = { "X", "Y", "Z" };
        public readonly static Enum[] Axises = { EAxis.X, EAxis.Y, EAxis.Z };
    }
}
