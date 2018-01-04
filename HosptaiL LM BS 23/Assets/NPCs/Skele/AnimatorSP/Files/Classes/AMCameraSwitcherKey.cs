using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MH;

[System.Serializable]
public class AMCameraSwitcherKey : AMKey {
	
	public int type = 0;		// 0 = camera, 1 = color
	public Color color;
    public RebindTr _obj = new RebindTr();

	public int cameraFadeType = (int)AMTween.Fade.CrossFade;
	public List<float> cameraFadeParameters = new List<float>();
	public Texture2D irisShape;
	public bool still = false;	// is it still or does it use render texture

    public Camera camera
    {
        get
        {
            var tr = _obj.tr;
            if (tr == null)
                return null;
            return tr.GetComponent<Camera>();
        }
        set
        {
            Transform tr = null;
            if (value != null)
                tr = value.transform;

            AMUtil.recordObject(this, "set camera ref");
            _obj.tr = tr;
        }
    }
	
	public bool setCamera(Camera camera) {
		if(camera != this.camera) {
            //AMUtil.recordObject(this, "set camera");
			this.camera = camera;
			return true;	
		}
		return false;
	}
	
	public bool setColor(Color color) {
		if(color != this.color) {
            AMUtil.recordObject(this, "set color");
            this.color = color;
			return true;	
		}
		return false;
	}
	
	public bool setType(int type) {
		if(type != this.type) {
            AMUtil.recordObject(this, "set type");
            this.type = type;
			return true;	
		}
		return false;
	}
	
	public bool setStill(bool still) {
		if(still != this.still) {
            AMUtil.recordObject(this, "set still");
            this.still = still;
			return true;	
		}
		return false;
	}
	
	public bool setCameraFadeType(int cameraFadeType) {
		if(cameraFadeType != this.cameraFadeType) {
            AMUtil.recordObject(this, "set camera fade type");
            this.cameraFadeType = cameraFadeType;
			return true;	
		}
		return false;
	}
	
	public override AMKey CreateClone ()
	{
		
		AMCameraSwitcherKey a = ScriptableObject.CreateInstance<AMCameraSwitcherKey>();
		a.frame = frame;
		a.type = type;
		a.camera = camera;
		a.color = color;
		a.cameraFadeType = cameraFadeType;
		a.cameraFadeParameters = new List<float>(cameraFadeParameters);
		a.irisShape = irisShape;
		a.still = still;
		a.easeType = easeType;
		a.customEase = new List<float>(customEase);
		return a;
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
