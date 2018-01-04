using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MH
{
    [Serializable]
    public class AMScaleAction : AMAction
    {
        public int endFrame;
        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;
        public RebindTr _obj = new RebindTr();

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

        public override string ToString(int codeLanguage, int frameRate)
        {
            if (getNumberOfFrames() <= 0) return null;
            string s;

            if (codeLanguage == 0)
            {
                // c#
                s = "// AMScaleTrack hasn't implement AMAction.ToString(), indeed I'm considering removing CodeView at all. (<ゝω·)☆";
            }
            else
            {
                // js
                s = "// AMScaleTrack hasn't implement AMAction.ToString(), indeed I'm considering removing CodeView at all. (<ゝω·)☆";
            }
            return s;
        }
        public override int getNumberOfFrames()
        {
            return endFrame - startFrame;
        }

        public float getTime(int frameRate)
        {
            return (float)getNumberOfFrames() / (float)frameRate;
        }

        public override void execute(int frameRate, float delay)
        {
            if (!obj) return;
            if (endFrame == -1) return;

            //var tr = obj.transform;

            if (hasCustomEase())
                AMTween.ScaleTo(obj.gameObject, AMTween.Hash(
                    "delay", getWaitTime(frameRate, delay),
                    "time", getTime(frameRate),
                    "scale", endScale,
                    "easecurve", easeCurve)
                    );
            else
                AMTween.ScaleTo(obj.gameObject, AMTween.Hash(
                    "delay", getWaitTime(frameRate, delay),
                    "time", getTime(frameRate),
                    "scale", endScale,
                    "easetype", (AMTween.EaseType)easeType
                    )
                );
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
