using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    [Serializable]
    public class AMScaleKey : AMKey
    {
        public Vector3 scale;

        public bool setScale(Vector3 scale)
        {
            if (scale != this.scale)
            {
                AMUtil.recordObject(this, "set scale");
                this.scale = scale;
                return true;
            }
            return false;
        }

        // copy properties from key
        public override AMKey CreateClone()
        {
            AMScaleKey a = ScriptableObject.CreateInstance<AMScaleKey>();
            a.frame = frame;
            a.scale = scale;
            a.easeType = easeType;
            a.customEase = new List<float>(customEase);

            return a;
        }
    }
}
