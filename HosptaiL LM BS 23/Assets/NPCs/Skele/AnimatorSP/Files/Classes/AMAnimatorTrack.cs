#if UNITY_5
#define U5
#endif

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
    public class AMAnimatorTrack : AMTrack
    {
        [SerializeField]
        private RebindTr _obj = new RebindTr();

        public Transform obj
        {
            get
            {
                return _obj.tr;
            }
            set
            {
                var tr = value;
                if( tr.GetComponent<Animator>() == null )
                    return;

                _obj.tr = value;
            }
        }

        public Animator animator {
            get{
                return _obj.tr.SafeGetComponent<Animator>();
            }
        }

        public override string getTrackType()
        {
            return "Animator";
        }

        // add a new key
        // the parameters should be set by AMTimeline inspector
        public void addKey(int _frame)
        {
            foreach (AMAnimatorKey key in keys)
            {
                // if key exists on frame, update key
                if (key.frame == _frame)
                {
                    AMUtil.recordObject(key, "update key");
                    // update cache
                    updateCache();
                    return;
                }
            }

            AMAnimatorKey a = ScriptableObject.CreateInstance<AMAnimatorKey>();
            a.frame = _frame;
            a.easeType = (int)AMTween.EaseType.linear;
            // add a new key
            AMUtil.recordObject(this, "add key");
            keys.Add(a);
            // update cache
            updateCache();
        }

        // preview a frame in the scene view
        // 1. revert all layer's cur-state to normalizedTime = 0
        // 2. re-evaluate all parameters setting from head to specified frame
        public void previewFrame(float frame, float frameRate, AMTrack extraTrack = null)
        {
            if (!obj) return;
            if (cache.Count == 0) return;

            // 1. revert
            var ator = this.animator;
            ator.Rebind(); //revert back to default states + default parameters
            ator.Update(0);

            // 2. re-evaluate
            float prevFrame = 0;
            for (int i = 0; i<= cache.Count-1; ++i)
            {
                AMAnimatorAction action = cache[i] as AMAnimatorAction;
                if (action.startFrame > frame)
                { //update from prevFrame to frame, over
                    float time = (frame - prevFrame) / frameRate;
                    prevFrame = frame;
                    ator.Update(time);
                    break;
                }
                else if( action.startFrame <= frame)
                { //update from prevFrame to startFrame, keep on
                    float time = (action.startFrame - prevFrame) / frameRate;
                    prevFrame = action.startFrame;
                    ator.Update(time);

                    // apply the action's modification to ator
                    for (int iidx = 0; iidx < action.m_infos.Count; ++iidx)
                    {
                        var oneInfo = action.m_infos[iidx];
                        oneInfo.Apply(ator, true);
                    }

                    // check same
                    if (Mathf.Approximately(action.startFrame,frame) )
                        break;
                }
            }

            if (frame > prevFrame)
            {
                float time = (frame - prevFrame) / frameRate;
                prevFrame = frame;
                ator.Update(time);
            }

        }

        // update cache (optimized)
        public override void updateCache()
        {
            // destroy cache
            destroyCache(); //undo handled inside
            // create new cache
            _clearCache(); //undo handled inside
            // sort keys
            sortKeys(); //undo handled inside

            for (int i = 0; i < keys.Count; i++)
            {
                AMAnimatorAction a = ScriptableObject.CreateInstance<AMAnimatorAction>();
                a.startFrame = keys[i].frame;
                //if (keys.Count > (i + 1))
                //    a.endFrame = keys[i + 1].frame;
                //else
                //    a.endFrame = -1;

                AMAnimatorKey k = (AMAnimatorKey)keys[i];
                a.obj = obj;
                a.CopyKeyInfos(k.infos);
                
                a.easeType = (keys[i] as AMAnimatorKey).easeType;
                a.customEase = new List<float>(keys[i].customEase);
                // add to cache
                cache.Add(a);
            }
        }

        private void _clearCache()
        {
            AMUtil.recordObject(this, "clear cache");
            cache = new List<AMAction>();
        }

       
        public override List<GameObject> getDependencies()
        {
            List<GameObject> ls = new List<GameObject>();
            if (obj) ls.Add(obj.gameObject);
            return ls;
        }

        public override List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences)
        {
            if (!obj) return new List<GameObject>();
            for (int i = 0; i < oldReferences.Count; i++)
            {
                if (oldReferences[i] == obj.gameObject)
                {
                    obj = newReferences[i].transform;
                    break;
                }
            }
            return new List<GameObject>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// will write the transformPath based on the AnimatorData monoBehaviour
        /// </summary>
        public override void SaveAsset(AnimatorData mb, AMTake take)
        {
            base.SaveAsset(mb, take);
        }
#endif

        public override void rebind(RebindOption opt)
        {
            base.rebind(opt);
            _obj.Rebind(opt);

            rebind4Actions(opt);
        }

        public override void unbind(RebindOption opt)
        {
            base.unbind(opt);
            _obj.Unbind();

            unbind4Actions(opt);
        }

    }

}
