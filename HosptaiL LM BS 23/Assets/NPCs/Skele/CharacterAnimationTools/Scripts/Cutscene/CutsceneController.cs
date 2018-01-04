using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
using MH;

using ReplRecord = System.Collections.Generic.Dictionary<UnityEngine.GameObject, CutsceneController.ReplData>;
using BlendOutRecord = System.Collections.Generic.Dictionary<UnityEngine.GameObject, CutsceneController.BlendOutData>;
using SwapObjList = System.Collections.Generic.List<CutsceneController.SwapObjPair>;


/// <summary>
/// Cutscene Controller (abbr. CC)
/// Control the cutscene for Skele
/// 
/// one CC should corresponds only one AnimClip
/// 
/// 1. as the receiver of Animation Event, and call corresponding MB during Runtime
/// 2. save some specific data
/// 3. provide public control interface
/// </summary>
public class CutsceneController : MonoBehaviour
{
	#region "configurable data"
    // configurable data

    //[Tooltip("fade in duration, negative value means no blend-in")]
    [Range(-0.1f, 1f)]
    public float m_BlendInTime = -0.1f;

    //[Tooltip("fade out duration, will start after cutscene finishes, negative value means no blend-out")]
    [Range(-0.1f, 1f)]
    public float m_BlendOutTime = -0.1f;

    //[Tooltip("What to do with the Cutscene instance when play is over")]
    public EndAction m_EndAction = EndAction.Destroy;

    //[Tooltip("Whether re-enable Animators after play is over")]
    //public bool m_ReEnableAnimatorWhenOver = true;

    //[Tooltip("Config the to-swap GOs at edit-time")]
    public List<SwapObjPairTrPath> m_ToSwapObjTrPaths;

    //[Tooltip("The mapping table from tag to time")]
    public List<TimeTag> m_TimeTags;

    #endregion "configurable data"

	#region "data"
    // data

    public delegate void PlayStateChangeHandler(CutsceneController cc);
    public event PlayStateChangeHandler OnPlayStopped;

    private SwapObjList m_ToSwapObjPairs;
    private Transform m_EvtGORootTr = null;
    private AnimationState m_AnimState = null;
    private Animation m_Anim = null;

    private bool m_bIsAnimStopped = true;

    private ReplRecord m_ReplRecord = new ReplRecord();
    private BlendOutRecord m_BlendOutCtrl = new BlendOutRecord();
    private float m_BlendOutElapsedTime = 0f;

    private JumpTimeType m_JumpTimeType = JumpTimeType.None;
    private float m_toJumpTime; // negative means no jump request

    //private Skele_CRCont m_LateUpdateCR = new Skele_CRCont(); //coroutines executed at lateupdate

    private int m_StartFrameNumber; //the frame when the CC starts, used to check whether animation is playing,
                                    //The Animation component will not set Animation.isPlaying = true until next animation tick

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	void Awake()
	{
        m_EvtGORootTr = transform.Find(EventGOS);
        Dbg.Assert(m_EvtGORootTr != null, "CutsceneController.Start: failed to find EventGO root: {0}", EventGOS);

        Animation anim = m_Anim = GetComponent<Animation>();
        Dbg.Assert(anim != null, "CutsceneController.Start: failed to get animation component: {0}", name);

        m_ToSwapObjPairs = new SwapObjList();

        if( anim.GetClipCount() == 0 && anim.clip == null)
        {
            Dbg.LogWarn("CutsceneController.Start: No Clip is assigned to Animation yet, disable CutsceneController: {0}", name);
            enabled = false;
        }
        else
        {
            if( anim.clip != null )
            {
                m_AnimState = m_Anim[anim.clip.name];
            }
            else
            {
                for (var ie = anim.GetEnumerator(); ie.MoveNext(); )
                {
                    m_AnimState = (AnimationState)ie.Current;
                    m_Anim.clip = m_AnimState.clip;
                    break;
                }
            }
        }

        m_Anim.playAutomatically = false; //not allow auto-play, always call StartCC/StopCC instead

        if( ! ResMgr.HasInst )
        {
            ResMgr.Create();
        }
    }

