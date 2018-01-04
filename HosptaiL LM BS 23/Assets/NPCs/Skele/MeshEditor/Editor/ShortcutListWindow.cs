using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class ShortcutListWindow : EditorWindow
    {
        private static ShortcutListWindow sm_Instance = null;

        public static void OpenWindow()
        {
            sm_Instance = (ShortcutListWindow)GetWindow(typeof(ShortcutListWindow), true, "Shortcuts", true);
        }

        void OnDestroy()
        {
            sm_Instance = null;
        }

        public static void ToggleWindow()
        {
            if( sm_Instance == null )
            {
                OpenWindow();
            }
            else
            {
                sm_Instance.Close();
            }
        }

        void OnGUI()
        {
            EUtil.PushGUIEnable(false);
            EditorGUILayout.TextArea(
@"W/E/R: move/rotate/scale
Q: Focus on pivot
A: Select all/none
Z: Toggle transparent mode
B: Toggle Border-selection
O: Toggle Soft-selection
S: Toggle Pivot-Orientation
D: Toggle Pivot-Position
[ ]: Tune the soft-selection range 
ESC: Cancel

Ctrl+RMB: Set 3D-cursor position
Ctrl+LMB: Loop selection

Alt+LMB: Rotate around pivot
Mousewheel: zoom in/out
MMB: Pan view
"
            );
            EUtil.PopGUIEnable();
        }
    }
}
