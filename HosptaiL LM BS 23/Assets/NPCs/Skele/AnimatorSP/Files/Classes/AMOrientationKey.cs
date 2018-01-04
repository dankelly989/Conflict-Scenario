using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MH;

[System.Serializable]
public class AMOrientationKey : AMKey {

    public RebindTr _obj = new RebindTr();
    public Transform target
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
	
	public bool setTarget(Transform target) {
		if(target != this.target) {
            AMUtil.recordObject(this, "set orient target");
			this.target = target;
			return true;	
		}
		return false;
	}
	
	public override AMKey CreateClone ()
	{
		
		AMOrientationKey a = ScriptableObject.CreateInstance<AMOrientationKey>();
		a.frame = frame;
		a.target = target;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		return a;
	}

    public override void rebind(RebindOption opt)
    {
        base.rebind(opt);
        _obj.Rebind(opt);
    }
}
