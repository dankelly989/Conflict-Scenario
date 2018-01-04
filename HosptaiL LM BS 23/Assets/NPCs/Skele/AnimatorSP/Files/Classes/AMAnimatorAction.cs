using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    [Serializable]
    public class AMAnimatorAction : AMAction
    {
        public RebindTr _obj = new RebindTr();

        public List<AnimatorKeyInfo> m_infos = new List<AnimatorKeyInfo>();

        public Transform obj
        {
            get
            {
                return _obj.tr;
            }
            set
            {
                _obj.tr = value;
            }
        }

        public int Count
        {
            get { return m_infos.Count; }
        }

        public void CopyKeyInfos(List<AnimatorKeyInfo> infos)
        {
            m_infos.Clear();
            for (int i = 0; i < infos.Count; ++i)
            {
                var o = infos[i].Clone();
                m_infos.Add(o);
            }
        }

        public override string ToString(int codeLanguage, int frameRate)
        {
            if (getNumberOfFrames() <= 0) return null;
            string s;

            if (codeLanguage == 0)
            {
                // c#
                s = "// AMAnimatorAction hasn't implement AMAction.ToString(), indeed I'm considering removing CodeView at all. (<ゝω·)☆";
            }
            else
            {
                // js
                s = "// AMAnimatorAction hasn't implement AMAction.ToString(), indeed I'm considering removing CodeView at all. (<ゝω·)☆";
            }
            return s;
        }
        //public override int getNumberOfFrames()
        //{
        //    return endFrame - startFrame;
        //}

        //public float getTime(int frameRate)
        //{
        //    return (float)getNumberOfFrames() / (float)frameRate;
        //}

        public override void execute(int frameRate, float delay)
        {
            if (!obj) return;
            //if (endFrame == -1) return;

            var go = obj.gameObject;
            var tr = obj.transform;
            var ator = tr.GetComponent<Animator>();
            if (ator == null) return;

            //if this take starts at frame after the startFrame, the animator will already
            //be updated in previewFrame by AMAnimatorTrack
            float curFrame = delay * frameRate;
            if (curFrame > startFrame)
                return;

            float waitTime = getWaitTime(frameRate, delay);
            CoroutineBehaviour.StartCoroutineDelay(go, _DoAnimator, waitTime);
        }

        // execute in Coroutine
        private void _DoAnimator(GameObject go)
        {
            if (!go)
            {
                Dbg.LogWarn("AMAnimatorAction._DoAnimator: go is invalid");
                return;
            }

            var ator = go.AssertGetComponent<Animator>();

            for (int i = 0; i < m_infos.Count; ++i)
            {
                var keyInfo = m_infos[i];
                switch (keyInfo.animAction)
                {
                    case AnimatorKeyInfo.eAction.Switch:
                        {
                            if (Mathf.Approximately(keyInfo.fadeTime, 0))
                            {
                                ator.Play(keyInfo.targetState);
                            }
                            else
                            {
                                ator.CrossFade(keyInfo.targetState, keyInfo.fadeTime);
                            }
                        }
                        break;
                    case AnimatorKeyInfo.eAction.Transition:
                        {
                            switch (keyInfo.animParam)
                            {
                                case AnimatorKeyInfo.eParam.Bool: ator.SetBool(keyInfo.paramName, keyInfo.vBool); break;
                                case AnimatorKeyInfo.eParam.Float: ator.SetFloat(keyInfo.paramName, keyInfo.vFloat); break;
                                case AnimatorKeyInfo.eParam.Integer: ator.SetInteger(keyInfo.paramName, keyInfo.vInt); break;
                                case AnimatorKeyInfo.eParam.Trigger: ator.SetTrigger(keyInfo.paramName); break;
                                default: Dbg.LogErr("AMAnimatorAction.execute: unexpected transition param type: {0}", keyInfo.animParam); break;
                            }
                        }
                        break;
                    case AnimatorKeyInfo.eAction.END:
                        break; // do nothing
                    default:
                        Dbg.LogErr("AMAnimatorAction.execute: unexpected animAction: {0}", keyInfo.animAction);
                        break;
                }
            }
        }

        public override void rebind(RebindOption opt)
        {
            base.rebind(opt);
            _obj.Rebind(opt);
        }

        public override void unbind(RebindOption opt)
        {
            base.unbind(opt);
            _obj.Unbind();
        }
    }
}