    void LateUpdate()
    {
        //m_LateUpdateCR.Execute();

        bool bIsStopped = _IsAnimStopped(this);
        if (bIsStopped != m_bIsAnimStopped)
        {
            m_bIsAnimStopped = bIsStopped;
            _OnAnimStoppedChange(bIsStopped);
        }

        _ExecuteJumpTime(this);

        _ExecuteBlendIn(this);
        _ExecuteBlendOut(this);
    }

    #endregion "unity event handlers"

	#region "public method"
    // public method

    /// <summary>
    /// given an external GO (X) outside the CC,
    /// replace the GO (Y) inside CC with X, and change X's name to be Y's name
    /// 
    /// this could be used to change the character appearance during cut-scene playing
    /// </summary>
    public void SwapGO(GameObject extGO, GameObject intGO, bool bRevertPoseAtEnd, bool bReEnableAnimator)
    {
        Transform extTr = extGO.transform;
        ReplData rData = new ReplData();
        rData.m_ExtGOOrigName = extGO.name;
        rData.m_ExtGOOrigParent = extTr.parent;
        rData.m_IntGO = intGO;
        rData.m_RevertPose = bRevertPoseAtEnd;
        rData.m_ReEnableAnimator = bReEnableAnimator;
        var blendInDataBasedOnCC = rData.m_ExtGOTrDataBasedOnCC = new XformData();
        m_ReplRecord.Add(extGO, rData); 

        intGO.SetActive(false);
        extGO.name = intGO.name;
        intGO.name = "_" + intGO.name; //make intGO a different name

        if (m_BlendInTime > 0 || rData.m_RevertPose)
        {
            _PrepareBlend(extTr, rData.m_ExtGOBlendData);
        }

        Misc.AddChild(intGO.transform.parent, extGO); // but UndoAllReplace needs localPosition based on old parent's transform

        blendInDataBasedOnCC.CopyFrom(extTr);  // blend-in needs localposition based on CC's transform
        extGO.transform.CopyLocal(intGO.transform);

        _SetAnimatorEnableState(extGO, false);
    }

    /// <summary>
    /// start the cutscene
    /// </summary>
    public static void StartCC(CutsceneController cc)
    {
        if (!cc.gameObject.activeSelf)
            cc.gameObject.SetActive(true);

        Transform cctr = cc.transform;
        var toSwapLst = cc.m_ToSwapObjPairs;
        //1. add data from user-setting
        for (int idx = 0; idx < cc.m_ToSwapObjTrPaths.Count; ++idx)
        {
            toSwapLst.Add(cc.m_ToSwapObjTrPaths[idx].Convert(cctr));
        } 

        //2. execute swap
        for (int idx = 0; idx < toSwapLst.Count; ++idx)
        {
            var pr = toSwapLst[idx];
            var extGO = pr.m_ExternalGO;
            var intGO = pr.m_InternalGO;
            cc.SwapGO(extGO, intGO, pr.m_RevertPose, pr.m_ReEnableAnimator);
        }

        cc.m_BlendOutElapsedTime = 0;
        cc.m_Anim.Play();
        cc.m_StartFrameNumber = Time.frameCount;
    }

    public static void StopCC(CutsceneController cc)
    {
        cc.m_Anim.Stop();
        cc.m_StartFrameNumber = -1;
    }

    ///// <summary>
    ///// getter
    ///// </summary>
    //public static Skele_CRCont GetLateUpdateCRCont(CutsceneController cc)
    //{
    //    return cc.m_LateUpdateCR;
    //}

    /// <summary>
    /// getter
    /// </summary>
    public static SwapObjList GetSwapObjList(CutsceneController cc)
    {
        return cc.m_ToSwapObjPairs;
    }

