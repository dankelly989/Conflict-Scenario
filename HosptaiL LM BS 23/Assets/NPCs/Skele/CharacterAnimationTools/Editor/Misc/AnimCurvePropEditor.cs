using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace MH
{

public class AnimCurvePropEditor : EditorWindow
{
	#region "data"
    // data

    private static AnimCurvePropEditor ms_Instance;
    private OpMode m_OpMode = OpMode.AddPathPrefixOnMultiCurveBinding;
    private AnimationClip m_CurClip = null;
    //private object m_uawstate = null;

    private Vector2 m_scrollPos = Vector2.zero;

    private string m_Prefix = string.Empty;

    private string m_NewType = string.Empty;
    private string m_NewPath = string.Empty;
    private string m_NewProperty = string.Empty;
    private bool m_bIsPPtrCurve = false;
    private string m_TypeFullName = string.Empty;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    [MenuItem("Window/Skele/AnimCurvePropEditor")]
    public static void OpenWindow()
    {
        ms_Instance = (AnimCurvePropEditor)GetWindow(typeof(AnimCurvePropEditor));
        EUtil.SetEditorWindowTitle(ms_Instance, "AnimCurvePropEditor");
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }


    void OnGUI()
    {
        if( !EUtil.IsUnityAnimationWindowOpen() )
        {
            EditorGUILayout.LabelField("Animation Window is not open...");
            return;
        }

        // mode switch
        EditorGUILayout.BeginHorizontal();
        {
            EUtil.PushGUIEnable(m_OpMode != OpMode.AddPathPrefixOnMultiCurveBinding);
            if (EUtil.Button("Batch AddPrefix", Color.green) )
            {
                _OnOpModeChange(m_OpMode, OpMode.AddPathPrefixOnMultiCurveBinding);
                m_OpMode = OpMode.AddPathPrefixOnMultiCurveBinding;
            }
            EUtil.PopGUIEnable();

            EUtil.PushGUIEnable(m_OpMode != OpMode.ChangePath);
            if (EUtil.Button("Change Path", Color.green))
            {
                _OnOpModeChange(m_OpMode, OpMode.ChangePath);
                m_OpMode = OpMode.ChangePath;
            }
            EUtil.PopGUIEnable();

            EUtil.PushGUIEnable(m_OpMode != OpMode.EditSingleCurveBinding);
            if(EUtil.Button("Edit Single Curve", Color.green) )
            {
                _OnOpModeChange(m_OpMode, OpMode.EditSingleCurveBinding);
                m_OpMode = OpMode.EditSingleCurveBinding;
            }
            EUtil.PopGUIEnable();

            EUtil.PushGUIEnable(m_OpMode != OpMode.AddNewCurve);
            if( EUtil.Button("Add New Curve", Color.green))
            {
                _OnOpModeChange(m_OpMode, OpMode.AddNewCurve);
                m_OpMode = OpMode.AddNewCurve;
            }
            EUtil.PopGUIEnable();
        }
        EditorGUILayout.EndHorizontal();

        // specific GUI

        switch( m_OpMode )
        {
            case OpMode.AddPathPrefixOnMultiCurveBinding:
                {
                    _GUI_BatchAddPrefix();
                }
                break;
            case OpMode.ChangePath:
                {
                    _GUI_ChangePath();
                }
                break;
            case OpMode.EditSingleCurveBinding:
                {
                    _GUI_EditSingleCurve();
                }
                break;
            case OpMode.AddNewCurve:
                {
                    _GUI_AddNewCurve();
                }
                break;
            default:
                Dbg.LogErr("AnimCurvePropEditor.OnGUI: unexpected OpMode: {0}", m_OpMode);
                break;
        }
    }

    void OnDestroy()
    {
    }

    void OnPlayModeChanged()
    {
        if (ms_Instance != null)
        {
            ms_Instance.Close();
        }
    }
	
    #endregion "unity event handlers"

	#region "public method"
    // public method

    #endregion "public method"

	#region "private method"

    private void _OnOpModeChange(OpMode oldMode, OpMode newMode)
    {
        switch(newMode)
        {
            case OpMode.EditSingleCurveBinding:
                {
                    m_NewType = string.Empty;
                }
                break;
        }
    }

    private void _ModifyCurveBinding(EditorCurveBinding oneBinding, string newPath, string newProp, Type newType)
    {
        //object animWndCurve = oneInfo.m_AnimationWindowCurve;
        if (oneBinding.isPPtrCurve)
        {
            EditorCurveBinding newBinding = EditorCurveBinding.PPtrCurve( newPath,
                newType, newProp);

            //var array = (ObjectReferenceKeyframe[])RCall.CallMtd("UnityEditorInternal.AnimationWindowCurve", "ToObjectCurve", animWndCurve);
            ObjectReferenceKeyframe[] array = AnimationUtility.GetObjectReferenceCurve(m_CurClip, oneBinding);
            AnimationUtility.SetObjectReferenceCurve(m_CurClip, newBinding, array); //add new

            AnimationUtility.SetObjectReferenceCurve(m_CurClip, oneBinding, null); //remove old
        }
        else
        {
            EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve(newPath,
                newType, newProp);

            //AnimationCurve curve = (AnimationCurve)RCall.CallMtd("UnityEditorInternal.AnimationWindowCurve", "ToAnimationCurve", animWndCurve);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(m_CurClip, oneBinding);
            Dbg.Assert(curve != null, "AnimCurvePropEditor._ModifyCurveBinding: failed to get editorCurve");
            AnimationUtility.SetEditorCurve(m_CurClip, newBinding, curve); //add new

            AnimationUtility.SetEditorCurve(m_CurClip, oneBinding, null); //remove old
        }
    }

    private void _GUI_BatchAddPrefix()
    {
        EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
        object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
        m_CurClip = RCall.GetProp("UnityEditorInternal.AnimationWindowState",
                "activeAnimationClip", uawstate) as AnimationClip;

        //1. collect the TrPaths from active curves
        List<BindingInfo> bindingInfos = new List<BindingInfo>();

        IList curves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "activeCurves", uawstate);
        for (int idx = 0; idx < curves.Count; ++idx)
        {
            object oneCurve = curves[idx]; //AnimationWindowCurve
            EditorCurveBinding oneBinding = (EditorCurveBinding)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "binding", oneCurve);
            bindingInfos.Add(new BindingInfo(oneBinding, oneCurve));
        }

        //2. display
        EditorGUILayout.LabelField("Selected curves:");
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, false, true, GUILayout.Height(200f));
        {
            foreach (var oneInfo in bindingInfos)
            {
                EditorCurveBinding oneBinding = oneInfo.m_Binding;
                GUILayout.BeginHorizontal();
                {
                    EUtil.PushContentColor(Color.yellow);
                    GUILayout.Label(oneBinding.path);
                    EUtil.PopContentColor();
                    EUtil.PushContentColor(Color.green);
                    GUILayout.Label(oneBinding.propertyName);
                    EUtil.PopContentColor();
                }
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        //3. apply prefix
        m_Prefix = EditorGUILayout.TextField("Prefix:", m_Prefix);

        EUtil.PushGUIEnable(m_CurClip != null && bindingInfos.Count > 0);
        if (EUtil.Button("Apply Prefix", Color.green))
        {
            _DoApplyPrefix(bindingInfos, m_Prefix);
        }
        EUtil.PopGUIEnable();
    }

    private void _DoApplyPrefix(List<BindingInfo> bindingInfos, string prefix)
    {
        if (prefix.EndsWith("/"))
        {
            prefix = prefix.Substring(0, prefix.Length - 1);
        }

        Undo.RegisterCompleteObjectUndo(m_CurClip, "Apply Prefix");
        foreach (var oneInfo in bindingInfos)
        {
            EditorCurveBinding oneBinding = oneInfo.m_Binding;
            _ModifyCurveBinding(oneBinding, prefix + "/" + oneBinding.path, oneBinding.propertyName, oneBinding.type);
        }

    }

    private void _GUI_EditSingleCurve()
    {
        EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
        object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
        m_CurClip = RCall.GetProp("UnityEditorInternal.AnimationWindowState",
                "activeAnimationClip", uawstate) as AnimationClip;

        IList curves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "activeCurves", uawstate);
        if (curves.Count != 1)
        {
            EditorGUILayout.LabelField(string.Format("Please select ONE curve, you have currently selected {0} curves", curves.Count));
            return;
        }

        object oneCurve = curves[0]; //AnimationWindowCurve
        EditorCurveBinding oneBinding = (EditorCurveBinding)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "binding", oneCurve);

        EditorGUILayout.LabelField("Path:");
        EUtil.PushContentColor(Color.yellow);
        EditorGUILayout.SelectableLabel(oneBinding.path);
        EUtil.PopContentColor();

        EditorGUILayout.LabelField("Property:");
        EUtil.PushContentColor(Color.green);
        EditorGUILayout.SelectableLabel(oneBinding.propertyName);
        EUtil.PopContentColor();

        EditorGUILayout.LabelField("IsPPtrCurve:  " + oneBinding.isPPtrCurve.ToString() );

        EditorGUILayout.Separator();

        m_NewType = EditorGUILayout.TextField("Type:", m_NewType);
        if (m_NewType.Length == 0)
            m_NewType = oneBinding.type.ToString();

        m_NewPath = EditorGUILayout.TextField("New Path:", m_NewPath);
        m_NewProperty = EditorGUILayout.TextField("New Property:", m_NewProperty);

        Type newTp = RCall.GetTypeFromString(m_NewType, true);

        EUtil.PushGUIEnable(m_NewProperty.Length > 0 && newTp != null);
        if (EUtil.Button("Apply Change", Color.green))
        {
            Undo.RegisterCompleteObjectUndo(m_CurClip, "Apply Curve Change");
            _ModifyCurveBinding(oneBinding, m_NewPath, m_NewProperty, newTp);
        }
        EUtil.PopGUIEnable();

    }

    private void _GUI_AddNewCurve()
    {
        EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
        object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
        m_CurClip = RCall.GetProp("UnityEditorInternal.AnimationWindowState",
                "activeAnimationClip", uawstate) as AnimationClip;

        if( m_CurClip == null )
        {
            EditorGUILayout.LabelField("Need an animation clip first...");
            return;
        }

        m_NewPath = EditorGUILayout.TextField("Path:", m_NewPath);
        m_NewProperty = EditorGUILayout.TextField("Property:", m_NewProperty);
        m_TypeFullName = EditorGUILayout.TextField("TypeFullName:", m_TypeFullName);
        m_bIsPPtrCurve = EditorGUILayout.Toggle("IsPPtrCurve:", m_bIsPPtrCurve);

        EditorGUILayout.Separator();

        bool bOK = true;
        EUtil.PushGUIColor(Color.red);
        if( string.IsNullOrEmpty(m_NewProperty) )
        {
            bOK = false;
            EditorGUILayout.LabelField("Property is not specified");
        }
        if( string.IsNullOrEmpty(m_TypeFullName) || RCall.GetTypeFromString(m_TypeFullName, true) == null)
        {
            bOK = false;
            EditorGUILayout.LabelField(string.Format("No type is found for name: {0}", m_TypeFullName));
        }
        EUtil.PopGUIColor();

        EUtil.PushGUIEnable(bOK);
        {
            if( EUtil.Button("Add New Curve", Color.green) )
            {
                Type tp = RCall.GetTypeFromString(m_TypeFullName);
                if (m_bIsPPtrCurve)
                {
                    EditorCurveBinding newBinding = EditorCurveBinding.PPtrCurve(m_NewPath, tp, m_NewProperty);
                    AnimationUtility.SetObjectReferenceCurve(m_CurClip, newBinding, new ObjectReferenceKeyframe[0]);
                }
                else
                {
                    EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve(m_NewPath, tp, m_NewProperty);
                    AnimationUtility.SetEditorCurve(m_CurClip, newBinding, new AnimationCurve());
                }
            }
        }
        EUtil.PopGUIEnable();
    }

    private void _GUI_ChangePath()
    {
        EditorWindow uaw = (EditorWindow)EUtil.GetUnityAnimationWindow();
        object uawstate = EUtil.GetUnityAnimationWindowState(uaw);
        m_CurClip = RCall.GetProp("UnityEditorInternal.AnimationWindowState",
                "activeAnimationClip", uawstate) as AnimationClip;

        //1. collect the TrPaths from active curves
        List<EditorCurveBinding> bindingInfos = new List<EditorCurveBinding>();

        IList curves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "activeCurves", uawstate);
        if( curves.Count <= 0 )
        {
            EditorGUILayout.LabelField("Please Select a Curve first...");
            return;
        }

        EditorCurveBinding theBinding = (EditorCurveBinding)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "binding", curves[0]);
        string toChangePath = theBinding.path;

        IList allCurves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "allCurves", uawstate);
        foreach(var oneUAWCurve in allCurves)
        {
            EditorCurveBinding oneBinding = (EditorCurveBinding)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "binding", oneUAWCurve);
            if( oneBinding.path == toChangePath )
            {
                bindingInfos.Add(oneBinding);
            }
        }

        //2. display
        EditorGUILayout.LabelField("Affected curves:");
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, false, true, GUILayout.Height(200f));
        {
            foreach (var oneInfo in bindingInfos)
            {
                EditorCurveBinding oneBinding = oneInfo;
                GUILayout.BeginHorizontal();
                {
                    EUtil.PushContentColor(Color.yellow);
                    GUILayout.Label(oneBinding.path);
                    EUtil.PopContentColor();
                    EUtil.PushContentColor(Color.green);
                    GUILayout.Label(oneBinding.propertyName);
                    EUtil.PopContentColor();
                }
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        //3. apply prefix
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("R", GUILayout.Width(20)))
        {
            GUIUtility.keyboardControl = 0;
            m_NewPath = toChangePath;
        }
        m_NewPath = EditorGUILayout.TextField("New Path:", m_NewPath);
        EditorGUILayout.EndHorizontal();

        EUtil.PushGUIEnable(m_CurClip != null && bindingInfos.Count > 0);
        if (EUtil.Button("Change Path!", Color.green))
        {
            _DoChangePath(bindingInfos, m_NewPath);
        }
        EUtil.PopGUIEnable();
    }

    private void _DoChangePath(List<EditorCurveBinding> bindingInfos, string newPath)
    {
        Undo.RegisterCompleteObjectUndo(m_CurClip, "Change Path");
        foreach (var oneBinding in bindingInfos)
        {            
            _ModifyCurveBinding(oneBinding, newPath, oneBinding.propertyName, oneBinding.type);
        }
    }

    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    public enum OpMode
    {
        INVALID = -1,
        ChangePath,
        EditSingleCurveBinding,
        AddPathPrefixOnMultiCurveBinding,
        AddNewCurve,
    }

    #endregion "constant data"

	#region "Inner struct"
	// "Inner struct" 

    public class BindingInfo
    {
        public EditorCurveBinding m_Binding;
        public object m_AnimationWindowCurve;
        public BindingInfo(EditorCurveBinding binding, object curve)
        {
            m_Binding = binding;
            m_AnimationWindowCurve = curve;
        }
    }
	
	#endregion "Inner struct"
}

}
