using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH.Skele
{
    public class HotFix
    {
        public static void FixRotation(Transform tr)
        {
#if UNITY_5_4_OR_NEWER
            //-------tr.SetLocalEulerHint(tr.GetLocalEulerAngles(_rotateOrder));------//
            Vector3 ea = (Vector3)RCall.CallMtd("UnityEngine.Transform", "GetLocalEulerAngles", tr, 4);
            Vector3 rnd = UnityEngine.Random.onUnitSphere * 0.001f;
            Vector3 v = ea + rnd;
            RCall.CallMtd("UnityEngine.Transform", "SetLocalEulerHint", tr, v);

            tr.localEulerAngles = v;

            //--------tr.SendTransformChangedScale();--------//
            if (tr.parent != null)
            {
                RCall.CallMtd("UnityEngine.Transform", "SendTransformChangedScale", tr);
            }
#endif
        }

        public static void FixRotation(IList<Transform> trs)
        {
#if UNITY_5_4_OR_NEWER
            foreach(Transform tr in trs)
            {
                FixRotation(tr);
            }
#endif
        }

    }
}
