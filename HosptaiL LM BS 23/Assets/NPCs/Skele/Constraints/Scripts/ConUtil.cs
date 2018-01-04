using System;
using System.Collections.Generic;
using UnityEngine;
using MH.Curves;

namespace MH.Constraints
{
    public enum EClampRegion
    {
        Inside,
        Outside,
        OnSurface,
    }

    public enum ETransformData
    {
        Position,
        Rotation,
        Scale,
    }

    public enum ELimitAffect
    {
        None = 0,
        MinX = 1, MaxX = 2,
        MinY = 4, MaxY = 8,
        MinZ = 16, MaxZ = 32,
        Full = 63,
    }

    public enum ELimitEuler
    {
        None = 0,
        X = 1, Y = 2, Z = 4,
        XYZ = 7,
    }

    public class ConUtil
    {
        /// <summary>
        /// ensure the a0 & a1 not on same axis;
        /// 
        /// return the modified value for a1
        /// </summary>
        public static EAxisD EnsureAxisNotColinear(EAxisD a0, EAxisD a1)
        {
            if (a0 != a1 && ((int)a0 << 3) != (int)a1 && ((int)a1 << 3) != (int)a0)
                return a1; //good

            if (a1 != EAxisD.X && a1 != EAxisD.InvX) return EAxisD.X;
            if (a1 != EAxisD.Y && a1 != EAxisD.InvY) return EAxisD.Y;
            if (a1 != EAxisD.Z && a1 != EAxisD.InvZ) return EAxisD.Z;

            Dbg.LogErr("EnsureAxisNotColinear: error: {0}, {1}", a0, a1);
            return 0;
        }

        public static Vector3 ApplyTiltToUp(List<CtrlPt> pts, float t, ref Vector3 tan, ref Vector3 baseUp)
        {
            // then calculate the interpolated tilt
            int numSections = pts.Count - 1;
            int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
            float u = t * (float)numSections - (float)currPt;

            float startTilt = pts[currPt].tilt;
            float endTilt = pts[currPt + 1].tilt;
            float curTilt = Misc.Lerp(startTilt, endTilt, u);

            // combine the value to get up
            Vector3 currUp = Quaternion.AngleAxis(curTilt, tan) * baseUp;
            return currUp;
        }

        public readonly static Color GizmosColor = new Color32(40, 100, 237, 255);
        public readonly static Color TransparentGizmosColor = new Color32(40, 100, 237, 127);
    }

    /// <summary>
    /// used to record init & last transform info for constraints
    /// </summary>
    [Serializable]
    public class TrInitInfo
    {
        public Transform tr = null;
        public bool inited = false;
        public Vector3 locPos = new Vector3(float.NaN, float.NaN, float.NaN);
        //public Quaternion locRot = new Quaternion(0, 0, 0, 0);
        public Vector3 locRot = Vector3.zero;
        public Vector3 locScale = new Vector3(float.NaN, float.NaN, float.NaN);

        public Vector3 lastLocPos = new Vector3(float.NaN, float.NaN, float.NaN);
        //public Quaternion lastLocRot = new Quaternion(0, 0, 0, 0);
        public Vector3 lastLocRot = Vector3.zero;
        public Vector3 lastLocScale = new Vector3(float.NaN, float.NaN, float.NaN);

        public TrInitInfo(Transform tr)
        {
            this.tr = tr;
        }

        public bool TentativeResetInitInfo()
        {
            if (!inited)
            {
                ResetInitInfo();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ResetInitInfo()
        {
            inited = true;
            lastLocPos = locPos = tr.localPosition;
            //lastLocRot = locRot = tr.localRotation;
            lastLocRot = locRot = tr.localEulerAngles;
            lastLocScale = locScale = tr.localScale;
        }

        public void RevertToInitInfo()
        {
            tr.localPosition = locPos;
            //tr.localRotation = locRot;
            tr.localEulerAngles = locRot;
            tr.localScale = locScale;
        }

        public void RecordLastLocInfo()
        {
            lastLocPos = tr.localPosition;
            //lastLocRot = tr.localRotation;
            lastLocRot = tr.localEulerAngles;
            lastLocScale = tr.localScale;
        }

        public void UpdateInitInfo()
        {
            //////////////////////////////////////////////////
            // why use direct assignment like "locPos = curLocPos" not the incremental "locPos += curLocPos - recordLocPos"?
            // as for drag of PositionHandle, the position of the handle in each iteration is reset onto the track, which will gives a wrong delta of (curLocPos - recordLocPos)
            //////////////////////////////////////////////////
            Vector3 curLocPos = tr.localPosition;
            var recordLocPos = lastLocPos;
            if (curLocPos != recordLocPos)
                locPos = curLocPos;

            //Quaternion curLocRot = tr.localRotation;
            //var recordLocRot = lastLocRot;
            //if (curLocRot != recordLocRot)
            //    locRot = curLocRot;

            Vector3 curLocRot = tr.localEulerAngles;
            var recordLocRot = lastLocRot;
            if (curLocRot != recordLocRot)
                locRot = curLocRot;

            Vector3 curLocScale = tr.localScale;
            var recordLocScale = lastLocScale;
            if (curLocScale != recordLocScale)
                locScale = curLocScale;
        }
    }
}
