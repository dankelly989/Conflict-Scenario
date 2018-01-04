using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    /// <summary>
    /// used to rotate scene view like blender does
    /// </summary>
	class ViewRotate
	{
        public static void RotateViewByEvent()
        {
            Event e = Event.current;

            if( e.rawType == EventType.KeyDown )
            {
                switch( e.keyCode )
                {
                    case KeyCode.Keypad1: 
                    case KeyCode.End:
                        RotateView(e.control ? Dir.Back : Dir.Front); break;
                    case KeyCode.Keypad3:
                    case KeyCode.PageDown:
                        RotateView(e.control ? Dir.Left : Dir.Right); break;
                    case KeyCode.Keypad7: 
                    case KeyCode.Home:
                        RotateView(e.control ? Dir.Bottom : Dir.Top); break;
                    case KeyCode.Keypad5: ToggleOrtho(); break;
                }
            }
        }

        public static void ToggleOrtho()
        {
            var v = EUtil.GetSceneView();
            v.orthographic = !v.orthographic; 
        }

        public static void RotateView(Dir d, bool ortho = true)
        {
            int idx = d - Dir.Front;
            EUtil.RotateView(Quaternion.LookRotation(DirVec[idx]), ortho);
        }
        
        public enum Dir
        {
            Front, Back, Left, Right, Top, Bottom, END
        }
        private readonly static Vector3[] DirVec = {
            Vector3.back,
            Vector3.forward,
            Vector3.right, //left-view look right
            Vector3.left,  //right-view look left
            Vector3.down, 
            Vector3.up
        };
	}
}
