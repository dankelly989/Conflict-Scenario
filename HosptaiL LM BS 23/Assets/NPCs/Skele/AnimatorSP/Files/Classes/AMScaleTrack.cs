using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    [Serializable]
    public class AMScaleTrack : AMTrack
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
                //if (value != null && cache.Count <= 0) cachedInitialScale = value.localScale;
                _obj.tr = value;
            }
        }

        public override string getTrackType()
        {
            return "Scale";
        }
        

        // add a new key, default interpolation and easeType
        public void addKey(int _frame, Vector3 _scale)
        {
            foreach (AMScaleKey key in keys)
            {
                // if key exists on frame, update key
                if (key.frame == _frame)
                {
                    AMUtil.recordObject(key, "update key");
                    key.scale = _scale;
                    // update cache
                    updateCache();
                    return;
                }
            }

            AMScaleKey a = ScriptableObject.CreateInstance<AMScaleKey>();
            a.frame = _frame;
            a.scale = _scale;
            a.easeType = (int)AMTween.EaseType.linear;
            // add a new key
            AMUtil.recordObject(this, "add key");
            keys.Add(a);
            // update cache
            updateCache();
        }

        // preview a frame in the scene view
        public override void previewFrame(float frame, AMTrack extraTrack = null)
        {
            if (!obj) return;
            if (cache.Count <= 1) return;
            // if before first frame
            if (frame <= (float)cache[0].startFrame)
            {
                obj.localScale = (cache[0] as AMScaleAction).startScale;
                return;
            }
            // if beyond last frame
            if (frame >= (float)(cache[cache.Count - 2] as AMScaleAction).endFrame)
            {
                obj.localScale = (cache[cache.Count - 2] as AMScaleAction).endScale;
                return;
            }
            // if lies on curve
            for (int i = 0; i<= cache.Count-2; ++i)
            {
                AMScaleAction action = cache[i] as AMScaleAction;
                if (((int)frame < action.startFrame) || ((int)frame > action.endFrame)) continue;
                
                float _value;
                float framePositionInPath = frame - (float)action.startFrame;
                if (framePositionInPath < 0f) framePositionInPath = 0f;

                AMTween.EasingFunction ease;
                AnimationCurve curve = null;

                if (action.hasCustomEase())
                {
                    ease = AMTween.customEase;
                    curve = action.easeCurve;
                }
                else
                {
                    ease = AMTween.GetEasingFunction((AMTween.EaseType)action.easeType);
                }

                _value = ease(0f, 1f, framePositionInPath / action.getNumberOfFrames(), curve);

                obj.localScale = Vector3.Lerp(action.startScale, action.endScale, _value);
                return;
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
                AMScaleAction a = ScriptableObject.CreateInstance<AMScaleAction>();
                a.startFrame = keys[i].frame;
                if (keys.Count > (i + 1))
                    a.endFrame = keys[i + 1].frame;
                else
                    a.endFrame = -1;

                a.obj = obj;
                a.startScale = (keys[i] as AMScaleKey).scale;
                if (a.endFrame != -1)
                    a.endScale = (keys[i + 1] as AMScaleKey).scale;

                a.easeType = (keys[i] as AMScaleKey).easeType;
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

        // get the starting translation key for the action where the frame lies
        public AMScaleKey getActionStartKeyFor(int frame)
        {
            foreach (AMScaleAction action in cache)
            {
                if ((frame < action.startFrame) || (frame >= action.endFrame)) continue;
                return (AMScaleKey)getKeyOnFrame(action.startFrame);
            }
            Debug.LogError("Animator: Action for frame " + frame + " does not exist in cache.");
            return new AMScaleKey();
        }

        public Vector3 getInitialScale()
        {
            return (keys[0] as AMScaleKey).scale;
        }

        //public override AnimatorTimeline.JSONInit getJSONInit()
        //{
        //    if (!obj || keys.Count <= 0) return null;
        //    AnimatorTimeline.JSONInit init = new AnimatorTimeline.JSONInit();
        //    init.type = "position";
        //    init.go = obj.gameObject.name;
        //    AnimatorTimeline.JSONVector3 v = new AnimatorTimeline.JSONVector3();
        //    v.setValue(getInitialPosition());
        //    init.position = v;
        //    return init;
        //}

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
