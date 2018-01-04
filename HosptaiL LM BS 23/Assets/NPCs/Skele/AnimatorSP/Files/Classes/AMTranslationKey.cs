using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MH;

[System.Serializable]
public class AMTranslationKey : AMKey {
	
	public enum Interpolation {
		Curve = 0,
		Linear = 1
	}
	public static string[] InterpolationNames = new string[]{"Curve","Linear"};
	public Vector3 position;
	public int interp = 0;			// interpolation
	
	public bool setInterpolation(int _interp) {
		if(_interp != interp) {
            AMUtil.recordObject(this, "change interpolation");
			interp = _interp;
			return true;
		}
		return false;
	}
	public bool setPosition(Vector3 position) {
		if(position != this.position) {
            AMUtil.recordObject(this, "change position");
            this.position = position;
			return true;	
		}
		return false;
	}
	
	// copy properties from key
	public override AMKey CreateClone ()
	{
		
		AMTranslationKey a = ScriptableObject.CreateInstance<AMTranslationKey>();
		a.frame = frame;
		a.position = position;
		a.interp = interp;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		
		return a;
	}
}
