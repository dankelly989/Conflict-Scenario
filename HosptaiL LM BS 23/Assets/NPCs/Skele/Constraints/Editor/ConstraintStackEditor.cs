using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MH.Skele;

namespace MH.Constraints
{
    [CustomEditor(typeof(ConstraintStack))]
    public class ConstraintStackEditor : Editor
    {
		#region "data"
	    // data

        private SerializedProperty m_spInitInfo;

	    #endregion "data"
	
		#region "unity event handlers"
	    // unity event handlers

        void OnEnable()
        {
            m_spInitInfo = serializedObject.FindProperty("m_initInfo");
            Dbg.Assert(m_spInitInfo != null, "ConstraintStrackEditor.OnEnable: failed to get property: m_initInfo");
        }

        void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            ConstraintStack cstack = (ConstraintStack)target;

            serializedObject.Update();

            GUILayout.Space(3f);

            // constraints
            for (int i = 0; i < cstack.constraintCount; ++i)
            {
                BaseConstraint c = cstack.Get(i);
                if (!c)
                { //remove broken reference
                    cstack.RemoveAt(i);
                    --i;
                    continue;
                }

                // draw constraint & buttons
                EditorGUILayout.BeginHorizontal();
                {
                    Color btnColor = c.IsActiveConstraint ? Color.white : Color.red;
                    EUtil.PushBackgroundColor(btnColor);
                    var content = new GUIContent(c.GetType().Name, "Click to fold other components");
                    Rect rc = GUILayoutUtility.GetRect(content, EditorStyles.toolbarButton);
                    if (GUI.Button(rc, content, EditorStyles.toolbarButton))
                    {
                        _FoldComponents(cstack);
                        var wnd = EditorEditorWindow.OpenWindowWithActivatorRect(c, rc);
                        EUtil.SetEditorWindowTitle(wnd, content.text);
                    }
                    EditorGUI.ProgressBar(rc, c.Influence, content.text);
                    EUtil.PopBackgroundColor();

                    if (GUILayout.Button(new GUIContent(c.IsActiveConstraint ? EConUtil.activeBtn : EConUtil.inactiveBtn, "Toggle constraint active state"), EditorStyles.toolbarButton, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        c.IsActiveConstraint = !c.IsActiveConstraint;
                        EditorUtility.SetDirty(cstack);
                    }

                    EUtil.PushGUIEnable(i != 0);
                    if (GUILayout.Button(new GUIContent(EConUtil.upBtn, "move up"), EditorStyles.toolbarButton, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        cstack.Swap(i, i - 1);
                        EditorUtility.SetDirty(cstack);
                        //ComponentUtility.MoveComponentUp(c);
                    }
                    EUtil.PopGUIEnable();

                    EUtil.PushGUIEnable(i != cstack.constraintCount - 1);
                    if (GUILayout.Button(new GUIContent(EConUtil.downBtn, "move down"), EditorStyles.toolbarButton, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        cstack.Swap(i, i + 1);
                        EditorUtility.SetDirty(cstack);
                        //ComponentUtility.MoveComponentDown(c);
                    }
                    EUtil.PopGUIEnable();

                    if (GUILayout.Button(new GUIContent(EConUtil.deleteBtn, "delete the constraint from stack"), EditorStyles.toolbarButton, GUILayout.Height(20), GUILayout.Width(20)))
                    {
                        MUndo.RecordObject(cstack, "Remove Constraint");
                        cstack.RemoveAt(i);
                        EditorUtility.SetDirty(cstack);
                        _FoldComponents(cstack);
                        EditorGUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndHorizontal();

            } //for(int i=0; i<cstack.constraintCount; ++i)

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            int newOrder = EditorGUILayout.IntField(new GUIContent("Exec Order", "used to help decide evaluation order, the smaller the earlier"), cstack.ExecOrder);
            if (EditorGUI.EndChangeCheck())
                cstack.ExecOrder = newOrder;

            { //new constraint window
                EUtil.DrawSplitter(new Color(1, 1, 1, 0.3f));

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15f);
                var content = new GUIContent("Add Constraint", "Add new constraint into current stack");
                Rect btnRc = GUILayoutUtility.GetRect(content, GUI.skin.button);
                if (GUI.Button(btnRc, content))
                {
                    var wnd = ScriptableObject.CreateInstance<ConstraintsWindow>();
                    Rect wndRc = EUtil.GetRectByActivatorRect(wnd.position, btnRc);
                    wnd.position = wndRc;
                    wnd.SetConstraintStack(cstack);
                    wnd.ShowPopup();
                    wnd.Focus();
                }
                GUILayout.Space(15f);
                EditorGUILayout.EndHorizontal();
            }
            

            if( Pref.ShowInitInfos)
                EditorGUILayout.PropertyField(m_spInitInfo, true);



            serializedObject.ApplyModifiedProperties();
        }

	    #endregion "unity event handlers"
	
		#region "public method"
	    // public method
	
	    #endregion "public method"
	
		#region "private method"

        private static void _FoldComponents(ConstraintStack cstack)
        {
            EUtil.FoldInspectorComponents(cstack.gameObject, FoldIgnoreComponents);
            //Transform tr = cstack.transform;
            //var insWnd = EditorWindow.focusedWindow;
            //ActiveEditorTracker tracker = (ActiveEditorTracker)RCall.CallMtd("UnityEditor.InspectorWindow", "GetTracker", insWnd, null);
            //var editors = tracker.activeEditors;
            //for (int eidx = 0; eidx < editors.Length; ++eidx)
            //{
            //    var e = editors[eidx];
            //    bool shouldOpen = (e.target == cstack || e.target == tr);
            //    tracker.SetVisible(eidx, shouldOpen ? 1 : 0); //fold
            //    //RCall.CallMtd("UnityEditor.Editor", "InternalSetHidden", e, true); //hide
            //}
        }

	    // private method
	
	    #endregion "private method"
	
		#region "constant data"
	    // constant data

        private readonly static string[] FoldIgnoreComponents = { "ConstraintStack", "Transform" };
	
	    #endregion "constant data"
    }
}