    /// <summary>
    /// getter
    /// </summary>
    public static AnimationState GetAnimState(CutsceneController cc)
    {
        return cc.m_AnimState;
    }

    /// <summary>
    /// undo all replace operations
    /// </summary>
    public static void UndoAllReplace(CutsceneController cc)
    {
        for(var ie = cc.m_ReplRecord.GetEnumerator(); ie.MoveNext(); )
        {
            var pr = ie.Current;
            GameObject extGO = pr.Key;
            //Transform extTr = extGO.transform;
            ReplData rData = pr.Value;

            //Dbg.Log("Undo Replace: extGO: {0}", rData.m_ExtGOOrigName);

            //1
            extGO.name = rData.m_ExtGOOrigName;
       

            //3
            if( rData.m_ExtGOOrigParent )
            {
                Misc.AddChild(rData.m_ExtGOOrigParent, extGO);
            }
            else
            {
                extGO.transform.parent = null;
            }

            //2
            if (rData.m_RevertPose)
            {
                _ExecuteRevertPose(rData);
            }

            //4
            if( rData.m_IntGO )
            {
                if( cc.m_BlendOutTime < 0 )
                    rData.m_IntGO.SetActive(true);
                rData.m_IntGO.name = rData.m_IntGO.name.Substring(1);
            }

            //5
            if (rData.m_ReEnableAnimator)
                _SetAnimatorEnableState(extGO, true);

            //6. prepare blend-out
            if (cc.m_BlendOutTime >= 0)
            {
                BlendOutData boData = new BlendOutData();
                boData.m_InternalGO = rData.m_IntGO;
                cc.m_BlendOutCtrl[extGO] = boData;
                cc._PrepareBlend(extGO.transform, boData.m_BlendDataLst);
            }

        }

        cc.m_ReplRecord.Clear();
        cc.m_ToSwapObjPairs.Clear();
    }

    /// <summary>
    /// if not found, return negative value
    /// </summary>
    public static float GetTimeByTag(CutsceneController cc, string tag)
    {
        for(int idx = 0; idx < cc.m_TimeTags.Count; ++idx )
        {
            TimeTag t = cc.m_TimeTags[idx];
            if( t.m_Name == tag )
            {
                return t.m_Time;
            }
        }
        Dbg.LogErr("CutsceneController.GetTimeByTag: failed to find tag: {0}", tag);
        return float.NegativeInfinity;
    }

    /// <summary>
    /// time jump, silent jump will not cause pass-by events be fired
    /// 
    /// NOTE: DON'T directly change AnimationState.time unless you know what you're doing
    /// </summary>
    public static void JumpToTimeTag(CutsceneController cc, string tag, bool bSilent = true)
    {
        float time = GetTimeByTag(cc, tag);
        if (time != float.NegativeInfinity)
        {
            JumpToTime(cc, time, bSilent);
        }
        else
        {
            Dbg.LogWarn("CutsceneController.JumpToTimeTag: unknown tag: {0}", tag);
        }
    }

    public static void JumpToTime(CutsceneController cc, float time, bool bSilent = true)
    {
        cc.m_JumpTimeType = bSilent ? JumpTimeType.SilentJump : JumpTimeType.NormalJump;
        cc.m_toJumpTime = time;
    }

    public static float GetCurrentTime(CutsceneController cc)
    {
        return cc.m_AnimState.time;
    }

    public static float GetLength(CutsceneController cc)
    {
        return cc.m_AnimState.length;
    }

    #endregion "public method"

	#region "AnimEvent Handlers"
	// "AnimEvent Handlers" 

