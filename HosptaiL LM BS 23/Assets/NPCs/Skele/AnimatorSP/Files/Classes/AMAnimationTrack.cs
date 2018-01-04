#if UNITY_5
#define U5
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MH;
[System.Serializable]
public class AMAnimationTrack : AMTrack {
	// to do
	// sample currently selected clip

    public RebindTr _obj = new RebindTr();
    public GameObject obj
    {
        get { return _obj.gameObject; }
        set { _obj.tr = value.transform; }
    }
	
	public override string getTrackType() {
		return "Animation";	
	}
	
	public bool setObject(GameObject obj) {
		if(this.obj != obj) {
			this.obj = obj;
			return true;
		}
		return false;
	}
	// add a new key
	public void addKey(int _frame, AnimationClip _clip, WrapMode _wrapMode) {
		foreach(AMAnimationKey key in keys) {
			// if key exists on frame, update key
			if(key.frame == _frame) {
                AMUtil.recordObject(key, "update key");
				key.amClip = _clip;
				key.wrapMode = _wrapMode;
				// update cache
				updateCache();
				return;
			}
		}
		AMAnimationKey a = ScriptableObject.CreateInstance<AMAnimationKey>();
		a.frame = _frame;
		a.amClip = _clip;
		a.wrapMode = _wrapMode;
		// add a new key
        AMUtil.recordObject(this, "add key");
        keys.Add(a);
		// update cache
		updateCache();
	}
	// update cache
	public override void updateCache() {
        AMUtil.recordObject(this, "update cache");
		// destroy cache
		destroyCache();
		// create new cache
		cache = new List<AMAction>();
		// sort keys
		sortKeys();
		// add all clips to list
		for(int i=0;i<keys.Count;i++) {
			AMAnimationAction a = ScriptableObject.CreateInstance<AMAnimationAction> ();
			a.startFrame = keys[i].frame;
			a.obj = obj;
			a.amClip = (keys[i] as AMAnimationKey).amClip;
			a.wrapMode = (keys[i] as AMAnimationKey).wrapMode;
			a.crossfade = (keys[i] as AMAnimationKey).crossfade;
			a.crossfadeTime = (keys[i] as AMAnimationKey).crossfadeTime;
			cache.Add (a);
		}
		base.updateCache();
	}
	// preview a frame in the scene view
	public void previewFrame(float frame, float frameRate) {
		if(!obj) return;
		if(cache.Count <= 0) return;
		bool found = false;
		for(int i=cache.Count-1;i>=0;i--) {
			if(cache[i].startFrame <= frame) {
				
				AnimationClip amClip = (cache[i] as AMAnimationAction).amClip;
				if(!amClip) {
					// do nothing
				} else { 
					amClip.wrapMode = (cache[i] as AMAnimationAction).wrapMode;
#if U5
					amClip.SampleAnimation(obj,getTime (frameRate,frame-cache[i].startFrame));
#else
                    amClip.SampleAnimation(obj, getTime(frameRate, frame - cache[i].startFrame));
#endif
				}
				found = true;
				break;
			}
					
		}
		// sample default animation if not found
#if U5
		if(!found && obj.GetComponent<Animation>().clip) obj.GetComponent<Animation>().clip.SampleAnimation(obj,0f);
#else
        if (!found && obj.GetComponent<Animation>().clip) obj.GetComponent<Animation>().clip.SampleAnimation(obj, 0f);
#endif
	}
	public float getTime(float frameRate,float numberOfFrames) {
		return (float)numberOfFrames/(float)frameRate;	
	}
	
	public override AnimatorTimeline.JSONInit getJSONInit ()
	{
		// no initial values to set
		return null;
	}
	
	public override List<GameObject> getDependencies() {
		List<GameObject> ls = new List<GameObject>();
		if(obj) ls.Add(obj);
		return ls;
	}
	
	public override List<GameObject> updateDependencies (List<GameObject> newReferences, List<GameObject> oldReferences)
	{
		List<GameObject> lsFlagToKeep = new List<GameObject>();
		if(!obj) return lsFlagToKeep;
		for(int i=0;i<oldReferences.Count;i++) {
			if(oldReferences[i] == obj) {
				// missing animation
				if(!newReferences[i].GetComponent(typeof(Animation))) {
					Debug.LogWarning("Animator: Animation Track component 'Animation' not found on new reference for GameObject '"+obj.name+"'. Duplicate not replaced.");
					lsFlagToKeep.Add(oldReferences[i]);
					return lsFlagToKeep;
				}
				obj = newReferences[i];
				break;
			}
		}
		
		return lsFlagToKeep;
	}

#if UNITY_EDITOR
    /// <summary>
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
