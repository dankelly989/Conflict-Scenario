#if UNITY_5 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
#define U5
#endif

using System;
using System.Collections.Generic;
using UnityEditor;

using MH;
using UnityEngine;

using EvtGORefDict = System.Collections.Generic.Dictionary<UnityEngine.Transform, bool>;
using StrLst = System.Collections.Generic.List<System.String>;
using TrLst = System.Collections.Generic.List<UnityEngine.Transform>;
using System.Collections;

/// <summary>
/// Cutscene controller 's editor
/// </summary>
public class CCEditor : EditorWindow
{
	#region "configurable data"
    // configurable data

    #endregion "configurable data"

	#region "data"
    // data

	#region "Unity internal data"
    // internal data: base info per update
    private object m_UAW; //the UAW instance, could be null
    private object m_AnimationWindowState; //the animation window state [internal class]
    private object m_AnimationEventPopup; //the timeline animation event editor popup

    private Transform m_ClipRoot; //the GO which has the Animation/Animator component for current clip
    private Transform m_evtGoRoot; //the root of all event GOs
    private AnimationClip m_CurClip; //current editing animation clip
	
	#endregion "Unity internal data"

    private static CCEditor ms_instance = null;

    // data
    private CutsceneController m_CC;
    private bool m_bIsUAWOpen = false; //is UnityAnimationWindow open (abbr. UAW)
    private EvtGORefDict m_evtGORefDict; //in every update, used to check if a EventGO is ref-ed by AnimEvt
    private bool m_bShowMark4SelectedCurves = true; //if enabled, will draw marks for selected curves' transform

    // time & counter
    //private int m_loopCnt = 0;

    //private double m_curTime;
    //private float m_deltaTime;

    //tmp storage
    private HashSet<string> m_StrSet = new HashSet<string>();
    
    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	#region "Init & Fini"
	// "Init & Fini" 

    [MenuItem("Window/Skele/CCEditor")]
    public static void OpenWindow()
    {
        if (ms_instance == null)
        {
            CCEditor inst = (CCEditor)GetWindow(typeof(CCEditor));

            inst.m_evtGORefDict = new EvtGORefDict();
            //inst.m_curTime = EditorApplication.timeSinceStartup;

            EditorApplication.playmodeStateChanged += OnPlayModeChanged;
            SceneView.onSceneGUIDelegate += inst.OnSceneGUI;

            ms_instance = inst;
            //Dbg.Log("CCEditor Created...");
        }
    }

    void OnDestroy()
    {
        ms_instance = null;

        EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        //Dbg.Log("CCEditor Closed...");
    }

    private static void OnPlayModeChanged()
    {
        //if (EditorApplication.isPlayingOrWillChangePlaymode)
        //{
        //    ms_instance.Close();
        //}
    }

	#endregion "Init & Fini"

    void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
            
        bool bIsCutScene = m_evtGoRoot != null;
        EUtil.PushGUIColor(bIsCutScene ? Color.green : Color.red);
        EditorGUILayout.LabelField(string.Format("AnimClip Root: {0}",
            (m_ClipRoot == null ? "null" : m_ClipRoot.name)
        ));
        EUtil.PopGUIColor();