    private void Spawn(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void Camera(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void GUI(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void Sound(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void JumpTo(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void PlaySpeed(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void SendMsg(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void LoadLevel(string name)
    {
        _ExecAnimEvent(this, name);
    }

    private void SetTransform(string name)
    {
        _ExecAnimEvent(this, name);
    }

    ////cannot replace obj after Animation starts
    //private void Replace(string name)
    //{
    //    _ExecAnimEvent(this, name);
    //}

	#endregion "AnimEvent Handlers"

	#region "private method"
    // private method

    private static void _SetAnimatorEnableState(GameObject go, bool state)
    {
        Animation animation = go.GetComponent<Animation>();
        if (animation != null)
            animation.enabled = state;

        Animator animator = go.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = state;
    }


    private static void _ExecAnimEvent(CutsceneController cc, string name)
    {
        Transform tr = _GetEventTrByName(cc, name);
        CC_EvtActions[] actions = tr.GetComponents<CC_EvtActions>();
        Dbg.Assert(actions != null && actions.Length > 0, "CutsceneController._ExecAnimEvent: failed to get CC_EvtActions: {0}", name);

        for( int idx = 0; idx < actions.Length; ++idx)
        {
            CC_EvtActions action = actions[idx];
            if (action.enabled)
                action.OnAnimEvent();
        }
        
    }

    /// <summary>
    /// get the EventGO by name relative to EventGORoot
    /// </summary>
    private static Transform _GetEventTrByName(CutsceneController cc, string name)
    {
        Transform tr = cc.m_EvtGORootTr.Find(name);
        Dbg.Assert(tr != null, "CutsceneController._GetEventTrByName: failed to get EventGO of name: {0}", name);

        return tr;
    }

    private void _OnAnimStoppedChange(bool bIsStopped)
    {
        if( bIsStopped )
        {
            // resample to the end pos, or we'll see the animation at the start pos
            m_AnimState.normalizedTime = 1f;
            m_Anim.Sample();

            // callback before other actions
            if (OnPlayStopped != null)
            {
                OnPlayStopped(this);// callback for play over
            }

            UndoAllReplace(this);

            _ResetAnimationState(this);

            //SendMessage("OnCutsceneStopped", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            //Dbg.Log("Anim Started: {0}", m_AnimState.name);
        }
    }

    private static void _ExecuteEndAction(CutsceneController cc)
    {
        switch (cc.m_EndAction)
        {
            case EndAction.Destroy:
                {
                    GameObject.Destroy(cc.gameObject);
                }
                break;
            case EndAction.Deactivate:
                {
                    cc.gameObject.SetActive(false);
                }
                break;
            default:
                Dbg.LogErr("CutsceneController._OnAnimStoppedChange: unexpected EndAction: {0}", cc.m_EndAction);
                break;
        }
    }

    /// <summary>
    /// reset animationState, 
    /// so the existing binding is broken
    /// </summary>
    private static void _ResetAnimationState(CutsceneController cc)
    {
        Animation anim = cc.m_Anim;
        AnimationClip clip = anim.clip;
        anim.RemoveClip(clip);
        anim.AddClip(clip, clip.name);
        anim.clip = clip;
        cc.m_AnimState = anim[clip.name];
    }

    private static bool _IsAnimStopped(CutsceneController cc)
    {
        return !cc.m_Anim.isPlaying && Time.frameCount != cc.m_StartFrameNumber;
    }

    private static void _ExecuteJumpTime(CutsceneController cc)
    {
        switch (cc.m_JumpTimeType)
        {
            case JumpTimeType.None:
                break;
            case JumpTimeType.NormalJump:
                {
                    cc.m_AnimState.time = cc.m_toJumpTime;
                }
                break;
            case JumpTimeType.SilentJump:
                { //reset the AnimState, or events will be fired
                    //AnimationClip clip = m_AnimState.clip;

                    bool bIsPlaying = cc.m_Anim.isPlaying;
                    float oldSpeed = cc.m_AnimState.speed;

                    _ResetAnimationState(cc);

                    cc.m_AnimState.time = cc.m_toJumpTime; // set new time
                    cc.m_AnimState.speed = oldSpeed;

                    if (bIsPlaying)
                        cc.m_Anim.Play();
                }
                break;
            default:
                Dbg.LogErr("CutsceneController.LateUpdate: unexpected JumpType: {0}", cc.m_JumpTimeType);
                break;
        }
        cc.m_JumpTimeType = JumpTimeType.None;
    }

    /// <summary>
    /// execute blend-in
    /// </summary>
    private static void _ExecuteBlendIn(CutsceneController cc)
    {
        if( cc.m_ReplRecord.Count == 0 )
            return;

        float curAnimTime = cc.m_AnimState.time;
        if (curAnimTime > cc.m_BlendInTime)
            return;

        float t = curAnimTime / cc.m_BlendInTime;

        for(var ie = cc.m_ReplRecord.GetEnumerator(); ie.MoveNext(); )
        {
            ReplData rData = ie.Current.Value;
            Transform extTr = ie.Current.Key.transform;
            for( int idx = 0; idx < rData.m_ExtGOBlendData.Count; ++idx )
            {
                BlendData bdata = rData.m_ExtGOBlendData[idx];
                if( bdata.m_Tr == extTr )
                {
                    XformData origBasedOnCC = rData.m_ExtGOTrDataBasedOnCC;
                    extTr.localPosition = Vector3.Lerp(origBasedOnCC.pos, extTr.localPosition, t);
                    extTr.localRotation = Quaternion.Slerp(origBasedOnCC.rot, extTr.localRotation, t);
                    extTr.localScale = Vector3.Lerp(origBasedOnCC.scale, extTr.localScale, t);                    
                }
                else
                {
                    bdata.DoBlend(t);
                }
            }
        }
    }

    private void _ExecuteBlendOut(CutsceneController cc)
    {
        if (!_IsAnimStopped(cc))
            return;

        if( m_BlendOutElapsedTime > m_BlendOutTime )
        { //if no blend-out, m_BlendOutTime will be negative, so _ExecuteEndAction will be executed no matter we have blend-out or not
            _ExecuteEndAction(cc);
            for (var ie = cc.m_BlendOutCtrl.GetEnumerator(); ie.MoveNext(); )
            {
                BlendOutData boData = ie.Current.Value;
                boData.m_InternalGO.SetActive(true);
            }
            m_BlendOutCtrl.Clear();
        }
        else
        {
            float t = m_BlendOutElapsedTime / cc.m_BlendOutTime;

            for (var ie = cc.m_BlendOutCtrl.GetEnumerator(); ie.MoveNext(); )
            {
                BlendOutData boData = ie.Current.Value;
                for (int idx = 0; idx < boData.m_BlendDataLst.Count; ++idx)
                {
                    BlendData bdata = boData.m_BlendDataLst[idx];
                    bdata.DoBlend(t);
                }
            }
            m_BlendOutElapsedTime += Time.deltaTime;
        }

    }

    private static void _ExecuteRevertPose(ReplData rData)
    {
        for (int idx = 0; idx < rData.m_ExtGOBlendData.Count; ++idx)
        {
            BlendData bdata = rData.m_ExtGOBlendData[idx];
            bdata.DoRevert();
        }
    }

    /// <summary>
    /// record recursively
    /// </summary>
    private void _PrepareBlend(Transform extTr, List<BlendData> blendDatas)
    {
        BlendData newData = new BlendData();
        newData.m_Tr = extTr;
        newData.m_StartTrData = XformData.Create(extTr);
        blendDatas.Add(newData);

        for (int idx = 0; idx < extTr.childCount; ++idx)
        {
            Transform tr = extTr.GetChild(idx);
            _PrepareBlend(tr, blendDatas);
        }
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    public const string EventGOS = "__CCEvents";

    public enum JumpTimeType
    {
        None,
        NormalJump, //will cause pass-by events be fired
        SilentJump //won't cause pass-by events be fired
    }

    public enum EndAction
    {
        Deactivate,
        Destroy
    }

    #endregion "constant data"

	#region "Inner struct"
	// "Inner struct" 

    /// <summary>
    /// used for blend-out
    /// </summary>
    public class BlendOutData
    {
        public GameObject m_InternalGO; //will be activated after blend-out
        public List<BlendData> m_BlendDataLst = new List<BlendData>();
    }

    /// <summary>
    /// used by Replace functionality
    /// </summary>
    public class ReplData
    {
        public string     m_ExtGOOrigName;
        public Transform  m_ExtGOOrigParent;
        public GameObject m_IntGO;
        public XformData m_ExtGOTrDataBasedOnCC; //the xform data based on CC

        public bool m_RevertPose;
        public bool m_ReEnableAnimator;

        public List<BlendData> m_ExtGOBlendData = new List<BlendData>();
    }

    /// <summary>
    /// used to blend from current action to cutscene action
    /// </summary>
    public class BlendData
    {
        public Transform m_Tr;
        public XformData m_StartTrData;

        public void DoBlend(float t)
        {
            if (!m_Tr)
            {
                Dbg.LogWarn("CutsceneController.DoBlend: a GO {0} cannot be found, hierarchy changed before blend-in finishes?", m_Tr.name);
                return;
            }

            m_Tr.localPosition = Vector3.Lerp(m_StartTrData.pos, m_Tr.localPosition, t);
            m_Tr.localRotation = Quaternion.Slerp(m_StartTrData.rot, m_Tr.localRotation, t);
            m_Tr.localScale = Vector3.Lerp(m_StartTrData.scale, m_Tr.localScale, t);
        }

        public void DoRevert()
        {
            if (!m_Tr)
            {
                Dbg.LogWarn("CutsceneController.DoRevert: a GO {0} cannot be found, hierarchy changed during cutscene?", m_Tr.name);
                return;
            }

            m_StartTrData.Apply(m_Tr);
        }
    }

    [Serializable]
    public class SwapObjPairTrPath
    { //use ExternalGO to replace InternalGO
        public CCTrPath m_ExternalGO;  
        public CCTrPath m_InternalGO;
        public bool m_RevertPose = false;
        public bool m_ReEnableAnimator = true;

        public SwapObjPair Convert(Transform ccroot)
        {
            GameObject extGO = m_ExternalGO.GetTransform(ccroot).gameObject;
            GameObject intGO = m_InternalGO.GetTransform(ccroot).gameObject;

            SwapObjPair pr = new SwapObjPair(extGO, intGO, m_RevertPose, m_ReEnableAnimator);

            return pr;
        }
    }

    public class SwapObjPair
    { //use ExternalGO to replace InternalGO
        public GameObject m_ExternalGO;  
        public GameObject m_InternalGO;
        public bool m_RevertPose = false; // whether revert transforms back to start condition
        public bool m_ReEnableAnimator = true;

        public SwapObjPair(){}
        public SwapObjPair(GameObject outGO, GameObject intGO)
        {
            m_ExternalGO = outGO;
            m_InternalGO = intGO;
        }
        public SwapObjPair(GameObject outGO, GameObject intGO, bool bRevertPoseOnEnd)
        {
            m_ExternalGO = outGO;
            m_InternalGO = intGO;
            m_RevertPose = bRevertPoseOnEnd;
        }
        public SwapObjPair(GameObject outGO, GameObject intGO, bool bRevertPoseOnEnd, bool bReEnableAnimator)
        {
            m_ExternalGO = outGO;
            m_InternalGO = intGO;
            m_RevertPose = bRevertPoseOnEnd;
            m_ReEnableAnimator = bReEnableAnimator;
        }
        
    }
	
	#endregion "Inner struct"

}

[Serializable]
public class TimeTag
{
    public string m_Name; // tag name
    public float m_Time; //absolute time
}

