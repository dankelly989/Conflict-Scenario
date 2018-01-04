using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor Utility
/// </summary>
namespace MH
{

    public class GUIUtil
    {
        public static Stack<Color> ms_clrStack = new Stack<Color>();
        public static Stack<Color> ms_contentClrStack = new Stack<Color>();
        public static Stack<bool> ms_enableStack = new Stack<bool>();
        public static Stack<GUISkin> ms_SkinStack = new Stack<GUISkin>();
        public static Stack<int> ms_depthStack = new Stack<int>();
        public static Stack<Matrix4x4> ms_matrixStack = new Stack<Matrix4x4>();

        public static void PushMatrix(Matrix4x4 m)
        {
            ms_matrixStack.Push(GUI.matrix);
            GUI.matrix = m;
        }

        public static Matrix4x4 PopMatrix()
        {
            Matrix4x4 m = GUI.matrix;
            GUI.matrix = ms_matrixStack.Pop();
            return m;
        }

        ///// <summary>
        ///// GUI.depth only effect between different OnGUI calls, 
        ///// in same OnGUI(), change this has no effect;
        ///// </summary>
        //public static void PushDepth(int depth)
        //{
        //    ms_depthStack.Push(GUI.depth);
        //    GUI.depth = depth;
        //}

        //public static int PopDepth()
        //{
        //    int d = GUI.depth;
        //    GUI.depth = ms_depthStack.Pop();
        //    return d;
        //}

        public static void PushSkin(GUISkin skin)
        {
            ms_SkinStack.Push(GUI.skin);
            GUI.skin = skin;
        }

        public static GUISkin PopSkin()
        {
            GUISkin s = GUI.skin;
            GUI.skin = ms_SkinStack.Pop();
            return s;
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

        public static bool Button(string msg, Color c)
        {
            PushGUIColor(c);
            bool bClick = GUILayout.Button(msg);
            PopGUIColor();
            return bClick;
        }

        public static bool Button(string msg, string tips)
        {
            bool bClick = GUILayout.Button(new GUIContent(msg, tips));
            return bClick;
        }

        public static bool Button(string msg, string tips, Color c)
        {
            PushGUIColor(c);
            bool bClick = GUILayout.Button(new GUIContent(msg, tips));
            PopGUIColor();
            return bClick;
        }


    }

}
