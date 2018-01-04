using UnityEngine;
using System.Collections;
using MH;

[System.Serializable]
public class AMAnimationKey : AMKey {
	
	public WrapMode wrapMode;	// animation wrap mode
	public AnimationClip amClip;
	public bool crossfade = true;
	public float crossfadeTime = 0.3f;
	
	public bool setWrapMode(WrapMode wrapMode) {
		if(this.wrapMode != wrapMode) {
            AMUtil.recordObject(this, "set wrap mode");
			this.wrapMode = wrapMode;
			return true;
		}
		return false;
	}
	
	public bool setAmClip(AnimationClip clip) {
		if(amClip != clip) {
            AMUtil.recordObject(this, "set animation clip");
			amClip = clip;
			return true;
		}
		return false;
	}
	
	public bool setCrossFade(bool crossfade) {
		if(this.crossfade != crossfade) {
            AMUtil.recordObject(this, "set crossFade");
            this.crossfade = crossfade;
			return true;
		}
		return false;
	}
	
	public bool setCrossfadeTime(float crossfadeTime) {
		if(this.crossfadeTime != crossfadeTime) {
            AMUtil.recordObject(this, "set fade time");
			this.crossfadeTime = crossfadeTime;
			return true;
		}
		return false;
	}
	
		// copy properties from key
	public override AMKey CreateClone ()
	{
		
		AMAnimationKey a = ScriptableObject.CreateInstance<AMAnimationKey>();
		a.frame = frame;
		a.wrapMode = wrapMode;
		a.amClip = amClip;
		a.crossfade = crossfade;
		
		return a;
	}
}