        Color c;
        String msg;
        if( bIsCutScene )
        {
            if( EUtil.Button("Sync AnimEvents with EventGO", Color.green) )
            {
                _MatchEventGOAndAnimEvent();
            }

            if( EUtil.Button("Remove Animation/Animator on decendants", Color.yellow))
            {
                _RemoveAnimCompsOnDecendants();
            }

            if( EUtil.Button("Fix Duplicate Transform Path", Color.white) )
            {
                _FixDuplicateTransformPath();
            }

            c = m_bShowMark4SelectedCurves ? Color.green : Color.red;
            msg = m_bShowMark4SelectedCurves ? "Hide Marks for Selected Curves" : "Show Marks for Selected Curves";
            if( EUtil.Button(msg, c) )
            {
                m_bShowMark4SelectedCurves = !m_bShowMark4SelectedCurves;
                SceneView.RepaintAll();
            }
        }
        else
        {
            if (EUtil.Button("New CCRoot", Color.green))
            {
                GameObject newCCRoot = new GameObject();
                newCCRoot.name = "NewCCRoot";
                newCCRoot.AddComponent<CutsceneController>();
                var anim = newCCRoot.AddComponent<Animation>();
                anim.playAutomatically = false;

                Misc.ForceGetGO(EventGOS, newCCRoot);
                Selection.activeGameObject = newCCRoot;
            }

            c = m_bShowMark4SelectedCurves ? Color.green : Color.red;
            msg = m_bShowMark4SelectedCurves ? "Hide Marks for Selected Curves" : "Show Marks for Selected Curves";
            if (EUtil.Button(msg, c))
            {
                m_bShowMark4SelectedCurves = !m_bShowMark4SelectedCurves;
                SceneView.RepaintAll();
            }
        }
    }

    void OnInspectorUpdate() // freq: 10
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (ms_instance == null)
            _ReInit();

        _CheckUAWOpen();
        if( !m_bIsUAWOpen )
            return;

        _UpdateInternalInfo();
        if (m_CurClip == null || m_evtGoRoot == null)
            return;
    }

    void OnSelectionChange()
    {
        SceneView.RepaintAll();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (m_AnimationWindowState != null && m_bShowMark4SelectedCurves)
        {
            TrLst activeTrs = _GetActiveCurveTransforms();

            Color prevClr = Handles.color;
            Handles.color = Color.green;

            Vector3 camPos = sceneView.camera.transform.position;

            for (int idx = 0; idx < activeTrs.Count; ++idx)
            {
                Transform tr = activeTrs[idx];
                Handles.DrawSolidDisc(tr.position, camPos - tr.position, SMREditor.ms_boneSize);
            }
            Handles.color = prevClr;
        }
    }

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public static bool HasInstance()
    {
        return ms_instance != null;
    }

    public static CCEditor Instance
    {
        get
        {
            if (ms_instance == null)
            {
                OpenWindow();
            }
            return ms_instance;
        }
    }

    #endregion "public method"

	#region "private method"
    // private method

    private void _ReInit()
    {
        OpenWindow();
    }

    //private void _UpdateTime()
    //{
    //    // update time
    //    double prevTime = m_curTime;
    //    m_curTime = EditorApplication.timeSinceStartup;
    //    m_deltaTime = (float)(m_curTime - prevTime);
    //}

    private void _CheckUAWOpen()
    {
        m_UAW = EUtil.GetUnityAnimationWindow();
        bool bIsOpen = (m_UAW != null);

        if( m_bIsUAWOpen != bIsOpen )
        {
            _OnUAWOpenChange(bIsOpen);
            m_bIsUAWOpen = bIsOpen;
        }
    }

    /// <summary>
    /// [HACK TRICK]
    /// get a TransformLst containing all active curves' transform in UAW
    /// </summary>
    private TrLst _GetActiveCurveTransforms()
    {
        //1. collect the TrPaths from active curves
        HashSet<string> strset = m_StrSet;
        strset.Clear();
 
        IList curves = (IList)RCall.GetProp("UnityEditorInternal.AnimationWindowState", "activeCurves", m_AnimationWindowState);
        for (int idx = 0; idx < curves.Count; ++idx)
        {
            object oneCurve = curves[idx]; //AnimationWindowCurve
            string onePath = (string)RCall.GetProp("UnityEditorInternal.AnimationWindowCurve", "path", oneCurve);
            strset.Add(onePath);
        }

        //2. transform to Transform
        TrLst lst = new TrLst();
        for (var ie = strset.GetEnumerator(); ie.MoveNext();  )
        {
            string trPath = ie.Current;
            Transform tr = m_ClipRoot.Find(trPath);
            if (tr != null)
                lst.Add(tr);
            else
                Dbg.LogWarn("CCEditor._GetActiveCurveTransforms: cannot get transform for: {0}", trPath);
        }

        return lst;
    }

    /// <summary>
    /// [HACK TRICK]
    /// use reflection to take out the data
    /// </summary>
    private bool _IsAnimEventPopupOpen()
    {
        Type t = RCall.GetTypeFromString("UnityEditor.AnimationEventPopup");
        UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(t);
        return array.Length > 0;
    }

    /// <summary>
    /// [HACK TRICK]
    /// update every loop,
    /// current in-edit clip, clip's rootTr
    /// </summary>
    private void _UpdateInternalInfo()
    {
        AnimationClip curClip = null;
        Transform rootTr = null;

        m_AnimationWindowState = EUtil.GetUnityAnimationWindowState(m_UAW);
        if( m_AnimationWindowState != null )
        {
            curClip = RCall.GetField("UnityEditorInternal.AnimationWindowState", 
                "m_ActiveAnimationClip", m_AnimationWindowState) as AnimationClip;

#if U5
            GameObject rootGO = RCall.GetProp("UnityEditorInternal.AnimationWindowState", 
               "activeRootGameObject", m_AnimationWindowState ) as GameObject;
#else
            GameObject rootGO = RCall.GetField("UnityEditorInternal.AnimationWindowState",
                "m_RootGameObject", m_AnimationWindowState) as GameObject;
#endif
            if( rootGO != null )
                rootTr = rootGO.transform;
        }
        else
        {
            curClip = null;
            rootTr = null;
        }

        if (curClip != m_CurClip)
        {
            _OnCurrentClipChange(m_CurClip, curClip);
        }
        if (rootTr != m_ClipRoot)
        {
            _OnRootTransformChange(m_ClipRoot, rootTr);
        }
    }

    


    /// <summary>
    /// make the AnimEvent and EventGO to be matched
    /// maybe not one-one match, multiple animEvent could match same EventGO
    /// </summary>
    private void _MatchEventGOAndAnimEvent()
    {
        //////////////////////////////////////////////////
        //  1. for each AnimEvent, if there is not a corresponding eventGO, create it;
        //  2. for each eventGO, if there is not a corresponding AnimEvent, the delete the eventGO
        //////////////////////////////////////////////////

        //if( _IsAnimEventPopupOpen() )
        //{
        //    return; //don't execute matching if the AnimEventPopup is open
        //}

        // create a Dictionary for eventGO
        for( int idx = 0; idx < m_evtGoRoot.childCount; ++idx )
        {
            Transform ctr = m_evtGoRoot.GetChild(idx);
            m_evtGORefDict[ctr] = false;
        }

        // get the AnimEvent list
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(m_CurClip);

        // step 1
        for( int idx = 0; idx < events.Length; ++idx )
        {
            AnimationEvent evt = events[idx];
            string goName = evt.stringParameter;
            string funcName = evt.functionName;

            if( goName == null )
            {
                Dbg.LogWarn("CCEditor._MatchEventGOAndAnimEvent: found an event not specifying the GOName, at time: {0}", evt.time);
                ArrayUtility.RemoveAt(ref events, idx);
                --idx;
            }

            Transform oneTr = m_evtGoRoot.Find(goName);
            if( null == oneTr )
            { 
                //create the go 
                GameObject newEvtGO = new GameObject(goName);
                Misc.AddChild(m_evtGoRoot, newEvtGO);
                Dbg.Log("Sync AnimEvent with EventGO: create EventGO for: {0}", goName);

                //add component according to the funcName, the init-work should be executed by the MB's awake() or start()
                //CC_EvtActions newAct = newEvtGO.AddComponent("CC_" + funcName) as CC_EvtActions;
                //Dbg.Assert(m_CC != null, "CCEditor._MatchEventGOAndAnimEvent: failed to get CutsceneController");
                //newAct.CC = m_CC;
                
                string tpName = "MH.CC_"+funcName;
                Type tp = RCall.GetTypeFromString(tpName);
                Dbg.Assert(tp != null, "CCEditor._MatchEventGOAndAnimEvent: failed to get type from string: {0}", tpName);
                CC_EvtActions newAct = newEvtGO.AddComponent(tp) as CC_EvtActions;
                Dbg.Assert(m_CC != null, "CCEditor._MatchEventGOAndAnimEvent: failed to get CutsceneController");
                newAct.CC = m_CC;
            }
            else
            {
                m_evtGORefDict[oneTr] = true; //this event go is ref-ed, don't delete it
            }
        }

        // step 2
        for( var ie = m_evtGORefDict.GetEnumerator(); ie.MoveNext(); )
        {
            var pr = ie.Current;
            bool inUse = pr.Value;
            if( ! inUse )
            {
                Transform tr = pr.Key;
                Dbg.Log("Sync AnimEvent with EventGO: delete EventGO: {0}", tr.name);
                GameObject.DestroyImmediate(tr.gameObject);
            }
        }

        m_evtGORefDict.Clear(); //clear the tmp data
    }

    /// <summary>
    /// remove animation/animator component in the cutscene hierarchy
    /// </summary>
    private void _RemoveAnimCompsOnDecendants()
    {
        _Recur_CheckAnimationComponents(m_ClipRoot);
    }
    private void _Recur_CheckAnimationComponents(Transform tr)
    {
        for (int idx = 0; idx < tr.childCount; ++idx)
        {
            Transform ctr = tr.GetChild(idx);
            if (ctr.GetComponent<Animation>() != null)
            {
                GameObject.DestroyImmediate(ctr.GetComponent<Animation>());
                Dbg.Log("CCEditor: Removed Animation component on {0}", ctr.name);
            }
            else if (ctr.GetComponent<Animator>() != null)
            {
                GameObject.DestroyImmediate(ctr.GetComponent<Animator>());
                Dbg.Log("CCEditor: Removed Animator component on {0}", ctr.name);
            }
            _Recur_CheckAnimationComponents(ctr);
        }
    }

    /// <summary>
    /// check if there're go with duplicate transformPath in the cutscene hierarchy
    /// </summary>
    private void _FixDuplicateTransformPath()
    {
        _Recur_FixDuplicateTransformPath(m_ClipRoot);
    }
    private void _Recur_FixDuplicateTransformPath(Transform tr)
    {
        // first check children's dup name
        HashSet<string> checker = new HashSet<string>();
        int dupIdx = 0;
        for (int idx = 0; idx < tr.childCount; ++idx)
        {
            Transform ctr = tr.GetChild(idx);
            string ctrName = ctr.name;
            if (checker.Contains(ctrName))
            {
                ++dupIdx;
                string newName = string.Format("{0}_{1}", ctr.name, dupIdx);
                Dbg.Log("CCEditor: change duplicate GO name from \"{0}\" to \"{1}\"", ctr.name, newName);
                ctr.name = newName;
            }
            checker.Add(ctr.name);
        }
        checker.Clear();

        // then recurse into each child
        for (int idx = 0; idx < tr.childCount; ++idx)
        {
            Transform ctr = tr.GetChild(idx);
            _Recur_FixDuplicateTransformPath(ctr);
        }
    }

	#region "OnChange callbacks"

    /// <summary>
    /// 1. if newTr doesn't have CutsceneController attached, then do nothing
    /// 2. set the m_evtGoRoot for the new clipRoot
    /// </summary>
    private void _OnRootTransformChange(Transform oldTr, Transform newTr)
    {
        //Dbg.Log("RootGO changed: {0}", newTr == null ? "null" : newTr.name);

        m_evtGoRoot = null; //reset first, will be set to non-null value if needed
        m_ClipRoot = newTr;

        if( newTr != null )
        {
            //1.
            m_CC = newTr.GetComponent<CutsceneController>();
            if (m_CC != null)
            {
                //2.
                GameObject eventGO = Misc.ForceGetGO(EventGOS, m_ClipRoot.gameObject);
                Dbg.Assert(eventGO != null, "CCEditor._OnRootTransformChange: failed to force_get eventGO: {0}", EventGOS);
                m_evtGoRoot = eventGO.transform;
            }
            else
            {
            }
        }

        Repaint();
    }

    private void _OnCurrentClipChange(AnimationClip oldClip, AnimationClip newClip)
    {
        //Dbg.Log("ClipChanged: {0}", newClip == null ? "null" : newClip.name);

        m_CurClip = newClip;

        Repaint();
    }


    private void _OnUAWOpenChange(bool bNew)
    {
        if( bNew ) //onopen
        {

        }
        else //onclose
        {

        }

        Repaint();
    }

	#endregion "OnChange callbacks"

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string EventGOS = CutsceneController.EventGOS;

    #endregion "constant data"
}
