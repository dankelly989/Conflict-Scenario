using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    [Serializable]
    public class AMAnimatorKey : AMKey
    {
        [SerializeField]
        private List<AnimatorKeyInfo> m_infos = new List<AnimatorKeyInfo>();

        public List<AnimatorKeyInfo> infos
        {
            get { return m_infos; }
        }

        // copy properties from key
        public override AMKey CreateClone()
        {
            AMAnimatorKey a = ScriptableObject.CreateInstance<AMAnimatorKey>();
            a.frame = frame;
            a.easeType = easeType;
            a.customEase = new List<float>(customEase);

            for (int i = 0; i < m_infos.Count; ++i)
            {
                var newInst = new AnimatorKeyInfo(m_infos[i]);
                a.m_infos.Add(newInst);
            }

            return a;
        }
    }

    [Serializable]
    public class AnimatorKeyInfo : ICloneable
    {
        public eAction animAction = eAction.Switch;

        public float fadeTime = 0; //if 0, means no crossfade
        public string targetState = string.Empty; //need to use layer.state format

        public eParam animParam = eParam.Bool;

        public string paramName = string.Empty;
        public bool vBool = false;
        public float vFloat = 0f;
        public int vInt = 0;

        public enum eAction
        {
            Switch, //play / crossfade to specified state
            Transition, // use setXXX to control Animator state
            END,
        }

        public enum eParam
        {
            Bool,
            Float,
            Integer,
            Trigger,
        }

        public AnimatorKeyInfo() { }
        public AnimatorKeyInfo(AnimatorKeyInfo o)
        {
            CopyFrom(o);
        }

        public void CopyFrom(AnimatorKeyInfo o)
        {
            animAction = o.animAction;
            fadeTime = o.fadeTime;
            targetState = o.targetState;
            animParam = o.animParam;
            paramName = o.paramName;
            vBool = o.vBool;
            vFloat = o.vFloat;
            vInt = o.vInt;
        }

        // apply the changes to specified animator
        public void Apply(Animator ator, bool doUpdate = false)
        {
            switch (animAction)
            {
                case eAction.Switch:
                    {
                        if (fadeTime > 0)
                            ator.CrossFade(targetState, fadeTime);
                        else
                            ator.Play(targetState);

                        if(doUpdate)
                            ator.Update(0);
                    }
                    break;
                case eAction.Transition:
                    {
                        switch (animParam)
                        {
                            case eParam.Bool: ator.SetBool(paramName, vBool); break;
                            case eParam.Float: ator.SetFloat(paramName, vFloat); break;
                            case eParam.Integer: ator.SetInteger(paramName, vInt); break;
                            case eParam.Trigger: ator.SetTrigger(paramName); break;
                        }
                        if (doUpdate)
                            ator.Update(0);
                    }
                    break;
                default:
                    Dbg.LogErr("AnimatorKeyInfo.Apply: unexpected action: {0}", animAction);
                    break;
            }
        }

        public AnimatorKeyInfo Clone()
        {
            var o = new AnimatorKeyInfo();
            o.CopyFrom(this);
            return o;
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
