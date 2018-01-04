#if UNITY_5 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
#define U5
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace MH
{

using MatchMap = System.Collections.Generic.Dictionary<string, MatchBone>;
using ConvertMap = System.Collections.Generic.Dictionary<string, string>; //<from path, to path>

/// <summary>
/// used to make a new animation clip that mirror the joints 
/// </summary>
public class AnimMirrorEditorWindow : EditorWindow
{
	#region "configurable data"
    // configurable data

    #endregion "configurable data"

	#region "data"
    // data

    private static AnimMirrorEditorWindow ms_Instance;
    private AnimationClip m_Clip;
    private Transform m_AnimRoot; //the animator/animation gameobject

    private Axis m_SymBoneAxis = Axis.YZ;
    private Axis m_NonSymBoneAxis = Axis.YZ;

    private MatchMap m_MatchMap;
    private List<Regex> m_REs;
    private List<string> m_ReplaceStrs;

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

//#if !UNITY_5_0
    [MenuItem("Window/Skele/Anim Mirror Tool")]
//#endif
    public static void OpenWindow()
    {
        ms_Instance = (AnimMirrorEditorWindow)GetWindow(typeof(AnimMirrorEditorWindow));
        EUtil.SetEditorWindowTitle(ms_Instance, "Animation Mirror Tool");
    }

    void OnEnable()
    {
        m_MatchMap = new MatchMap();
    }

    void OnInspectorUpdate()
    {
    }

    private Vector2 m_scrollPos = Vector2.zero;
    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        m_AnimRoot = EditorGUILayout.ObjectField("AnimatorGO", m_AnimRoot, typeof(Transform), true) as Transform;
        if( EditorGUI.EndChangeCheck() )
        {
            if( m_AnimRoot.GetComponent<Animator>() == null && 
                m_AnimRoot.GetComponent<Animation>() == null)
            {
                Dbg.LogWarn("The AnimatorGO must has Animation/Animator component!");
                m_AnimRoot = null;
            }
        }

        EditorGUI.BeginChangeCheck();
        m_Clip = EditorGUILayout.ObjectField("AnimClip", m_Clip, typeof(AnimationClip), false) as AnimationClip;
        if( EditorGUI.EndChangeCheck() )
        {
            _GetREs();
            _GenMatchMap();
        }

        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.MaxHeight(200f));
        {
            List<string> toDel = new List<string>();
            foreach( var entry in m_MatchMap )
            {
                string leftPath = entry.Key;
                string rightPath = entry.Value.path;
                bool bFound = entry.Value.found;

                GUILayout.BeginHorizontal();
                {
                    if(EUtil.Button("X", "Remove this entry", Color.red, GUILayout.Width(20)))
                    {
                        toDel.Add(leftPath);
                    }

                    string line;
                    if( bFound )
                    {
                        line = string.Format("{0} <-> {1}", leftPath, rightPath);
                    }
                    else
                    {
                        line = string.Format("{0} -> {1}", leftPath, rightPath);
                    }

                    GUILayout.Label(line);
                }
                GUILayout.EndHorizontal();
            }

            foreach( var d in toDel )
            {
                m_MatchMap.Remove(d);
            }
        }
        EditorGUILayout.EndScrollView();

        {
            m_SymBoneAxis = (Axis)EditorGUILayout.EnumPopup("Sym Bone Axis", m_SymBoneAxis, GUILayout.MaxWidth(200));
            m_NonSymBoneAxis = (Axis)EditorGUILayout.EnumPopup("Non-Sym Bone Axis", m_NonSymBoneAxis, GUILayout.MaxWidth(200));
        }

        EUtil.PushGUIEnable(m_AnimRoot != null && m_Clip != null);
        if( EUtil.Button("Apply Change!", Color.green))
        {
            string oriPath = AssetDatabase.GetAssetPath(m_Clip);
            //string oriName = Path.GetFileNameWithoutExtension(oriPath);
            string oriDir = Path.GetDirectoryName(oriPath);
            string newPath = EditorUtility.SaveFilePanelInProject("Save Mirrored clip", m_Clip.name + "_Mirror", "anim", "Select path", oriDir);
            if( ! string.IsNullOrEmpty(newPath) ) 
            {
                _CreateMirrorClip(newPath);
            }
        }
        EUtil.PopGUIEnable();
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

    private void _GenMatchMap()
    {
        EditorCurveBinding[] allBindings = AnimationUtility.GetCurveBindings(m_Clip);
        for( int idx = 0; idx < allBindings.Length; ++idx )
        {
            string thisPath = allBindings[idx].path;
            int matchREIdx = _IsMatchRE(thisPath);
            if (matchREIdx < 0)
            {
                continue;
            }
            else
            {
                --idx;
            }

            string thatPath = _GetMatchPath(thisPath, matchREIdx);
            bool bFound = _FindByPath(thatPath, allBindings);
            m_MatchMap[thisPath] = new MatchBone(thatPath, bFound);
            
             _ClearEntries(ref allBindings, thisPath, thatPath);
        }
    }

    private void _ClearEntries(ref EditorCurveBinding[] allBindings, string thisPath, string thatPath)
    {
        for(int idx = 0; idx < allBindings.Length; ++idx )
        {
            string p = allBindings[idx].path;
            if( p == thisPath || p == thatPath )
            {
                ArrayUtility.RemoveAt(ref allBindings, idx);
                --idx;
            }
        }
    }

    private bool _FindByPath(string thatPath, EditorCurveBinding[] allBindings)
    {
        foreach( var oneBinding in allBindings )
        {
            string onePath = oneBinding.path;
            if (thatPath == onePath)
                return true;
        }
        return false;
    }

    private string _GetMatchPath(string thisPath, int REidx)
    {
        Regex r = m_REs[REidx];
        string repl = m_ReplaceStrs[REidx];
        string newStr = r.Replace(thisPath, repl);
        return newStr;
    }

    private void _GetREs()
    {
        var res = AssetDatabase.LoadAssetAtPath(RELstPath, typeof(MirrorNameRegex)) as MirrorNameRegex;
        if( res == null )
        {
            res = ScriptableObject.CreateInstance<MirrorNameRegex>();
            var lst = res.m_REPrLst = new List<MirrorNameRegex.REPair>();
            lst.Add(new MirrorNameRegex.REPair("Left", "Right")); //mixamo 
            lst.Add(new MirrorNameRegex.REPair("_L\\b", "_R"));
            lst.Add(new MirrorNameRegex.REPair("\\.L\\b", ".R"));
            lst.Add(new MirrorNameRegex.REPair(" L ", " R "));  //bip
            lst.Add(new MirrorNameRegex.REPair("Right", "Left"));
            lst.Add(new MirrorNameRegex.REPair("_R\\b", "_L"));
            lst.Add(new MirrorNameRegex.REPair("\\.R\\b", ".L"));
            lst.Add(new MirrorNameRegex.REPair(" R ", " L "));
            AssetDatabase.CreateAsset(res, RELstPath);
        }

        m_REs = new List<Regex>();
        m_ReplaceStrs = new List<string>();
        foreach (var REPair in res.m_REPrLst)
        {
            m_REs.Add(new Regex(REPair.fromBoneRE));
            m_ReplaceStrs.Add(REPair.replaceString);
        }
    }

    private int _IsMatchRE(string p)
    {
        for(int idx = 0; idx < m_REs.Count; ++idx)
        {
            Regex r = m_REs[idx];
            if( r.IsMatch(p) )
            {
                return idx;
            }
        }

        return -1;
    }

    private void _CreateMirrorClip(string newPath)
    {
        ConvertMap convMap = new ConvertMap();
        foreach (var entry in m_MatchMap)
        {
            string fromPath = entry.Key;
            string toPath = entry.Value.path;
            bool bFound = entry.Value.found;

            convMap[fromPath] = toPath;
            if( bFound )
            {
                convMap[toPath] = fromPath;
            }
        }

        AnimationClip newClip = new AnimationClip();

        float totalTime = m_Clip.length;
        float deltaTime = 1 / m_Clip.frameRate;

        var allBindings = AnimationUtility.GetCurveBindings(m_Clip);
        //collect bindings based on same path
        Dictionary<string, CurveBindingGroup> bindingMap = new Dictionary<string, CurveBindingGroup>();        
        foreach( var oneBinding in allBindings )
        {
            string bindingPath = oneBinding.path;
            string bindingProp = oneBinding.propertyName;

            CurveBindingGroup grp = null;
            if( ! bindingMap.TryGetValue(bindingPath, out grp) )
            {
                grp = new CurveBindingGroup();
                grp.path = bindingPath;
                bindingMap.Add(bindingPath, grp);
            }

            if( bindingProp.StartsWith("m_LocalPosition") )
            {
                grp.HasPosCurves = true;
            }
            else if (bindingProp.StartsWith("m_LocalRotation") )
            {
                grp.HasRotCurves = true;
            }
            else
            {
                grp.OtherCurves.Add(oneBinding);
            }            
        }

        // fix
        foreach (var oneEntry in bindingMap)
        {
            string oldBindingPath = oneEntry.Key;
            CurveBindingGroup grp = oneEntry.Value;

            // get newBindingPath
            string newBindingPath = oldBindingPath;
            if( convMap.ContainsKey(oldBindingPath) )
            {
                newBindingPath = convMap[oldBindingPath];
            }

            Axis selfAxisValue = _GetAxisValue(oldBindingPath, convMap);
            Axis parentAxisValue = _GetAxisValueForParent(oldBindingPath, convMap);

            // fix rotation curve and bindingProp
            if( grp.HasRotCurves )
            {
                AnimationCurve xCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalRotation.x"));
                AnimationCurve yCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalRotation.y"));
                AnimationCurve zCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalRotation.z"));
                AnimationCurve wCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalRotation.w"));

                AnimationCurve newXCurve = null;
                AnimationCurve newYCurve = null;
                AnimationCurve newZCurve = null;
                AnimationCurve newWCurve = null;

                if( parentAxisValue != selfAxisValue )
                {
                    newXCurve = new AnimationCurve();
                    newYCurve = new AnimationCurve();
                    newZCurve = new AnimationCurve();
                    newWCurve = new AnimationCurve();

                    Vector3 planeNormal = Vector3.zero;
                    switch (parentAxisValue)
                    {
                        case Axis.XY: planeNormal = Vector3.forward; break;
                        case Axis.XZ: planeNormal = Vector3.up; break;
                        case Axis.YZ: planeNormal = Vector3.right; break;
                        default: Dbg.LogErr("AnimMirrorEditorWindow._CreateMirrorClip: unexpected parentAxisValue: {0}", parentAxisValue); break;
                    }

                    for (float t = 0; t <= totalTime; )
                    {
                        Quaternion oldQ = _BakeQ(xCurve, yCurve, zCurve, wCurve, t);
                        Quaternion newQ = _ReflectQ(oldQ, selfAxisValue, planeNormal);
                        newXCurve.AddKey(t, newQ.x);
                        newYCurve.AddKey(t, newQ.y);
                        newZCurve.AddKey(t, newQ.z);
                        newWCurve.AddKey(t, newQ.w);

                        if (Mathf.Approximately(t, totalTime))
                            break;
                        t = Mathf.Min(totalTime, t + deltaTime);
                    } 
                }
                else
                {
                    newXCurve = xCurve;
                    newYCurve = yCurve;
                    newZCurve = zCurve;
                    newWCurve = wCurve;

                    switch (parentAxisValue)
                    {
                        case Axis.XY:
                            {
                                newXCurve = _NegateCurve(xCurve);
                                newYCurve = _NegateCurve(yCurve);
                            }
                            break;
                        case Axis.XZ:
                            {
                                newXCurve = _NegateCurve(xCurve);
                                newZCurve = _NegateCurve(zCurve);
                            }
                            break;
                        case Axis.YZ:
                            {
                                newYCurve = _NegateCurve(yCurve);
                                newZCurve = _NegateCurve(zCurve);
                            }
                            break;
                        default:
                            Dbg.LogErr("AnimMirrorEditorWindow._CreateMirrorClip: unexpected parentAxisValue: {0}", parentAxisValue);
                            break;
                    }
                }

                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalRotation.x"), newXCurve);
                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalRotation.y"), newYCurve);
                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalRotation.z"), newZCurve);
                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalRotation.w"), newWCurve);
            }

            // fix position curve
            if (grp.HasPosCurves)
            {
                AnimationCurve xCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalPosition.x"));
                AnimationCurve yCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalPosition.y"));
                AnimationCurve zCurve = AnimationUtility.GetEditorCurve(m_Clip, EditorCurveBinding.FloatCurve(oldBindingPath, typeof(Transform), "m_LocalPosition.z"));
                AnimationCurve newXCurve = xCurve;
                AnimationCurve newYCurve = yCurve;
                AnimationCurve newZCurve = zCurve;

                Axis posAxisValue = _GetAxisValueForParent(oldBindingPath, convMap);
                switch (posAxisValue)
                {
                    case Axis.XZ:
                        {
                            newYCurve = _NegateCurve(newYCurve);
                        }
                        break;
                    case Axis.XY:
                        {
                            newZCurve = _NegateCurve(newZCurve);
                        }
                        break;
                    case Axis.YZ:
                        {
                            newXCurve = _NegateCurve(newXCurve);
                        }
                        break;
                    default:
                        Dbg.LogErr("AnimMirrorEditorWindow._CreateMirrorClip: unexpected mirror axis value (2nd): {0}", posAxisValue);
                        break;
                }

                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalPosition.x"), newXCurve);
                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalPosition.y"), newYCurve);
                AnimationUtility.SetEditorCurve(newClip, EditorCurveBinding.FloatCurve(newBindingPath, typeof(Transform), "m_LocalPosition.z"), newZCurve);
            }

            // other curves
            foreach( var oneBinding in grp.OtherCurves )
            {
                Type tp = oneBinding.type;
                string propName = oneBinding.propertyName;

                if (oneBinding.isPPtrCurve)
                {
                    EditorCurveBinding newBinding = EditorCurveBinding.PPtrCurve(newBindingPath, tp, propName);
                    ObjectReferenceKeyframe[] array = AnimationUtility.GetObjectReferenceCurve(m_Clip, oneBinding);
                    AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, array); //add new
                }
                else
                {
                    EditorCurveBinding newBinding = EditorCurveBinding.FloatCurve(newBindingPath, tp, propName);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(m_Clip, oneBinding);
                    AnimationUtility.SetEditorCurve(newClip, newBinding, curve); //add new
                }
            }

        } //end of foreach

        // finishing part
#if !U5
        var oldAnimType = (ModelImporterAnimationType)RCall.CallMtd("UnityEditor.AnimationUtility", "GetAnimationType", null, m_Clip);
        AnimationUtility.SetAnimationType(newClip, oldAnimType);
#endif

        AnimationClipSettings oldSettings = AnimationUtility.GetAnimationClipSettings(m_Clip);
        RCall.CallMtd("UnityEditor.AnimationUtility", "SetAnimationClipSettings", null, newClip, oldSettings);

        newClip.EnsureQuaternionContinuity();

        // save to disk
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath(newPath, typeof(AnimationClip)) as AnimationClip;
        if( existingClip != null )
        {
            EditorUtility.CopySerialized(newClip, existingClip);
        }
        else
        {
            AssetDatabase.CreateAsset(newClip, newPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Dbg.Log("Created mirror-ed animation clip at: {0}", newPath);
    }

    private Quaternion _ReflectQ(Quaternion oldQ, Axis axisValue, Vector3 planeNormal)
    {
        switch (axisValue)
        {
            case Axis.XY: return QUtil.Reflect_XY(oldQ, planeNormal);
            case Axis.XZ: return QUtil.Reflect_XZ(oldQ, planeNormal);
            case Axis.YZ: return QUtil.Reflect_YZ(oldQ, planeNormal);

            default:
                Dbg.LogErr("AnimMirrorEditorWindow._ReflectQ: unexpected axisValue: {0}", axisValue);
                return Quaternion.identity;
        }
    }

    private Quaternion _BakeQ(AnimationCurve xCurve, AnimationCurve yCurve, AnimationCurve zCurve, AnimationCurve wCurve, float t)
    {
        float x = xCurve.Evaluate(t);
        float y = yCurve.Evaluate(t);
        float z = zCurve.Evaluate(t);
        float w = wCurve.Evaluate(t);

        Quaternion q = new Quaternion(x, y, z, w);
        return QUtil.Normalize(q);
    }

    private Axis _GetAxisValue(string bindingPath, ConvertMap convMap)
    {
        if( convMap.ContainsKey(bindingPath) )
        {
            return m_SymBoneAxis;
        }
        else
        {
            return m_NonSymBoneAxis;
        }
    }

    /// <summary>
    /// get the axis value for the parent joint of the specified bindingPath
    /// </summary>
    private Axis _GetAxisValueForParent(string bindingPath, ConvertMap convMap)
    {
        Transform curBone = m_AnimRoot.Find(bindingPath);
        if( curBone == null )
        {
            Dbg.LogErr("AnimMirrorEditorWindow._GetAxisValueForParent: failed to find transform named: {0}, Did you specify wrong AnimRoot?", bindingPath);
            return Axis.XZ;    
        }

        if( curBone == m_AnimRoot )
        {
            return m_NonSymBoneAxis; //if bindingPath points to AnimRoot, then just return non-sym bone axis, should be OK?
        }

        Transform parentBone = curBone.parent;

        string trPath = AnimationUtility.CalculateTransformPath(parentBone, m_AnimRoot);

        if( convMap.ContainsKey(trPath) )
        {
            return m_SymBoneAxis;
        }
        else
        {
            return m_NonSymBoneAxis;
        }
    }

    private static AnimationCurve _NegateCurve(AnimationCurve oldCurve)
    {
        var keys = oldCurve.keys;
        //Dbg.Log("{0}|{1}, keys cnt: {2}", bindingPath, bindingProp, oldCurve.length);
        for (int keyIdx = 0; keyIdx < keys.Length; ++keyIdx)
        {
            keys[keyIdx].value = -keys[keyIdx].value;
        }
        AnimationCurve theCurve = new AnimationCurve(keys);
        return theCurve;
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string RELstPath = MirrorCtrl.RELstPath;



    #endregion "constant data"

	#region "inner struct"
	// "inner struct" 

    /// <summary>
    /// contains curvebindings for one path
    /// </summary>
    public class CurveBindingGroup
    {
        public string path;
        public bool HasPosCurves = false;
        public bool HasRotCurves = false;
        public List<EditorCurveBinding> OtherCurves = new List<EditorCurveBinding>();
    }
	
	#endregion "inner struct"
}



}
