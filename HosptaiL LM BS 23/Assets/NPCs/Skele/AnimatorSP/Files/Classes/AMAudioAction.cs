using UnityEngine;
using System.Collections;
using MH;

[System.Serializable]
public class AMAudioAction : AMAction {

    public RebindTr _obj = new RebindTr();
	public AudioClip audioClip;
	public bool loop;

    public AudioSource audioSource
    {
        get
        {
            var tr = _obj.tr;
            if (tr == null)
                return null;
            return tr.GetComponent<AudioSource>();
        }
        set
        {
            Transform tr = null;
            if (value != null)
                tr = value.transform;

            _obj.tr = tr;
        }
    }

    private AudioSource cacheSource;
	
	public override void execute(int frameRate, float delay) {
        if (!audioSource || !audioClip)
        {
            CoroutineBehaviour.StartCoroutineDelay(audioSource.gameObject, _StopClip, getWaitTime(frameRate, delay));
        }
        else
        {
            AMTween.PlayAudio(audioSource, AMTween.Hash("delay", getWaitTime(frameRate, delay), "audioclip", audioClip, "loop", loop));
        }
	}
	
	public string ToString(int codeLanguage, int frameRate, string audioClipVarName) {
		if((audioClip == null) || (audioSource == null)) return null;
		string s = "";
	
		if(codeLanguage == 0) {
			// c#
			s += "AMTween.PlayAudio(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"audioclip\", "+audioClipVarName+", \"loop\", "+loop.ToString ().ToLower()+"));";
		} else {
			// js
			s += "AMTween.PlayAudio(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"audioclip\": "+audioClipVarName+", \"loop\": "+loop.ToString ().ToLower()+"});";

		}
		return s;
	}
	
	public ulong getTimeInSamples(int frequency, float time) {
		return (ulong)((44100/frequency)*frequency*time);	
	}
	public int getNumberOfFrames(int frameRate) {
		if(!audioClip) return -1;
		if(loop) return -1;
		return Mathf.CeilToInt(audioClip.length*frameRate);
	}
	
	public override AnimatorTimeline.JSONAction getJSONAction (int frameRate)
	{
		if(!audioSource || !audioClip) return null;
		AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
		a.method = "playaudio";
		a.go = audioSource.gameObject.name;
		a.delay = getWaitTime(frameRate,0f);
		a.strings = new string[]{audioClip.name};
		a.bools = new bool[]{loop};
		
		return a;
	}

    public override void rebind(RebindOption opt)
    {
        base.rebind(opt);
        _obj.Rebind(opt);
        cacheSource = audioSource;
    }

    public override void unbind(RebindOption opt)
    {
        base.unbind(opt);
        _obj.Unbind();
    }

    private void _StopClip()
    {
        cacheSource.Stop();
    }
}
